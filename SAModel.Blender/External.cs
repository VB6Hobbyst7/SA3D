using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Weighted;
using SATools.SAModel.ObjData;
using System.Numerics;

namespace SATools.SAModel.Blender
{
    public static class External
    {
        public static int HookTest(int x)
        {
            return x * 2;
        }

        public static void ExportModel(NodeStruct[] nodes, WeightedBufferAttach[] weightedAttaches, AttachFormat format, bool njFile, string filepath, bool optimize, bool ignoreWeights)
        {
            if (nodes.Length == 0)
            {
                throw new InvalidDataException("No nodes passed over");
            }

            ObjectNode[] objNodes = new ObjectNode[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                NodeStruct node = nodes[i];

                node.localMatrix.TransformsFromBlenderMatrix(
                    out Vector3 position,
                    out Vector3 rotation,
                    out Vector3 scale);

                ObjectNode? parent = node.parentIndex >= 0 ? objNodes[node.parentIndex] : null;
                ObjectNode objNode = new(parent)
                {
                    Name = node.name,
                    Position = position,
                    Rotation = rotation,
                    Scale = scale
                };

                objNode.SetAllObjectAttributes(node.attributes, true);
            }
            ObjectNode root = objNodes[0];

            WeightedBufferAttach.FromWeightedBuffer(root, weightedAttaches, optimize, ignoreWeights, format);

            File.WriteAllBytes(filepath, ModelFile.Write(format, njFile, root));
        }

        private static void TransformsFromBlenderMatrix(this Matrix4x4 matrix, out Vector3 position, out Vector3 rotation, out Vector3 scale)
        {
            position = Vector3.Zero;
            rotation = Vector3.Zero;
            scale = Vector3.One;
        }
    }
}
