using System;
using System.IO;
using Reloaded.Memory.Streams.Writers;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// 2D Vector structure
    /// </summary>
    public struct Vector2 : IDataStructOut
    {
        #region Constants

        /// <summary>
        /// Equal to (1, 0)
        /// </summary>
        public static readonly Vector2 UnitX
            = new(1, 0);

        /// <summary>
        /// Equal to (0, 1)
        /// </summary>
        public static readonly Vector2 UnitY
            = new(0, 1);

        /// <summary>
        /// Equal to (0, 0)
        /// </summary>
        public static readonly Vector2 Zero
            = new(0, 0);

        #endregion

        /// <summary>
        /// X Component of the Vector
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y Component of the Vector
        /// </summary>
        public float Y { get; set; }

        public float this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    _ => throw new IndexOutOfRangeException($"Index {index} out of range"),
                };
            }
            set
            {
                switch(index)
                {
                    case 0:
                        X = value;
                        return;
                    case 1:
                        Y = value;
                        return;
                    default:
                        throw new IndexOutOfRangeException($"Index {index} out of range");
                }
            }
        }

        /// <summary>
        /// Length of the Vector
        /// </summary>
        public float Length
            => (float)Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// Returns the greatest of the 3 values in the vector
        /// </summary>
        public float GreatestValue
            => X > Y ? X : Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2(Vector3 vec3)
        {
            X = vec3.X;
            Y = vec3.Y;
        }

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

        public void Write(EndianMemoryStream writer, IOType type)
        {
            switch(type)
            {
                case IOType.Short:
                    writer.WriteInt16((short)X);
                    writer.WriteInt16((short)Y);
                    break;
                case IOType.Float:
                    writer.WriteSingle(X);
                    writer.WriteSingle(Y);
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for Vector2");
            }
        }

        public void WriteNJA(TextWriter writer, IOType type)
        {
            writer.Write("( ");
            switch(type)
            {
                case IOType.Short:
                    writer.Write((short)X);
                    writer.Write(", ");
                    writer.Write((short)Y);
                    break;
                case IOType.Float:
                    writer.Write(X.ToC());
                    writer.Write(", ");
                    writer.Write(Y.ToC());
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for Vector2");
            }
            writer.Write(")");
        }

        #endregion

        #region Arithmetic Operators/Methods

        /// <summary>
        /// Returns a Normalized version of this vector
        /// </summary>
        public Vector2 Normalized()
            => this / Length;

        /// <summary>
        /// Returns a Vector2 with all values rounded
        /// </summary>
        public Vector2 Rounded(int floatingPoint)
            => new((float)Math.Round(X, floatingPoint), (float)Math.Round(Y, floatingPoint));

        /// <summary>
        /// Calculates the distance between two points
        /// </summary>
        public static float Distance(Vector2 from, Vector2 to)
            => (float)Math.Sqrt(Math.Pow(from.X - to.X, 2) + Math.Pow(from.Y - from.Y, 2));

        /// <summary>
        /// Linear interpolation
        /// </summary>
        public static Vector2 Lerp(Vector2 min, Vector2 max, float t)
            => min + (max - min) * t;

        public static Vector2 operator -(Vector2 l)
            => new(-l.X, -l.Y);

        public static Vector2 operator +(Vector2 l, Vector2 r)
            => new(l.X + r.X, l.Y + r.Y);

        public static Vector2 operator -(Vector2 l, Vector2 r)
            => l + (-r);

        public static float operator *(Vector2 l, Vector2 r)
            => l.X * r.X + l.Y * r.Y;

        public static Vector2 operator *(Vector2 l, float r)
            => new(l.X * r, l.Y * r);

        public static Vector2 operator *(float l, Vector2 r)
            => r * l;

        public static Vector2 operator /(Vector2 l, float r)
            => l * (1 / r);

        #endregion

        #region Logical Operators/Methods

        public override bool Equals(object obj)
        {
            return obj is Vector2 vector &&
                   X == vector.X &&
                   Y == vector.Y;
        }

        public override int GetHashCode()
            => HashCode.Combine(X, Y);

        public static bool operator ==(Vector2 l, Vector2 r)
            => l.Equals(r);

        public static bool operator !=(Vector2 l, Vector2 r)
            => !l.Equals(r);

        #endregion

        public override string ToString() => $"({X}, {Y})";
    }
}
