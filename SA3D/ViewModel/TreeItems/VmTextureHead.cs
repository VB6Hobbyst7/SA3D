﻿using SATools.SAArchive;
using System.Collections.Generic;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmTextureHead : ITreeItemData
    {
        public TextureSet Textures { get; set; }

        public TreeItemType ItemType
            => TreeItemType.TextureHead;

        public string ItemName
            => "Textures";

        public bool CanExpand
            => Textures?.Textures.Count > 0;

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();
            foreach (Texture t in Textures.Textures)
                result.Add(new VmTexture(t));
            return result;
        }

        public void Select(VmTreeItem parent)
        {

        }

        public VmTextureHead(TextureSet textureSet)
        {
            Textures = textureSet;
        }
    }
}
