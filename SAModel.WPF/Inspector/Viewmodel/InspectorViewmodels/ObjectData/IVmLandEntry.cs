using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Numerics;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ObjectData
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
                OnPropertyChanged(nameof(ModelBounds));
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
                OnPropertyChanged(nameof(ModelBounds));
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
                OnPropertyChanged(nameof(ModelBounds));
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
                OnPropertyChanged(nameof(ModelBounds));
            }
        }

        [DisplayName("World Matrix")]
        [Tooltip("World transform matrix created from the transform properties")]
        public Matrix4x4 WorldMatrix
            => LandEntry.WorldMatrix;

        [DisplayName("Bounding Sphere")]
        [Tooltip("World transform bounding sphere for culling and collision detection")]
        public Bounds ModelBounds
        {
            get => LandEntry.ModelBounds;
            set => LandEntry.UpdateBounds(value);
        }

        [DisplayName("Surface Attributes")]
        [Tooltip("Geometry rendering and collision information")]
        public SurfaceAttributes SurfaceAttributes
        {
            get => LandEntry.SurfaceAttributes;
            set => LandEntry.SurfaceAttributes = value;
        }

        [DisplayName("SA1 Surface Attributes")]
        [Tooltip("Geometry rendering and collision information for SA1")]
        public SA1SurfaceAttributes SA1SurfaceAttributes
            => LandEntry.SurfaceAttributes.ToSA1();

        [DisplayName("SA2 Surface Attributes")]
        [Tooltip("Geometry rendering and collision information for SA2")]
        public SA2SurfaceAttributes SA2SurfaceAttributes
            => LandEntry.SurfaceAttributes.ToSA2();

        [DisplayName("Rotate ZYX")]
        [Tooltip("Inverted Rotational order")]
        public bool RotateZYX
        {
            get => LandEntry.RotateZYX;
            set => LandEntry.RotateZYX = value;
        }

        [DisplayName("Block Bit")]
        [Tooltip("Block mapping bits")]
        [Hexadecimal]
        public uint BlockBit
        {
            get => LandEntry.BlockBit;
            set => LandEntry.BlockBit = value;
        }

        [DisplayName("Unknown")]
        [Tooltip("Unknown field for SA2 geometry")]
        [Hexadecimal(true)]
        public uint Unknown
        {
            get => LandEntry.Unknown;
            set => LandEntry.Unknown = value;
        }

        public IVmLandEntry() : base() { }

        public IVmLandEntry(LandEntry source) : base(source) { }
    }
}
