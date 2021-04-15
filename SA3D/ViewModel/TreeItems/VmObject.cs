using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.Graphics;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmObject : BaseViewModel, ITreeItemData
    {
        public TreeItemType ItemType
            => TreeItemType.Object;

        public string ItemName
            => TaskData.Name;

        public bool CanExpand => true;

        public GameTask TaskData { get; }

        public ModelFile ModelFileInfo { get; }

        public void Expand(VmTreeItem parent, ObservableCollection<VmTreeItem> output)
        {
            if(TaskData is DisplayTask dtsk)
            {
                output.Add(new(parent, new VmModelHead(dtsk.Model)));
                output.Add(new(parent, new VmTextureHead(dtsk.TextureSet)));

                if(TaskData is DebugTask dbtsk)
                    output.Add(new(parent, new VmAnimHead(dbtsk.Motions)));
            }
        }

        public void Select(VmTreeItem parent, VmMain main)
        {

        }

        public VmObject(GameTask taskData, ModelFile modelFileInfo)
        {
            TaskData = taskData;
            ModelFileInfo = modelFileInfo;
        }
    }
}
