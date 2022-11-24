using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.ObjData;
using System.Collections.Generic;

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

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();
            result.Add(new VmNJObject(ObjectData));
            return result;
        }

        public void Select(VmTreeItem parent)
        {

        }

        public VmModelHead(NJObject objectData)
        {
            ObjectData = objectData;
        }
    }
}
