using Reloaded.Memory.Streams.Writers;
using System;
using System.IO;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// A BASIC primitive
    /// </summary>
    [Serializable]
    public abstract class Poly : ICloneable
    {
        /// <summary>
        /// Indices for position and normal data
        /// </summary>
        public ushort[] Indices { get; protected set; }

        protected Poly() { }

        /// <summary>
        /// Type of the primitive
        /// </summary>
        public abstract BASICPolyType Type { get; }

        /// <summary>
        /// Size of the primitive
        /// </summary>
        public virtual uint Size => (uint)Indices.Length * 2;

        /// <summary>
        /// Write the contents to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public virtual void Write(EndianMemoryStream writer)
        {
            foreach(ushort i in Indices)
                writer.WriteUInt16(i);
        }

        /// <summary>
        /// Writes the indices as an NJA struct
        /// </summary>
        /// <param name="writer">The output stream</param>
        public virtual void WriteNJA(TextWriter writer)
        {
            foreach(ushort i in Indices)
            {
                writer.Write(i);
                writer.Write(", ");
            }
        }

        /// <summary>
        /// Reads a primitive from a file
        /// </summary>
        /// <param name="type">Primitive type to read</param>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the primitive is located</param>
        /// <returns></returns>
        public static Poly Read(BASICPolyType type, byte[] source, ref uint address)
        {
            switch(type)
            {
                case BASICPolyType.Triangles:
                    return Triangle.Read(source, ref address);
                case BASICPolyType.Quads:
                    return Quad.Read(source, ref address);
                case BASICPolyType.NPoly:
                case BASICPolyType.Strips:
                    return Strip.Read(source, ref address);
                default:
                    throw new ArgumentException("Unknown poly type!", "type");
            }
        }

        object ICloneable.Clone() => Clone();

        public Poly Clone()
        {
            Poly p = (Poly)MemberwiseClone();
            p.Indices = (ushort[])Indices.Clone();
            return p;
        }

        public override string ToString() => $"{Type}: {Indices.Length}";
    }

    /// <summary>
    /// A primitive with three corners
    /// </summary>
    [Serializable]
    public class Triangle : Poly
    {
        public override BASICPolyType Type => BASICPolyType.Triangles;

        /// <summary>
        /// Creates a new empty triangle
        /// </summary>
        public Triangle()
        {
            Indices = new ushort[3];
        }

        /// <summary>
        /// Reads a triangle from a file
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the triangle is located</param>
        /// <returns></returns>
        public static Triangle Read(byte[] source, ref uint address)
        {
            Triangle t = new Triangle();
            t.Indices[0] = source.ToUInt16(address);
            t.Indices[1] = source.ToUInt16(address + 2);
            t.Indices[2] = source.ToUInt16(address + 4);
            address += 6;
            return t;
        }
    }

    /// <summary>
    /// A primitive with four corners
    /// </summary>
    [Serializable]
    public class Quad : Poly
    {
        public override BASICPolyType Type => BASICPolyType.Quads;

        /// <summary>
        /// Creates a new empty quad
        /// </summary>
        public Quad()
        {
            Indices = new ushort[4];
        }

        /// <summary>
        /// Reads a quad from a file
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the quad is located</param>
        /// <returns></returns>
        public static Quad Read(byte[] source, ref uint address)
        {
            Quad t = new Quad();
            t.Indices[0] = source.ToUInt16(address);
            t.Indices[1] = source.ToUInt16(address + 2);
            t.Indices[2] = source.ToUInt16(address + 4);
            t.Indices[3] = source.ToUInt16(address + 6);
            address += 8;
            return t;
        }
    }

    /// <summary>
    /// A triangle strip primitive
    /// </summary>
    [Serializable]
    public class Strip : Poly
    {
        /// <summary>
        /// Culling start direction
        /// </summary>
        public bool Reversed { get; private set; }

        public override BASICPolyType Type => BASICPolyType.Strips;

        public override uint Size => base.Size + 2;

        /// <summary>
        /// Creates an empty strip with fixed length
        /// </summary>
        /// <param name="indexCount">Length of the indices</param>
        /// <param name="reversed">Culling start direction</param>
        public Strip(int indexCount, bool reversed)
        {
            Indices = new ushort[indexCount];
            Reversed = reversed;
        }

        /// <summary>
        /// Creates a new strip from existing indices
        /// </summary>
        /// <param name="indices">Indices</param>
        /// <param name="reversed">Culling start direction</param>
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
            for(int i = 0; i < indices.Length; i++)
            {
                indices[i] = source.ToUInt16(address);
                address += 2;
            }
            return new Strip(indices, reversed);
        }

        public override void Write(EndianMemoryStream writer)
        {
            writer.WriteUInt16((ushort)((Indices.Length & 0x7FFF) | (Reversed ? 0x8000 : 0)));
            base.Write(writer);
        }

        public override void WriteNJA(TextWriter writer)
        {
            writer.Write("Strip(");
            writer.Write(Reversed ? "NJD_TRIMESH_END , " : "0, ");
            writer.Write(Indices.Length & 0x7FFF);
            writer.Write("), ");
            base.WriteNJA(writer);
        }

        public override string ToString() => $"{Type}: {Reversed} - {Indices.Length}";
    }
}
