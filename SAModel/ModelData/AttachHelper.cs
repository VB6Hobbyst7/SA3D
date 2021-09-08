using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.HelperExtensions;
using static SATools.SACommon.MathHelper;

namespace SATools.SAModel.ModelData
{
    /// <summary>
    /// Helper methods for generating attaches
    /// </summary>
    public static class AttachHelper
    {
        public struct VertexWeightSet
        {
            public VertexWeights[] vertices;
            public BufferMesh[] displayMeshes;

            public VertexWeightSet(VertexWeights[] vertices, BufferMesh[] displayMeshes)
            {
                this.vertices = vertices;
                this.displayMeshes = displayMeshes;
            }
        }

        /// <summary>
        /// Vertex in local space with weight information
        /// </summary>
        public struct VertexWeights : IComparable<VertexWeights>, IEquatable<VertexWeights>
        {

            public Vector3 Position { get; set; }

            public Vector3 Normal { get; set; }

            public (int joint, float weight)[] Skinning { get; set; }

            public VertexWeights(Vector3 position, Vector3 normal, (int joint, float weight)[] skinning)
            {
                Position = position;
                Normal = normal;
                Skinning = skinning;

                if(skinning.Length > 1)
                    Array.Sort(Skinning);
            }

            public override bool Equals(object obj)
            {
                return obj is VertexWeights other
                    && Position == other.Position
                    && Normal == other.Normal
                    && Enumerable.SequenceEqual(Skinning, other.Skinning);
            }

            public override int GetHashCode()
                => HashCode.Combine(Position, Normal, Skinning);

            public int CompareTo(VertexWeights other)
            {
                if(other.Skinning.Length != Skinning.Length)
                    return Skinning.Length - other.Skinning.Length;

                for(int i = 0; i < Skinning.Length; i++)
                {
                    int result = Skinning[i].joint - other.Skinning[i].joint;
                    if(result != 0)
                        return result;
                }

                return 0;
            }

            public static bool operator ==(VertexWeights left, VertexWeights right)
                => left.Equals(right);

            public static bool operator !=(VertexWeights left, VertexWeights right)
                => !(left == right);

            public override string ToString()
            {
                string result = Skinning.Length.ToString();

                if(Skinning.Length > 0)
                {
                    result += " - ";
                    foreach((int index, _) in Skinning)
                        result += index + ", ";
                }

                return result;
            }

            bool IEquatable<VertexWeights>.Equals(VertexWeights other)
                => Equals(other);
        }

