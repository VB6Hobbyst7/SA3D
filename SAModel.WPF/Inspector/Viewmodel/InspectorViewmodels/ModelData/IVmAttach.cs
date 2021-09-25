using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using SATools.SAWPF.BaseViewModel;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData
{
    internal class IVmAttach : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(Attach);

        private Attach Attach
            => (Attach)_source;

        [Tooltip("C label of the LandTable")]
        public string Name
        {
            get => Attach.Name;
            set => Attach.Name = value;
        }

        [Tooltip("Format of the attach")]
        public AttachFormat Format
            => Attach.Format;

        [DisplayName("Buffer mesh data")]
        [Tooltip("Buffer/Intermediate data for rendering and conversion")]
        public BufferMesh[] MeshData
        {
            get => Attach.MeshData;
            set => Attach.MeshData = value;
        }

        [DisplayName("Buffer has opaque")]
        [Tooltip("Whether the buffer mesh data contains opaque meshes")]
        public bool BufferHasOpaque
            => Attach.BufferHasOpaque;

        [DisplayName("Buffer has transparent")]
        [Tooltip("Whether the buffer mesh data contains transparent meshes")]
        public bool BufferHasTransparent
            => Attach.BufferHasTransparent;

        [DisplayName("Has weight")]
        [Tooltip("Whether the attach depends on vertex information by other attaches")]
        public bool HasWeight
            => Attach.HasWeight;

        [DisplayName("Mesh bounds")]
        [Tooltip("The meshes bounds")]
        public Bounds MeshBounds
        {
            get => Attach.MeshBounds;
            set => Attach.MeshBounds = value;
        }

        [DisplayName("Recalculate bounds")]
        [Tooltip("Uses vertex data of the attach to determine tight bounds")]
        public RelayCommand CmdRecalculateBounds
            => new(RecalculateBounds);

        public IVmAttach() : base() { }

        public IVmAttach(Attach source) : base(source)
        {

        }

        private void RecalculateBounds()
        {
            Attach.RecalculateBounds();
            OnPropertyChanged(nameof(MeshBounds));
        }
    }
}
