﻿using System;
using System.Numerics;
using SATools.SACommon;
using static SATools.SACommon.MathHelper;
using Reloaded.Memory.Streams.Writers;
using System.IO;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// Vector 3 Extensions
    /// </summary>
    public static class Vector3Extensions
    {
        #region I/O
        /// <summary>
        /// Reads a vector3 from a byte source
        /// </summary>
        /// <param name="source"></param>
        /// <param name="address"></param>
        /// <param name="type"></param>
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
                        source.ToSingle(address + 4),
                        source.ToSingle(address + 8),
                        source.ToSingle(address + 12)
                        );

                    address += 16;
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for struct Vector3");
            }
            return result;
        }

        public static Vector3 FromQuaternion(Vector4 q)
            => FromQuaternion(q.W, q.X, q.Y, q.Z);

        public static Vector3 FromQuaternion(Quaternion q)
            => FromQuaternion(q.W, q.X, q.Y, q.Z);

        public static Vector3 FromQuaternion(float w, float x, float y, float z)
        {
            // if the input quaternion is normalized, this is exactly one. Otherwise, this acts as a correction factor for the quaternion's not-normalizedness
            float unit = (y * y) + (x * x) + (z * z) + (w * w);

            // this will have a magnitude of 0.5 or greater if and only if this is a singularity case
            float test = y * w - x * z;

            Vector3 v = new();

            if(test > 0.4995f * unit) // singularity at north pole
            {
                v.Y = Pi / 2;
                v.X = 2f * (float)Math.Atan2(x, y);
            }
            else if(test < -0.4995f * unit) // singularity at south pole
            {
                v.Y = -Pi / 2;
                v.X = -2f * (float)Math.Atan2(x, y);
            }
            else
            {
                v.Y = (float)Math.Asin(2f * (w * y - x * z));
                v.X = (float)Math.Atan2(2f * w * x + 2f * z * y, 1 - 2f * (y * y + x * x));
                v.Z = (float)Math.Atan2(2f * w * z + 2f * y * x, 1 - 2f * (z * z + y * y));
            }
            v *= Rad2Deg;

            static void bamsClamp(ref float t)
            {
                t %= 360;
                if(t < -180)
                    t += 360;
                else if(t > 180)
                    t -= 360;
            }

            bamsClamp(ref v.X);
            bamsClamp(ref v.Y);
            bamsClamp(ref v.Z);
            return v;
        }

        /// <summary>
        /// Writes a Vector3 to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="type">Datatype to write object as</param>
        public static void Write(this Vector3 vector, EndianWriter writer, IOType type)
        {
            switch(type)
            {
                case IOType.Short:
                    writer.WriteInt16((short)vector.X);
                    writer.WriteInt16((short)vector.Y);
                    writer.WriteInt16((short)vector.Z);
                    break;
                case IOType.Float:
                    writer.WriteSingle(vector.X);
                    writer.WriteSingle(vector.Y);
                    writer.WriteSingle(vector.Z);
                    break;
                case IOType.BAMS16:
                    writer.WriteInt16((short)DegToBAMS(vector.X));
                    writer.WriteInt16((short)DegToBAMS(vector.Y));
                    writer.WriteInt16((short)DegToBAMS(vector.Z));
                    break;
                case IOType.BAMS32:
                    writer.WriteInt32(DegToBAMS(vector.X));
                    writer.WriteInt32(DegToBAMS(vector.Y));
                    writer.WriteInt32(DegToBAMS(vector.Z));
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for struct Vector3");
            }
        }

        /// <summary>
        /// Writes a vector3 to a text stream as an NJAscii struct
        /// </summary>
        /// <param name="writer">Output text stream</param>
        /// <param name="type">Output type</param>
        public static void WriteNJA(this Vector3 vector, TextWriter writer, IOType type)
        {
            writer.Write("( ");
            switch(type)
            {
                case IOType.Short:
                    writer.Write((short)vector.X);
                    writer.Write(", ");
                    writer.Write((short)vector.Y);
                    writer.Write(", ");
                    writer.Write((short)vector.Z);
                    break;
                case IOType.Float:
                    writer.Write(vector.X.ToC());
                    writer.Write(", ");
                    writer.Write(vector.Y.ToC());
                    writer.Write(", ");
                    writer.Write(vector.Z.ToC());
                    break;
                case IOType.BAMS16:
                    writer.Write(((short)DegToBAMS(vector.X)).ToCHex());
                    writer.Write(", ");
                    writer.Write(((short)DegToBAMS(vector.Y)).ToCHex());
                    writer.Write(", ");
                    writer.Write(((short)DegToBAMS(vector.Z)).ToCHex());
                    break;
                case IOType.BAMS32:
                    writer.Write(DegToBAMS(vector.X).ToCHex());
                    writer.Write(", ");
                    writer.Write(DegToBAMS(vector.Y).ToCHex());
                    writer.Write(", ");
                    writer.Write(DegToBAMS(vector.Z).ToCHex());
                    break;
                default:
                    throw new ArgumentException($"Type {type} not available for Vector2");
            }
            writer.Write(")");
        }

        #endregion

        #region Arithmetic stuff
        /// <summary>
        /// Returns the greatest of the 3 values in the vector
        /// </summary>
        public static float GreatestValue(this Vector3 vector)
        {
            float r = vector.X;
            if(vector.Y > r)
                r = vector.Y;
            if(vector.Z > r)
                r = vector.Z;
            return r;
        }

        /// <summary>
        /// Round a Vector3
        /// </summary>
        /// <param name="digits">Floating point precision to round to</param>
        /// <returns></returns>
        public static Vector3 Rounded(this Vector3 vector, int digits)
        {
            return new(
                (float)Math.Round(vector.X, digits),
                (float)Math.Round(vector.Y, digits),
                (float)Math.Round(vector.Z, digits));
        }

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

            static void boundsCheck(float i, ref float p, ref float n)
            {
                if(i > p)
                    p = i;
                else if(i < n)
                    n = i;
            }

            foreach(Vector3 p in points)
            {
                boundsCheck(p.X, ref Positive.X, ref Negative.X);
                boundsCheck(p.Y, ref Positive.Y, ref Negative.Y);
                boundsCheck(p.Z, ref Positive.Z, ref Negative.Z);
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

        public static Matrix4x4 CreateRotationMatrix(Vector3 rotation, bool ZYX)
        {
            var matX = Matrix4x4.CreateRotationX(DegToRad(rotation.X));
            var matY = Matrix4x4.CreateRotationY(DegToRad(rotation.Y));
            var matZ = Matrix4x4.CreateRotationZ(DegToRad(rotation.Z));

            return ZYX ? matZ * matY * matX : matX * matY * matZ;
        }

        public static Matrix4x4 CreateTransformMatrix(Vector3 position, Vector3 rotation, Vector3 scale, bool rotateZYX)
            => Matrix4x4.CreateScale(scale) * CreateRotationMatrix(rotation, rotateZYX) * Matrix4x4.CreateTranslation(position);

        public static Matrix4x4 GetNormalMatrix(this Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var result);
            result = Matrix4x4.Transpose(result);
            return result;
        }

        #endregion
    }
}
