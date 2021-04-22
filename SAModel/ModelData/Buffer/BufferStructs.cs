using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using System.Numerics;
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
        public Vector3 Position { get; set; }

        /// <summary>
        /// Normal of the vertex
        /// </summary>
        public Vector3 Normal { get; set; }

        /// <summary>
        /// Index of the vertex for the buffer array
        /// </summary>
        public ushort Index { get; set; }

        /// <summary>
        /// Weight of the vertex
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// Creates a new buffer vertex
        /// </summary>
        /// <param name="position">Local position</param>
        /// <param name="normal">Local normal</param>
        /// <param name="index">Buffer index</param>
        public BufferVertex(Vector3 position, Vector3 normal, ushort index)
        {
            this.Position = position;
            this.Normal = normal;
            this.Index = index;
            Weight = 1;
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
            this.Position = position;
            this.Normal = normal;
            this.Index = index;
            this.Weight = weight;
        }

        /// <summary>
        /// Returns true if the position and normal are equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool EqualPosNrm(BufferVertex other)
        {
            return Position == other.Position && Normal == other.Normal;
        }

        /// <summary>
        /// Writes the buffer vertex to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            Position.Write(writer, IOType.Float);
            Normal.Write(writer, IOType.Float);
            writer.WriteUInt16(Index);
            writer.WriteUInt16((ushort)(Weight * ushort.MaxValue));
        }

        /// <summary>
        /// Reads a buffer vertex from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the buffer vertex is located</param>
        /// <returns></returns>
        public static BufferVertex Read(byte[] source, ref uint address)
        {
            Vector3 pos = Vector3Extensions.Read(source, ref address, IOType.Float);
            Vector3 nrm = Vector3Extensions.Read(source, ref address, IOType.Float);
            ushort index = source.ToUInt16(address);
            ushort weight = source.ToUInt16(address + 2);
            address += 4;

            return new BufferVertex(pos, nrm, index, weight / (float)ushort.MaxValue);
        }

        public override string ToString()
        {
            return Weight != 1 ? $"{Index}: \t{Position}; \t{Normal}" : $"{Index}: \t{Position}; \t{Normal}; \t{Weight}";
        }

        public static BufferVertex operator +(BufferVertex l, BufferVertex r)
        {
            return new BufferVertex()
            {
                Position = l.Position + r.Position,
                Normal = l.Normal + r.Normal,
                Index = l.Index,
                Weight = 1
            };
        }
        public static BufferVertex operator *(BufferVertex l, float r)
        {
            return new BufferVertex()
            {
                Position = l.Position * r,
                Normal = l.Normal * r,
                Index = l.Index,
                Weight = l.Weight
            };
        }
        public static BufferVertex operator *(float l, BufferVertex r) => r * l;

    }

    /// <summary>
    /// A single corner in a triangle
    /// </summary>
    public struct BufferCorner : IEquatable<BufferCorner>
    {
        /// <summary>
        /// Buffer index for the vertex
        /// </summary>
        public ushort VertexIndex { get; set; }

        /// <summary>
        /// Color of the corner
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// UV of the corner
        /// </summary>
        public Vector2 Uv { get; set; }

        /// <summary>
        /// Creates a new buffer corner
        /// </summary>
        /// <param name="vertexIndex">Fuffer index for the vertex</param>
        /// <param name="color">Color</param>
        /// <param name="uv">Texture coordinate</param>
        public BufferCorner(ushort vertexIndex, Color color, Vector2 uv)
        {
            this.VertexIndex = vertexIndex;
            this.Color = color;
            this.Uv = uv;
        }

        /// <summary>
        /// Writes the buffer corner to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            writer.WriteUInt16(VertexIndex);
            Color.Write(writer, IOType.ARGB8_32);
            Uv.Write(writer, IOType.Float);
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
            Vector2 uv = Vector2Extensions.Read(source, ref address, IOType.Float);

            return new BufferCorner(index, col, uv);
        }

        public override string ToString()
        {
            return $"{VertexIndex}: \t{Color}; \t{Uv}";
        }

        public override bool Equals(object obj)
        {
            return obj is BufferCorner corner &&
                   VertexIndex == corner.VertexIndex &&
                   Color == corner.Color &&
                   Uv == corner.Uv;
        }

        public override int GetHashCode() 
            => System.HashCode.Combine(VertexIndex, Color, Uv);
        bool IEquatable<BufferCorner>.Equals(BufferCorner other)
            => Equals(other);

        public static bool operator ==(BufferCorner l, BufferCorner r) => l.Equals(r);
        public static bool operator !=(BufferCorner l, BufferCorner r) => !l.Equals(r);
    }
}
