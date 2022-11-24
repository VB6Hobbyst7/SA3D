using SATools.SACommon;
using System;
using System.Numerics;
using static SATools.SACommon.MathHelper;

namespace SATools.SAModel.Structs
{
    public static class QuaternionExtensions
    {
        public static Quaternion Read(byte[] source, ref uint address)
        {
            Quaternion result = new();
            result.W = source.ToSingle(address);
            result.X = source.ToSingle(address + 4);
            result.Y = source.ToSingle(address + 8);
            result.Z = source.ToSingle(address + 12);

            address += 16;

            return result;
        }

        public static void Write(this Quaternion quaternion, EndianWriter writer)
        {
            writer.WriteSingle(quaternion.W);
            writer.WriteSingle(quaternion.X);
            writer.WriteSingle(quaternion.Y);
            writer.WriteSingle(quaternion.Z);
        }

        public static Matrix4x4 CreateTransformMatrix(Vector3 position, Quaternion rotation, Vector3 scale)
            => Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(rotation)) * Matrix4x4.CreateTranslation(position);

        /// <summary>
        /// Converts a quaternion to euler angles
        /// </summary>
        public static Vector3 ToEuler(float w, float x, float y, float z, bool RotateZYX)
        {
            // normalize the values
            Quaternion vN = Quaternion.Normalize(new(x, y, z, w));
            x = vN.X;
            y = vN.Y;
            z = vN.Z;
            w = vN.W;

            // this will have a magnitude of 0.5 or greater if and only if this is a singularity case
            Vector3 v = new();
            if (RotateZYX)
            {
                float test = (w * x) - (y * z); // -M32 / 2

                if (test > 0.4995f) // singularity at north pole
                {
                    v.Z = 2f * MathF.Atan2(z, x);
                    v.X = Pi * 0.5f;
                }
                else if (test < -0.4995f) // singularity at south pole
                {
                    v.Z = -2f * MathF.Atan2(z, x);
                    v.X = -Pi * 0.5f;
                }
                else
                {
                    v.X = MathF.Asin(2f * test); // asin(-M32)
                    v.Y = MathF.Atan2(2f * ((w * y) + (z * x)), 1 - (2f * ((y * y) + (x * x)))); // atan2(M31, M33)
                    v.Z = MathF.Atan2(2f * ((w * z) + (y * x)), 1 - (2f * ((z * z) + (x * x)))); // atan2(M12,M22)
                }

                // here, z is only sometimes correct... weird. But the part above works, so idc this is only reference either way
                // x = MathF.Atan(-mtx.M32 / MathF.Sqrt(1 - (mtx.M32 * mtx.M32)));
                // y = MathF.Atan(mtx.M31 / mtx.M33);
                // z = MathF.Atan(mtx.M12 / mtx.M22); 
            }
            else
            {
                float test = (w * y) - (x * z); // -M13 / 2

                if (test > 0.4995f) // singularity at north pole
                {
                    v.X = 2f * MathF.Atan2(x, y);
                    v.Y = Pi * 0.5f;
                }
                else if (test < -0.4995f) // singularity at south pole
                {
                    v.X = -2f * MathF.Atan2(x, y);
                    v.Y = -Pi * 0.5f;
                }
                else
                {
                    v.X = MathF.Atan2(2f * ((w * x) + (z * y)), 1 - (2f * ((y * y) + (x * x)))); //atan2(M23,M33)
                    v.Y = MathF.Asin(2f * test); // asin(-M13)
                    v.Z = MathF.Atan2(2f * ((w * z) + (y * x)), 1 - (2f * ((z * z) + (y * y)))); //atan2(M12,M11)
                }

                // x = MathF.Atan(mtx.M23 / mtx.M33);
                // y = MathF.Atan(-mtx.M13 / MathF.Sqrt(1 - (mtx.M13 * mtx.M13)));
                // z = MathF.Atan(mtx.M12 / mtx.M11);
            }
            v *= Rad2Deg;

            static void bamsNormalize(ref float t)
            {
                t %= 360;
                if (t < -180)
                    t += 360;
                else if (t > 180)
                    t -= 360;
            }

            bamsNormalize(ref v.X);
            bamsNormalize(ref v.Y);
            bamsNormalize(ref v.Z);
            return v;
        }

        /// <summary>
        /// Converts a quaternion to euler angles (ZYX not working)
        /// </summary>
        public static Vector3 ToEuler(this Quaternion q, bool RotateZYX)
            => ToEuler(q.W, q.X, q.Y, q.Z, RotateZYX);


        public static Quaternion FromEuler(float x, float y, float z, bool RotateZYX)
        {
            Matrix4x4 mtx = Vector3Extensions.CreateRotationMatrix(new(x, y, z), RotateZYX);
            return Quaternion.CreateFromRotationMatrix(mtx);
        }

        public static Quaternion FromEuler(Vector3 rotation, bool RotateZYX)
            => FromEuler(rotation.X, rotation.Y, rotation.Z, RotateZYX);

    }
}

