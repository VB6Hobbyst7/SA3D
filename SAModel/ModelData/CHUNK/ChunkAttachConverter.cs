using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SAModel.ModelData.CHUNK
{
    public static class ChunkAttachConverter
    {

        private static List<PolyChunk>[] PolyChunkCache = Array.Empty<List<PolyChunk>>();
        /// <summary>
        /// Vertex cache needed for the polygon colors
        /// </summary>
        private static readonly ChunkVertex[] VertexCache = new ChunkVertex[0x10000];

        public static void ConvertModelFromChunk(NJObject model, bool optimize = true)
        {
            if(model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            HashSet<ChunkAttach> attaches = new();
            NJObject[] models = model.GetObjects();

            foreach(NJObject obj in models)
            {
                if(obj.Attach == null)
                    continue;
                if(obj.Attach.Format != AttachFormat.CHUNK)
                    throw new FormatException("Not all Attaches inside the model are a CHUNK attaches! Cannot convert");

                ChunkAttach atc = (ChunkAttach)obj.Attach;

                attaches.Add(atc);
            }

            Array.Clear(PolyChunkCache, 0, PolyChunkCache.Length);
            Array.Clear(VertexCache, 0, VertexCache.Length);

            foreach(ChunkAttach atc in attaches)
            {
                List<BufferMesh> meshes = new();

                BufferVertex[] vertices = null;
                bool continueWeight = false;

                if(atc.VertexChunks != null)
                {
                    for(int i = 0; i < atc.VertexChunks.Length; i++)
                    {
                        VertexChunk cnk = atc.VertexChunks[i];

                        List<BufferVertex> vertexList = new();
                        if(!cnk.HasWeight)
                        {
                            for(int j = 0; j < cnk.Vertices.Length; j++)
                            {
                                ChunkVertex vtx = cnk.Vertices[j];
                                int vtxIndex = j + cnk.IndexOffset;
                                VertexCache[vtxIndex] = vtx;
                                vertexList.Add(new BufferVertex(vtx.Position, vtx.Normal, (ushort)(j + cnk.IndexOffset)));
                            }
                        }
                        else
                        {
                            for(int j = 0; j < cnk.Vertices.Length; j++)
                            {
                                ChunkVertex vtx = cnk.Vertices[j];
                                int vtxIndex = vtx.Index + cnk.IndexOffset;
                                VertexCache[vtxIndex] = vtx;
                                vertexList.Add(new BufferVertex(vtx.Position, vtx.Normal, (ushort)vtxIndex, vtx.Weight));
                            }
                        }
                        vertices = vertexList.ToArray();
                        continueWeight = cnk.WeightStatus != WeightStatus.Start;

                        if(i < atc.VertexChunks.Length - 1)
                        {
                            meshes.Add(new BufferMesh(vertices, continueWeight));
                        }
                    }
                }


                List<PolyChunk> active = new();

                if(atc.PolyChunks != null)
                {
                    int cacheID = -1;
                    foreach(PolyChunk cnk in atc.PolyChunks)
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
                        MaterialAttributes = MaterialAttributes.useTexture
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

                                material.SetAttribute(MaterialAttributes.Flat, stripCnk.FlatShading);
                                material.SetAttribute(MaterialAttributes.noAmbient, stripCnk.IgnoreAmbient);
                                material.SetAttribute(MaterialAttributes.noDiffuse, stripCnk.IgnoreLight);
                                material.SetAttribute(MaterialAttributes.noSpecular, stripCnk.IgnoreSpecular);
                                material.SetAttribute(MaterialAttributes.normalMapping, stripCnk.EnvironmentMapping);
                                material.UseAlpha = stripCnk.UseAlpha;
                                material.Culling = !stripCnk.DoubleSide;

                                List<BufferCorner> corners = new();
                                List<uint> triangles = new();

                                bool hasColor = cnk.Type.StripHasColor();

                                foreach (var s in stripCnk.Strips)
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
                                    {
                                        Color color = hasColor
                                            ? c.Color 
                                            : VertexCache[c.Index].Diffuse;

                                        corners.Add(new BufferCorner(c.Index, color, c.Texcoord));
                                    }
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
                        meshes[i].Optimize();
                }

                atc.MeshData = meshes.ToArray();
            }
        }
    }
}
