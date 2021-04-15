using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmModelHead : BaseViewModel, ITreeItemData
    {
        public NJObject ObjectData { get; }

        public TreeItemType ItemType
            => TreeItemType.ModelHead;

        public string ItemName
            => "Model";

        public bool CanExpand => true;

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {
            output.Add(new(parent, new VmNJObject(ObjectData)));
        }

        public void Select(VmTreeItem parent, VmMain main)
        {

        }

        public VmModelHead(NJObject objectData)
        {
            ObjectData = objectData;
        }
    }
}
