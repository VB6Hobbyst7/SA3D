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
    public class VmNJObject : BaseViewModel, ITreeItemData
    {
        public NJObject ObjectData { get; }

        public TreeItemType ItemType
            => TreeItemType.Model;

        public string ItemName
            => ObjectData.Name;

        public bool CanExpand 
            => ObjectData.ChildCount > 0;

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();
            for(int i = 0; i < ObjectData.ChildCount; i++)
                result.Add(new VmNJObject(ObjectData[i]));
            return result;
        }

        public void Select(VmTreeItem parent) 
            => VmMain.Context.ActiveNJO = ObjectData;

        public VmNJObject(NJObject objectData)
        {
            ObjectData = objectData;
        }
    }
}
