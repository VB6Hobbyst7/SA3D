using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SAModel.Blender
{
    public static class External
    {
        public static int HookTest(int x)
        {
            return x * 2;
        }

        public static void ExportModel(NodeStruct[] nodes, WeightedBufferAttach[] attaches, string filepath)
        {
            if(nodes.Length == 0)
            {
                throw new InvalidDataException("No nodes passed over");
            }

            ObjectNode[] objNodes = new ObjectNode[nodes.Length];
            for(int i = 0; i < nodes.Length; i++)
            {
                NodeStruct node = nodes[i];
                ObjectNode? parent = node.parentIndex >= 0 ? objNodes[node.parentIndex] : null;
                ObjectNode objNode = new(parent);

            }
        }
    }
}
