using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmGeometry : ITreeItemData
    {
        public LandEntry LandEntry { get; }

        public bool IsCollision { get; }

        public TreeItemType ItemType
            => IsCollision ? TreeItemType.Collision : TreeItemType.Visual;

        public string ItemName
            => LandEntry.Name;

        public bool CanExpand => false;

        public List<ITreeItemData> Expand() => null;

        public void Select(VmTreeItem parent)
        {
            VmMain.Context.ActiveLE = LandEntry;
        }

        public VmGeometry(LandEntry geometry, bool isCollision)
        {
            LandEntry = geometry;
            IsCollision = isCollision;
        }
    }
}
