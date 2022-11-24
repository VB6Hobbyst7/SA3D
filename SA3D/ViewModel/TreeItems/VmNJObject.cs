using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.ObjData;
using System.Collections.Generic;
using System.Numerics;

namespace SATools.SA3D.ViewModel.TreeItems
{
    public class VmNJObject : BaseViewModel, ITreeItemData
    {
        public ObjectNode ObjectData { get; }

        #region Transform wrappers

        public float PositionX
        {
            get => ObjectData.Position.X;
            set
            {
                Vector3 v = ObjectData.Position;
                v.X = value;
                ObjectData.Position = v;
            }
        }

        public float PositionY
        {
            get => ObjectData.Position.Y;
            set
            {
                Vector3 v = ObjectData.Position;
                v.Y = value;
                ObjectData.Position = v;
            }
        }

        public float PositionZ
        {
            get => ObjectData.Position.Z;
            set
            {
                Vector3 v = ObjectData.Position;
                v.Z = value;
                ObjectData.Position = v;
            }
        }


        public float RotationX
        {
            get => ObjectData.Rotation.X;
            set
            {
                Vector3 v = ObjectData.Rotation;
                v.X = value;
                ObjectData.Rotation = v;
            }
        }

        public float RotationY
        {
            get => ObjectData.Rotation.Y;
            set
            {
                Vector3 v = ObjectData.Rotation;
                v.Y = value;
                ObjectData.Rotation = v;
            }
        }

        public float RotationZ
        {
            get => ObjectData.Rotation.Z;
            set
            {
                Vector3 v = ObjectData.Rotation;
                v.Z = value;
                ObjectData.Rotation = v;
            }
        }


        public float ScaleX
        {
            get => ObjectData.Scale.X;
            set
            {
                Vector3 v = ObjectData.Scale;
                v.X = value;
                ObjectData.Scale = v;
            }
        }

        public float ScaleY
        {
            get => ObjectData.Scale.Y;
            set
            {
                Vector3 v = ObjectData.Scale;
                v.Y = value;
                ObjectData.Scale = v;
            }
        }

        public float ScaleZ
        {
            get => ObjectData.Scale.Z;
            set
            {
                Vector3 v = ObjectData.Scale;
                v.Z = value;
                ObjectData.Scale = v;
            }
        }

        #endregion

        public TreeItemType ItemType
            => TreeItemType.Model;

        public string ItemName
            => ObjectData.Name;

        public bool CanExpand
            => ObjectData.ChildCount > 0;

        public List<ITreeItemData> Expand()
        {
            List<ITreeItemData> result = new();
            for (int i = 0; i < ObjectData.ChildCount; i++)
                result.Add(new VmNJObject(ObjectData[i]));
            return result;
        }

        public void Select(VmTreeItem parent)
            => VmMain.Context.ActiveNJO = ObjectData;

        public VmNJObject(ObjectNode objectData)
        {
            ObjectData = objectData;
        }
    }
}
