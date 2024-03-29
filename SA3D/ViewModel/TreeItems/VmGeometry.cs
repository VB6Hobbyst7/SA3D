﻿using SATools.SAModel.ObjectData;
using System.Collections.Generic;

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
