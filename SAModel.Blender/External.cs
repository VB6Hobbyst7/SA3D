using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.Weighted;
using SATools.SAModel.ObjectData;
using SATools.SAModel.Structs;
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

            Node[] objNodes = new Node[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                NodeStruct node = nodes[i];

                Matrix4x4 localMatrix = node.worldMatrix;
                if (node.parentIndex >= 0)
                {
                    Matrix4x4.Invert(nodes[node.parentIndex].worldMatrix, out Matrix4x4 invertedWorld);
                    localMatrix *= invertedWorld;
                }

                Matrix4x4.Decompose(localMatrix, out Vector3 scale, out Quaternion rotation, out Vector3 position);
                Vector3 euler = rotation.ToEuler(node.attributes.HasFlag(NodeAttributes.RotateZYX));

                Node? parent = node.parentIndex >= 0 ? objNodes[node.parentIndex] : null;
                Node objNode = new(parent)
                {
                    Name = node.name,
                    Position = position,
                    Rotation = euler,
                    Scale = scale
                };

                objNode.SetAllObjectAttributes(node.attributes, true);

                objNodes[i] = objNode;
            }
            Node root = objNodes[0];

            WeightedBufferAttach[] attaches = new WeightedBufferAttach[weightedAttaches.Length];
            for (int i = 0; i < weightedAttaches.Length; i++)
            {
                MeshStruct mesh = weightedAttaches[i];

                BufferMaterial[] materials = new BufferMaterial[mesh.materials.Length];
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = mesh.materials[j].ToBufferMaterial();
                }

                attaches[i] = WeightedBufferAttach.Create(mesh.vertices, mesh.corners, materials, objNodes);
            }

            WeightedBufferAttach.FromWeightedBuffer(root, attaches, optimize, ignoreWeights, format);

            File.WriteAllBytes(filepath, ModelFile.Write(format, njFile, root));
        }
    }
}
