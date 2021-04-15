using SATools.SAArchive;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmTexture : ITreeItemData
    {
        public Texture Texture { get; set; }

        public TreeItemType ItemType
            => TreeItemType.Texture;

        public string ItemName
            => Texture.Name;

        public bool CanExpand => false;

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {

        }

        public void Select(VmTreeItem parent, VmMain main)
        {

        }

        public VmTexture(Texture texture)
        {
            Texture = texture;
        }
    }
}
