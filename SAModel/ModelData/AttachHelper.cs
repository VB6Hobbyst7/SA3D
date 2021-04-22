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

        public static Attach FromBufferMesh(BufferMesh[] buffer, AttachFormat format)
        {
            return format switch
            {
                AttachFormat.BASIC => new BASIC.BasicAttach(buffer),
                AttachFormat.CHUNK => new CHUNK.ChunkAttach(buffer),
                AttachFormat.GC => new GC.GCAttach(buffer),
                _ => new Attach(buffer),
            };
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
        public static void FromWeightedBuffer(NJObject[] nodes, Matrix4x4 meshMatrix, VertexWeights[] vertices, BufferMesh[] displayMeshes, AttachFormat format)
        {
            // Check if the format that we wanna convert to even supports weights
            if(format == AttachFormat.BASIC || format == AttachFormat.GC)
                throw new ArgumentException($"Format {format} does not support weighted data!");

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

                nodes[i].Attach = FromBufferMesh(meshes, format);
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
    }
}
