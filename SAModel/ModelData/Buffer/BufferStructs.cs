using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.Buffer
{
    /// <summary>
    /// A point in space with normal direction and weight
    /// </summary>
    public struct BufferVertex
    {
        /// <summary>
        /// Position of the vertex
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Normal of the vertex
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// Index of the vertex for the buffer array
        /// </summary>
        public ushort index;

        /// <summary>
        /// Weight of the vertex
        /// </summary>
        public float weight;

        /// <summary>
        /// Creates a new buffer vertex
        /// </summary>
        /// <param name="position">Local position</param>
        /// <param name="normal">Local normal</param>
        /// <param name="index">Buffer index</param>
        public BufferVertex(Vector3 position, Vector3 normal, ushort index)
        {
            this.position = position;
            this.normal = normal;
            this.index = index;
            weight = 1;
        }

        /// <summary>
        /// Creates a new buffer vertex
        /// </summary>
        /// <param name="position">Local position</param>
        /// <param name="normal">Local normal</param>
        /// <param name="index">Buffer index</param>
        /// <param name="weight">Weight of the vertex</param>
        public BufferVertex(Vector3 position, Vector3 normal, ushort index, float weight)
        {
            this.position = position;
            this.normal = normal;
            this.index = index;
            this.weight = weight;
        }

        /// <summary>
        /// Returns true if the position and normal are equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool EqualPosNrm(BufferVertex other)
        {
            return position == other.position && normal == other.normal;
        }

        /// <summary>
        /// Writes the buffer vertex to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            position.Write(writer, IOType.Float);
            normal.Write(writer, IOType.Float);
            writer.WriteUInt16(index);
            writer.WriteUInt16((ushort)(weight * ushort.MaxValue));
        }

        /// <summary>
        /// Reads a buffer vertex from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the buffer vertex is located</param>
        /// <returns></returns>
        public static BufferVertex Read(byte[] source, ref uint address)
        {
            Vector3 pos = Vector3.Read(source, ref address, IOType.Float);
            Vector3 nrm = Vector3.Read(source, ref address, IOType.Float);
            ushort index = source.ToUInt16(address);
            ushort weight = source.ToUInt16(address + 2);
            address += 4;

            return new BufferVertex(pos, nrm, index, weight / (float)ushort.MaxValue);
        }

        public override string ToString()
        {
            return weight != 1 ? $"{index}: \t{position}; \t{normal}" : $"{index}: \t{position}; \t{normal}; \t{weight}";
        }

        public static BufferVertex operator +(BufferVertex l, BufferVertex r)
        {
            return new BufferVertex()
            {
                position = l.position + r.position,
                normal = l.normal + r.normal,
                index = l.index,
                weight = 1
            };
        }
        public static BufferVertex operator *(BufferVertex l, float r)
        {
            return new BufferVertex()
            {
                position = l.position * r,
                normal = l.normal * r,
                index = l.index,
                weight = l.weight
            };
        }
        public static BufferVertex operator *(float l, BufferVertex r) => r * l;

    }

    /// <summary>
    /// A single corner in a triangle
    /// </summary>
    public struct BufferCorner
    {
        /// <summary>
        /// Buffer index for the vertex
        /// </summary>
        public ushort vertexIndex;

        /// <summary>
        /// Color of the corner
        /// </summary>
        public Color color;

        /// <summary>
        /// UV of the corner
        /// </summary>
        public Vector2 uv;

        /// <summary>
        /// Creates a new buffer corner
        /// </summary>
        /// <param name="vertexIndex">Fuffer index for the vertex</param>
        /// <param name="color">Color</param>
        /// <param name="uv">Texture coordinate</param>
        public BufferCorner(ushort vertexIndex, Color color, Vector2 uv)
        {
            this.vertexIndex = vertexIndex;
            this.color = color;
            this.uv = uv;
        }

        /// <summary>
        /// Writes the buffer corner to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            writer.WriteUInt16(vertexIndex);
            color.Write(writer, IOType.ARGB8_32);
            uv.Write(writer, IOType.Float);
        }

        /// <summary>
        /// Reads a buffer corner from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the buffer corner is located</param>
        /// <returns></returns>
        public static BufferCorner Read(byte[] source, ref uint address)
        {
            ushort index = source.ToUInt16(address);
            address += 2;
            Color col = Color.Read(source, ref address, IOType.ARGB8_32);
            Vector2 uv = Vector2.Read(source, ref address, IOType.Float);

            return new BufferCorner(index, col, uv);
        }

        public override string ToString()
        {
            return $"{vertexIndex}: \t{color}; \t{uv}";
        }

        public override bool Equals(object obj)
        {
            return obj is BufferCorner corner &&
                   vertexIndex == corner.vertexIndex &&
                   color == corner.color &&
                   uv == corner.uv;
        }

        public override int GetHashCode()
        {
            var hashCode = 381559495;
            hashCode = hashCode * -1521134295 + vertexIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + color.GetHashCode();
            hashCode = hashCode * -1521134295 + uv.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(BufferCorner l, BufferCorner r) => l.Equals(r);
        public static bool operator !=(BufferCorner l, BufferCorner r) => !l.Equals(r);
    }
}
