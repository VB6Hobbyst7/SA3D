using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel
{
    public interface ITreeItemData
    {
        public TreeItemType ItemType { get; }

        public string ItemName { get; }

        public bool CanExpand { get; }

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output);

        public void Select(VmTreeItem parent, VmMain main);
    }
}
