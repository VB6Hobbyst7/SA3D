using SATools.SAModel.ModelData.CHUNK;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK
{
    class IVmChunkAttach : IVmAttach
    {
        protected override Type ViewmodelType
            => typeof(ChunkAttach);

        private ChunkAttach Attach
            => (ChunkAttach)Source;

        [DisplayName("Vertex Name")]
        [Tooltip("C Label of the vertex chunk array")]
        public string VertexName
        {
            get => Attach.VertexName;
            set => Attach.VertexName = value;
        }

        [DisplayName("Vertex Chunks")]
        public VertexChunk[] VertexChunks
            => Attach.VertexChunks;

        [DisplayName("Poly Name")]
        [Tooltip("C Label of the poly chunk array")]
        public string PolyName
        {
            get => Attach.PolyName;
            set => Attach.PolyName = value;
        }

        [DisplayName("Poly Chunks")]
        public PolyChunk[] PolyChunks
            => Attach.PolyChunks;

        public IVmChunkAttach() : base() { }

        public IVmChunkAttach(object source) : base(source) { }
    }
}
