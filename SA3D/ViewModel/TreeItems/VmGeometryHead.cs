using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmGeometryHead : ITreeItemData
    {
        public LandEntry[] Geometry { get; set; }

        public bool IsCollision { get; }

        public TreeItemType ItemType
            => IsCollision ? TreeItemType.CollisionHead : TreeItemType.VisualHead;

        public string ItemName
            => IsCollision ? "Collision Geometry" : "Visual Geometry";

        public bool CanExpand
            => Geometry.Length > 0;

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();

            foreach(var vsg in Geometry)
            {
                result.Add(new VmGeometry(vsg, IsCollision));
            }

            return result;
        }

        public void Select(VmTreeItem parent)
        {

        }

        public VmGeometryHead(LandEntry[] geometry, bool collision)
        {
            IsCollision = collision;
            Geometry = geometry;
        }
    }
}
