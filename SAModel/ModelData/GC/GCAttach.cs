using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.GC;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.Helper;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A GC format attach
    /// </summary>
    [Serializable]
    public class GCAttach : Attach
    {
        /// <summary>
        /// Seperate sets of vertex data in this attach
        /// </summary>
        public VertexSet[] VertexData { get; }

        /// <summary>
        /// Meshes with opaque rendering properties
        /// </summary>
        public Mesh[] OpaqueMeshes { get; }

        /// <summary>
        /// Meshes with translucent rendering properties
        /// </summary>
        public Mesh[] TranslucentMeshes { get; }

        public override bool HasWeight => false;

        public override AttachFormat Format => AttachFormat.GC;

        /// <summary>
        /// Creates a new GC attach
        /// </summary>
        /// <param name="name">Name of the attach</param>
        /// <param name="vertexData">Vertex data</param>
        /// <param name="opaqueMeshes">Opaque meshes</param>
        /// <param name="translucentMeshes">Translucent meshes</param>
        public GCAttach(VertexSet[] vertexData, Mesh[] opaqueMeshes, Mesh[] translucentMeshes)
        {
            VertexData = vertexData;
            OpaqueMeshes = opaqueMeshes;
            TranslucentMeshes = translucentMeshes;

            MeshBounds = Bounds.FromPoints(VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Position).Data.Cast<Vector3>().ToArray());

            Name = "attach_" + GenerateIdentifier();
        }

        /// <summary>
        /// Converts mesh buffer data to a GC attach
        /// </summary>
        /// <param name="name">Name of the mesh</param>
        /// <param name="meshdata">Buffer mesh data</param>
        public GCAttach(BufferMesh[] meshdata) : base(meshdata)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load a gc attach from a file
        /// </summary>
        /// <param name="source">Byte source from a file</param>
        /// <param name="address">Address at which the attach is located</param>
        /// <param name="imageBase">Address image base</param>
        /// <param name="labels">Labels for the data to use</param>
        /// <returns></returns>
        public static GCAttach Read(byte[] source, uint address, uint imageBase, Dictionary<uint, string> labels)
        {
            string name;
            if(labels.ContainsKey(address))
                name = labels[address];
            else
                name = "attach_" + address.ToString("X8");

            // The struct is 36/0x24 bytes long

            uint vertexAddress = source.ToUInt32(address) - imageBase;
            //uint gap = ByteConverter.ToUInt32(file, address + 4);
            uint opaqueAddress = source.ToUInt32(address + 8) - imageBase;
            uint translucentAddress = source.ToUInt32(address + 12) - imageBase;

            int opaqueCount = source.ToInt16(address + 16);
            int translucentCount = source.ToInt16(address + 18);
            address += 20;
            Bounds bounds = Bounds.Read(source, ref address);

            // reading vertex data
            List<VertexSet> vertexData = new();
            VertexSet vertexSet = VertexSet.Read(source, vertexAddress, imageBase);
            while(vertexSet.Attribute != VertexAttribute.Null)
            {
                vertexData.Add(vertexSet);
                vertexAddress += 16;
                vertexSet = VertexSet.Read(source, vertexAddress, imageBase);
            }

            IndexAttributeFlags indexFlags = IndexAttributeFlags.HasPosition;

            List<Mesh> opaqueMeshes = new();
            for(int i = 0; i < opaqueCount; i++)
            {
                Mesh mesh = Mesh.Read(source, opaqueAddress, imageBase, ref indexFlags);
                opaqueMeshes.Add(mesh);
                opaqueAddress += 16;
            }

            indexFlags = IndexAttributeFlags.HasPosition;

            List<Mesh> translucentMeshes = new();
            for(int i = 0; i < translucentCount; i++)
            {
                Mesh mesh = Mesh.Read(source, translucentAddress, imageBase, ref indexFlags);
                translucentMeshes.Add(mesh);
                translucentAddress += 16;
            }

            return new GCAttach(vertexData.ToArray(), opaqueMeshes.ToArray(), translucentMeshes.ToArray())
            {
                Name = name,
                MeshBounds = bounds
            };
        }

        public override void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[] textures)
        {
            throw new NotSupportedException("GC attach doesnt have an available NJA format");
        }

        public override uint Write(EndianMemoryStream writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            // writing vertex data
            foreach(VertexSet vtx in VertexData)
            {
                vtx.WriteData(writer);
            }

            uint vtxAddr = (uint)writer.Stream.Position + imageBase;

            // writing vertex attributes
            foreach(VertexSet vtx in VertexData)
            {
                vtx.WriteAttribute(writer, imageBase);
            }

            // empty vtx attribute
            byte[] nullVtx = new byte[16];
            nullVtx[0] = 0xFF;
            writer.Write(nullVtx);

            // writing geometry data
            IndexAttributeFlags indexFlags = IndexAttributeFlags.HasPosition;
            foreach(Mesh m in OpaqueMeshes)
            {
                IndexAttributeFlags? t = m.IndexFlags;
                if(t.HasValue)
                    indexFlags = t.Value;
                m.WriteData(writer, indexFlags);
            }
            foreach(Mesh m in TranslucentMeshes)
            {
                IndexAttributeFlags? t = m.IndexFlags;
                if(t.HasValue)
                    indexFlags = t.Value;
                m.WriteData(writer, indexFlags);
            }

            // writing geometry properties
            uint opaqueAddress = (uint)writer.Stream.Position + imageBase;
            foreach(Mesh m in OpaqueMeshes)
            {
                m.WriteProperties(writer, imageBase);
            }
            uint translucentAddress = (uint)writer.Stream.Position + imageBase;
            foreach(Mesh m in TranslucentMeshes)
            {
                m.WriteProperties(writer, imageBase);
            }

            uint address = (uint)writer.Stream.Position + imageBase;
            labels.Add(Name, address);

            writer.WriteUInt32(vtxAddr);
            writer.WriteUInt32(0);
            writer.WriteUInt32(opaqueAddress);
            writer.WriteUInt32(translucentAddress);
            writer.WriteUInt16((ushort)OpaqueMeshes.Length);
            writer.WriteUInt16((ushort)TranslucentMeshes.Length);
            MeshBounds.Write(writer);
            return address;
        }

        internal override BufferMesh[] buffer(bool optimize)
        {
            List<BufferMesh> meshes = new();

            List<Vector3> positions = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Position)?.Data.Cast<Vector3>().ToList();
            List<Vector3> normals = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Normal)?.Data.Cast<Vector3>().ToList();
            List<Color> colors = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Color0)?.Data.Cast<Color>().ToList();
            List<Vector2> uvs = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Tex0)?.Data.Cast<Vector2>().ToList();

            BufferMaterial material = new()
            {
                Diffuse = Color.White,
                TextureFiltering = FilterMode.Bilinear,
                MaterialFlags = MaterialFlags.noSpecular
            };
            material.SetFlag(MaterialFlags.Flat, colors != null);

            BufferMesh ProcessMesh(Mesh m)
            {
                // setting the material properties according to the parameters
                foreach(Parameter param in m.Parameters)
                {
                    switch(param.Type)
                    {
                        case ParameterType.BlendAlpha:
                            BlendAlphaParameter blend = param as BlendAlphaParameter;
                            material.SourceBlendMode = blend.SourceAlpha;
                            material.DestinationBlendmode = blend.DestAlpha;
                            break;
                        case ParameterType.AmbientColor:
                            AmbientColorParameter ambientCol = param as AmbientColorParameter;
                            material.Ambient = ambientCol.AmbientColor;
                            break;
                        case ParameterType.Texture:
                            TextureParameter tex = param as TextureParameter;
                            material.TextureIndex = tex.TextureID;
                            material.MirrorU = tex.Tiling.HasFlag(GCTileMode.MirrorU);
                            material.MirrorV = tex.Tiling.HasFlag(GCTileMode.MirrorV);
                            material.WrapU = tex.Tiling.HasFlag(GCTileMode.WrapU);
                            material.WrapV = tex.Tiling.HasFlag(GCTileMode.WrapV);

                            //material.WrapU &= tex.Tile.HasFlag(GCTileMode.Unk_1);
                            //material.WrapV &= tex.Tile.HasFlag(GCTileMode.Unk_1);
                            break;
                        case ParameterType.TexCoordGen:
                            TexCoordGenParameter gen = param as TexCoordGenParameter;
                            material.SetFlag(MaterialFlags.normalMapping, gen.TexGenSrc == TexGenSrc.Normal);
                            break;
                    }
                }

                // filtering out the double loops
                List<BufferVertex> vertices = new();
                List<BufferCorner> corners = new();
                List<uint> trianglelist = new();

                foreach(Poly p in m.Polys)
                {
                    uint[] indices = new uint[p.Corners.Length];

                    for(int i = 0; i < p.Corners.Length; i++)
                    {
                        Corner c = p.Corners[i];
                        indices[i] = (uint)corners.Count;
                        corners.Add(new BufferCorner((ushort)vertices.Count, colors?[c.Color0Index] ?? Color.White, uvs?[c.UV0Index] ?? new Vector2()));
                        vertices.Add(new BufferVertex(positions[c.PositionIndex], normals?[c.NormalIndex] ?? Vector3.UnitY, (ushort)vertices.Count));
                    }


                    // converting indices to triangles
                    if(p.Type == PolyType.Triangles)
                        trianglelist.AddRange(indices);
                    else if(p.Type == PolyType.TriangleStrip)
                    {
                        bool rev = true;
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

                return new BufferMesh(vertices.ToArray(), false, corners.ToArray(), trianglelist.ToArray(), (BufferMaterial)material.Clone());
            }

            material.UseAlpha = false;
            foreach(Mesh m in OpaqueMeshes)
                meshes.Add(ProcessMesh(m));

            material.UseAlpha = true;
            material.Culling = true;
            foreach(Mesh m in TranslucentMeshes)
                meshes.Add(ProcessMesh(m));

            if(optimize)
            {
                // all meshes should use the same vertices
                BufferMesh[] meshData = new BufferMesh[meshes.Count];
                List<BufferVertex> vertices = new();

                int mi = 0;
                foreach(BufferMesh m in meshes)
                {
                    ushort[] vIDs = new ushort[m.Vertices.Length];

                    for(ushort i = 0; i < vIDs.Length; i++)
                    {
                        BufferVertex vtx = new(m.Vertices[i].position, m.Vertices[i].normal, (ushort)vertices.Count);
                        int index = vertices.FindIndex(x => x.EqualPosNrm(vtx));
                        if(index == -1)
                        {
                            vIDs[i] = vtx.index;
                            vertices.Add(vtx);
                        }
                        else
                            vIDs[i] = (ushort)index;
                    }

                    for(int i = 0; i < m.Corners.Length; i++)
                    {
                        m.Corners[i].vertexIndex = vIDs[m.Corners[i].vertexIndex];
                    }

                    meshData[mi] = m.Optimize(vertices.ToArray(), false);
                    mi++;
                }

                var firstMesh = meshData[0];
                meshData[0] = new BufferMesh(vertices.ToArray(), false, firstMesh.Corners, firstMesh.TriangleList, firstMesh.Material);
                return meshData;
            }
            else
                return meshes.ToArray();
        }

        public override Attach Clone() => new GCAttach(VertexData.ContentClone(), OpaqueMeshes.ContentClone(), TranslucentMeshes.ContentClone())
        {
            Name = Name,
            MeshBounds = MeshBounds
        };

        public override string ToString() => $"{Name} - GC: {VertexData.Length} - {OpaqueMeshes.Length} - {TranslucentMeshes.Length}";
    }
}

namespace SATools.SAModel.ModelData
{
    public static partial class AttachExtensions
    {
        public static GCAttach AsGC(this Attach atc)
        {
            return new GCAttach(atc.MeshData)
            {
                Name = atc.Name
            };
        }
    }
}
