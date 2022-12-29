using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using System;
using System.Collections.ObjectModel;
using System.Numerics;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ObjectData
{
    internal class IVmNJObject : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(ObjectNode);

        private ObjectNode NJObject
            => (ObjectNode)Source;

        [Tooltip("C label of the NJObject Object")]
        public string Name
        {
            get => NJObject.Name;
            set => NJObject.Name = value;
        }

        [Tooltip("Parent Object")]
        public ObjectNode Parent
        {
            get => NJObject.Parent;
            set => value.AddChild(NJObject);
        }

        [Tooltip("Children objects")]
        public ReadOnlyCollection<ObjectNode> Children
            => NJObject.Children;

        [Tooltip("Mesh information")]
        public Attach Attach
        {
            get => NJObject.Attach;
            set => NJObject.Attach = value;
        }

        [DisplayName("Has Weight")]
        [Tooltip("Whether the model is weighted")]
        public bool HasWeight
            => NJObject.HasWeight;


        [Tooltip("World space position")]
        public Vector3 Position
        {
            get => NJObject.Position;
            set
            {
                NJObject.Position = value;
                OnPropertyChanged(nameof(LocalMatrix));
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [Tooltip("World space euler rotation")]
        public Vector3 Rotation
        {
            get => NJObject.Rotation;
            set
            {
                NJObject.Rotation = value;
                OnPropertyChanged(nameof(QuaternionRotation));
                OnPropertyChanged(nameof(LocalMatrix));
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [DisplayName("Quaternion Rotation")]
        [Tooltip("Quaternion world space euler rotation")]
        public Quaternion QuaternionRotation
        {
            get => NJObject.QuaternionRotation;
            set
            {
                NJObject.QuaternionRotation = value;
                OnPropertyChanged(nameof(Rotation));
                OnPropertyChanged(nameof(LocalMatrix));
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }


        [Tooltip("World space scale")]
        public Vector3 Scale
        {
            get => NJObject.Scale;
            set
            {
                NJObject.Scale = value;
                OnPropertyChanged(nameof(LocalMatrix));
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [DisplayName("Local Matrix")]
        [Tooltip("Local transform matrix created from the transform properties")]
        public Matrix4x4 LocalMatrix
            => NJObject.LocalMatrix;

        [DisplayName("World Matrix")]
        [Tooltip("World transform matrix based on local matrix and parent world matrix")]
        public Matrix4x4 WorldMatrix
            => NJObject.GetWorldMatrix();


        [DisplayName("No Position")]
        public bool NoPosition
        {
            get => NJObject.NoPosition;
            set => NJObject.NoPosition = value;
        }

        [DisplayName("No Rotation")]
        public bool NoRotation
        {
            get => NJObject.NoRotation;
            set => NJObject.NoRotation = value;
        }

        [DisplayName("No Scale")]
        public bool NoScale
        {
            get => NJObject.NoScale;
            set => NJObject.NoScale = value;
        }

        [DisplayName("Skip Draw")]
        public bool SkipDraw
        {
            get => NJObject.SkipDraw;
            set => NJObject.SkipDraw = value;
        }

        [DisplayName("Skip Children")]
        public bool SkipChildren
        {
            get => NJObject.SkipChildren;
            set => NJObject.SkipChildren = value;
        }

        [DisplayName("Rotate ZYX")]
        [Tooltip("Inverted Rotational order (preserve the euler rotation)")]
        public bool RotateZYX
        {
            get => NJObject.RotateZYX;
            set
            {
                NJObject.SetRotationZYX(value, true);
                OnPropertyChanged(nameof(Rotation));
                OnPropertyChanged(nameof(RotateZYXKeep));
            }
        }

        [DisplayName("Rotate ZYX (Keep Rotation)")]
        [Tooltip("Inverted Rotational order. (keep the euler values)")]
        public bool RotateZYXKeep
        {
            get => NJObject.RotateZYX;
            set
            {
                NJObject.SetRotationZYX(value, false);
                OnPropertyChanged(nameof(RotateZYX));
            }
        }

        [DisplayName("Animate")]
        public bool Animate
        {
            get => NJObject.Animate;
            set => NJObject.Animate = value;
        }

        [DisplayName("Morph")]
        public bool Morph
        {
            get => NJObject.Morph;
            set => NJObject.Morph = value;
        }

        public IVmNJObject() : base() { }

        public IVmNJObject(object source) : base(source) { }
    }
}
