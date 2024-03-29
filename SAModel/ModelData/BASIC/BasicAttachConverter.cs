﻿using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.Weighted;
using SATools.SAModel.ObjectData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// Provides buffer conversion methods for BASIC
    /// </summary>
    public static class BasicAttachConverter
    {
        public static void ConvertModelToBasic(Node model, bool optimize = true, bool ignoreWeights = false, bool forceUpdate = false)
        {
            if (model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            if (model.AttachFormat == AttachFormat.BASIC && !forceUpdate)
                return;

            var weightedMeshes = WeightedBufferAttach.ToWeightedBuffer(model, true);

            ConvertWeightedToBasic(model, weightedMeshes, optimize, ignoreWeights);
        }

        public static void ConvertWeightedToBasic(Node model, WeightedBufferAttach[] meshData, bool optimize = true, bool ignoreWeights = false)
        {
            if (meshData.Any(x => x.DependingNodeIndices.Count > 0) && !ignoreWeights)
            {
                throw new FormatException("Model is weighted, cannot convert to GC format!");
            }

            Node[] nodes = model.GetObjects();
            BasicAttach[] attaches = new BasicAttach[nodes.Length];

            foreach (var weightedAttach in meshData)
            {
                Node node = nodes[weightedAttach.DependencyRootIndex];

                Matrix4x4 worldMatrix = node.GetWorldMatrix();
                Matrix4x4.Invert(worldMatrix, out Matrix4x4 invertedWorldMatrix);
                Matrix4x4 normalMtx = Matrix4x4.Transpose(invertedWorldMatrix);
                Matrix4x4.Invert(normalMtx, out Matrix4x4 invertedNormalMtx);

                Vector3[] positions = new Vector3[weightedAttach.Vertices.Length];
                Vector3[]? normals = new Vector3[positions.Length];
                bool hasNormals = false;

                for (int i = 0; i < positions.Length; i++)
                {
                    var vtx = weightedAttach.Vertices[i];

                    Vector4 localPos = Vector4.Transform(vtx.Position, invertedWorldMatrix);
                    positions[i] = new(localPos.X, localPos.Y, localPos.Z);
                    normals[i] = Vector3.TransformNormal(vtx.Normal, invertedNormalMtx);

                    if (vtx.Normal != Vector3.UnitY)
                    {
                        hasNormals = true;
                    }
                }

                if (!hasNormals)
                    normals = null;

                // putting together polygons
                Mesh[] meshes = new Mesh[weightedAttach.Corners.Length];
                Material[] materials = new Material[weightedAttach.Corners.Length];

                for (int i = 0; i < weightedAttach.Corners.Length; i++)
                {
                    // creating the material
                    BufferMaterial bmat = weightedAttach.Materials[i];

                    Material mat = new()
                    {
                        DiffuseColor = bmat.Diffuse,
                        SpecularColor = bmat.Specular,
                        Exponent = bmat.SpecularExponent,
                        TextureID = bmat.TextureIndex,
                        FilterMode = bmat.TextureFiltering,
                        MipmapDAdjust = bmat.MipmapDistanceAdjust,
                        SuperSample = bmat.AnisotropicFiltering,
                        ClampU = bmat.ClampU,
                        ClampV = bmat.ClampV,
                        MirrorU = bmat.MirrorU,
                        MirrorV = bmat.MirrorV,
                        UseAlpha = bmat.UseAlpha,
                        SourceAlpha = bmat.SourceBlendMode,
                        DestinationAlpha = bmat.DestinationBlendmode,
                        DoubleSided = !bmat.Culling,

                        IgnoreLighting = bmat.HasAttribute(MaterialAttributes.NoDiffuse),
                        IgnoreSpecular = bmat.HasAttribute(MaterialAttributes.NoSpecular),
                        UseTexture = bmat.HasAttribute(MaterialAttributes.UseTexture),
                        EnvironmentMap = bmat.HasAttribute(MaterialAttributes.NormalMapping)
                    };

                    materials[i] = mat;

                    // creating the polygons

                    BufferCorner[] bCorners = weightedAttach.Corners[i];
                    IPoly[] triangles = new IPoly[bCorners.Length / 3];
                    Vector2[] texcoords = new Vector2[bCorners.Length];
                    Color[] colors = new Color[bCorners.Length];

                    Triangle current = new();
                    for (int j = 0; j < bCorners.Length; j++)
                    {
                        BufferCorner corner = bCorners[j];

                        int vIndex = j % 3;
                        current.Indices[vIndex] = corner.VertexIndex;
                        if (vIndex == 2)
                        {
                            triangles[(j - 2) / 3] = current;
                            current = new Triangle();
                        }

                        texcoords[j] = corner.Texcoord;
                        colors[j] = corner.Color;
                    }

                    bool hasTexcoords = texcoords.Any(x => x != default);
                    bool hasColors = colors.Any(x => x != Color.White);

                    Mesh basicmesh = new(BASICPolyType.Triangles, triangles, false, hasColors, hasTexcoords, (ushort)i);
                    if (hasColors)
                        basicmesh.Colors = colors;
                    if (hasTexcoords)
                        basicmesh.Texcoords = texcoords;

                    meshes[i] = basicmesh;
                }

                BasicAttach result = new(positions, normals, meshes, materials);

                if (optimize)
                {
                    // TODO write optimize logic for buffer -> BASIC
                }

                result.RecalculateBounds();

                attaches[weightedAttach.DependencyRootIndex] = result;
            }

            // Linking the attaches to the nodes
            bool regenerateMeshdata = false;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i]._attach == null && attaches[i] != null
                    || nodes[i]._attach != null && attaches[i] == null)
                {
                    regenerateMeshdata = true;
                }

                nodes[i]._attach = attaches[i];
            }

            if (regenerateMeshdata)
            {
                ConvertModelFromBasic(model, optimize);
            }
        }

        /// <summary>
        /// Generates Buffer meshes for all attaches in the model
        /// </summary>
        /// <param name="model">The tip of the model hierarchy to convert</param>
        /// <param name="optimize">Whether the buffer model should be optimized</param>
        public static void ConvertModelFromBasic(Node model, bool optimize = true)
        {
            if (model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            HashSet<BasicAttach> attaches = new();
            Node[] models = model.GetObjects();

            foreach (Node obj in models)
            {
                if (obj.Attach == null)
                    continue;
                if (obj.Attach.Format != AttachFormat.BASIC)
                    throw new FormatException("Not all Attaches inside the model are a BASIC attaches! Cannot convert");

                BasicAttach atc = (BasicAttach)obj.Attach;

                attaches.Add(atc);
            }

            foreach (BasicAttach atc in attaches)
            {
                // get the vertices
                BufferVertex[] verts = new BufferVertex[atc.Positions.Length];
                for (ushort i = 0; i < verts.Length; i++)
                    verts[i] = new BufferVertex(atc.Positions[i], atc.Normals?[i] ?? Vector3.UnitY, i);

                List<BufferMesh> meshes = new();
                foreach (Mesh mesh in atc.Meshes)
                {
                    // creating the material
                    BufferMaterial bMat;
                    if (atc.Materials != null && mesh.MaterialID < atc.Materials.Length)
                    {
                        Material mat = atc.Materials[mesh.MaterialID];
                        bMat = new BufferMaterial()
                        {
                            Diffuse = mat.DiffuseColor,
                            Specular = mat.SpecularColor,
                            SpecularExponent = mat.Exponent,
                            TextureIndex = mat.TextureID,
                            TextureFiltering = mat.FilterMode,
                            MipmapDistanceAdjust = mat.MipmapDAdjust,
                            AnisotropicFiltering = mat.SuperSample,
                            ClampU = mat.ClampU,
                            ClampV = mat.ClampV,
                            MirrorU = mat.MirrorU,
                            MirrorV = mat.MirrorV,
                            UseAlpha = mat.UseAlpha,
                            SourceBlendMode = mat.SourceAlpha,
                            DestinationBlendmode = mat.DestinationAlpha,
                            Culling = !mat.DoubleSided,
                            MaterialAttributes = MaterialAttributes.NoAmbient
                        };
                        //bMat.SetAttribute(MaterialAttributes.Flat, mesh.Colors != null);
                        bMat.SetAttribute(MaterialAttributes.NoDiffuse, mat.IgnoreLighting);
                        bMat.SetAttribute(MaterialAttributes.NoSpecular, mat.IgnoreSpecular);
                        bMat.SetAttribute(MaterialAttributes.UseTexture, mat.UseTexture);
                        bMat.SetAttribute(MaterialAttributes.NormalMapping, mat.EnvironmentMap);
                    }
                    else
                    {
                        bMat = new BufferMaterial()
                        {
                            Diffuse = new Color(0xF9, 0xF9, 0xF9, 0xFF)
                        };
                    }

                    List<BufferCorner> corners = new();
                    List<uint> triangles = new();
                    int polyIndex = 0;

                    foreach (IPoly p in mesh.Polys)
                    {
                        uint l = (uint)corners.Count;
                        switch (mesh.PolyType)
                        {
                            case BASICPolyType.Triangles:
                                triangles.AddRange(new uint[] { l, l + 1, l + 2 });
                                break;
                            case BASICPolyType.Quads:
                                triangles.AddRange(new uint[] { l, l + 1, l + 2, /**/ l + 2, l + 1, l + 3 });
                                break;
                            case BASICPolyType.NPoly:
                            case BASICPolyType.Strips:
                                Strip s = (Strip)p;
                                bool rev = s.Reversed;
                                for (uint i = 2; i < s.Indices.Length; i++)
                                {
                                    uint li = l + i;
                                    if (!rev)
                                        triangles.AddRange(new uint[] { li - 2, li - 1, li });
                                    else
                                        triangles.AddRange(new uint[] { li - 1, li - 2, li });
                                    rev = !rev;
                                }
                                break;
                            default:
                                break;
                        }

                        for (int i = 0; i < p.Indices.Length; i++)
                        {
                            corners.Add(new BufferCorner(p.Indices[i], mesh.Colors?[polyIndex] ?? Color.White, mesh.Texcoords?[polyIndex] ?? Vector2.Zero));
                            polyIndex++;
                        }
                    }

                    if (meshes.Count == 0)
                        meshes.Add(new BufferMesh(verts, false, corners.ToArray(), triangles.ToArray(), bMat));
                    else
                        meshes.Add(new BufferMesh(corners.ToArray(), triangles.ToArray(), bMat));
                }

                if (optimize)
                {
                    for (int i = 0; i < meshes.Count; i++)
                        meshes[i].Optimize();
                }

                atc.MeshData = meshes.ToArray();
            }
        }
    }
}
