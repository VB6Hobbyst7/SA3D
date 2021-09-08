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

        public bool CanExpand
            => false;

        public List<ITreeItemData> Expand()
            => null;


        public void Select(VmTreeItem parent)
        {

        }

        public VmTexture(Texture texture)
        {
            Texture = texture;
        }
    }
}
