using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjectData;

namespace SATools.SAModel.Blender
{
    public static class Flags
    {
        public static MaterialAttributes ComposeMaterialAttributes(bool flat, bool noAmbient, bool noDiffuse, bool noSpecular, bool useTexture, bool normalMapping)
        {
            MaterialAttributes result = default;

            if (flat) result |= MaterialAttributes.Flat;
            if (noAmbient) result |= MaterialAttributes.NoAmbient;
            if (noDiffuse) result |= MaterialAttributes.NoDiffuse;
            if (noSpecular) result |= MaterialAttributes.NoSpecular;
            if (useTexture) result |= MaterialAttributes.UseTexture;
            if (normalMapping) result |= MaterialAttributes.NormalMapping;

            return result;
        }

        public static (bool flat, bool noAmbient, bool noDiffuse, bool noSpecular, bool useTexture, bool normalMapping) DecomposeMaterialAttributes(this MaterialAttributes attributes)
        {
            return (
                attributes.HasFlag(MaterialAttributes.Flat),
                attributes.HasFlag(MaterialAttributes.NoAmbient),
                attributes.HasFlag(MaterialAttributes.NoDiffuse),
                attributes.HasFlag(MaterialAttributes.NoSpecular),
                attributes.HasFlag(MaterialAttributes.UseTexture),
                attributes.HasFlag(MaterialAttributes.NormalMapping)
            );
        }

        public static NodeAttributes ComposeNodeAttributes(bool noPosition, bool noRotation, bool noScale, bool skipDraw, bool skipChildren, bool rotateZYX, bool noAnimate, bool noMorph)
        {
            NodeAttributes result = default;

            if (noPosition) result |= NodeAttributes.NoPosition;
            if (noRotation) result |= NodeAttributes.NoRotation;
            if (noScale) result |= NodeAttributes.NoScale;
            if (skipDraw) result |= NodeAttributes.SkipDraw;
            if (skipChildren) result |= NodeAttributes.SkipChildren;
            if (rotateZYX) result |= NodeAttributes.RotateZYX;
            if (noAnimate) result |= NodeAttributes.NoAnimate;
            if (noMorph) result |= NodeAttributes.NoMorph;

            return result;
        }

        public static (bool noPosition, bool noRotation, bool noScale, bool skipDraw, bool skipChildren, bool rotateZYX, bool noAnimate, bool noMorph) DecomposeNodeAttributes(this NodeAttributes attributes)
        {
            return (
                attributes.HasFlag(NodeAttributes.NoPosition),
                attributes.HasFlag(NodeAttributes.NoRotation),
                attributes.HasFlag(NodeAttributes.NoScale),
                attributes.HasFlag(NodeAttributes.SkipDraw),
                attributes.HasFlag(NodeAttributes.SkipChildren),
                attributes.HasFlag(NodeAttributes.RotateZYX),
                attributes.HasFlag(NodeAttributes.NoAnimate),
                attributes.HasFlag(NodeAttributes.NoMorph)
            );
        }

    }
}