using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// Provides buffer conversion methods for GC
    /// </summary>
    public static class GCAttachConverter
    {
        /// <summary>
        /// Converts a buffer model to the GC format
        /// </summary>
        /// <param name="model">The model to converter</param>
        /// <param name="optimize">Whether to optimize the attaches</param>
        /// <param name="ignoreWeights">If conversion should still happen, despite weights existing</param>
        /// <param name="forceUpdate">Whether to convert, regardless of whether the attaches are already GC</param>
        public static void ConvertModelToGC(NJObject model, bool optimize = true, bool ignoreWeights = false, bool forceUpdate = false)
        {
            if(model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            if(model.AttachFormat == AttachFormat.GC && !forceUpdate)
                return;

            if(model.HasWeight && !ignoreWeights)
                throw new FormatException("Model is weighted, cannot convert to basic format!");

            AttachHelper.ProcessWeightlessModel(model, (cacheAtc, ogAtc) =>
            {
                // getting the vertex information
                Vector3[] positions = new Vector3[cacheAtc.vertices.Length];
                Vector3[] normals = new Vector3[positions.Length];

                for(int i = 0; i < positions.Length; i++)
                {
                    var vtx = cacheAtc.vertices[i];
                    positions[i] = vtx.Position;
                    normals[i] = vtx.Normal;
                }

                // getting the corner information
                int cornerCount = 0;
                for(int i = 0; i < cacheAtc.corners.Length; i++)
                    cornerCount += cacheAtc.corners[i].Length;

                Vector2[] texcoords = new Vector2[cornerCount];
                Color[] colors = new Color[cornerCount];
                Corner[][] corners = new Corner[cacheAtc.corners.Length][];

                ushort cornerIndex = 0;
                for(int i = 0; i < corners.Length; i++)
                {
                    BufferCorner[] bufferCorners = cacheAtc.corners[i];
                    Corner[] meshCorners = new Corner[bufferCorners.Length];
                    for(int j = 0; j < bufferCorners.Length; j++)
                    {
                        BufferCorner bcorner = bufferCorners[j];

                        texcoords[cornerIndex] = bcorner.Texcoord;
                        colors[cornerIndex] = bcorner.Color;

                        meshCorners[j] = new Corner() 
                        { 
                            PositionIndex = bcorner.VertexIndex,
                            NormalIndex = bcorner.VertexIndex,
                            UV0Index = cornerIndex,
                            Color0Index = cornerIndex
                        };

                        cornerIndex++;
                    }
                    corners[i] = meshCorners;
                }

                bool hasUVs = texcoords.Any(x => x != default);
                // if it has no normals, always use colors (even if they are all white)
                bool hasColors = colors.Any(x => x != Color.White) || !normals.Any(x => x != Vector3.UnitY);

                // Puttin together the vertex sets
                bool first = true;
                VertexSet[] vertexData = new VertexSet[2 + (hasUVs ? 1 : 0)];

                IndexAttributeParameter iaParam = new() { IndexAttributes = IndexAttributes.HasPosition };
                if(positions.Length > 256)
                    iaParam.IndexAttributes |= IndexAttributes.Position16BitIndex;
                vertexData[0] = new VertexSet(positions, false);

                if(hasColors)
                {
                    iaParam.IndexAttributes |= IndexAttributes.HasColor;
                    if(colors.Length > 256)
                        iaParam.IndexAttributes |= IndexAttributes.Color16BitIndex;

                    vertexData[1] = new VertexSet(colors);
                }
                else
                {
                    iaParam.IndexAttributes |= IndexAttributes.HasNormal;
                    if(normals.Length > 256)
                        iaParam.IndexAttributes |= IndexAttributes.Normal16BitIndex;

                    vertexData[1] = new VertexSet(normals, true);
                }

                if(hasUVs)
                {
                    iaParam.IndexAttributes |= IndexAttributes.HasUV;
                    if(texcoords.Length > 256)
                        iaParam.IndexAttributes |= IndexAttributes.UV16BitIndex;
                    vertexData[2] = new VertexSet(texcoords);
                }

                // stitching polygons together
                BufferMaterial currentMaterial = null;
                Mesh ProcessBufferMesh(int index)
                {
                    // generating parameter info
                    List<IParameter> parameters = new();

                    BufferMaterial cacheMaterial = cacheAtc.materials[index];
                    if(first)
                    {
                        first = false;

                        parameters.Add(new VtxAttrFmtParameter(VertexAttribute.Position));
                        parameters.Add(new VtxAttrFmtParameter(hasColors ? VertexAttribute.Color0 : VertexAttribute.Normal));
                        if(hasUVs)
                            parameters.Add(new VtxAttrFmtParameter(VertexAttribute.Tex0));
                        parameters.Add(iaParam);

                        if(cacheMaterial == null)
                        {
                            currentMaterial = new BufferMaterial()
                            {
                                MaterialAttributes = MaterialAttributes.noSpecular
                            };
                        }
                        else
                            currentMaterial = cacheMaterial;

                        parameters.Add(new LightingParameter() {
                            LightingAttributes = LightingParameter.DefaultLighting.LightingAttributes,
                            ShadowStencil = currentMaterial.ShadowStencil
                        });

                        parameters.Add(new BlendAlphaParameter() { 
                            SourceAlpha = currentMaterial.SourceBlendMode,
                            DestAlpha = currentMaterial.DestinationBlendmode
                        });

                        parameters.Add(new AmbientColorParameter() { 
                            AmbientColor = currentMaterial.Ambient
                        });

                        TextureParameter texParam = new();
                        texParam.TextureID = (ushort)currentMaterial.TextureIndex;

                        if(currentMaterial.MirrorU)
                            texParam.Tiling |= GCTileMode.MirrorU;

                        if(currentMaterial.MirrorV)
                            texParam.Tiling |= GCTileMode.MirrorV;

                        if(currentMaterial.WrapU)
                            texParam.Tiling |= GCTileMode.WrapU;

                        if(currentMaterial.WrapV)
                            texParam.Tiling |= GCTileMode.WrapV;

                        parameters.Add(texParam);

                        parameters.Add(Unknown9Parameter.DefaultValues);
                        parameters.Add(new TexCoordGenParameter() {
                            TexCoordID = currentMaterial.TexCoordID,
                            TexGenType = currentMaterial.TexGenType,
                            TexGenSrc = currentMaterial.TexGenSrc,
                            MatrixID = currentMaterial.MatrixID });
                    }
                    else
                    {
                        if(currentMaterial.ShadowStencil != cacheMaterial.ShadowStencil)
                        {
                            parameters.Add(new LightingParameter()
                            {
                                ShadowStencil = currentMaterial.ShadowStencil
                            });
                        }

                        if(currentMaterial.SourceBlendMode != cacheMaterial.SourceBlendMode
                        || currentMaterial.DestinationBlendmode != cacheMaterial.DestinationBlendmode)
                        {
                            parameters.Add(new BlendAlphaParameter()
                            {
                                SourceAlpha = currentMaterial.SourceBlendMode,
                                DestAlpha = currentMaterial.DestinationBlendmode
                            });
                        }

                        if(currentMaterial.Ambient != cacheMaterial.Ambient)
                        {
                            parameters.Add(new AmbientColorParameter() {
                                AmbientColor = currentMaterial.Ambient 
                            });
                        }

                        if(currentMaterial.TextureIndex != cacheMaterial.TextureIndex
                        || currentMaterial.MirrorU != cacheMaterial.MirrorU
                        || currentMaterial.MirrorV != cacheMaterial.MirrorV
                        || currentMaterial.WrapU != cacheMaterial.WrapU
                        || currentMaterial.WrapV != cacheMaterial.WrapV)
                        {
                            TextureParameter texParam = new();
                            texParam.TextureID = (ushort)currentMaterial.TextureIndex;

                            if(currentMaterial.MirrorU)
                                texParam.Tiling |= GCTileMode.MirrorU;

                            if(currentMaterial.MirrorV)
                                texParam.Tiling |= GCTileMode.MirrorV;

                            if(currentMaterial.WrapU)
                                texParam.Tiling |= GCTileMode.WrapU;

                            if(currentMaterial.WrapV)
                                texParam.Tiling |= GCTileMode.WrapV;

                            parameters.Add(texParam);
                        }

                        if(currentMaterial.TexCoordID != cacheMaterial.TexCoordID
                        || currentMaterial.TexGenType != cacheMaterial.TexGenType
                        || currentMaterial.TexGenSrc != cacheMaterial.TexGenSrc
                        || currentMaterial.MatrixID != cacheMaterial.MatrixID)
                        {
                            parameters.Add(new TexCoordGenParameter() {
                                TexCoordID = currentMaterial.TexCoordID,
                                TexGenType = currentMaterial.TexGenType,
                                TexGenSrc = currentMaterial.HasAttribute(MaterialAttributes.normalMapping) ? TexGenSrc.Normal : currentMaterial.TexGenSrc,
                                MatrixID = currentMaterial.MatrixID});
                        }

                        currentMaterial = cacheMaterial;
                    }

                    // note: a single triangle polygon can only carry 0xFFFF corners, so about 22k tris
                    Corner[] triangleCorners = corners[index];

                    List<Poly> polygons = new();
                    if(triangleCorners.Length > 0xFFFF)
                    {
                        int remainingLength = triangleCorners.Length;
                        int offset = 0;
                        while(remainingLength > 0)
                        {
                            Corner[] finalCorners = new Corner[Math.Max(0xFFFF, remainingLength)];
                            Array.Copy(triangleCorners, offset, finalCorners, 0, finalCorners.Length);
                            offset += finalCorners.Length;
                            remainingLength -= finalCorners.Length;

                            Poly triangle = new(PolyType.Triangles, finalCorners);
                            polygons.Add(triangle);
                        }
                    }
                    else
                        polygons.Add(new(PolyType.Triangles, triangleCorners));

                    return new Mesh(parameters.ToArray(), polygons.ToArray());
                }

                List<int> opaqueMeshIndices = new();
                List<int> translucentMeshIndices = new();

                for(int i = 0; i < cacheAtc.materials.Length; i++)
                    (cacheAtc.materials[i].UseAlpha ? translucentMeshIndices : opaqueMeshIndices).Add(i);

                Mesh[] opaqueMeshes = opaqueMeshIndices.Select(x => ProcessBufferMesh(x)).ToArray();

                first = true;
                Mesh[] translucentMeshes = translucentMeshIndices.Select(x => ProcessBufferMesh(x)).ToArray();

                GCAttach result = new(vertexData, opaqueMeshes, translucentMeshes);

                if(optimize)
                {
                    result.OptimizeVertexData();
                    result.OptimizePolygonData();
                }

                result.Name = ogAtc.Name;

                return result;
            });
        }

        public static void ConvertModelFromGC(NJObject model, bool optimize = true)
        {
            if(model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            HashSet<GCAttach> attaches = new();
            NJObject[] models = model.GetObjects();

            foreach(NJObject obj in models)
            {
                if(obj.Attach == null)
                    continue;
                if(obj.Attach.Format != AttachFormat.GC)
                    throw new FormatException("Not all Attaches inside the model are a BASIC attaches! Cannot convert");

                GCAttach atc = (GCAttach)obj.Attach;

                attaches.Add(atc);
            }

            foreach(GCAttach atc in attaches)
            {
                List<BufferMesh> meshes = new();

                Vector3[] positions = null;
                Vector3[] normals = null;
                Color[] colors = null;
                Vector2[] uvs = null;

                if(atc.VertexData.TryGetValue(VertexAttribute.Position, out VertexSet tmp))
                    positions = tmp.Vector3Data;

                if(atc.VertexData.TryGetValue(VertexAttribute.Normal, out tmp))
                    normals = tmp.Vector3Data;

                if(atc.VertexData.TryGetValue(VertexAttribute.Tex0, out tmp))
                    uvs = tmp.UVData;

                if(atc.VertexData.TryGetValue(VertexAttribute.Color0, out tmp))
                    colors = tmp.ColorData;

                BufferMaterial material = new()
                { 
                    MaterialAttributes = MaterialAttributes.noSpecular
                };
                material.SetAttribute(MaterialAttributes.Flat, colors != null);
                float uvFac = 1;

                // the generates vertices
                List<BufferVertex> bufferVertices = new();
                // uint = posIndex | (nrmIndex << 16)
                Dictionary<uint, ushort> vertexIndices = new();

                // if there are no normals, then we can already initialize the entire thing with all positions
                if(normals == null)
                {
                    for(ushort i = 0; i < positions.Length; i++)
                    {
                        bufferVertices.Add(new BufferVertex(positions[i], Vector3.UnitY, i));
                        vertexIndices.Add(i, i);
                    }
                }

                BufferMesh ProcessMesh(Mesh m)
                {
                    // setting the material properties according to the parameters
                    foreach(IParameter param in m.Parameters)
                    {
                        switch(param.Type)
                        {
                            case ParameterType.VtxAttrFmt:
                                VtxAttrFmtParameter vaf = (VtxAttrFmtParameter)param;

                                if(vaf.VertexAttribute == VertexAttribute.Tex0 
                                    && (vaf.Unknown & 0xF0) == 0)
                                {
                                    uvFac = 1 << (vaf.Unknown & 0x7);
                                    if((vaf.Unknown & 0x8) > 0)
                                        uvFac = 1 / uvFac;
                                }

                                break;
                            case ParameterType.BlendAlpha:
                                BlendAlphaParameter blend = (BlendAlphaParameter)param;
                                material.SourceBlendMode = blend.SourceAlpha;
                                material.DestinationBlendmode = blend.DestAlpha;
                                break;
                            case ParameterType.AmbientColor:
                                AmbientColorParameter ambientCol = (AmbientColorParameter)param;
                                material.Ambient = ambientCol.AmbientColor;
                                break;
                            case ParameterType.Texture:
                                material.SetAttribute(MaterialAttributes.useTexture, true);
                                TextureParameter tex = (TextureParameter)param;
                                material.TextureIndex = tex.TextureID;
                                material.MirrorU = tex.Tiling.HasFlag(GCTileMode.MirrorU);
                                material.MirrorV = tex.Tiling.HasFlag(GCTileMode.MirrorV);

                                if(tex.Tiling.HasFlag(GCTileMode.Unk_1))
                                {
                                    material.WrapU = tex.Tiling.HasFlag(GCTileMode.WrapU);
                                    material.WrapV = tex.Tiling.HasFlag(GCTileMode.WrapV);
                                }
                                else
                                {
                                    material.WrapU = true;
                                    material.WrapV = true;
                                }
                                break;
                            case ParameterType.TexCoordGen:
                                TexCoordGenParameter gen = (TexCoordGenParameter)param;
                                material.SetAttribute(MaterialAttributes.normalMapping, gen.TexGenSrc == TexGenSrc.Normal);
                                material.MatrixID = gen.MatrixID;
                                material.TexCoordID = gen.TexCoordID;
                                material.TexGenSrc = gen.TexGenSrc;
                                material.TexGenType = gen.TexGenType;
                                break;
                        }
                    }

                    // filtering out the double loops
                    List<BufferCorner> corners = new();
                    List<uint> trianglelist = new();

                    foreach(Poly p in m.Polys)
                    {
                        // inverted culling is done manually in the gc strips, so we have to account for that
                        bool rev = p.Corners[0].PositionIndex != p.Corners[1].PositionIndex;
                        int offset = rev ? 0 : 1;
                        uint[] indices = new uint[p.Corners.Length - offset];

                        for(int i = offset; i < p.Corners.Length; i++)
                        {
                            Corner c = p.Corners[i];
                            indices[i - offset] = (uint)corners.Count;

                            uint posnrmIndex = c.PositionIndex | ((uint)c.NormalIndex << 16);

                            if(!vertexIndices.TryGetValue(posnrmIndex, out ushort vtxIndex))
                            {
                                vtxIndex = (ushort)bufferVertices.Count;
                                bufferVertices.Add(new(positions[c.PositionIndex], normals?[c.NormalIndex] ?? Vector3.UnitY, vtxIndex));
                                vertexIndices.Add(posnrmIndex, vtxIndex);
                            }

                            Vector2 uv = uvs != null ? uvs[c.UV0Index] * uvFac : default;
                            corners.Add(new BufferCorner(vtxIndex, colors?[c.Color0Index] ?? Color.White, uv));
                        }

                        // converting indices to triangles
                        if(p.Type == PolyType.Triangles)
                        {
                            // gc has inverted culling, dont even ask me
                            for(int i = 0; i < indices.Length; i += 3)
                            {
                                uint index = indices[i];
                                indices[i] = indices[i + 1];
                                indices[i + 1] = index;
                            }
                            trianglelist.AddRange(indices);

                        }
                        else if(p.Type == PolyType.TriangleStrip)
                        {
                            uint[] newIndices = new uint[(indices.Length - 2) * 3];
                            for(int i = 0; i < indices.Length - 2; i++)
                            {
                                int index = i * 3;
                                if(!rev)
                                {
                                    newIndices[index] = indices[i];
                                    newIndices[index + 1] = indices[i + 1];
                                }
                                else
                                {
                                    newIndices[index] = indices[i + 1];
                                    newIndices[index + 1] = indices[i];
                                }

                                newIndices[index + 2] = indices[i + 2];
                                rev = !rev;
                            }
                            trianglelist.AddRange(newIndices);
                        }
                        else
                            throw new Exception($"Primitive type {p.Type} not a valid triangle format");
                    }

                    return new(corners.ToArray(), trianglelist.ToArray(), material.Clone());
                }

                material.UseAlpha = false;
                foreach(Mesh m in atc.OpaqueMeshes)
                    meshes.Add(ProcessMesh(m));

                material.UseAlpha = true;
                material.Culling = true;
                foreach(Mesh m in atc.TransparentMeshes)
                    meshes.Add(ProcessMesh(m));

                // inject the vertex information into the first mesh
                for(int i = 0; i < meshes.Count; i++)
                {
                    BufferMesh vtxMesh = meshes[i];
                    meshes[i] = new(bufferVertices.ToArray(), false, vtxMesh.Corners, vtxMesh.TriangleList, vtxMesh.Material);

                }

                if(optimize)
                {
                    for(int i = 0; i < meshes.Count; i++)
                        meshes[i].Optimize();
                }

                atc.MeshData = meshes.ToArray();
            }
        }
    }
}
