using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using static SATools.SACommon.MathHelper;
using SAVector2 = SATools.SAModel.Structs.Vector2;
using SAVector3 = SATools.SAModel.Structs.Vector3;

namespace SATools.SAModel.Graphics.OpenGL
{
    internal static class Converters
    {
        internal static Vector3 ToGL(this SAVector3 vec3)
            => new(vec3.X, vec3.Y, vec3.Z);

        internal static Vector4 ToGL4(this SAVector3 vec3)
            => new(vec3.X, vec3.Y, vec3.Z, 1);

        internal static SAVector3 ToSA(this Vector3 vec3)
            => new(vec3.X, vec3.Y, vec3.Z);

        internal static Vector2 ToGL(this SAVector2 vec2)
            => new(vec2.X, vec2.Y);

        internal static SAVector2 ToSA(this Vector2 vec2)
            => new(vec2.X, vec2.Y);

        internal static Matrix4 CreateRotationMatrix(this SAVector3 rotation, bool ZYX)
        {
            var matX = Matrix4.CreateRotationX(DegToRad(rotation.X));
            var matY = Matrix4.CreateRotationY(DegToRad(rotation.Y));
            var matZ = Matrix4.CreateRotationZ(DegToRad(rotation.Z));

            return ZYX ? matZ * matY * matX : matX * matY * matZ;
        }

        internal static Matrix4 GenMatrix(SAVector3 position, SAVector3 rotation, SAVector3 scale, bool rotateZYX)
            => Matrix4.CreateScale(scale.ToGL()) * CreateRotationMatrix(rotation, rotateZYX) * Matrix4.CreateTranslation(position.ToGL());

        internal static Matrix4 LocalMatrix(this NJObject obj)
            => GenMatrix(obj.Position, obj.Rotation, obj.Scale, obj.RotateZYX);

        internal static Matrix4 LocalMatrix(this LandEntry obj)
            => GenMatrix(obj.Position, obj.Rotation, obj.Scale, obj.RotateZYX);

        internal static Matrix4 CreateViewMatrix(SAVector3 position, SAVector3 rotation)
             => Matrix4.CreateTranslation(-position.ToGL()) * rotation.CreateRotationMatrix(true);

        internal static BlendingFactor ToGLBlend(this BlendMode instr)
        {
            switch(instr)
            {
                default:
                case BlendMode.Zero:
                    return BlendingFactor.Zero;
                case BlendMode.One:
                    return BlendingFactor.One;
                case BlendMode.Other:
                    return BlendingFactor.SrcColor;
                case BlendMode.OtherInverted:
                    return BlendingFactor.OneMinusSrcColor;
                case BlendMode.SrcAlpha:
                    return BlendingFactor.SrcAlpha;
                case BlendMode.SrcAlphaInverted:
                    return BlendingFactor.OneMinusSrcAlpha;
                case BlendMode.DstAlpha:
                    return BlendingFactor.DstAlpha;
                case BlendMode.DstAlphaInverted:
                    return BlendingFactor.OneMinusDstAlpha;
            }
        }

        internal static TextureMinFilter ToGLMinFilter(this FilterMode filter)
        {
            return filter switch
            {
                FilterMode.PointSampled => TextureMinFilter.NearestMipmapNearest,
                FilterMode.Bilinear => TextureMinFilter.NearestMipmapLinear,
                FilterMode.Trilinear => TextureMinFilter.LinearMipmapLinear,
                _ => throw new InvalidCastException($"{filter} has no corresponding OpenGL filter"),
            };
        }

        internal static TextureMagFilter ToGLMagFilter(this FilterMode filter)
        {
            return filter switch
            {
                FilterMode.PointSampled => TextureMagFilter.Nearest,
                FilterMode.Bilinear or FilterMode.Trilinear => TextureMagFilter.Linear,
                _ => throw new InvalidCastException($"{filter} has no corresponding OpenGL filter"),
            };
        }

        internal static TextureWrapMode WrapModeU(this BufferMaterial mat)
        {
            if(mat.ClampU)
                return TextureWrapMode.ClampToEdge;
            else
                return mat.MirrorU ? TextureWrapMode.MirroredRepeat : TextureWrapMode.Repeat;
        }

        internal static TextureWrapMode WrapModeV(this BufferMaterial mat)
        {
            if(mat.ClampV)
                return TextureWrapMode.ClampToEdge;
            else
                return mat.MirrorV ? TextureWrapMode.MirroredRepeat : TextureWrapMode.Repeat;
        }

    }
}
