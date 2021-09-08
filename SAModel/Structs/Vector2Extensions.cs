using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// Vector2 Extensions
    /// </summary>
    public static class Vector2Extensions
    {
        #region I/O

        /// <summary>
        /// Reads a vector2 from a byte source
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the vector2 object is located</param>
        /// <param name="type">How the bytes should be read</param>
        /// <returns></returns>
        public static Vector2 Read(byte[] source, ref uint address, IOType type)
        {
            Vector2 result;
            switch(type)
            {
                case IOType.Short:
                    result = new()
                    {
                        X = source.ToInt16(address),
                        Y = source.ToInt16(address + 2)
                    };
                    address += 4;
                    break;
                case IOType.Float:
                    result = new()
                    {
                        X = source.ToSingle(address),
                        Y = source.ToSingle(address + 4)
                    };
                    address += 8;
                    break;
                default:
                    throw new ArgumentException($"{type} is not available for Vector2");
            }
            return result;
        }

        /// <summary>
        /// Writes a Vector2 to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="type">Datatype to write object as</param>
        public static void Write(this Vector2 vector, EndianWriter writer, IOType type)
        {
            switch(type)
            {
                case IOType.Short:
                    writer.WriteInt16((short)vector.X);
                    writer.WriteInt16((short)vector.Y);
                    break;
                case IOType.Float:
                    writer.WriteSingle(vector.X);
                    writer.WriteSingle(vector.Y);
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for Vector2");
            }
        }

        /// <summary>
        /// Writes a vector2 to a text stream as an NJAscii struct
        /// </summary>
        /// <param name="writer">Output text stream</param>
        /// <param name="type">Output type</param>
        public static void WriteNJA(this Vector2 vector, TextWriter writer, IOType type)
        {
            writer.Write("( ");
            switch(type)
            {
                case IOType.Short:
                    writer.Write((short)vector.X);
                    writer.Write(", ");
                    writer.Write((short)vector.Y);
                    break;
                case IOType.Float:
                    writer.Write(vector.X.ToC());
                    writer.Write(", ");
                    writer.Write(vector.Y.ToC());
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for Vector2");
            }
            writer.Write(")");
        }

        #endregion

        #region Arithmetic methods

        /// <summary>
        /// Returns a Vector2 with all values rounded
        /// </summary>
        public static Vector2 Rounded(this Vector2 vector, int digits)

        {
            return new(
                (float)Math.Round(vector.X, digits),
                (float)Math.Round(vector.Y, digits));
        }

        /// <summary>
        /// Linear interpolation
        /// </summary>
        public static Vector2 Lerp(Vector2 min, Vector2 max, float t)
            => min + (max - min) * t;

        #endregion
    }
}
