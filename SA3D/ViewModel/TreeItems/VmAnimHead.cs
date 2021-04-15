using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {
            foreach(Motion motion in Animations)
            {
                output.Add(new(parent, new VmAnimation(motion)));
            }
        }

        public void Select(VmTreeItem parent, VmMain main)
        {

        }

        public VmAnimHead(List<Motion> animations)
        {
            Animations = animations;
        }
    }
}
