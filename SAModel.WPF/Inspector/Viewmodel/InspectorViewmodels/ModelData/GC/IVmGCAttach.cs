using SATools.SAModel.ModelData.GC;
using System;
using System.Linq;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.GC
{
    internal class IVmGCAttach : IVmAttach
    {
        protected override Type ViewmodelType
            => typeof(GCAttach);

        private GCAttach Attach
            => (GCAttach)_source;


        [DisplayName("Vertex Data")]
        [Tooltip("Vertex data for assembling the polygons")]
        public VertexSet[] VertexData { get; }

        [DisplayName("Opaque Meshes")]
        [Tooltip("Opaque Polygon information batch")]
        public Mesh[] OpaqueMeshes
            => Attach.OpaqueMeshes;

        [DisplayName("Transparent Meshes")]
        [Tooltip("Transparent Polygon information batch")]
        public Mesh[] TransparentMeshes
            => Attach.TransparentMeshes;

        public IVmGCAttach() : base() { }

        public IVmGCAttach(GCAttach source) : base(source)
        {
            VertexData = Attach.VertexData.Values.ToArray();
        }
    }
}
