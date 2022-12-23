using SATools.SAModel.ModelData.BASIC;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.CHUNK;
using SATools.SAModel.ModelData.GC;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.HelperExtensions;

namespace SATools.SAModel.ModelData.Weighted
{
    /// <summary>
    /// Helper class for model conversions
    /// </summary>
    public class WeightedBufferAttach
    {
        private readonly struct BufferResult : IOffsetableAttachResult
        {
            public int VertexCount { get; }
            public int[] AttachIndices { get; }
            public Attach[] Attaches { get; }

            public BufferResult(int vertexCount, int[] attachIndices, Attach[] attaches)
            {
                VertexCount = vertexCount;
                AttachIndices = attachIndices;
                Attaches = attaches;
            }

            public void ModifyVertexOffset(int offset)
            {
                foreach (Attach atc in this.Attaches)
                {
                    foreach (BufferMesh bm in atc.MeshData)
                    {
                        bm.VertexWriteOffset = (ushort)(bm.VertexWriteOffset + offset);
                        bm.VertexReadOffset = (ushort)(bm.VertexReadOffset + offset);
                    }
                }
            }
        }

        public WeightedVertex[] Vertices { get; }
        public BufferCorner[][] Corners { get; }
        public BufferMaterial[] Materials { get; }
        public HashSet<int> DependingNodeIndices { get; }
        public int DependencyRootIndex { get; }

        private WeightedBufferAttach(WeightedVertex[] vertices, BufferCorner[][] corners, BufferMaterial[] materials, HashSet<int> dependingNodes, int dependencyRoot)
        {
            Vertices = vertices;
            Corners = corners;
            Materials = materials;
            DependingNodeIndices = dependingNodes;
            DependencyRootIndex = dependencyRoot;
        }

        public static WeightedBufferAttach Create(WeightedVertex[] vertices, BufferCorner[][] corners, BufferMaterial[] materials, ObjectNode[] nodes)
        {
            HashSet<int> dependingNodes = new();

            foreach (WeightedVertex vtx in vertices)
            {
                for (int i = 0; i < vtx.Weights.Length; i++)
                {
                    if (vtx.Weights[i] > 0)
                        dependingNodes.Add(i);
                }
            }

            int dependencyRoot = GetCommonNodeIndex(nodes, dependingNodes);

            return new(vertices, corners, materials, dependingNodes, dependencyRoot);
        }


