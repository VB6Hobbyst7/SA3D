using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.HelperExtensions;

namespace SATools.SAModel.ModelData
{
    /// <summary>
    /// Vertex in local space with weight information
    /// </summary>
    public struct WeightedVertex : IComparable<WeightedVertex>, IEquatable<WeightedVertex>
    {
        public Vector3 Position { get; set; }

        public Vector3 Normal { get; set; }

        public SortedDictionary<int, float> Weights { get; set; }

        public WeightedVertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
            Weights = new();
        }

        public override bool Equals(object obj)
        {
            return obj is WeightedVertex other
                && Position == other.Position
                && Normal == other.Normal
                && Weights.SequenceEqual(other.Weights);
        }

        public override int GetHashCode()
            => HashCode.Combine(Position, Normal, Weights);

        public int CompareTo(WeightedVertex other)
        {
            if (other.Weights.Count != Weights.Count)
                return Weights.Count - other.Weights.Count;

            for (int i = 0; i < Weights.Count; i++)
            {
                int result = Weights.Keys.ElementAt(i) - other.Weights.Keys.ElementAt(i);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        public static bool operator ==(WeightedVertex left, WeightedVertex right)
            => left.Equals(right);

        public static bool operator !=(WeightedVertex left, WeightedVertex right)
            => !(left == right);

        public override string ToString()
        {
            string result = Weights.Count.ToString();

            if (Weights.Count > 0)
            {
                result += " - ";
                foreach (int joint in Weights.Keys)
                    result += joint + ", ";
            }

            return result;
        }

        bool IEquatable<WeightedVertex>.Equals(WeightedVertex other)
            => Equals(other);

        public WeightedVertex clone()
        {
            WeightedVertex result = new(Position, Normal);
            foreach (var pair in Weights)
            {
                result.Weights.Add(pair.Key, pair.Value);
            }

            return result;
        }
    }

    public interface IOffsetableAttachResult
    {
        public int VertexCount { get; }
        public int[] AttachIndices { get; }
        public Attach[] Attaches { get; }

        public void ModifyVertexOffset(int offset);

        /// <summary>
        /// Checks for any vertex overlaps in the models and sets their vertex offset accordingly
        /// </summary>
        public static void PlanVertexOffsets<T>(T[] attaches) where T : IOffsetableAttachResult
        {
            int nodeCount = attaches.Max(x => x.AttachIndices.Max()) + 1;
            List<(int start, int end)>[] ranges = new List<(int start, int end)>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
                ranges[i] = new();

            foreach (IOffsetableAttachResult cr in attaches)
            {
                int startNode = cr.AttachIndices.Min();
                int endNode = cr.AttachIndices.Max();
                HashSet<(int start, int end)> blocked = new();

                for (int i = startNode; i <= endNode; i++)
                {
                    foreach (var r in ranges[i])
                        blocked.Add(r);
                }

                int lowestAvailableStart = 0xFFFF;

                if (blocked.Count == 0)
                {
                    lowestAvailableStart = 0;
                }
                else
                {
                    foreach (var (blockedStart, blockedEnd) in blocked)
                    {
                        if (blockedEnd >= lowestAvailableStart)
                            continue;

                        int checkStart = blockedEnd;
                        int checkEnd = checkStart + cr.VertexCount;
                        bool fits = true;
                        foreach (var checkrange in blocked)
                        {
                            if (!(checkrange.start > checkEnd || checkrange.end < checkStart))
                            {
                                fits = false;
                                break;
                            }
                        }

                        if (fits)
                        {
                            lowestAvailableStart = blockedEnd;
                        }
                    }
                }

                int lowestAvailableEnd = lowestAvailableStart + cr.VertexCount;


                for (int i = startNode; i <= endNode; i++)
                {
                    ranges[i].Add((lowestAvailableStart, lowestAvailableEnd));
                }

                if (lowestAvailableStart > 0)
                {
                    cr.ModifyVertexOffset(lowestAvailableStart);
                }
            }
        }
    }

    /// <summary>
    /// Helper methods for generating attaches
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
                foreach (int index in vtx.Weights.Keys)
                {
                    dependingNodes.Add(index);
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
                                    cache[index] = new(default, default);
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
                                cache[index] = new(pos, nrm);
                            }

                            cache[index].Weights.Add(i, vtx.Weight);
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
                                vertices.Add(cache[vertexIndex].clone());
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
                        materials.Add(bufferMesh.Material);
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

                foreach (BufferMesh mesh in node.Attach.MeshData)
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

                    if (!wVert.Weights.TryGetValue(nodeIndex, out float weight))
                        continue;

                    BufferVertex vert = new(wVert.Position, wVert.Normal, (ushort)i, weight);

                    if (wVert.Weights.Min(x => x.Key) == nodeIndex)
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
                    vertexMeshes.Add(
                        new(initVerts.ToArray(),
                            false,
                            0));
                }

                if (continueVerts.Count > 0)
                {
                    vertexMeshes.Add(
                        new(continueVerts.ToArray(),
                            true,
                            0));
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
                var (distinctVerts, vertMap) = vertices.CreateDistinctMap();
                vertices = distinctVerts;

                foreach (BufferMesh polyMesh in polygonMeshes)
                {
                    for (int i = 0; i < polyMesh.Corners.Length; i++)
                    {
                        polyMesh.Corners[i].VertexIndex = (ushort)vertMap[polyMesh.Corners[i].VertexIndex];
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
                var (distinctCorners, cornerMap) = wba.Corners[i].CreateDistinctMap();
                result.Add(new(distinctCorners, (uint[])(object)cornerMap, wba.Materials[i], 0));
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
                ObjectNode node = nodes[i];
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
