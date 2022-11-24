using SATools.SACommon;
using System;
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
        public BufferCorner[] Corners { get; private set; }

        /// <summary>
        /// Index list for all triangles, which refer to the <see cref="Corners"/> <br/>
        /// If null, use the corners in order
        /// </summary>
        public uint[] TriangleList { get; private set; }

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
            Vertices = vertices == null || vertices.Length == 0 ? throw new ArgumentNullException(nameof(vertices), "Vertices can't be null") : vertices;
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
            Corners = corners == null || corners.Length == 0 ? throw new ArgumentNullException(nameof(corners), "Corners can't be null or empty") : corners;
            TriangleList = triangleList != null && triangleList.Length == 0 ? throw new ArgumentNullException(nameof(triangleList), "Triangle list cant be empty") : triangleList;
            Material = material ?? throw new ArgumentNullException(nameof(material), "Material can't be null");
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
            Corners = corners == null || corners.Length == 0 ? throw new ArgumentNullException(nameof(corners), "Corners can't be null or empty") : corners;
            TriangleList = triangleList != null && triangleList.Length == 0 ? throw new ArgumentNullException(nameof(triangleList), "Triangle list cant be empty") : triangleList;
            Material = material ?? throw new ArgumentNullException(nameof(material), "Material can't be null");
            VertexReadOffset = vertexReadOffset;
        }

        /// <summary>
        /// Optimizes the triangles and removes degenerate triangles <br/>
        /// Keeps the original vertex data
        /// </summary>
        public void Optimize()
        {
            if (Corners == null)
                return;

            BufferCorner[] corners = Corners;
            if (TriangleList != null)
            {
                corners = new BufferCorner[TriangleList.Length];
                for (int i = 0; i < TriangleList.Length; i++)
                    corners[i] = Corners[TriangleList[i]];
            }

            // filter degenerate triangles
            int newArraySize = corners.Length;
            for (int i = 0; i < newArraySize; i += 3)
            {
                ushort index1 = corners[i].VertexIndex;
                ushort index2 = corners[i + 1].VertexIndex;
                ushort index3 = corners[i + 2].VertexIndex;

                if (index1 == index2 || index2 == index3 || index3 == index1)
                {
                    corners[i] = corners[newArraySize - 3];
                    corners[i + 1] = corners[newArraySize - 2];
                    corners[i + 2] = corners[newArraySize - 1];
                    i -= 3;
                    newArraySize -= 3;
                }
            }
            Array.Resize(ref corners, newArraySize);

            // get the triangle mapping
            (BufferCorner[] distinct, int[] map) = corners.CreateDistinctMap();

            Corners = distinct;
            TriangleList = (uint[])(object)map; // i cant believe this works lol
        }

        /// <summary>
        /// Writes the buffer mesh to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public uint Write(EndianWriter writer, uint imageBase)
        {
            uint vtxAddr = 0;
            if (Vertices != null)
            {
                vtxAddr = writer.Position + imageBase;
                foreach (BufferVertex vtx in Vertices)
                    vtx.Write(writer);
            }

            uint cornerAddr = 0;
            if (Corners != null)
            {
                cornerAddr = writer.Position + imageBase;
                foreach (BufferCorner c in Corners)
                    c.Write(writer);
            }

            uint triangleAddr = 0;
            if (TriangleList != null)
            {
                triangleAddr = writer.Position + imageBase;
                foreach (uint t in TriangleList)
                    writer.WriteUInt32(t);
            }

            uint address = writer.Position + imageBase;
            writer.WriteUInt16((ushort)(Vertices == null ? 0 : Vertices.Length));
            writer.WriteUInt16((ushort)(ContinueWeight ? 1u : 0u));
            writer.WriteUInt32((uint)(Corners == null ? 0 : Corners.Length));
            writer.WriteUInt32((uint)(TriangleList == null ? 0 : TriangleList.Length));
            if (Material == null)
                writer.Write(new byte[32]);
            else
                Material.Write(writer);
            writer.WriteUInt32(vtxAddr);
            writer.WriteUInt32(cornerAddr);
            writer.WriteUInt32(triangleAddr);

            return address;
        }

        /// <summary>
        /// Reads a buffer mesh from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the buffermesh is located</param>
        public static BufferMesh Read(byte[] source, uint address, uint imageBase)
        {
            BufferVertex[] vertices = new BufferVertex[source.ToUInt16(address)];
            bool continueWeight = source.ToUInt16(address + 2) != 0;
            BufferCorner[] corners = new BufferCorner[source.ToUInt32(address + 4)];
            uint[] triangles = new uint[source.ToUInt32(address + 8)];

            address += 12;
            BufferMaterial material = BufferMaterial.Read(source, ref address);

            uint tmpAddr = source.ToUInt32(address) - imageBase;

            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = BufferVertex.Read(source, ref tmpAddr);

            tmpAddr = source.ToUInt32(address += 4) - imageBase;

            for (int i = 0; i < corners.Length; i++)
                corners[i] = BufferCorner.Read(source, ref tmpAddr);

            tmpAddr = source.ToUInt32(address += 4) - imageBase;

            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = source.ToUInt32(tmpAddr);
                tmpAddr += 4;
            }

            if (vertices.Length == 0)
                return new BufferMesh(corners, triangles, material);
            else if (corners.Length == 0)
                return new BufferMesh(vertices, continueWeight);
            else
                return new BufferMesh(vertices, continueWeight, corners, triangles, material);
        }

        object ICloneable.Clone() => Clone();

        public BufferMesh Clone() => new((BufferVertex[])Vertices.Clone(), ContinueWeight, (BufferCorner[])Corners.Clone(), (uint[])TriangleList.Clone(), Material.Clone());

    }
}
