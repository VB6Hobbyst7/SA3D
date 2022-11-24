using OpenTK.Graphics.OpenGL4;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using System;

namespace SATools.SAModel.Graphics.OpenGL
{
    internal static class Converters
    {
        internal static BlendingFactor ToGLBlend(this BlendMode instr)
        {
            return instr switch
            {
                BlendMode.One => BlendingFactor.One,
                BlendMode.Other => BlendingFactor.SrcColor,
                BlendMode.OtherInverted => BlendingFactor.OneMinusSrcColor,
                BlendMode.SrcAlpha => BlendingFactor.SrcAlpha,
                BlendMode.SrcAlphaInverted => BlendingFactor.OneMinusSrcAlpha,
                BlendMode.DstAlpha => BlendingFactor.DstAlpha,
                BlendMode.DstAlphaInverted => BlendingFactor.OneMinusDstAlpha,
                _ => BlendingFactor.Zero,
            };
        }

        internal static TextureMinFilter ToGLMinFilter(this FilterMode filter)
        {
            return filter switch
            {
                FilterMode.PointSampled => TextureMinFilter.NearestMipmapNearest,
                FilterMode.Bilinear => TextureMinFilter.LinearMipmapNearest,
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
            if (mat.ClampU)
                return mat.MirrorU ? TextureWrapMode.MirroredRepeat : TextureWrapMode.ClampToEdge;
            else
                return TextureWrapMode.Repeat;
        }

        internal static TextureWrapMode WrapModeV(this BufferMaterial mat)
        {
            if (mat.ClampV)
                return mat.MirrorV ? TextureWrapMode.MirroredRepeat : TextureWrapMode.ClampToEdge;
            else
                return TextureWrapMode.Repeat;
        }

    }
}
