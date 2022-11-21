using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
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
        public Vector4 Position { get; set; }

        public Vector3 Normal { get; set; }

        public SortedDictionary<int, float> Weights { get; set; }

        public WeightedVertex(Vector4 position, Vector3 normal)
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

    /// <summary>
    /// Helper methods for generating attaches
    /// </summary>
    public class WeightedBufferAttach
    {
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

        public static WeightedBufferAttach Create(WeightedVertex[] vertices, BufferCorner[][] corners, BufferMaterial[] materials, NJObject[] nodes)
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


        /// <summary>
        /// Converts buffer meshes to weighted buffer meshes
        /// </summary>
        /// <param name="model">Model to convert</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static WeightedBufferAttach[] ToWeightedBuffer(NJObject model)
        {
            // checking if all the meshes have buffer information
            NJObject[] nodes = model.GetObjects();
            foreach (NJObject node in nodes)
            {
                if (node.Attach == null)
                    continue;

                if (node.Attach.MeshData == null)
                    throw new FormatException("Not all attaches have meshdata! Please generate Meshdata before converting");
            }

            WeightedVertex[] cache = new WeightedVertex[0x10000];
            int[] vertexMap = new int[cache.Length];

            // The world matrix for each object
            Dictionary<NJObject, Matrix4x4> worldMatrices = new();

            List<WeightedBufferAttach> result = new();

            for (int i = 0; i < nodes.Length; i++)
            {
                NJObject node = nodes[i];

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
                            Vector4 pos = Vector4.Transform(vtx.Position, worldMatrix) * vtx.Weight;
                            Vector3 nrm = Vector3.TransformNormal(vtx.Normal, normalMtx) * vtx.Weight;

                            int index = vtx.Index + bufferMesh.VertexWriteOffset;

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

        /// <summary>
        /// Takes (simplified) mesh data and convertes it to working mesh data. <br/>
        /// Attaches it to the "bones" afterwards. <br/>
        /// WARNING: May take some time, as it optimizes all data!
        /// </summary>
        /// <param name="nodes">All bones sorted by index. [0] has to be the root! bones need to be in hierarchy order</param>
        /// <param name="vertices">Vertex data</param>
        /// <param name="displayMeshes">Polygon info</param>
        public static void FromWeightedBuffer(NJObject[] nodes, Matrix4x4 meshMatrix, WeightedVertex[] vertices, BufferMesh[] displayMeshes)
        {
            // lets optimize our data size a bit;
            // First, we remove any duplicates
            WeightedVertex[] distinctVerts = vertices.GetDistinct();

            // then we sort the vertices; the comparer interface compares
            // them based on their weight definitions, as this is important
            Array.Sort(distinctVerts);

            // gotta get the index map to translates the indices on polygons
            int[] vertexIndexMap = CreateIndexMap(vertices, distinctVerts);

            // now we optimize the display meshes
            BufferMesh[] optimizedDisplayMeshes = new BufferMesh[displayMeshes.Length + 1];
            for (int i = 0; i < displayMeshes.Length; i++)
                optimizedDisplayMeshes[i + 1] = OptimizePolygons(displayMeshes[i], vertexIndexMap);

            // last thing to do: create the buffer meshes
            // first we gotta get the matrices
            (Matrix4x4 pos, Matrix4x4 nrm, List<BufferVertex> verts)[] matrices = new (Matrix4x4 pos, Matrix4x4 nrm, List<BufferVertex> verts)[nodes.Length];

            Dictionary<NJObject, Matrix4x4> worldMatrices = new();
            for (int i = 0; i < nodes.Length; i++)
            {
                Matrix4x4 nodeMatrix = GetWorldMatrix(nodes[i], worldMatrices);
                Matrix4x4.Invert(nodeMatrix, out Matrix4x4 NodeMatrixI);
                Matrix4x4 posMatrix = NodeMatrixI * meshMatrix;

                Matrix4x4.Invert(posMatrix, out Matrix4x4 posMatrixI);
                Matrix4x4 nrm = Matrix4x4.Transpose(posMatrixI);

                matrices[i] = (posMatrix, nrm, new());
            }

            // Now we create the corrected vertex data for each bone
            ushort vIndex = 0;
            foreach (var v in distinctVerts)
            {
                if (v.Weights.Count == 0)
                {
                    (var posmtx, var nrmmtx, var list) = matrices[0];
                    var pos = Vector3.Transform(new(v.Position.X, v.Position.Y, v.Position.Z), posmtx);
                    var nrm = Vector3.Transform(v.Normal, nrmmtx);

                    list.Add(new BufferVertex(pos, nrm, vIndex));
                }
                else
                {
                    foreach ((int index, float weight) in v.Weights)
                    {
                        (var posmtx, var nrmmtx, var list) = matrices[index];
                        var pos = Vector3.Transform(new(v.Position.X, v.Position.Y, v.Position.Z), posmtx);
                        var nrm = Vector3.Transform(v.Normal, nrmmtx);

                        list.Add(new BufferVertex(pos, nrm, vIndex, weight));
                    }
                }
                vIndex++;
            }

            // lastly we create the attaches (and correct
            // the bone info a bit for optimal meshes)
            bool initializerDone = false;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (matrices[i].verts.Count == 0)
                    continue;

                var verts = matrices[i].verts;
                BufferVertex[] vertsArray = null;

                bool isInitializer = initializerDone;
                if (!initializerDone)
                {
                    List<BufferVertex> clearVerts = new();
                    int j = 0;
                    foreach (var vert in distinctVerts)
                    {
                        clearVerts.Add(new(default, default, (ushort)j, 0));
                        j++;
                    }

                    clearVerts.RemoveAll(x => verts.Any(y => y.Index == x.Index));
                    verts.AddRange(clearVerts);
                    vertsArray = verts.OrderBy(x => x.Index).ToArray();
                    initializerDone = true;
                }
                else
                    vertsArray = verts.ToArray();

                ushort offset = vertsArray[0].Index;
                if (offset > 0)
                    for (int j = 0; j < vertsArray.Length; j++)
                        vertsArray[j].Index -= offset;

                BufferMesh mesh = new(vertsArray, isInitializer, offset);

                BufferMesh[] meshes;
                if (i == nodes.Length - 1)
                {
                    optimizedDisplayMeshes[0] = mesh;
                    meshes = optimizedDisplayMeshes;
                }
                else
                    meshes = new BufferMesh[] { mesh };

                nodes[i].Attach = new(meshes);
            }
        }

        /// <summary>
        /// Optimizes a display mesh
        /// </summary>
        /// <param name="original">The original display mesh (will not be altered)</param>
        /// <param name="vertexIndexMap">A vetex index map, if one exists</param>
        /// <returns></returns>
        private static BufferMesh OptimizePolygons(BufferMesh original, int[] vertexIndexMap)
        {
            // First we optimize the corners
            BufferCorner[] corners;
            ushort newReadOffset = original.VertexReadOffset;

            // we start by replacing the vertex indices if an index map was passed
            if (vertexIndexMap != null)
            {
                newReadOffset = (ushort)original.Corners.Min(x => vertexIndexMap[x.VertexIndex]);
                corners = new BufferCorner[original.Corners.Length];
                for (int i = 0; i < original.Corners.Length; i++)
                {
                    BufferCorner corner = original.Corners[i];
                    corner.VertexIndex = (ushort)(vertexIndexMap[corner.VertexIndex + original.VertexReadOffset] - newReadOffset);
                    corners[i] = corner;
                }
            }
            else
                corners = original.Corners;

            // next we remove all duplicates
            (BufferCorner[] distinctCorners, int[] cornerIndexMap) = corners.CreateDistinctMap();

            uint[] triangleVertices = null;
            if (cornerIndexMap != null)
            {
                if (original.TriangleList == null)
                {
                    triangleVertices = new uint[original.Corners.Length];
                    for (int i = 0; i < triangleVertices.Length; i++)
                        triangleVertices[i] = (uint)cornerIndexMap[i];
                }
                else
                {
                    triangleVertices = new uint[original.TriangleList.Length];
                    for (int i = 0; i < triangleVertices.Length; i++)
                        triangleVertices[i] = (uint)cornerIndexMap[original.TriangleList[i]];
                }
            }
            else
                triangleVertices = original.TriangleList;

            return new(distinctCorners, triangleVertices, original.Material.Clone(), newReadOffset);
        }

        private static Matrix4x4 GetWorldMatrix(NJObject njobject, Dictionary<NJObject, Matrix4x4> worldMatrices)
        {
            if (worldMatrices.TryGetValue(njobject, out Matrix4x4 local))
                return local;

            local = njobject.LocalMatrix;

            if (njobject.Parent != null)
                local *= GetWorldMatrix(njobject.Parent, worldMatrices);

            worldMatrices.Add(njobject, local);

            return local;
        }

        private static int GetCommonNodeIndex(NJObject[] nodes, HashSet<int> indices)
        {
            Dictionary<NJObject, int> indexMap = new();
            for (int i = 0; i < nodes.Length; i++)
            {
                indexMap.Add(nodes[i], i);
            }

            int[] parentIndices = new int[nodes.Length];

            foreach (int i in indices)
            {
                NJObject node = nodes[i];
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
