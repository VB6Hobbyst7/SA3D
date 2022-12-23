using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using SATools.SAModel.ModelData.GC;
using System.Numerics;

namespace SATools.SAModel.Blender
{
    public struct NodeStruct
    {
        public string name;
        public Matrix4x4 localMatrix;
        public int parentIndex;
        public ObjectAttributes attributes;
    }

    public struct MaterialStruct
    {
        public Color diffuse;
        public Color specular;
        public float specularExponent;
        public Color ambient;
        public MaterialAttributes materialAttributes;
        public bool useAlpha;
        public bool culling;
        public BlendMode sourceBlendMode;
        public BlendMode destinationBlendmode;
        public uint textureIndex;
        public FilterMode textureFiltering;
        public bool anisotropicFiltering;
        public float mipmapDistanceAdjust;
        public bool clampU;
        public bool mirrorU;
        public bool clampV;
        public bool mirrorV;
        public byte shadowStencil;
        public TexCoordID texCoordID;
        public TexGenType texGenType;
        public TexGenSrc texGenSrc;
        public TexGenMatrix matrixID;
    }
}