        /// <summary>
        /// Takes (simplified) mesh data and convertes it to working mesh data. <br/>
        /// Attaches it to the "bones" afterwards. <br/>
        /// WARNING: May take some time, as it optimizes all data!
        /// </summary>
        /// <param name="nodes">All bones sorted by index. [0] has to be the root! bones need to be in hierarchy order</param>
        /// <param name="meshIndex">In most formats, the mesh gets its own object, but it can also be the same as the root index</param>
        /// <param name="rootIndex">Skeleton root node index</param>
        /// <param name="vertices">Vertex data</param>
        /// <param name="displayMeshes">Polygon info</param>
        /// <param name="format">Attach format to convert to</param>
        public static void FromWeightedBuffer(NJObject[] nodes, Matrix4x4 meshMatrix, VertexWeights[] vertices, BufferMesh[] displayMeshes)
        {
            // lets optimize our data size a bit;
            // First, we remove any duplicates
            VertexWeights[] distinctVerts = vertices.GetDistinct();

            // then we sort the vertices; the comparer interface compares
            // them based on their weight definitions, as this is important
            Array.Sort(distinctVerts);

            // gotta get the index map to translates the indices on polygons
            int[] vertexIndexMap = CreateIndexMap(vertices, distinctVerts);

            // now we optimize the display meshes
            BufferMesh[] optimizedDisplayMeshes = new BufferMesh[displayMeshes.Length + 1];
            for(int i = 0; i < displayMeshes.Length; i++)
                optimizedDisplayMeshes[i + 1] = OptimizePolygons(displayMeshes[i], vertexIndexMap);

            // last thing to do: create the buffer meshes
            // first we gotta get the matrices
            (Matrix4x4 pos, Matrix4x4 nrm, List<BufferVertex> verts)[] matrices = new (Matrix4x4 pos, Matrix4x4 nrm, List<BufferVertex> verts)[nodes.Length];

            Dictionary<NJObject, Matrix4x4> worldMatrices = new();
            for(int i = 0; i < nodes.Length; i++)
            {
                Matrix4x4 nodeMatrix = nodes[i].GetWorldMatrix(worldMatrices);
                Matrix4x4.Invert(nodeMatrix, out Matrix4x4 NodeMatrixI);
                Matrix4x4 posMatrix = NodeMatrixI * meshMatrix;

                Matrix4x4.Invert(posMatrix, out Matrix4x4 posMatrixI);
                Matrix4x4 nrm = Matrix4x4.Transpose(posMatrixI);

                matrices[i] = (posMatrix, nrm, new());
            }

            // Now we create the corrected vertex data for each bone
            ushort vIndex = 0;
            foreach(var v in distinctVerts)
            {
                if(v.Skinning.Length == 0)
                {
                    (var posmtx, var nrmmtx, var list) = matrices[0];
                    var pos = Vector3.Transform(v.Position, posmtx);
                    var nrm = Vector3.Transform(v.Normal, nrmmtx);

                    list.Add(new BufferVertex(pos, nrm, vIndex));
                }
                else
                {
                    foreach((int index, float weight) in v.Skinning)
                    {
                        (var posmtx, var nrmmtx, var list) = matrices[index];
                        var pos = Vector3.Transform(v.Position, posmtx);
                        var nrm = Vector3.Transform(v.Normal, nrmmtx);

                        list.Add(new BufferVertex(pos, nrm, vIndex, weight));
                    }
                }
                vIndex++;
            }

            // lastly we create the attaches (and correct
            // the bone info a bit for optimal meshes)
            bool initializerDone = false;
            for(int i = 0; i < nodes.Length; i++)
            {
                if(matrices[i].verts.Count == 0)
                    continue;

                var verts = matrices[i].verts;
                BufferVertex[] vertsArray = null;

                bool isInitializer = initializerDone;
                if(!initializerDone)
                {
                    List<BufferVertex> clearVerts = new();
                    int j = 0;
                    foreach(var vert in distinctVerts)
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
                if(offset > 0)
                    for(int j = 0; j < vertsArray.Length; j++)
                        vertsArray[j].Index -= offset;

                BufferMesh mesh = new(vertsArray, isInitializer, offset);

                BufferMesh[] meshes;
                if(i == nodes.Length - 1)
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
        public static BufferMesh OptimizePolygons(BufferMesh original, int[] vertexIndexMap)
        {
            // First we optimize the corners
            BufferCorner[] corners;
            ushort newReadOffset = original.VertexReadOffset;

            // we start by replacing the vertex indices if an index map was passed
            if(vertexIndexMap != null)
            {
                newReadOffset = (ushort)original.Corners.Min(x => vertexIndexMap[x.VertexIndex]);
                corners = new BufferCorner[original.Corners.Length];
                for(int i = 0; i < original.Corners.Length; i++)
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
            if(cornerIndexMap != null)
            {
                if(original.TriangleList == null)
                {
                    triangleVertices = new uint[original.Corners.Length];
                    for(int i = 0; i < triangleVertices.Length; i++)
                        triangleVertices[i] = (uint)cornerIndexMap[i];
                }
                else
                {
                    triangleVertices = new uint[original.TriangleList.Length];
                    for(int i = 0; i < triangleVertices.Length; i++)
                        triangleVertices[i] = (uint)cornerIndexMap[original.TriangleList[i]];
                }
            }
            else
                triangleVertices = original.TriangleList;

            return new(distinctCorners, triangleVertices, original.Material.Clone(), newReadOffset);
        }

        private static Matrix4x4 GetWorldMatrix(this NJObject njobject, Dictionary<NJObject, Matrix4x4> worldMatrices)
        {
            if(worldMatrices.TryGetValue(njobject, out Matrix4x4 local))
                return local;

            local = njobject.LocalMatrix;

            if(njobject.Parent != null)
                local *= njobject.Parent.GetWorldMatrix(worldMatrices);

            worldMatrices.Add(njobject, local);

            return local;
        }

        #region Weightless stuff

        /// <summary>
        /// A single cached vertex
        /// </summary>
        private struct CachedVertex : IEquatable<CachedVertex>
        {
            public Vector4 position;
            public Vector3 normal;

            public Vector3 V3Position => new(position.X, position.Y, position.Z);

            public CachedVertex(Vector4 position, Vector3 normal)
            {
                this.position = position;
                this.normal = normal;
            }
            public bool Equals(CachedVertex other)
            {
                return V3Position == other.V3Position
                    && normal == other.normal;
            }

            public override string ToString()
            {
                return $"({position.X:f3}, {position.Y:f3}, {position.Z:f3}, {position.W:f3}) - ({normal.X:f3}, {normal.Y:f3}, {normal.Z:f3})";
            }

            public override bool Equals(object obj)
            {
                return obj is CachedVertex vertex && Equals(vertex);
            }

            public override int GetHashCode() => HashCode.Combine(position, normal, V3Position);
        }

        /// <summary>
        /// Vertex Cache size
        /// </summary>
        private const int VertexCacheSize = 0xFFFF;

        /// <summary>
        /// Vertex cache
        /// </summary>
        private static readonly CachedVertex[] _vertexCache
            = new CachedVertex[VertexCacheSize];

        private static readonly ushort[] _vertexCacheMap
            = new ushort[VertexCacheSize];

        /// <summary>
        /// Used for converting weightless attach information
        /// </summary>
        public struct WeightlessBufferAttach
        {
            readonly public BufferVertex[] vertices;
            readonly public BufferCorner[][] corners;
            readonly public BufferMaterial[] materials;

            public WeightlessBufferAttach(BufferVertex[] vertices, BufferCorner[][] corners, BufferMaterial[] materials)
            {
                this.vertices = vertices;
                this.corners = corners;
                this.materials = materials;
            }
        }

        /// <summary>
        /// Processes weightless buffer attaches to assist in conversion (may also be used on weighted models, but wont give fancy results)
        /// </summary>
        public static void ProcessWeightlessModel(NJObject model, Func<WeightlessBufferAttach, Attach, Attach> loopaction)
        {
            // checking if all the meshes have buffer information
            NJObject[] models = model.GetObjects();
            foreach(NJObject obj in models)
            {
                if(obj.Attach == null)
                    continue;

                if(obj.Attach.MeshData == null)
                    throw new FormatException("Not all attaches have meshdata! Please generate Meshdata before converting");
            }

            // clearing the cache
            Array.Clear(_vertexCache, 0, VertexCacheSize);

            // The world matrix for each object
            Dictionary<NJObject, Matrix4x4> worldMatrices = new();

            // The new attach for each NJ object
            Dictionary<NJObject, Attach> newAttaches = new();

            // Used for checking if a mesh was already converted; <Old, New>
            Dictionary<Attach, Attach> attachMap = new();

            foreach(NJObject obj in models)
            {
                // get the world matrix
                Matrix4x4 worldMatrix = obj.LocalMatrix;
                if(obj.Parent != null)
                    worldMatrix *= worldMatrices[obj.Parent];
                worldMatrices.Add(obj, worldMatrix);

                // nothing to convert
                if(obj.Attach == null)
                    continue;

                // whether the attach was already converted
                // we are still gonna move the vertex information to the cache,
                // just to be sure that all data is correct
                bool alreadyConverted = attachMap.TryGetValue(obj.Attach, out Attach newAtc);

                // getting the other matrices
                Matrix4x4.Invert(worldMatrix, out Matrix4x4 invertedWorldMatrix);
                Matrix4x4 normalMtx = Matrix4x4.Transpose(invertedWorldMatrix);

                BufferMesh[] bufferMeshes = obj.Attach.MeshData;

                // preparing output collections
                List<BufferVertex> vertices = new();
                BufferCorner[][] corners = new BufferCorner[bufferMeshes.Length][];
                BufferMaterial[] materials = new BufferMaterial[bufferMeshes.Length];

                HashSet<ushort> containingIndices = new();

                for(int i = 0; i < bufferMeshes.Length; i++)
                {
                    BufferMesh bufferMesh = bufferMeshes[i];
                    materials[i] = bufferMesh.Material;

                    if(bufferMesh.Vertices != null)
                        CacheVertices(bufferMesh.Vertices, bufferMesh.VertexWriteOffset, bufferMesh.ContinueWeight, worldMatrix, normalMtx, containingIndices);

                    List<BufferCorner> meshCorners = new();

                    foreach(BufferCorner bc in bufferMesh.Corners)
                    {
                        ushort vertexIndex = (ushort)(bc.VertexIndex + bufferMesh.VertexReadOffset);

                        if(!containingIndices.Contains(vertexIndex))
                        {
                            CachedVertex vtx = _vertexCache[vertexIndex];

                            BufferVertex bufferVertex = new(
                                Vector3.Transform(vtx.V3Position, invertedWorldMatrix),
                                Vector3.TransformNormal(vtx.normal, invertedWorldMatrix),
                                (ushort)vertices.Count
                                );

                            _vertexCacheMap[vertexIndex] = bufferVertex.Index;
                            vertices.Add(bufferVertex);
                            containingIndices.Add(vertexIndex);
                        }

                        meshCorners.Add(new BufferCorner(
                            _vertexCacheMap[vertexIndex],
                            bc.Color,
                            bc.Texcoord
                            ));
                    }

                    if(bufferMesh.Corners != null)
                    {
                        corners[i] = bufferMesh.TriangleList != null
                            ? bufferMesh.TriangleList.Select(x => meshCorners[(int)x]).ToArray()
                            : meshCorners.ToArray();
                    }
                }


                if(!alreadyConverted)
                {
                    newAtc = loopaction.Invoke(new WeightlessBufferAttach(vertices.ToArray(), corners.ToArray(), materials), obj.Attach);
                    newAtc.MeshData = obj.Attach.MeshData;
                    attachMap.Add(obj.Attach, newAtc);
                }

                newAttaches.Add(obj, newAtc);
            }

            foreach(var p in newAttaches)
                p.Key._attach = p.Value;
        }

        private static void CacheVertices(BufferVertex[] vertices, ushort vertexIndexOffset, bool continueWeight, Matrix4x4 worldMatrix, Matrix4x4 normalWorldMatrix, HashSet<ushort> replaced)
        {
            foreach(BufferVertex vtx in vertices)
            {
                Vector4 pos = Vector4.Transform(vtx.Position, worldMatrix) * vtx.Weight;
                Vector3 nrm = Vector3.TransformNormal(vtx.Normal, normalWorldMatrix) * vtx.Weight;

                int index = vtx.Index + vertexIndexOffset;

                if(continueWeight)
                {
                    _vertexCache[index].position += pos;
                    _vertexCache[index].normal += nrm;
                }
                else
                {
                    _vertexCache[index] = new(pos, nrm);
                }

                replaced.Remove((ushort)index);
            }
        }

        #endregion
    }
}
