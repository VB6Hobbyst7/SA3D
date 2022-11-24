using SATools.SACommon;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SATools.SAModel.ModelData.CHUNK
{
    public static class ChunkAttachConverter
    {
        private readonly struct IndexedWeightVertex
        {
            public readonly int index;
            public readonly WeightedVertex vertex;

            public IndexedWeightVertex(int index, WeightedVertex vertex)
            {
                this.index = index;
                this.vertex = vertex;
            }
        }

        private readonly struct BinaryWeightColorVertex : IEquatable<BinaryWeightColorVertex>
        {
            public readonly int nodeIndex;
            public readonly Vector3 position;
            public readonly Color color;

            public BinaryWeightColorVertex(int nodeIndex, Vector3 position, Color color)
            {
                this.nodeIndex = nodeIndex;
                this.position = position;
                this.color = color;
            }

            public override bool Equals(object obj)
            {
                return obj is BinaryWeightColorVertex vertex &&
                       nodeIndex == vertex.nodeIndex &&
                       position.Equals(vertex.position) &&
                       color.Equals(vertex.color);
            }

            bool IEquatable<BinaryWeightColorVertex>.Equals(BinaryWeightColorVertex other) => Equals(other);

            public override int GetHashCode()
            {
                return HashCode.Combine(nodeIndex, position, color);
            }
        }

        private readonly struct ChunkResult : IOffsetableAttachResult
        {
            public int VertexCount { get; }
            public int[] AttachIndices { get; }
            public ChunkAttach[] Attaches { get; }

            Attach[] IOffsetableAttachResult.Attaches => Attaches;

            public ChunkResult(int vertexCount, int[] attachIndices, ChunkAttach[] attaches)
            {
                VertexCount = vertexCount;
                AttachIndices = attachIndices;
                Attaches = attaches;
            }

            public void ModifyVertexOffset(int offset)
            {
                foreach (ChunkAttach attach in Attaches)
                {
                    if (attach.VertexChunks != null)
                    {
                        foreach (VertexChunk vtx in attach.VertexChunks)
                        {
                            vtx.IndexOffset = (ushort)(vtx.IndexOffset + offset);
                        }
                    }

                    if (attach.PolyChunks != null)
                    {
                        foreach (PolyChunk poly in attach.PolyChunks)
                        {
                            if (poly is not PolyChunkStrip stripChunk)
                                continue;

                            foreach (var strip in stripChunk.Strips)
                            {
                                for (int i = 0; i < strip.Corners.Length; i++)
                                {
                                    strip.Corners[i].Index = (ushort)(strip.Corners[i].Index + offset);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ConvertModelToChunk(NJObject model, bool optimize = true, bool forceUpdate = false)
        {
            if (model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            if (model.AttachFormat == AttachFormat.CHUNK && !forceUpdate)
                return;

            var weightedMeshes = WeightedBufferAttach.ToWeightedBuffer(model, false);

            ConvertWeightedToChunk(model, weightedMeshes, optimize);
        }

        public static void ConvertWeightedToChunk(NJObject model, WeightedBufferAttach[] meshData, bool optimize = true)
        {
            List<ChunkResult> chunkResults = new();

            foreach (WeightedBufferAttach wba in meshData)
            {
                ChunkResult results;

                if (wba.DependingNodeIndices.Count > 0)
                {
                    bool binaryWeighted = true;
                    foreach (var vertex in wba.Vertices)
                    {
                        if (vertex.Weights.Count > 1)
                        {
                            binaryWeighted = false;
                            break;
                        }
                    }

                    if (binaryWeighted && wba.CheckHasColors())
                    {
                        results = ConvertWeightedBinaryColored(wba);
                    }
                    else
                    {
                        results = ConvertWeighted(wba);
                    }
                }
                else
                {
                    results = ConvertWeightless(wba);
                }

                chunkResults.Add(results);
            }

            IOffsetableAttachResult.PlanVertexOffsets(chunkResults.ToArray());

            NJObject[] nodes = model.GetObjects();
            List<ChunkAttach>[] nodeChunks = new List<ChunkAttach>[nodes.Length];
            for (int i = 0; i < nodeChunks.Length; i++)
                nodeChunks[i] = new();

            foreach (ChunkResult cr in chunkResults)
            {
                for (int i = 0; i < cr.AttachIndices.Length; i++)
                {
                    nodeChunks[cr.AttachIndices[i]].Add(cr.Attaches[i]);
                }
            }

            for (int i = 0; i < nodeChunks.Length; i++)
            {
                List<ChunkAttach> chunks = nodeChunks[i];
                NJObject node = nodes[i];
                if (chunks.Count == 0)
                {
                    node._attach = null;
                    continue;
                }
                else if (chunks.Count == 1)
                {
                    node._attach = chunks[0];
                }
                else
                {
                    List<VertexChunk> vertexChunks = new();
                    List<PolyChunk> polyChunks = new();

                    foreach (ChunkAttach atc in chunks)
                    {
                        vertexChunks.AddRange(atc.VertexChunks);
                        polyChunks.AddRange(atc.PolyChunks);
                    }

                    node._attach = new ChunkAttach(vertexChunks.ToArray(), polyChunks.ToArray());
                }

                // transforming vertices to the nodes local space

                Matrix4x4 worldMatrix = node.GetWorldMatrix();
                Matrix4x4.Invert(worldMatrix, out Matrix4x4 invertedWorldMatrix);

                foreach (VertexChunk vtx in ((ChunkAttach)node.Attach).VertexChunks)
                {
                    if (vtx.Type.VertexHasNormal())
                    {
                        for (int j = 0; j < vtx.Vertices.Length; j++)
                        {
                            ChunkVertex vert = vtx.Vertices[j];
                            vtx.Vertices[j].Position = Vector3.Transform(vert.Position, invertedWorldMatrix);
                            vtx.Vertices[j].Normal = Vector3.TransformNormal(vert.Normal, invertedWorldMatrix);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < vtx.Vertices.Length; j++)
                        {
                            ChunkVertex vert = vtx.Vertices[j];
                            vtx.Vertices[j].Position = Vector3.Transform(vert.Position, invertedWorldMatrix);
                        }
                    }
                }
            }

            ConvertModelFromChunk(model, optimize);
        }

        private static ChunkResult ConvertWeightless(WeightedBufferAttach wba)
        {
            ChunkVertex[] vertices;
            PolyChunkStrip.Strip.Corner[][] cornerSets = new PolyChunkStrip.Strip.Corner[wba.Corners.Length][];

            ChunkType type = ChunkType.Vertex_VertexNormal;
            if (wba.CheckHasColors())
            {
                type = ChunkType.Vertex_VertexDiffuse8;
                List<ChunkVertex> colorVertices = new();
                for (int i = 0; i < wba.Corners.Length; i++)
                {
                    var bufferCorners = wba.Corners[i];
                    PolyChunkStrip.Strip.Corner[] corners = new PolyChunkStrip.Strip.Corner[bufferCorners.Length];
                    for (int j = 0; j < bufferCorners.Length; j++)
                    {
                        BufferCorner bc = bufferCorners[j];
                        corners[j] = new()
                        {
                            Index = (ushort)colorVertices.Count,
                            Texcoord = bc.Texcoord
                        };

                        var vertex = wba.Vertices[bc.VertexIndex];
                        colorVertices.Add(new(vertex.Position, bc.Color, Color.White));
                    }
                    cornerSets[i] = corners;
                }

                // first, get rid of all duplicate vertices
                var (distinctVerts, vertMap) = colorVertices.CreateDistinctMap();
                vertices = distinctVerts.ToArray();

                for (int i = 0; i < cornerSets.Length; i++)
                {
                    var corners = cornerSets[i];
                    for (int j = 0; j < corners.Length; j++)
                    {
                        corners[i].Index = (ushort)vertMap[corners[i].Index];
                    }
                }
            }
            else
            {
                vertices = new ChunkVertex[wba.Vertices.Length];
                // converting the vertices 1:1, with normal information
                for (int i = 0; i < wba.Vertices.Length; i++)
                {
                    var vert = wba.Vertices[i];
                    Vector3 position = new(vert.Position.X, vert.Position.Y, vert.Position.Z);
                    vertices[i] = new(position, vert.Normal);
                }
            }

            VertexChunk vtxChunk = new(type, WeightStatus.Start, 0, vertices);

            List<PolyChunk> polyChunks = new();
            for (int i = 0; i < cornerSets.Length; i++)
            {
                polyChunks.AddRange(CreateStripChunk(cornerSets[i], wba.Materials[i]));
            }

            return new(
                vertices.Length,
                new int[] { wba.DependencyRootIndex },
                new[] {
                    new ChunkAttach(
                        new[] { vtxChunk },
                        polyChunks.ToArray())
                });
        }

        private static ChunkResult ConvertWeightedBinaryColored(WeightedBufferAttach wba)
        {
            List<BinaryWeightColorVertex> vertices = new();
            PolyChunkStrip.Strip.Corner[][] cornerSets = new PolyChunkStrip.Strip.Corner[wba.Corners.Length][];

            // Get every vertex per corner
            for (int i = 0; i < wba.Corners.Length; i++)
            {
                var bufferCorners = wba.Corners[i];
                PolyChunkStrip.Strip.Corner[] corners = new PolyChunkStrip.Strip.Corner[bufferCorners.Length];
                for (int j = 0; j < bufferCorners.Length; j++)
                {
                    BufferCorner bc = bufferCorners[j];
                    corners[j] = new()
                    {
                        Index = (ushort)vertices.Count,
                        Texcoord = bc.Texcoord
                    };

                    var vertex = wba.Vertices[bc.VertexIndex];
                    int nodeIndex = 0;
                    float weight = 0;
                    foreach (var weightPair in vertex.Weights)
                    {
                        if (weightPair.Value > weight)
                        {
                            weight = weightPair.Value;
                            nodeIndex = weightPair.Key;
                        }
                    }


                    Vector3 position = new(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);

                    vertices.Add(new(nodeIndex, position, bc.Color));
                }
                cornerSets[i] = corners;
            }

            // first, get rid of all duplicate vertices
            var (distinctVerts, vertMap) = vertices.CreateDistinctMap();

            // now sort the vertices by node index
            (int index, BinaryWeightColorVertex vert)[] sortedVertices = new (int index, BinaryWeightColorVertex)[distinctVerts.Length];
            for (int i = 0; i < sortedVertices.Length; i++)
            {
                sortedVertices[i] = (i, distinctVerts[i]);
            }

            sortedVertices = sortedVertices.OrderBy(x => x.vert.nodeIndex).ToArray();

            // Create a vertex chunk per node index
            List<(int nodeIndex, VertexChunk chunk)> vertexChunks = new();

            int currentNodeIndex = -1;
            List<ChunkVertex> chunkVertices = new();
            ushort currentVertexOffset = 0;
            int[] sortedVertMap = new int[sortedVertices.Length];
            for (int i = 0; i < sortedVertices.Length; i++)
            {
                var vert = sortedVertices[i];
                if (vert.vert.nodeIndex != currentNodeIndex)
                {
                    if (chunkVertices.Count > 0)
                    {
                        vertexChunks.Add((currentNodeIndex, new(
                            ChunkType.Vertex_VertexDiffuse8,
                            WeightStatus.Start,
                            currentVertexOffset,
                            chunkVertices.ToArray())));
                    }

                    currentVertexOffset = (ushort)i;
                    chunkVertices.Clear();
                    currentNodeIndex = vert.vert.nodeIndex;
                }

                chunkVertices.Add(new(vert.vert.position, vert.vert.color, Color.White));
                sortedVertMap[vert.index] = i;
            }

            vertexChunks.Add((currentNodeIndex, new(
                ChunkType.Vertex_VertexDiffuse8,
                WeightStatus.Start,
                currentVertexOffset,
                chunkVertices.ToArray())));

            // get the vertex chunks
            List<PolyChunk> polyChunks = new();
            for (int i = 0; i < cornerSets.Length; i++)
            {
                var corners = cornerSets[i];
                for (int j = 0; j < corners.Length; j++)
                {
                    corners[j].Index = (ushort)sortedVertMap[vertMap[corners[j].Index]];
                }

                polyChunks.AddRange(CreateStripChunk(corners, wba.Materials[i]));
            }

            // assemble the attaches
            List<int> nodeAttachIndices = new();
            List<ChunkAttach> attaches = new();

            for (int i = 0; i < vertexChunks.Count - 1; i++)
            {
                var (nodeIndex, chunks) = vertexChunks[i];
                nodeAttachIndices.Add(nodeIndex);
                attaches.Add(new(new[] { chunks }, null));
            }

            var (lastNodeindex, lastVertexChunk) = vertexChunks[^1];
            nodeAttachIndices.Add(lastNodeindex);
            attaches.Add(new(new[] { lastVertexChunk }, polyChunks.ToArray()));

            return new(distinctVerts.Length, nodeAttachIndices.ToArray(), attaches.ToArray());
        }

        private static ChunkResult ConvertWeighted(WeightedBufferAttach wba)
        {
            List<IndexedWeightVertex> singleWeights = new();
            List<IndexedWeightVertex> multiWeights = new();

            for (int i = 0; i < wba.Vertices.Length; i++)
            {
                WeightedVertex vtx = wba.Vertices[i];
                if (vtx.Weights.Count == 0)
                {
                    throw new InvalidDataException("Vertex has no specified weights");
                }
                else if (vtx.Weights.Count == 1)
                {
                    singleWeights.Add(new(i, vtx));
                }
                else
                {
                    multiWeights.Add(new(i, vtx));
                }
            }

            singleWeights = singleWeights.OrderBy(x => x.vertex.Weights.ElementAt(0).Key).ToList();
            int multiWeightOffset = singleWeights.Count;

            // grouping the vertices together by node
            List<(int nodeIndex, VertexChunk[] chunks)> vertexChunks = new();

            foreach (int nodeIndex in wba.DependingNodeIndices.OrderBy(x => x))
            {
                List<VertexChunk> chunks = new();

                // find out if any singleWeights belong to the node index
                int singleWeightIndexOffset = 0;
                List<ChunkVertex> singleWeightVerts = new();
                for (int i = 0; i < singleWeights.Count; i++)
                {
                    var vert = singleWeights[i].vertex;
                    bool contains = vert.Weights.ContainsKey(nodeIndex);
                    if (contains)
                    {
                        if (singleWeightVerts.Count == 0)
                        {
                            singleWeightIndexOffset = i;
                        }

                        Vector3 pos = new(vert.Position.X, vert.Position.Y, vert.Position.Z);
                        singleWeightVerts.Add(new(pos, vert.Normal, (ushort)i, 1f));
                    }
                    if (!contains && singleWeightVerts.Count > 0)
                    {
                        break;
                    }
                }

                if (singleWeightVerts.Count > 0)
                {
                    chunks.Add(
                        new VertexChunk(
                            ChunkType.Vertex_VertexNormal,
                            WeightStatus.Start,
                            (ushort)singleWeightIndexOffset,
                            singleWeightVerts.ToArray()));
                }

                // now the ones with weights. we differentiate between
                // those that initiate and those that continue
                List<ChunkVertex> initWeightsVerts = new();
                List<ChunkVertex> continueWeightsVerts = new();
                List<ChunkVertex> endWeightsVerts = new();

                for (int i = 0; i < multiWeights.Count; i++)
                {
                    var vert = multiWeights[i].vertex;

                    if (!vert.Weights.TryGetValue(nodeIndex, out float weight))
                        continue;

                    Vector3 pos = new(vert.Position.X, vert.Position.Y, vert.Position.Z);
                    ChunkVertex chunkVert = new(pos, vert.Normal, (ushort)(i + multiWeightOffset), weight);

                    if (vert.Weights.Min(x => x.Key) == nodeIndex)
                    {
                        initWeightsVerts.Add(chunkVert);
                    }
                    else if (vert.Weights.Max(x => x.Key) == nodeIndex)
                    {
                        endWeightsVerts.Add(chunkVert);
                    }
                    else
                    {
                        continueWeightsVerts.Add(chunkVert);
                    }
                }

                if (initWeightsVerts.Count > 0)
                {
                    chunks.Add(
                        new VertexChunk(
                            ChunkType.Vertex_VertexNormalNinjaAttributes,
                            WeightStatus.Start, 0,
                            initWeightsVerts.ToArray()));
                }

                if (continueWeightsVerts.Count > 0)
                {
                    chunks.Add(
                        new VertexChunk(
                            ChunkType.Vertex_VertexNormalNinjaAttributes,
                            WeightStatus.Middle, 0,
                            continueWeightsVerts.ToArray()));
                }

                if (endWeightsVerts.Count > 0)
                {
                    chunks.Add(
                        new VertexChunk(
                            ChunkType.Vertex_VertexNormalNinjaAttributes,
                            WeightStatus.End, 0,
                            endWeightsVerts.ToArray()));
                }

                vertexChunks.Add((nodeIndex, chunks.ToArray()));
            }

            vertexChunks = vertexChunks.OrderBy(x => x.nodeIndex).ToList();

            // mapping the indices for the polygons
            ushort[] indexMap = new ushort[wba.Vertices.Length];
            for (int i = 0; i < singleWeights.Count; i++)
                indexMap[singleWeights[i].index] = (ushort)i;

            for (int i = 0; i < multiWeights.Count; i++)
                indexMap[multiWeights[i].index] = (ushort)(i + multiWeightOffset);

            // assemble the polygon chunks
            List<PolyChunk> polyChunks = new();
            for (int i = 0; i < wba.Corners.Length; i++)
            {
                // mapping the triangles to the chunk format
                BufferCorner[] bufferCorners = wba.Corners[i];
                PolyChunkStrip.Strip.Corner[] corners = new PolyChunkStrip.Strip.Corner[bufferCorners.Length];
                for (int j = 0; j < bufferCorners.Length; j++)
                {
                    BufferCorner bc = bufferCorners[j];
                    corners[j] = new()
                    {
                        Index = indexMap[bc.VertexIndex],
                        Texcoord = bc.Texcoord
                    };
                }

                polyChunks.AddRange(CreateStripChunk(corners, wba.Materials[i]));
            }

            // assemble the attaches
            List<int> nodeAttachIndices = new();
            List<ChunkAttach> attaches = new();

            for (int i = 0; i < vertexChunks.Count - 1; i++)
            {
                var (nodeIndex, chunks) = vertexChunks[i];
                nodeAttachIndices.Add(nodeIndex);
                attaches.Add(new(chunks, null));
            }

            var (lastNodeindex, lastVertexChunk) = vertexChunks[^1];
            nodeAttachIndices.Add(lastNodeindex);
            attaches.Add(new(lastVertexChunk, polyChunks.ToArray()));

            return new(wba.Vertices.Length, nodeAttachIndices.ToArray(), attaches.ToArray());
        }

        private static PolyChunk[] CreateStripChunk(PolyChunkStrip.Strip.Corner[] corners, BufferMaterial material)
        {
            (PolyChunkStrip.Strip.Corner[] distinct, int[] map) = corners.CreateDistinctMap();

            int[][] stripMaps;
            if (map == null)
            {
                stripMaps = new int[distinct.Length / 3][];
                for (int i = 0; i < distinct.Length; i += 3)
                {
                    stripMaps[i] = new[] { i, i + 1, i + 2 };
                }
            }
            else
            {
                stripMaps = Strippifier.Strip(map);
            }

            PolyChunkStrip stripchunk = new((ushort)(corners.Length / 3), 0)
            {
                HasUV = true,
                FlatShading = material.HasAttribute(MaterialAttributes.Flat),
                IgnoreAmbient = material.HasAttribute(MaterialAttributes.noAmbient),
                IgnoreLight = material.HasAttribute(MaterialAttributes.noDiffuse),
                IgnoreSpecular = material.HasAttribute(MaterialAttributes.noSpecular),
                EnvironmentMapping = material.HasAttribute(MaterialAttributes.normalMapping),
                UseAlpha = material.UseAlpha,
                DoubleSide = !material.Culling
            };

            for (int i = 0; i < corners.Length; i += 3)
            {
                stripchunk.Strips[i / 3] = new PolyChunkStrip.Strip(new[] { corners[i], corners[i + 1], corners[i + 2] }, false);
            }

            /*for (int j = 0; j < stripMaps.Length; j++)
            {
                int[] strip = stripMaps[j];
                bool reversed = strip[0] == strip[1];
                int stripLength = strip.Length;
                if (reversed)
                {
                    stripLength--;
                }

                PolyChunkStrip.Strip.Corner[] stripCorners = new PolyChunkStrip.Strip.Corner[stripLength];
                int offset = strip.Length - stripLength;

                for (int k = offset; k < strip.Length; k++)
                    stripCorners[k - offset] = distinct[strip[k]];

                stripchunk.Strips[j] = new PolyChunkStrip.Strip(stripCorners, reversed);
            }*/

            return new PolyChunk[]
            {
                new PolyChunkMaterial()
                {
                    SourceAlpha = material.SourceBlendMode,
                    DestinationAlpha = material.DestinationBlendmode,
                    Diffuse = material.Diffuse,
                    Ambient = material.Ambient,
                    Specular = material.Specular,
                    SpecularExponent = (byte)material.SpecularExponent
                },

                new PolyChunkTextureID(false)
                {
                    ClampU = material.ClampU,
                    ClampV = material.ClampV,
                    MirrorU = material.MirrorU,
                    MirrorV = material.MirrorV,
                    FilterMode = material.TextureFiltering,
                    SuperSample = material.AnisotropicFiltering,
                    TextureID = (ushort)material.TextureIndex
                },

                stripchunk
            };
        }


        public static void ConvertModelFromChunk(NJObject model, bool optimize = true)
        {
            if (model.Parent != null)
                throw new FormatException($"Model {model.Name} is not hierarchy root!");

            HashSet<ChunkAttach> attaches = new();
            NJObject[] models = model.GetObjects();

            foreach (NJObject obj in models)
            {
                if (obj.Attach == null)
                    continue;
                if (obj.Attach.Format != AttachFormat.CHUNK)
                    throw new FormatException("Not all Attaches inside the model are a CHUNK attaches! Cannot convert");

                ChunkAttach atc = (ChunkAttach)obj.Attach;

                attaches.Add(atc);
            }

            List<PolyChunk>[] polyChunkCache = Array.Empty<List<PolyChunk>>();
            ChunkVertex[] vertexCache = new ChunkVertex[0x10000];

            foreach (ChunkAttach atc in attaches)
            {
                List<BufferMesh> meshes = new();

                BufferVertex[] vertices = null;
                bool continueWeight = false;

                if (atc.VertexChunks != null)
                {
                    for (int i = 0; i < atc.VertexChunks.Length; i++)
                    {
                        VertexChunk cnk = atc.VertexChunks[i];

                        List<BufferVertex> vertexList = new();
                        if (!cnk.HasWeight)
                        {
                            for (int j = 0; j < cnk.Vertices.Length; j++)
                            {
                                ChunkVertex vtx = cnk.Vertices[j];
                                int vtxIndex = j + cnk.IndexOffset;
                                vertexCache[vtxIndex] = vtx;
                                vertexList.Add(new BufferVertex(vtx.Position, vtx.Normal, (ushort)vtxIndex));
                            }
                        }
                        else
                        {
                            for (int j = 0; j < cnk.Vertices.Length; j++)
                            {
                                ChunkVertex vtx = cnk.Vertices[j];
                                int vtxIndex = vtx.Index + cnk.IndexOffset;
                                vertexCache[vtxIndex] = vtx;
                                vertexList.Add(new BufferVertex(vtx.Position, vtx.Normal, (ushort)vtxIndex, vtx.Weight));
                            }
                        }
                        vertices = vertexList.ToArray();
                        continueWeight = cnk.WeightStatus != WeightStatus.Start;

                        if (i < atc.VertexChunks.Length - 1)
                        {
                            meshes.Add(new BufferMesh(vertices, continueWeight));
                        }
                    }
                }


                List<PolyChunk> active = new();

                if (atc.PolyChunks != null)
                {
                    int cacheID = -1;
                    foreach (PolyChunk cnk in atc.PolyChunks)
                    {
                        switch (cnk.Type)
                        {
                            case ChunkType.Bits_CachePolygonList:
                                PolyChunkCachePolygonList cacheListCnk = (PolyChunkCachePolygonList)cnk;
                                cacheID = cacheListCnk.List;

                                if (polyChunkCache.Length <= cacheID)
                                    Array.Resize(ref polyChunkCache, cacheID + 1);

                                polyChunkCache[cacheID] = new List<PolyChunk>();
                                break;
                            case ChunkType.Bits_DrawPolygonList:
                                PolyChunkDrawPolygonList drawListCnk = (PolyChunkDrawPolygonList)cnk;
                                active.AddRange(polyChunkCache[drawListCnk.List]);
                                break;
                            default:
                                if (cacheID > -1)
                                    polyChunkCache[cacheID].Add(cnk);
                                else
                                    active.Add(cnk);
                                break;
                        }
                    }
                }


                if (active.Count > 0)
                {
                    BufferMaterial material = new()
                    {
                        MaterialAttributes = MaterialAttributes.useTexture
                    };
                    foreach (PolyChunk cnk in active)
                    {
                        switch (cnk.Type)
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
                                if (materialCnk.Diffuse.HasValue)
                                    material.Diffuse = materialCnk.Diffuse.Value;
                                if (materialCnk.Ambient.HasValue)
                                    material.Ambient = materialCnk.Ambient.Value;
                                if (materialCnk.Specular.HasValue)
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
                                    for (uint i = 2; i < s.Corners.Length; i++)
                                    {
                                        uint li = l + i;
                                        if (!rev)
                                            triangles.AddRange(new uint[] { li - 2, li - 1, li });
                                        else
                                            triangles.AddRange(new uint[] { li - 1, li - 2, li });
                                        rev = !rev;
                                    }

                                    foreach (var c in s.Corners)
                                    {
                                        Color color = hasColor
                                            ? c.Color
                                            : vertexCache[c.Index].Diffuse;

                                        corners.Add(new BufferCorner(c.Index, color, c.Texcoord));
                                    }
                                }

                                if (vertices != null)
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
                else if (vertices != null)
                {
                    meshes.Add(new BufferMesh(vertices, continueWeight));
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
