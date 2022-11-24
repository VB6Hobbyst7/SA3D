using SATools.SACommon;
using System;
using System.IO;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// A BASIC primitive
    /// </summary>
    public interface IPoly : ICloneable
    {
        /// <summary>
        /// Type of the primitive
        /// </summary>
        public BASICPolyType Type { get; }

        /// <summary>
        /// Indices for position and normal data
        /// </summary>
        public ushort[] Indices { get; }

        /// <summary>
        /// Size of the primitive
        /// </summary>
        public uint Size { get; }

        public static IPoly Read(BASICPolyType type, byte[] source, ref uint address)
        {
            return type switch
            {
                BASICPolyType.Triangles => Triangle.Read(source, ref address),
                BASICPolyType.Quads => Quad.Read(source, ref address),
                BASICPolyType.NPoly or BASICPolyType.Strips => Strip.Read(source, ref address),
                _ => throw new ArgumentException("Unknown poly type!", nameof(type)),
            };
        }

        /// <summary>
        /// Write the contents to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianWriter writer);

        /// <summary>
        /// Writes the indices as an NJA struct
        /// </summary>
        /// <param name="writer">The output stream</param>
        public void WriteNJA(TextWriter writer);

    }

    internal static class IPolyExtensions
    {
        internal static uint DefaultSize(this IPoly poly)
            => (uint)poly.Indices.Length * 2;

        internal static void DefaultWrite(this IPoly poly, EndianWriter writer)
        {
            foreach (ushort i in poly.Indices)
                writer.WriteUInt16(i);
        }

        internal static void DefaultWriteNJA(this IPoly poly, TextWriter writer)
        {
            foreach (ushort i in poly.Indices)
            {
                writer.Write(i);
                writer.Write(", ");
            }
        }

        internal static string DefaultToString(this IPoly poly)
            => $"{poly.Type}: {poly.Indices.Length}";
    }

    /// <summary>
    /// A primitive with three corners
    /// </summary>
    public struct Triangle : IPoly
    {
        private ushort[] _indices;

        public ushort[] Indices
        {
            get
            {
                if (_indices == null)
                    _indices = new ushort[3];
                return _indices;
            }
        }

        public BASICPolyType Type
            => BASICPolyType.Triangles;

        public uint Size
            => this.DefaultSize();

        public void Write(EndianWriter writer)
            => this.DefaultWrite(writer);

        public void WriteNJA(TextWriter writer)
            => this.DefaultWriteNJA(writer);

        /// <summary>
        /// Reads a triangle from a file
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the triangle is located</param>
        /// <returns></returns>
        public static Triangle Read(byte[] source, ref uint address)
        {
            Triangle t = new()
            {
                _indices = new ushort[] {
                    source.ToUInt16(address),
                    source.ToUInt16(address + 2),
                    source.ToUInt16(address + 4)
                }
            };
            address += 6;
            return t;
        }

        public override string ToString()
            => $"Triangle: [{_indices[0]}, {_indices[1]}, {_indices[2]}]";

        public object Clone() => this;
    }

    /// <summary>
    /// A primitive with three corners
    /// </summary>
    public struct Quad : IPoly
    {
        private ushort[] _indices;

        public ushort[] Indices
        {
            get
            {
                if (_indices == null)
                    _indices = new ushort[4];
                return _indices;
            }
        }

        public BASICPolyType Type
            => BASICPolyType.Quads;

        public uint Size
            => this.DefaultSize();

        public void Write(EndianWriter writer)
            => this.DefaultWrite(writer);

        public void WriteNJA(TextWriter writer)
            => this.DefaultWriteNJA(writer);

        /// <summary>
        /// Reads a triangle from a file
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the triangle is located</param>
        /// <returns></returns>
        public static Quad Read(byte[] source, ref uint address)
        {
            Quad t = new()
            {
                _indices = new ushort[] {
                    source.ToUInt16(address),
                    source.ToUInt16(address + 2),
                    source.ToUInt16(address + 4),
                    source.ToUInt16(address + 6)
                }
            };
            address += 8;
            return t;
        }

        public override string ToString()
            => $"Quad: [{_indices[0]}, {_indices[1]}, {_indices[2]}, {_indices[3]}]";

        public object Clone() => this;
    }

    /// <summary>
    /// A triangle strip
    /// </summary>
    public struct Strip : IPoly
    {
        public ushort[] Indices { get; set; }

        public bool Reversed { get; set; }

        public BASICPolyType Type
            => BASICPolyType.Strips;

        public uint Size
            => 2 + this.DefaultSize();

        public Strip(uint size, bool reversed)
        {
            Indices = new ushort[size];
            Reversed = reversed;
        }

        public Strip(ushort[] indices, bool reversed)
        {
            Indices = indices;
            Reversed = reversed;
        }

        /// <summary>
        /// Reads a strip from a file
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the strip is located</param>
        /// <returns></returns>
        public static Strip Read(byte[] source, ref uint address)
        {
            ushort header = source.ToUInt16(address);
            ushort[] indices = new ushort[header & 0x7FFF];
            bool reversed = (header & 0x8000) != 0;
            address += 2;
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = source.ToUInt16(address);
                address += 2;
            }
            return new Strip(indices, reversed);
        }

        public void Write(EndianWriter writer)
        {
            writer.WriteUInt16((ushort)((Indices.Length & 0x7FFF) | (Reversed ? 0x8000 : 0)));
            this.DefaultWrite(writer);
        }

        public void WriteNJA(TextWriter writer)
        {
            writer.Write("Strip(");
            writer.Write(Reversed ? "NJD_TRIMESH_END , " : "0, ");
            writer.Write(Indices.Length & 0x7FFF);
            writer.Write("), ");
            this.DefaultWriteNJA(writer);
        }

        public override string ToString()
            => $"{Type}: {Reversed} - {Indices.Length}";

        public object Clone() => this;
    }

}
