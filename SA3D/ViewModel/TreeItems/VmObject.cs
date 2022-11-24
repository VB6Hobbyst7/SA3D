using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.Graphics;
using SATools.SAModel.ObjData;
using System.Collections.Generic;

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

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();
            if (TaskData is DisplayTask dtsk)
            {
                result.Add(new VmModelHead(dtsk.Model));
                result.Add(new VmTextureHead(dtsk.TextureSet));

                if (TaskData is DebugTask dbtsk)
                    result.Add(new VmAnimHead(dbtsk.Motions));
            }
            return result;
        }

        public void Select(VmTreeItem parent)
        {

        }

        public VmObject(GameTask taskData, ModelFile modelFileInfo)
        {
            TaskData = taskData;
            ModelFileInfo = modelFileInfo;
        }
    }
}
