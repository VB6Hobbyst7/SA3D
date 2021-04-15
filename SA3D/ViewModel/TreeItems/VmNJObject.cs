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

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {
            for(int i = 0; i < ObjectData.ChildCount; i++)
            {
                output.Add(new(parent, new VmNJObject(ObjectData[i])));
            }
        }

        public void Select(VmTreeItem parent, VmMain main) 
            => main.RenderContext.ActiveNJO = ObjectData;

        public VmNJObject(NJObject objectData)
        {
            ObjectData = objectData;
        }
    }
}
