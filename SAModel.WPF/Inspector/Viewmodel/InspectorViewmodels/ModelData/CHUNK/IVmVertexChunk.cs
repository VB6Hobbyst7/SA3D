using SATools.SAModel.ModelData.CHUNK;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK
{
    internal class IVmVertexChunk : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(VertexChunk);

        private VertexChunk Chunk
            => (VertexChunk)Source;

        public ChunkType Type
            => Chunk.Type;

        [Hexadecimal]
        public byte Attributes
            => Chunk.Attributes;

        [DisplayName("Weight Status")]
        public WeightStatus WeightStatus 
            => Chunk.WeightStatus;

        public ChunkVertex[] Vertices
            => Chunk.Vertices;

        public IVmVertexChunk() : base() { }

        public IVmVertexChunk(object source) : base(source) { }
    }
}
