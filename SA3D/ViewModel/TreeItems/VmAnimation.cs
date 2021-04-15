using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.Graphics;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {
            
        }

        public void Select(VmTreeItem parent, VmMain main)
        {
            if(((VmObject)parent.Parent.Data).TaskData is DebugTask dbtsk)
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
