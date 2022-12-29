using SATools.SACommon;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.GC;
using SATools.SAModel.ModelData.Weighted;
using SATools.SAModel.ObjectData;
using SATools.SAModel.Structs;
using System.Numerics;

namespace SATools.SAModel.Blender
{
    public struct NodeStruct
    {
        public string name;
        public Matrix4x4 worldMatrix;
        public int parentIndex;
        public ObjectAttributes attributes;

        public NodeStruct(
            string name, int parentIndex, ObjectAttributes attributes,
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            this.name = name;
            this.worldMatrix = new(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
            this.parentIndex = parentIndex;
            this.attributes = attributes;
        }
    }

    public struct MaterialStruct
    {
        public Color Diffuse;
        public Color Specular;
        public float SpecularExponent;
        public Color Ambient;
        public MaterialAttributes MaterialAttributes;
        public bool UseAlpha;
        public bool Culling;
        public BlendMode SourceBlendMode;
        public BlendMode DestinationBlendmode;
        public uint TextureIndex;
        public FilterMode TextureFiltering;
        public bool AnisotropicFiltering;
        public float MipmapDistanceAdjust;
        public bool ClampU;
        public bool MirrorU;
        public bool ClampV;
        public bool MirrorV;
        public byte ShadowStencil;
        public TexCoordID TexCoordID;
        public TexGenType TexGenType;
        public TexGenSrc TexGenSrc;
        public TexGenMatrix MatrixID;

        public MaterialStruct(
            float dr, float dg, float db, float da,
            float sr, float sg, float sb, float sa,
            float specularExponent,
            float ar, float ag, float ab, float aa,
            MaterialAttributes materialAttributes,
            bool useAlpha,
            bool culling,
            BlendMode sourceBlendMode,
            BlendMode destinationBlendmode,
            uint textureIndex,
            FilterMode textureFiltering,
            bool anisotropicFiltering,
            float mipmapDistanceAdjust,
            bool clampU,
            bool mirrorU,
            bool clampV,
            bool mirrorV,
            byte shadowStencil,
            TexCoordID texCoordID,
            TexGenType texGenType,
            TexGenSrc texGenSrc,
            TexGenMatrix matrixID)
        {
            Diffuse = new(dr, dg, db, da);
            Specular = new(sr, sg, sb, sa);
            SpecularExponent = specularExponent;
            Ambient = new(ar, ag, ab, aa);
            MaterialAttributes = materialAttributes;
            UseAlpha = useAlpha;
            Culling = culling;
            SourceBlendMode = sourceBlendMode;
            DestinationBlendmode = destinationBlendmode;
            TextureIndex = textureIndex;
            TextureFiltering = textureFiltering;
            AnisotropicFiltering = anisotropicFiltering;
            MipmapDistanceAdjust = mipmapDistanceAdjust;
            ClampU = clampU;
            MirrorU = mirrorU;
            ClampV = clampV;
            MirrorV = mirrorV;
            ShadowStencil = shadowStencil;
            TexCoordID = texCoordID;
            TexGenType = texGenType;
            TexGenSrc = texGenSrc;
            MatrixID = matrixID;
        }

        internal BufferMaterial ToBufferMaterial()
        {
            return new()
            {
                Diffuse = Diffuse,
                Specular = Specular,
                SpecularExponent = SpecularExponent,
                Ambient = Ambient,
                MaterialAttributes = MaterialAttributes,
                UseAlpha = UseAlpha,
                Culling = Culling,
                SourceBlendMode = SourceBlendMode,
                DestinationBlendmode = DestinationBlendmode,
                TextureIndex = TextureIndex,
                TextureFiltering = TextureFiltering,
                AnisotropicFiltering = AnisotropicFiltering,
                MipmapDistanceAdjust = MipmapDistanceAdjust,
                ClampU = ClampU,
                MirrorU = MirrorU,
                ClampV = ClampV,
                MirrorV = MirrorV,
                ShadowStencil = ShadowStencil,
                TexCoordID = TexCoordID,
                TexGenType = TexGenType,
                TexGenSrc = TexGenSrc,
                MatrixID = MatrixID,
            };
        }
    }

    public struct MeshStruct
    {
        public string name;
        public WeightedVertex[] vertices;
        public BufferCorner[][] corners;
        public MaterialStruct[] materials;

        public MeshStruct(
            string name,
            WeightedVertex[] vertices,
            BufferCorner[][] corners,
            MaterialStruct[] materials)
        {
            this.name = name;
            this.vertices = vertices;
            this.corners = corners;
            this.materials = materials;
        }
    }
}
