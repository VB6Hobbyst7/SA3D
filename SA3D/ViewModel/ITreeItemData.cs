using System.Collections.Generic;

namespace SATools.SA3D.ViewModel
{
    public interface ITreeItemData
    {
        public TreeItemType ItemType { get; }

        public string ItemName { get; }

        public bool CanExpand { get; }

        public List<ITreeItemData> Expand();

        public void Select(VmTreeItem parent);
    }
}
