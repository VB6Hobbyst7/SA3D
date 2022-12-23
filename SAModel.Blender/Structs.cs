using SATools.SAModel.ObjData;
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
}
