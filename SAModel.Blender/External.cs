using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Weighted;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System.Numerics;

namespace SATools.SAModel.Blender
{
    public static class External
    {
        public static void ExportModel(NodeStruct[] nodes, MeshStruct[] weightedAttaches, AttachFormat format, bool njFile, string filepath, bool optimize, bool ignoreWeights)
        {
            if (nodes.Length == 0)
            {
                throw new InvalidDataException("No nodes passed over");
            }

            ObjectNode[] objNodes = new ObjectNode[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                NodeStruct node = nodes[i];

                Matrix4x4 localMatrix = node.worldMatrix;
                if(node.parentIndex >= 0)
                {
                    Matrix4x4.Invert(nodes[node.parentIndex].worldMatrix, out Matrix4x4 invertedWorld);
                    localMatrix = invertedWorld * localMatrix;
                }

                localMatrix.TransformsFromBlenderMatrix(
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

            WeightedBufferAttach[] attaches = new WeightedBufferAttach[weightedAttaches.Length];
            for(int i = 0; i < weightedAttaches.Length; i++)
            {
                MeshStruct mesh = weightedAttaches[i];

                BufferMaterial[] materials = new BufferMaterial[mesh.materials.Length];
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[i] = mesh.materials[i].ToBufferMaterial();
                }

                attaches[i] = WeightedBufferAttach.Create(mesh.vertices, mesh.corners, materials, objNodes);
            }

            WeightedBufferAttach.FromWeightedBuffer(root, attaches, optimize, ignoreWeights, format);

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
