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
            => Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);

        /// <summary>
        /// Converts a quaternion to euler angles (ZYX not working)
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
            float test = (y * w) - (x * z);

            Vector3 v = new();

            if(test > 0.4995f) // singularity at north pole
            {
                v.X = 2f * (float)Math.Atan2(x, y);
                v.Y = Pi / 2;
            }
            else if(test < -0.4995f) // singularity at south pole
            {
                v.X = -2f * (float)Math.Atan2(x, y);
                v.Y = -Pi / 2;
            }
            else
            {
                v.X = (float)Math.Atan2((2f * w * x) + (2f * z * y), 1 - (2f * ((y * y) + (x * x))));
                v.Y = (float)Math.Asin(2f * ((w * y) - (x * z)));
                v.Z = (float)Math.Atan2((2f * w * z) + (2f * y * x), 1 - (2f * ((z * z) + (y * y))));
            }
            v *= Rad2Deg;

            static void bamsNormalize(ref float t)
            {
                t %= 360;
                if(t < -180)
                    t += 360;
                else if(t > 180)
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
            var mtx = Vector3Extensions.CreateRotationMatrix(new(x, y, z), RotateZYX);

            float qW = MathF.Sqrt(1 + mtx.M11 + mtx.M22 + mtx.M33) / 2f;
            float w4 = 1 / (4 * qW);
            float qX = (mtx.M23 - mtx.M32) * w4;
            float qY = (mtx.M31 - mtx.M13) * w4;
            float qZ = (mtx.M12 - mtx.M21) * w4;

            return new(qX, qY, qZ, qW);
        }

        public static Quaternion FromEuler(Vector3 rotation, bool RotateZYX)
            => FromEuler(rotation.X, rotation.Y, rotation.Z, RotateZYX);

    }
}

