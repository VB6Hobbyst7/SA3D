using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.ObjectData.Animation;
using System.Collections.Generic;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmAnimHead : BaseViewModel, ITreeItemData
    {
        public List<Motion> Animations { get; }

        public TreeItemType ItemType
            => TreeItemType.AnimationHead;

        public string ItemName
            => "Animations";

        public bool CanExpand => Animations.Count > 0;

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();
            foreach (Motion motion in Animations)
            {
                result.Add(new VmAnimation(motion));
            }
            return result;
        }

        public void Select(VmTreeItem parent)
        {

        }

        public VmAnimHead(List<Motion> animations)
        {
            Animations = animations;
        }
    }
}
