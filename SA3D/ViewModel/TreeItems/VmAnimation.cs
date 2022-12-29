using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.Graphics;
using SATools.SAModel.ObjectData.Animation;
using System.Collections.Generic;

namespace SATools.SA3D.ViewModel.TreeItems
{
    class VmAnimation : BaseViewModel, ITreeItemData
    {
        public Motion Animation { get; }

        public TreeItemType ItemType
            => TreeItemType.Animation;

        public string ItemName
            => Animation.Name;

        public bool CanExpand => false;

        public List<ITreeItemData> Expand() => null;

        public void Select(VmTreeItem parent)
        {
            if (((VmObject)parent.Parent.Data).TaskData is DebugTask dbtsk)
            {
                dbtsk.MotionIndex = dbtsk.Motions.IndexOf(Animation);
            }
        }

        public VmAnimation(Motion animation)
        {
            Animation = animation;
        }
    }
}
