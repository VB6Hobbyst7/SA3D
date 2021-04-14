using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.Helper;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Chunk format mesh data
    /// </summary>
    public class ChunkAttach : Attach
    {
        private static List<PolyChunk>[] PolyChunkCache = new List<PolyChunk>[0];

        /// <summary>
        /// Vertex data of the model
        /// </summary>
        public VertexChunk[] VertexChunks { get; }

        /// <summary>
        /// C struct name of the vertex chunk array
        /// </summary>
        public string VertexName { get; set; }

        /// <summary>
        /// Polygon data of the model
        /// </summary>
        public PolyChunk[] PolyChunks { get; }

        /// <summary>
        /// C struct name for the polygon chunk array
        /// </summary>
        public string PolyName { get; set; }

        public override AttachFormat Format => AttachFormat.CHUNK;

        private bool hasWeight;

        public override bool HasWeight => hasWeight;

        public ChunkAttach(VertexChunk[] vertexChunks, PolyChunk[] polyChunks)
        {
            VertexChunks = vertexChunks;
            PolyChunks = polyChunks;
            List<Vector3> pos = new();
            if(VertexChunks != null)
                foreach(VertexChunk cnk in VertexChunks)
                    foreach(ChunkVertex vtx in cnk.Vertices)
                        pos.Add(vtx.Position);
            MeshBounds = Bounds.FromPoints(pos.ToArray());
            UpdateWeight();

            Name = "attach_" + GenerateIdentifier();
            VertexName = "vertex_" + GenerateIdentifier();
            PolyName = "poly_" + GenerateIdentifier();
        }

        public ChunkAttach(BufferMesh[] meshdata) : base(meshdata)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the <see cref="HasWeight"/> property, since calculating it requires might take longer
        /// </summary>
        public void UpdateWeight()
        {
            if(PolyChunks == null || !PolyChunks.Any(a => a is PolyChunkStrip))
            {
                hasWeight = VertexChunks != null && VertexChunks.Any(a => a.HasWeight);
                return;
            }
            List<int> ids = new();
            if(VertexChunks != null)
                foreach(var vc in VertexChunks)
                {
                    if(vc.HasWeight)
                    {
                        hasWeight = true;
                        return;
                    }
                    ids.AddRange(Enumerable.Range(vc.IndexOffset, vc.Vertices.Length));
                }
            hasWeight = PolyChunks.OfType<PolyChunkStrip>().SelectMany(a => a.Strips).SelectMany(a => a.Corners).Any(a => !ids.Contains(a.index));
        }

        /// <summary>
        /// Reads a Chunk attach from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the attach is located</param>
        /// <param name="imagebase">Imagebase for every address</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public static ChunkAttach Read(byte[] source, uint address, uint imagebase, Dictionary<uint, string> labels)
        {
            string name = labels.ContainsKey(address) ? labels[address] : "attach_" + address.ToString("X8");

            uint vertexAddress = source.ToUInt32(address);
            string vertexName = "vertex_" + GenerateIdentifier();
            VertexChunk[] vertexChunks = null;
            if(vertexAddress != 0)
            {
                vertexAddress -= imagebase;
                vertexName = labels.ContainsKey(vertexAddress) ? labels[vertexAddress] : "vertex_" + vertexAddress.ToString("X8");

                List<VertexChunk> chunks = new();
                VertexChunk cnk = VertexChunk.Read(source, ref vertexAddress);
                while(cnk != null)
                {
                    chunks.Add(cnk);
                    cnk = VertexChunk.Read(source, ref vertexAddress);
                }
                vertexChunks = chunks.ToArray();
            }

            uint polyAddress = source.ToUInt32(address += 4);
            string polyName = "poly_" + GenerateIdentifier();
            PolyChunk[] polyChunks = null;
            if(polyAddress != 0)
            {
                polyAddress -= imagebase;
                polyName = labels.ContainsKey(polyAddress) ? labels[polyAddress] : "poly_" + polyAddress.ToString("X8");

                List<PolyChunk> chunks = new();
                PolyChunk cnk = PolyChunk.Read(source, ref polyAddress);
                while(cnk != null && cnk.Type != ChunkType.End)
                {
                    chunks.Add(cnk);
                    cnk = PolyChunk.Read(source, ref polyAddress);
                }
                polyChunks = chunks.ToArray();

            }
            address += 4;
            return new ChunkAttach(vertexChunks, polyChunks)
            {
                Name = name,
                VertexName = vertexName,
                PolyName = polyName,
                MeshBounds = Bounds.Read(source, ref address)
            };
        }

        public override uint Write(EndianMemoryStream writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            // writing vertices
            uint vertexAddress = 0;
            if(VertexChunks != null && VertexChunks.Length > 0)
            {
                if(labels.ContainsKey(VertexName))
                    vertexAddress = labels[VertexName];
                else
                {
                    vertexAddress = (uint)writer.Stream.Position + imageBase;
                    foreach(VertexChunk cnk in VertexChunks)
                    {
                        cnk.Write(writer);
                    }
                    // end chunk
                    byte[] bytes = new byte[8];
                    bytes[0] = 255;
                    writer.Write(bytes);
                }
            }
            uint polyAddress = 0;
            if(PolyChunks != null && PolyChunks.Length > 0)
            {
                if(labels.ContainsKey(PolyName))
                    polyAddress = labels[PolyName];
                else
                {
                    polyAddress = (uint)writer.Stream.Position + imageBase;
                    foreach(PolyChunk cnk in PolyChunks)
                    {
                        cnk.Write(writer);
                    }
                    // end chunk
                    byte[] bytes = new byte[2];
                    bytes[0] = 255;
                    writer.Write(bytes);
                }
            }

            uint address = (uint)writer.Stream.Position + imageBase;
            labels.Add(Name, address);
            writer.WriteUInt32(vertexAddress);
            writer.WriteUInt32(polyAddress);
            MeshBounds.Write(writer);
            return address;
        }

        public override void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[] textures)
        {
            base.WriteNJA(writer, DX, labels, textures);
        }

        internal override BufferMesh[] buffer(bool optimize)
        {
            List<BufferMesh> meshes = new();

            BufferVertex[] vertices = null;
            bool continueWeight = false;

            if(VertexChunks != null)
            {
                for(int i = 0; i < VertexChunks.Length; i++)
                {
                    VertexChunk cnk = VertexChunks[i];

                    List<BufferVertex> vertexList = new();
                    if(!cnk.HasWeight)
                    {
                        for(int j = 0; j < cnk.Vertices.Length; j++)
                        {
                            ChunkVertex vtx = cnk.Vertices[j];
                            vertexList.Add(new BufferVertex(vtx.Position, vtx.Normal, (ushort)(j + cnk.IndexOffset)));
                        }
                    }
                    else
                    {
                        for(int j = 0; j < cnk.Vertices.Length; j++)
                        {
                            ChunkVertex vtx = cnk.Vertices[j];
                            vertexList.Add(new BufferVertex(vtx.Position, vtx.Normal, (ushort)(vtx.Index + cnk.IndexOffset), vtx.Weight));
                        }
                    }
                    vertices = vertexList.ToArray();
                    continueWeight = cnk.WeightStatus != WeightStatus.Start;

                    if(i < VertexChunks.Length - 1)
                    {
                        meshes.Add(new BufferMesh(vertices, continueWeight));
                    }
                }
            }


            List<PolyChunk> active = new();

            if(PolyChunks != null)
            {
                int cacheID = -1;
                foreach(PolyChunk cnk in PolyChunks)
                {
                    switch(cnk.Type)
                    {
                        case ChunkType.Bits_CachePolygonList:
                            PolyChunkCachePolygonList cacheListCnk = (PolyChunkCachePolygonList)cnk;
                            cacheID = cacheListCnk.List;

                            if(PolyChunkCache.Length <= cacheID)
                                Array.Resize(ref PolyChunkCache, cacheID + 1);

                            PolyChunkCache[cacheID] = new List<PolyChunk>();
                            break;
                        case ChunkType.Bits_DrawPolygonList:
                            PolyChunkDrawPolygonList drawListCnk = (PolyChunkDrawPolygonList)cnk;
                            active.AddRange(PolyChunkCache[drawListCnk.List]);
                            break;
                        default:
                            if(cacheID > -1)
                                PolyChunkCache[cacheID].Add(cnk);
                            else
                                active.Add(cnk);
                            break;
                    }
                }
            }


            if(active.Count > 0)
            {
                BufferMaterial material = new()
                {
                    Diffuse = Color.White,
                    Ambient = Color.White,
                    MaterialFlags = MaterialFlags.useTexture
                };
                foreach(PolyChunk cnk in active)
                {
                    switch(cnk.Type)
                    {
                        case ChunkType.Bits_BlendAlpha:
                            PolyChunkBlendAlpha blendCnk = (PolyChunkBlendAlpha)cnk;
                            material.SourceBlendMode = blendCnk.SourceAlpha;
                            material.DestinationBlendmode = blendCnk.DestinationAlpha;
                            break;
                        case ChunkType.Bits_MipmapDAdjust:
                            PolyChunksMipmapDAdjust mipmapCnk = (PolyChunksMipmapDAdjust)cnk;
                            material.MipmapDistanceAdjust = mipmapCnk.MipmapDAdjust;
                            break;
                        case ChunkType.Bits_SpecularExponent:
                            PolyChunkSpecularExponent specularCnk = (PolyChunkSpecularExponent)cnk;
                            material.SpecularExponent = specularCnk.SpecularExponent;
                            break;
                        case ChunkType.Tiny_TextureID:
                        case ChunkType.Tiny_TextureID2:
                            PolyChunkTextureID textureCnk = (PolyChunkTextureID)cnk;
                            material.TextureIndex = textureCnk.TextureID;
                            material.MirrorU = textureCnk.MirrorU;
                            material.MirrorV = textureCnk.MirrorV;
                            material.ClampU = textureCnk.ClampU;
                            material.ClampV = textureCnk.ClampV;
                            material.AnisotropicFiltering = textureCnk.SuperSample;
                            material.TextureFiltering = textureCnk.FilterMode;
                            break;
                        case ChunkType.Material:
                        case ChunkType.Material_Diffuse:
                        case ChunkType.Material_Ambient:
                        case ChunkType.Material_DiffuseAmbient:
                        case ChunkType.Material_Specular:
                        case ChunkType.Material_DiffuseSpecular:
                        case ChunkType.Material_AmbientSpecular:
                        case ChunkType.Material_DiffuseAmbientSpecular:
                        case ChunkType.Material_Diffuse2:
                        case ChunkType.Material_Ambient2:
                        case ChunkType.Material_DiffuseAmbient2:
                        case ChunkType.Material_Specular2:
                        case ChunkType.Material_DiffuseSpecular2:
                        case ChunkType.Material_AmbientSpecular2:
                        case ChunkType.Material_DiffuseAmbientSpecular2:
                            PolyChunkMaterial materialCnk = (PolyChunkMaterial)cnk;
                            material.SourceBlendMode = materialCnk.SourceAlpha;
                            material.DestinationBlendmode = materialCnk.DestinationAlpha;
                            if(materialCnk.Diffuse.HasValue)
                                material.Diffuse = materialCnk.Diffuse.Value;
                            if(materialCnk.Ambient.HasValue)
                                material.Ambient = materialCnk.Ambient.Value;
                            if(materialCnk.Specular.HasValue)
                            {
                                material.Specular = materialCnk.Specular.Value;
                                material.SpecularExponent = materialCnk.SpecularExponent;
                            }
                            break;
                        case ChunkType.Strip_Strip:
                        case ChunkType.Strip_StripUVN:
                        case ChunkType.Strip_StripUVH:
                        case ChunkType.Strip_StripNormal:
                        case ChunkType.Strip_StripUVNNormal:
                        case ChunkType.Strip_StripUVHNormal:
                        case ChunkType.Strip_StripColor:
                        case ChunkType.Strip_StripUVNColor:
                        case ChunkType.Strip_StripUVHColor:
                        case ChunkType.Strip_Strip2:
                        case ChunkType.Strip_StripUVN2:
                        case ChunkType.Strip_StripUVH2:
                            PolyChunkStrip stripCnk = (PolyChunkStrip)cnk;

                            material.SetFlag(MaterialFlags.Flat, stripCnk.FlatShading);
                            material.SetFlag(MaterialFlags.noAmbient, stripCnk.IgnoreAmbient);
                            material.SetFlag(MaterialFlags.noDiffuse, stripCnk.IgnoreLight);
                            material.SetFlag(MaterialFlags.noSpecular, stripCnk.IgnoreSpecular);
                            material.SetFlag(MaterialFlags.normalMapping, stripCnk.EnvironmentMapping);
                            material.UseAlpha = stripCnk.UseAlpha;
                            material.Culling = !stripCnk.DoubleSide;

                            List<BufferCorner> corners = new();
                            List<uint> triangles = new();

                            foreach(var s in stripCnk.Strips)
                            {
                                uint l = (uint)corners.Count;

                                bool rev = s.Reversed;
                                for(uint i = 2; i < s.Corners.Length; i++)
                                {
                                    uint li = l + i;
                                    if(!rev)
                                        triangles.AddRange(new uint[] { li - 2, li - 1, li });
                                    else
                                        triangles.AddRange(new uint[] { li - 1, li - 2, li });
                                    rev = !rev;
                                }

                                foreach(var c in s.Corners)
                                    corners.Add(new BufferCorner(c.index, c.color, c.uv));
                            }

                            if(vertices != null)
                            {
                                meshes.Add(new BufferMesh(vertices, continueWeight, corners.ToArray(), triangles.ToArray(), material.Clone()));
                                vertices = null;
                            }
                            else
                                meshes.Add(new BufferMesh(corners.ToArray(), triangles.ToArray(), material.Clone()));
                            break;
                    }
                }
            }
            else if(vertices != null)
            {
                meshes.Add(new BufferMesh(vertices, continueWeight));
            }

            if(optimize)
            {
                for(int i = 0; i < meshes.Count; i++)
                {
                    if(meshes[i].Corners != null)
                    {
                        meshes[i] = meshes[i].Optimize();
                    }
                }
            }

            return meshes.ToArray();
        }


        public override Attach Clone()
        {
            return new ChunkAttach(VertexChunks.ContentClone(), PolyChunks.ContentClone())
            {
                Name = Name,
                MeshBounds = MeshBounds,
                VertexName = VertexName,
                PolyName = PolyName,
            };
        }

        public override string ToString() => $"ChunkAttach - {Name} - {(VertexChunks == null ? 0 : VertexChunks.Length)} - {(PolyChunks == null ? 0 : PolyChunks.Length)}";
    }
}

namespace SATools.SAModel.ModelData
{
    public static partial class AttachExtensions
    {
        public static CHUNK.ChunkAttach AsChunk(this Attach atc)
        {
            return new CHUNK.ChunkAttach(atc.MeshData)
            {
                Name = atc.Name
            };
        }
    }
}
