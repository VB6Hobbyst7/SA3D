using SATools.SAArchive;
using System.Collections.Generic;

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
