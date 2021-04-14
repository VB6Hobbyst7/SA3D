using Reloaded.Memory.Streams.Writers;
using System;
using System.IO;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.MathHelper;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// 3D Vector structure
    /// </summary>
    public struct Vector3 : IDataStructOut
    {
        #region Constants 

        /// <summary>
        /// Equal to (1, 0, 0)
        /// </summary>
        public static readonly Vector3 UnitX
            = new(1, 0, 0);

        /// <summary>
        /// Equal to (0, 1, 0)
        /// </summary>
        public static readonly Vector3 UnitY
            = new(0, 1, 0);

        /// <summary>
        /// Equal to (0, 0, 1)
        /// </summary>
        public static readonly Vector3 UnitZ
            = new(0, 0, 1);

        /// <summary>
        /// Equal to (1, 1, 1)
        /// </summary>
        public static readonly Vector3 One
            = new(1, 1, 1);

        /// <summary>
        /// Equal to (0, 0, 0)
        /// </summary>
        public static readonly Vector3 Zero
            = new(0, 0, 0);

        #endregion

        /// <summary>
        /// X Component of the Vector
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y Component of the Vector
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Z Component of the Vector
        /// </summary>
        public float Z { get; set; }

        public float this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
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
                    case 2:
                        Z = value;
                        return;
                    default:
                        throw new IndexOutOfRangeException($"Index {index} out of range");
                }
            }
        }

        public float Length
            => (float)Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));

        /// <summary>
        /// Returns the greatest of the 3 values in the vector
        /// </summary>
        public float GreatestValue
        {
            get
            {
                float r = X;
                if(Y > r)
                    r = Y;
                if(Z > r)
                    r = Z;
                return r;
            }
        }


        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector2 vec2)
        {
            X = vec2.X;
            Y = vec2.X;
            Z = 0;
        }


        #region I/O

        /// <summary>
        /// Reads a vector3 object from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the vector3 is located</param>
        /// <param name="type">How the bytes should be read</param>
        /// <returns></returns>
        public static Vector3 Read(byte[] source, ref uint address, IOType type)
        {
            Vector3 result;
            switch(type)
            {
                case IOType.Short:
                    result = new Vector3()
                    {
                        X = source.ToInt16(address),
                        Y = source.ToInt16(address + 2),
                        Z = source.ToInt16(address + 4)
                    };
                    address += 6;
                    break;
                case IOType.Float:
                    result = new Vector3()
                    {
                        X = source.ToSingle(address),
                        Y = source.ToSingle(address + 4),
                        Z = source.ToSingle(address + 8)
                    };
                    address += 12;
                    break;
                case IOType.BAMS16:
                    result = new Vector3()
                    {
                        X = BAMSToDeg(source.ToInt16(address)),
                        Y = BAMSToDeg(source.ToInt16(address + 2)),
                        Z = BAMSToDeg(source.ToInt16(address + 4))
                    };
                    address += 6;
                    break;
                case IOType.BAMS32:
                    result = new Vector3()
                    {
                        X = BAMSToDeg(source.ToInt32(address)),
                        Y = BAMSToDeg(source.ToInt32(address + 4)),
                        Z = BAMSToDeg(source.ToInt32(address + 8))
                    };
                    address += 12;
                    break;
                case IOType.Quaternion:
                    result = FromQuaternion(
                        source.ToSingle(address),
                        -source.ToSingle(address + 4),
                        -source.ToSingle(address + 8),
                        source.ToSingle(address + 12)
                        );

                    address += 16;
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for struct Vector3");
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
                    writer.WriteInt16((short)Z);
                    break;
                case IOType.Float:
                    writer.WriteSingle(X);
                    writer.WriteSingle(Y);
                    writer.WriteSingle(Z);
                    break;
                case IOType.BAMS16:
                    writer.WriteInt16((short)DegToBAMS(X));
                    writer.WriteInt16((short)DegToBAMS(Y));
                    writer.WriteInt16((short)DegToBAMS(Z));
                    break;
                case IOType.BAMS32:
                    writer.WriteInt32(DegToBAMS(X));
                    writer.WriteInt32(DegToBAMS(Y));
                    writer.WriteInt32(DegToBAMS(Z));
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for struct Vector3");
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
                    writer.Write(", ");
                    writer.Write((short)Z);
                    break;
                case IOType.Float:
                    writer.Write(X.ToC());
                    writer.Write(", ");
                    writer.Write(Y.ToC());
                    writer.Write(", ");
                    writer.Write(Z.ToC());
                    break;
                case IOType.BAMS16:
                    writer.Write(((short)DegToBAMS(X)).ToCHex());
                    writer.Write(", ");
                    writer.Write(((short)DegToBAMS(Y)).ToCHex());
                    writer.Write(", ");
                    writer.Write(((short)DegToBAMS(Z)).ToCHex());
                    break;
                case IOType.BAMS32:
                    writer.Write(DegToBAMS(X).ToCHex());
                    writer.Write(", ");
                    writer.Write(DegToBAMS(Y).ToCHex());
                    writer.Write(", ");
                    writer.Write(DegToBAMS(Z).ToCHex());
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for Vector2");
            }
            writer.Write(")");
        }

        public static Vector3 FromQuaternion(float w, float x, float y, float z)
        {
            // if the input quaternion is normalized, this is exactly one. Otherwise, this acts as a correction factor for the quaternion's not-normalizedness
            float unit = (x * x) + (y * y) + (z * z) + (w * w);

            // this will have a magnitude of 0.5 or greater if and only if this is a singularity case
            float test = x * w - y * z;

            Vector3 v = new();

            if(test > 0.4995f * unit) // singularity at north pole
            {
                v.X = Pi / 2;
                v.Y = 2f * (float)Math.Atan2(y, x);
            }
            else if(test < -0.4995f * unit) // singularity at south pole
            { 
                v.X = -Pi / 2;
                v.Y = -2f * (float)Math.Atan2(y, x);
            }
            else
            {
                v.X = (float)Math.Atan2(2f * w * y + 2f * z * x, 1 - 2f * (x * x + y * y));
                v.Y = (float)Math.Asin(2f * (w * x - y * z));
                v.Z = (float)Math.Atan2(2f * w * z + 2f * x * y, 1 - 2f * (z * z + x * x));
            }
            v *= Rad2Deg;

            v.X %= 360;
            v.Y %= 360;
            v.Z %= 360;

            return v;
        }

        #endregion

        #region Arithmetic Operators/Methods

        /// <summary>
        /// Returns a Normalized version of this vector
        /// </summary>
        public Vector3 Normalized() => this / Length;

        public Vector3 Rounded(int floatingPoint)
            => new((float)Math.Round(X, floatingPoint), (float)Math.Round(Y, floatingPoint), (float)Math.Round(Z, floatingPoint));

        /// <summary>
        /// Calculates the distance between 2 points
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static float Distance(Vector3 l, Vector3 r) => (float)Math.Sqrt(Math.Pow(l.X - r.X, 2) + Math.Pow(l.Y - l.Y, 2) + Math.Pow(l.Z - l.Z, 2));

        /// <summary>
        /// Calculates the average position of a collection of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Vector3 Average(Vector3[] points)
        {
            Vector3 center = new();

            if(points == null || points.Length == 0)
                return center;

            foreach(Vector3 p in points)
                center += p;

            return center / points.Length;
        }

        /// <summary>
        /// Calculates the center of the bounds of a list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Vector3 Center(Vector3[] points)
        {
            if(points == null || points.Length == 0)
                return new Vector3();

            Vector3 Positive = points[0];
            Vector3 Negative = points[0];

            foreach(Vector3 p in points)
            {
                if(p.X > Positive.X)
                    Positive.X = p.X;
                if(p.Y > Positive.Y)
                    Positive.Y = p.Y;
                if(p.Z > Positive.Z)
                    Positive.Z = p.Z;

                if(p.X < Negative.X)
                    Negative.X = p.X;
                if(p.Y < Negative.Y)
                    Negative.Y = p.Y;
                if(p.Z < Negative.Z)
                    Negative.Z = p.Z;
            }

            return (Positive + Negative) / 2;
        }

        /// <summary>
        /// Linear interpolation
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 Lerp(Vector3 min, Vector3 max, float t)
            => min + (max - min) * t;


        public static Vector3 operator -(Vector3 v)
            => new(-v.X, -v.Y, -v.Z);

        public static Vector3 operator +(Vector3 l, Vector3 r)
            => new(l.X + r.X, l.Y + r.Y, l.Z + r.Z);

        public static Vector3 operator -(Vector3 l, Vector3 r)
            => l + (-r);

        public static float operator *(Vector3 l, Vector3 r)
            => l.X * r.X + l.Y * r.Y + l.Z * r.Z;

        public static Vector3 operator *(Vector3 l, float r)
            => new(l.X * r, l.Y * r, l.Z * r);

        public static Vector3 operator *(float l, Vector3 r)
            => r * l;

        public static Vector3 operator /(Vector3 l, float r)
            => l * (1 / r);

        #endregion

        #region Logical Operators/Methods

        public override bool Equals(object obj)
        {
            return obj is Vector3 vector &&
                   X == vector.X &&
                   Y == vector.Y &&
                   Z == vector.Z;
        }

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);

        public static bool operator ==(Vector3 l, Vector3 r) => l.Equals(r);
        public static bool operator !=(Vector3 l, Vector3 r) => !l.Equals(r);

        #endregion
        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
