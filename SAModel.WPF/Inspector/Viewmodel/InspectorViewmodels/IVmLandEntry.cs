using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using System;
using System.Numerics;

namespace SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels
{
    internal class IVmLandEntry : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(LandEntry);

        private LandEntry LandEntry
            => (LandEntry)_source;

        [Tooltip("C label of the LandEntry Object")]
        public string Name
        {
            get => LandEntry.Name;
            set => LandEntry.Name = value;
        }

        [Tooltip("Mesh information")]
        public Attach Attach
        {
            get => LandEntry.Attach;
            set => LandEntry.Attach = value;
        }

        [Tooltip("World space position")]
        public Vector3 Position
        {
            get => LandEntry.Position;
            set
            {
                LandEntry.Position = value;
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [Tooltip("World space euler rotation")]
        public Vector3 Rotation
        {
            get => LandEntry.Rotation;
            set
            {
                LandEntry.Rotation = value;
                OnPropertyChanged(nameof(QuaternionRotation));
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [DisplayName("Quaternion Rotation")]
        [Tooltip("Quaternion world space euler rotation")]
        public Quaternion QuaternionRotation
        {
            get => LandEntry.QuaternionRotation;
            set
            {
                LandEntry.QuaternionRotation = value;
                OnPropertyChanged(nameof(Rotation));
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [Tooltip("World space scale")]
        public Vector3 Scale
        {
            get => LandEntry.Scale;
            set
            {
                LandEntry.Scale = value;
                OnPropertyChanged(nameof(WorldMatrix));
            }
        }

        [DisplayName("World Matrix")]
        [Tooltip("World transform matrix created from the transform properties")]
        public Matrix4x4 WorldMatrix
            => LandEntry.WorldMatrix;

        public IVmLandEntry() : base() { }

        public IVmLandEntry(object source) : base(source) { }
    }
}
