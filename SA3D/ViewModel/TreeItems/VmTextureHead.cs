using SATools.SAArchive;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmTextureHead : ITreeItemData
    {
        public TextureSet Textures { get; set; }

        public TreeItemType ItemType 
            => TreeItemType.TextureHead;

        public string ItemName
            => "Textures";

        public bool CanExpand => Textures?.Textures.Count > 0;

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {
            foreach(Texture t in Textures.Textures)
            {
                output.Add(new(parent, new VmTexture(t)));
            }
        }

        public void Select(VmTreeItem parent, VmMain main)
        {

        }

        public VmTextureHead(TextureSet textureSet)
        {
            Textures = textureSet;
        }
    }
}
