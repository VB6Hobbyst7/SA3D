﻿using SATools.SACommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using static SATools.SACommon.MathHelper;

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
            switch (type)
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
                default:
                    throw new ArgumentException($"Type {type} not available for struct Vector3");
            }
            return result;
        }

        /// <summary>
        /// Writes a Vector3 to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="type">Datatype to write object as</param>
        public static void Write(this Vector3 vector, EndianWriter writer, IOType type)
        {
            switch (type)
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
            switch (type)
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
            if (vector.Y > r)
                r = vector.Y;
            if (vector.Z > r)
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

            if (points == null || points.Length == 0)
                return center;

            foreach (Vector3 p in points)
                center += p;

            return center / points.Length;
        }

        /// <summary>
        /// Calculates the center of the bounds of a list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Vector3 Center(IEnumerable<Vector3> points)
        {
            Vector3? first = null;
            foreach (Vector3 point in points)
            {
                first = point;
                break;
            }

            if (first == null)
                return default;

            Vector3 Positive = first.Value;
            Vector3 Negative = first.Value;

            static void boundsCheck(float i, ref float p, ref float n)
            {
                if (i > p)
                    p = i;
                else if (i < n)
                    n = i;
            }

            foreach (Vector3 p in points)
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
            => min + ((max - min) * t);

        public static Matrix4x4 CreateRotationMatrix(Vector3 rotation, bool ZYX)
        {
            float radX = DegToRad(rotation.X);
            float radY = DegToRad(rotation.Y);
            float radZ = DegToRad(rotation.Z);

            float sX = MathF.Sin(radX);
            float cX = MathF.Cos(radX);

            float sY = MathF.Sin(radY);
            float cY = MathF.Cos(radY);

            float sZ = MathF.Sin(radZ);
            float cZ = MathF.Cos(radZ);


            if (ZYX)
            {
                // Well, in sa2 it rotates in ZXY order, so thats what we do here. dont ask me...
                // equal to matZ * matX * matY
                return new()
                {
                    M11 = sY * sX * sZ + cY * cZ,
                    M12 = cX * sZ,
                    M13 = cY * sX * sZ - sY * cZ,

                    M21 = sY * sX * cZ - cY * sZ,
                    M22 = cX * cZ,
                    M23 = cY * sX * cZ + sY * sZ,

                    M31 = sY * cX,
                    M32 = -sX,
                    M33 = cY * cX,

                    M44 = 1
                };
            }
            else
            {
                // equal to matX * matY * matZ
                return new()
                {
                    M11 = cZ * cY,
                    M12 = sZ * cY,
                    M13 = -sY,

                    M21 = cZ * sY * sX - sZ * cX,
                    M22 = sZ * sY * sX + cZ * cX,
                    M23 = cY * sX,

                    M31 = cZ * sY * cX + sZ * sX,
                    M32 = sZ * sY * cX - cZ * sX,
                    M33 = cY * cX,

                    M44 = 1
                };
            }
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