        public bool CheckHasColors()
        {
            foreach (var corners in Corners)
            {
                foreach (var corner in corners)
                {
                    if (corner.Color != Color.White)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Converts buffer meshes to weighted buffer meshes
        /// </summary>
        /// <param name="model">Model to convert</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static WeightedBufferAttach[] ToWeightedBuffer(ObjectNode model, bool combineAtDependencyRoots)
        {
            // checking if all the meshes have buffer information
            ObjectNode[] nodes = model.GetObjects();
            foreach (ObjectNode node in nodes)
            {
                if (node.Attach == null)
                    continue;

                if (node.Attach.MeshData == null)
                    throw new FormatException("Not all attaches have meshdata! Please generate Meshdata before converting");
            }

            WeightedVertex[] cache = new WeightedVertex[0x10000];
            int[] vertexMap = new int[cache.Length];

            // The world matrix for each object
            Dictionary<ObjectNode, Matrix4x4> worldMatrices = new();

            List<WeightedBufferAttach> result = new();

            for (int i = 0; i < nodes.Length; i++)
            {
                ObjectNode node = nodes[i];

                // get the world matrix
                Matrix4x4 worldMatrix = node.LocalMatrix;
                if (node.Parent != null)
                    worldMatrix *= worldMatrices[node.Parent];
                worldMatrices.Add(node, worldMatrix);

                // nothing to convert
                if (node.Attach == null)
                    continue;

                // getting the other matrices
                Matrix4x4.Invert(worldMatrix, out Matrix4x4 invertedWorldMatrix);
                Matrix4x4 normalMtx = Matrix4x4.Transpose(invertedWorldMatrix);

                BufferMesh[] meshes = node.Attach.MeshData;

                List<WeightedVertex> vertices = new();
                Array.Fill(vertexMap, -1);

                List<BufferCorner[]> corners = new();
                List<BufferMaterial> materials = new();

                for (int j = 0; j < meshes.Length; j++)
                {
                    BufferMesh bufferMesh = meshes[j];

                    if (bufferMesh.Vertices != null)
                    {
                        foreach (BufferVertex vtx in bufferMesh.Vertices)
                        {
                            int index = vtx.Index + bufferMesh.VertexWriteOffset;
                            if (vtx.Weight == 0.0f)
                            {
                                if (!bufferMesh.ContinueWeight)
                                    cache[index] = new(default, default, nodes.Length);
                                continue;
                            }

                            Vector3 pos = Vector3.Transform(vtx.Position, worldMatrix) * vtx.Weight;
                            Vector3 nrm = Vector3.TransformNormal(vtx.Normal, normalMtx) * vtx.Weight;


                            if (bufferMesh.ContinueWeight)
                            {
                                cache[index].Position += pos;
                                cache[index].Normal += nrm;
                            }
                            else
                            {
                                cache[index] = new(pos, nrm, nodes.Length);
                            }

                            cache[index].Weights[i] = vtx.Weight;
                        }
                    }

                    if (bufferMesh.Corners != null)
                    {
                        List<BufferCorner> meshCorners = new();

                        foreach (BufferCorner corner in bufferMesh.Corners)
                        {
                            ushort vertexIndex = (ushort)(corner.VertexIndex + bufferMesh.VertexReadOffset);

                            if (vertexMap[vertexIndex] == -1)
                            {
                                vertexMap[vertexIndex] = vertices.Count;
                                vertices.Add(cache[vertexIndex].Clone());
                            }

                            meshCorners.Add(new(
                                (ushort)vertexMap[vertexIndex],
                                corner.Color,
                                corner.Texcoord
                                ));
                        }

                        corners.Add(
                            bufferMesh.TriangleList != null
                            ? bufferMesh.TriangleList.Select(x => meshCorners[(int)x]).ToArray()
                            : meshCorners.ToArray());
                        materials.Add(bufferMesh.Material ?? throw new NullReferenceException("Mesh with polygons has no material"));
                    }
                }

                if (corners.Count > 0)
                {
                    result.Add(WeightedBufferAttach.Create(vertices.ToArray(), corners.ToArray(), materials.ToArray(), nodes));
                }
            }

            if (!combineAtDependencyRoots)
            {
                return result.ToArray();
            }

            // check if any of the new attaches are rooted to the same node, and then combine those
            List<WeightedBufferAttach>[] sharedRoots = new List<WeightedBufferAttach>[nodes.Length];

            foreach (WeightedBufferAttach wba in result)
            {
                if (sharedRoots[wba.DependencyRootIndex] == null)
                {
                    sharedRoots[wba.DependencyRootIndex] = new();
                }

                sharedRoots[wba.DependencyRootIndex].Add(wba);
            }

            result.Clear();

            foreach (List<WeightedBufferAttach> sharedRoot in sharedRoots)
            {
                if (sharedRoot == null)
                    continue;
                if (sharedRoot.Count == 1)
                {
                    result.Add(sharedRoot[0]);
                    continue;
                }

                List<WeightedVertex> vertices = new();
                List<BufferCorner[]> corners = new();
                List<BufferMaterial> materials = new();
                HashSet<int> dependingNodes = new();

                foreach (WeightedBufferAttach wba in sharedRoot)
                {
                    int vertexOffset = vertices.Count;

                    if (vertexOffset > 0)
                    {
                        foreach (BufferCorner[] wbaCorners in wba.Corners)
                        {
                            for (int i = 0; i < wbaCorners.Length; i++)
                            {
                                wbaCorners[i].VertexIndex = (ushort)(wbaCorners[i].VertexIndex + vertexOffset);
                            }
                        }
                    }

                    corners.AddRange(wba.Corners);
                    vertices.AddRange(wba.Vertices);
                    materials.AddRange(wba.Materials);

                    foreach (int d in wba.DependingNodeIndices)
                        dependingNodes.Add(d);
                }

                result.Add(new WeightedBufferAttach(vertices.ToArray(), corners.ToArray(), materials.ToArray(), dependingNodes, sharedRoot[0].DependencyRootIndex));
            }


            return result.ToArray();
        }

        public static void FromWeightedBuffer(ObjectNode model, WeightedBufferAttach[] meshData, bool optimize, bool ignoreWeights, AttachFormat format)
        {
            switch (format)
            {
                case AttachFormat.Buffer:
                    FromWeightedBuffer(model, meshData, optimize);
                    break;
                case AttachFormat.BASIC:
                    BasicAttachConverter.ConvertWeightedToBasic(model, meshData, optimize, ignoreWeights);
                    break;
                case AttachFormat.CHUNK:
                    ChunkAttachConverter.ConvertWeightedToChunk(model, meshData, optimize);
                    break;
                case AttachFormat.GC:
                    GCAttachConverter.ConvertWeightedToGC(model, meshData, optimize, ignoreWeights);
                    break;
                default:
                    break;
            }
        }

        public static void FromWeightedBuffer(ObjectNode model, WeightedBufferAttach[] meshData, bool optimize)
        {
            List<BufferResult> bufferResults = new();

            foreach (WeightedBufferAttach wba in meshData)
            {
                BufferResult result;

                if (wba.DependingNodeIndices.Count > 0)
                {
                    result = ConvertWeighted(wba);
                }
                else
                {
                    result = ConvertWeightless(wba, optimize);
                }

                bufferResults.Add(result);
            }

            IOffsetableAttachResult.PlanVertexOffsets(bufferResults.ToArray());

            ObjectNode[] nodes = model.GetObjects();
            List<Attach>[] nodeAttaches = new List<Attach>[nodes.Length];
            for (int i = 0; i < nodeAttaches.Length; i++)
                nodeAttaches[i] = new();

            foreach (BufferResult br in bufferResults)
            {
                for (int i = 0; i < br.AttachIndices.Length; i++)
                {
                    nodeAttaches[br.AttachIndices[i]].Add(br.Attaches[i]);
                }
            }

            for (int i = 0; i < nodeAttaches.Length; i++)
            {
                List<Attach> attaches = nodeAttaches[i];
                ObjectNode node = nodes[i];
                if (attaches.Count == 0)
                {
                    node._attach = null;
                    continue;
                }
                else if (attaches.Count == 1)
                {
                    node._attach = attaches[0];
                }
                else
                {
                    List<BufferMesh> meshes = new();

                    foreach (Attach atc in attaches)
                    {
                        meshes.AddRange(atc.MeshData);
                    }

                    node._attach = new Attach(meshes.ToArray());
                }

                // transforming vertices to the nodes local space

                Matrix4x4 worldMatrix = node.GetWorldMatrix();
                Matrix4x4.Invert(worldMatrix, out Matrix4x4 invertedWorldMatrix);

                foreach (BufferMesh mesh in node._attach.MeshData)
                {
                    if (mesh.Vertices == null)
                        continue;

                    for (int j = 0; j < mesh.Vertices.Length; j++)
                    {
                        BufferVertex vert = mesh.Vertices[j];
                        mesh.Vertices[j].Position = Vector3.Transform(vert.Position, invertedWorldMatrix);
                        mesh.Vertices[j].Normal = Vector3.TransformNormal(vert.Normal, invertedWorldMatrix);
                    }

                }
            }
        }

        private static BufferResult ConvertWeighted(WeightedBufferAttach wba)
        {
            List<(int nodeIndex, BufferMesh[])> meshSets = new();
            foreach (int nodeIndex in wba.DependingNodeIndices.OrderBy(x => x))
            {
                List<BufferVertex> initVerts = new();
                List<BufferVertex> continueVerts = new();

                for (int i = 0; i < wba.Vertices.Length; i++)
                {
                    WeightedVertex wVert = wba.Vertices[i];

                    float weight = wVert.Weights[nodeIndex];
                    if (weight == 0)
                        continue;

                    BufferVertex vert = new(wVert.Position, wVert.Normal, (ushort)i, weight);

                    if (wVert.GetFirstWeightIndex() == nodeIndex)
                    {
                        initVerts.Add(vert);
                    }
                    else
                    {
                        continueVerts.Add(vert);
                    }
                }

                List<BufferMesh> vertexMeshes = new();

                if (initVerts.Count > 0)
                {
                    vertexMeshes.Add(new(initVerts.ToArray(), false, 0));
                }

                if (continueVerts.Count > 0)
                {
                    vertexMeshes.Add(new(continueVerts.ToArray(), true, 0));
                }

                meshSets.Add((nodeIndex, vertexMeshes.ToArray()));
            }

            BufferMesh[] polyMeshes = GetPolygonMeshes(wba);

            int[] nodeIndices = new int[meshSets.Count];
            Attach[] attaches = new Attach[meshSets.Count];

            for (int i = 0; i < meshSets.Count - 1; i++)
            {
                (int nodeIndex, BufferMesh[] vertexMeshes) = meshSets[i];
                nodeIndices[i] = nodeIndex;
                attaches[i] = new(vertexMeshes);
            }

            int lastIndex = meshSets.Count - 1;
            (int lastNodeIndex, BufferMesh[] lastMeshes) = meshSets[lastIndex];
            nodeIndices[lastIndex] = lastNodeIndex;

            List<BufferMesh> meshes = new();
            meshes.AddRange(lastMeshes);
            meshes.AddRange(polyMeshes);
            attaches[lastIndex] = new(meshes.ToArray());

            return new(
                wba.Vertices.Length,
                nodeIndices,
                attaches);
        }

        private static BufferResult ConvertWeightless(WeightedBufferAttach wba, bool optimize)
        {
            List<BufferMesh> meshes = new();

            BufferVertex[] vertices = new BufferVertex[wba.Vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                WeightedVertex wVert = wba.Vertices[i];
                vertices[i] = new(wVert.Position, wVert.Normal, (ushort)i);
            }

            BufferMesh[] polygonMeshes = GetPolygonMeshes(wba);

            if (optimize)
            {
                if (vertices.CreateDistinctMap(out BufferVertex[]? distinctVerts, out int[]? vertMap))
                {
                    vertices = distinctVerts;

                    foreach (BufferMesh polyMesh in polygonMeshes)
                    {
                        if (polyMesh.Corners == null)
                            continue;

                        for (int i = 0; i < polyMesh.Corners.Length; i++)
                        {
                            polyMesh.Corners[i].VertexIndex = (ushort)vertMap[polyMesh.Corners[i].VertexIndex];
                        }
                    }
                }
            }

            meshes.Add(new(vertices, false, 0));

            meshes.AddRange(polygonMeshes);

            return new(
                vertices.Length,
                new int[] { wba.DependencyRootIndex },
                new Attach[] { new(meshes.ToArray()) });
        }

        private static BufferMesh[] GetPolygonMeshes(WeightedBufferAttach wba)
        {
            List<BufferMesh> result = new();

            for (int i = 0; i < wba.Corners.Length; i++)
            {
                BufferCorner[] corners = wba.Corners[i];
                if (corners.CreateDistinctMap(out BufferCorner[]? distinctCorners, out int[]? cornerMap))
                {
                    result.Add(new(distinctCorners, (uint[])(object)cornerMap, wba.Materials[i], 0));
                }
                else
                {
                    uint[] triangleList = new uint[corners.Length];
                    for (uint j = 0; j < triangleList.Length; j++) triangleList[j] = j;
                    result.Add(new(corners, triangleList, wba.Materials[i], 0));
                }
            }

            return result.ToArray();
        }


        private static int GetCommonNodeIndex(ObjectNode[] nodes, HashSet<int> indices)
        {
            Dictionary<ObjectNode, int> indexMap = new();
            for (int i = 0; i < nodes.Length; i++)
            {
                indexMap.Add(nodes[i], i);
            }

            int[] parentIndices = new int[nodes.Length];

            foreach (int i in indices)
            {
                ObjectNode? node = nodes[i];
                while (node != null)
                {
                    parentIndices[indexMap[node]]++;
                    node = node.Parent;
                }

            }

            int target = indices.Count;
            for (int i = parentIndices.Length - 1; i >= 0; i--)
            {
                if (parentIndices[i] == target)
                    return i;
            }

            return 0;
        }

    }
}
