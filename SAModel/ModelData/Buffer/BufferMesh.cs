using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Numerics;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.Buffer
{
    /// <summary>
    /// Data set for a renderable mesh <br/>
    /// Can also contain only vertices
    /// </summary>
    [Serializable]
    public class BufferMesh : ICloneable
    {
        /// <summary>
        /// Materialdata <br/>
        /// Null if no surface data exists
        /// </summary>
        public BufferMaterial Material { get; }

        /// <summary>
        /// Vertex storage <br/>
        /// Can't be null
        /// </summary>
        public BufferVertex[] Vertices { get; }

        /// <summary>
        /// Polygon corners <br/>
        /// Null if no corners exist
        /// </summary>
        public BufferCorner[] Corners { get; }

        /// <summary>
        /// Index list for all triangles, which refer to the <see cref="Corners"/> <br/>
        /// Null if no triangles exist
        /// </summary>
        public uint[] TriangleList { get; }

        /// <summary>
        /// If true, the vertices will be added onto the existing buffered vertices
        /// </summary>
        public bool ContinueWeight { get; }

        /// <summary>
        /// Vertex offset for when writing vertices into the buffer
        /// </summary>
        public ushort VertexWriteOffset { get; set; }

        /// <summary>
        /// Vertex offset for the polycorners' vertex indices
        /// </summary>
        public ushort VertexReadOffset { get; set; }

        /// <summary>
        /// Creates a new Buffermesh from only vertex data (used for deforming models)
        /// </summary>
        /// <param name="vertices">Vertex data</param>
        /// <param name="continueWeight">Weight state</param>
        public BufferMesh(BufferVertex[] vertices, bool continueWeight, ushort vertexWriteOffset = 0)
        {
            Vertices = vertices ?? throw new ArgumentNullException("Vertices", "Vertices can't be null");
            ContinueWeight = continueWeight;
            VertexWriteOffset = vertexWriteOffset;
        }

        /// <summary>
        /// Creates a new Buffermesh with all data necessary
        /// </summary>
        /// <param name="vertices">Vertex data</param>
        /// <param name="continueWeight">Weight state</param>
        /// <param name="corners">Triangle corner data</param>
        /// <param name="triangleList">Triangle index list</param>
        /// <param name="material">Material</param>
        public BufferMesh(BufferVertex[] vertices, bool continueWeight, BufferCorner[] corners, uint[] triangleList, BufferMaterial material, ushort vertexWriteOffset = 0, ushort vertexReadOffset = 0)
            : this(vertices, continueWeight, vertexWriteOffset)
        {
            Corners = corners ?? throw new ArgumentNullException("Corners", "Corners can't be null");
            TriangleList = triangleList ?? throw new ArgumentNullException("TriangleList", "Triangle list can't be null");
            Material = material ?? throw new ArgumentNullException("Material", "Material can't be null");
            VertexReadOffset = vertexReadOffset;
        }

        /// <summary>
        /// Creates a new Buffermesh with only polygon data
        /// </summary>
        /// <param name="corners">Triangle corner data</param>
        /// <param name="triangleList">Triangle index list</param>
        /// <param name="material">Material</param>
        public BufferMesh(BufferCorner[] corners, uint[] triangleList, BufferMaterial material, ushort vertexReadOffset = 0)
        {
            Corners = corners ?? throw new ArgumentNullException("Corners", "Corners can't be null");
            TriangleList = triangleList;
            Material = material ?? throw new ArgumentNullException("Material", "Material can't be null");
            VertexReadOffset = vertexReadOffset;
        }

        /// <summary>
        /// Optimizes the triangles and removes degenerate triangles <br/>
        /// Since vertices might not be part of the mesh, manual reference is required<br/>
        /// </summary>
        /// <param name="vertices">Vertices used by the mesh</param>
        /// <param name="ownsVertices">Whether the vertices passed belong to this mesh or are located in another mesh. <br/>
        /// If set to false, the mesh wont hold any vertex data.</param>
        /// <returns></returns>
        public BufferMesh Optimize(BufferVertex[] vertices, bool ownsVertices)
        {
            return Optimize(vertices, ownsVertices, true);
        }

        /// <summary>
        /// Optimizes the triangles and removes degenerate triangles <br/>
        /// Keeps the original vertex data
        /// </summary>
        /// <returns></returns>
        public BufferMesh Optimize()
        {
            return Optimize(null, false, false);
        }

        private BufferMesh Optimize(BufferVertex[] vertices, bool ownsVertices, bool useVertices)
        {
            // filter degenerate triangles
            List<uint> triangles = new();
            if(!useVertices)
            {
                for(int i = 0; i < TriangleList.Length; i += 3)
                {
                    ushort index1 = Corners[(int)TriangleList[i]].VertexIndex;
                    ushort index2 = Corners[(int)TriangleList[i + 1]].VertexIndex;
                    ushort index3 = Corners[(int)TriangleList[i + 2]].VertexIndex;

                    if(index1 != index2 && index2 != index3 && index3 != index1)
                        triangles.AddRange(new uint[] { TriangleList[i], TriangleList[i + 1], TriangleList[i + 2] });
                }
            }
            else
            {
                for(int i = 0; i < TriangleList.Length; i += 3)
                {
                    Vector3 pos1 = vertices[Corners[(int)TriangleList[i]].VertexIndex].Position;
                    Vector3 pos2 = vertices[Corners[(int)TriangleList[i + 1]].VertexIndex].Position;
                    Vector3 pos3 = vertices[Corners[(int)TriangleList[i + 2]].VertexIndex].Position;
                    if(pos1 != pos2 && pos2 != pos3 && pos3 != pos1)
                        triangles.AddRange(new uint[] { TriangleList[i], TriangleList[i + 1], TriangleList[i + 2] });
                }
            }


            bool[] removedCorners = new bool[Corners.Length];
            triangles.ForEach(x => removedCorners[(int)x] = true);

            // filtering the double Corners
            List<BufferCorner> distCorners = new();
            uint[] cIDs = new uint[Corners.Length];
            for(int i = 0; i < Corners.Length; i++)
            {
                if(!removedCorners[i])
                    continue;
                int index = distCorners.FindIndex(x => x == Corners[i]);
                if(index < 0)
                {
                    cIDs[i] = (uint)distCorners.Count;
                    distCorners.Add(Corners[i]);
                }
                else
                    cIDs[i] = (uint)index;
            }

            // updating indices of the triangle list
            for(int i = 0; i < triangles.Count; i++)
                triangles[i] = cIDs[triangles[i]];

            if(useVertices)
            {
                if(ownsVertices)
                    return new BufferMesh(vertices, false, distCorners.ToArray(), triangles.ToArray(), Material);
                else
                    return new BufferMesh(distCorners.ToArray(), triangles.ToArray(), Material);
            }
            else
            {
                if(Vertices == null)
                    return new BufferMesh(distCorners.ToArray(), triangles.ToArray(), Material);
                else
                    return new BufferMesh(Vertices, ContinueWeight, distCorners.ToArray(), triangles.ToArray(), Material);
            }
        }

        /// <summary>
        /// Writes the buffer mesh to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            writer.WriteUInt16((ushort)(Vertices == null ? 0 : Vertices.Length));
            writer.WriteUInt16((ushort)(ContinueWeight ? 1u : 0u));
            writer.WriteUInt32((uint)(Corners == null ? 0 : Corners.Length));
            writer.WriteUInt32((uint)(TriangleList == null ? 0 : TriangleList.Length));
            if(Material == null)
                writer.Write(new byte[28]);
            else
                Material.Write(writer);

            if(Vertices != null)
                foreach(BufferVertex vtx in Vertices)
                    vtx.Write(writer);

            if(Corners != null)
                foreach(BufferCorner c in Corners)
                    c.Write(writer);

            if(TriangleList != null)
                foreach(uint t in TriangleList)
                    writer.WriteUInt32(t);
        }

        /// <summary>
        /// Reads a buffer mesh from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the buffermesh is located</param>
        public static BufferMesh Read(byte[] source, ref uint address)
        {
            BufferVertex[] vertices = new BufferVertex[source.ToUInt16(address)];
            bool continueWeight = source.ToUInt16(address + 2) != 0;
            BufferCorner[] corners = new BufferCorner[source.ToUInt32(address + 4)];
            uint[] triangles = new uint[source.ToUInt32(address + 8)];

            address += 12;
            BufferMaterial material = BufferMaterial.Read(source, ref address);

            for(int i = 0; i < vertices.Length; i++)
                vertices[i] = BufferVertex.Read(source, ref address);

            for(int i = 0; i < corners.Length; i++)
                corners[i] = BufferCorner.Read(source, ref address);

            for(int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = source.ToUInt32(address);
                address += 4;
            }

            if(vertices.Length == 0)
                return new BufferMesh(corners, triangles, material);
            else if(corners.Length == 0)
                return new BufferMesh(vertices, continueWeight);
            else
                return new BufferMesh(vertices, continueWeight, corners, triangles, material);
        }

        object ICloneable.Clone() => Clone();

        public BufferMesh Clone() => new((BufferVertex[])Vertices.Clone(), ContinueWeight, (BufferCorner[])Corners.Clone(), (uint[])TriangleList.Clone(), Material.Clone());

    }
}
