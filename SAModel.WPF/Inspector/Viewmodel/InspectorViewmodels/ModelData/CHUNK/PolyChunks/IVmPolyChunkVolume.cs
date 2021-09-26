using SATools.SAModel.ModelData.CHUNK;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK.PolyChunks
{
    internal class IVmPolyChunkVolume : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkVolume);

        private PolyChunkVolume Chunk
            => (PolyChunkVolume)Source;

        [Tooltip("Chunk Type")]
        public ChunkType Type
        {
            get => Chunk.Type;
            set => _ = Chunk.Type;
        }

        public PolyChunkVolume.IPoly[] Polys
            => Chunk.Polys;

        [DisplayName("User Attributes")]
        public byte UserAttributes
            => Chunk.UserAttributes;

        public IVmPolyChunkVolume() { }

        public IVmPolyChunkVolume(object source) : base(source) { }
    }

    internal class IVmVolumeTriangle : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkVolume.Triangle);

        private PolyChunkVolume.Triangle Triangle
            => (PolyChunkVolume.Triangle)Source;

        public ushort[] Indices
            => Triangle.Indices;

        [DisplayName("User Attributes")]
        public ushort[] UserAttributes
            => Triangle.UserAttributes;

        public IVmVolumeTriangle() { }

        public IVmVolumeTriangle(object source) : base(source) { }
    }

    internal class IVmVolumeQuad : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkVolume.Quad);

        private PolyChunkVolume.Quad Quad
            => (PolyChunkVolume.Quad)Source;

        public ushort[] Indices
            => Quad.Indices;

        [DisplayName("User Attributes")]
        public ushort[] UserAttributes
            => Quad.UserAttributes;

        public IVmVolumeQuad() { }

        public IVmVolumeQuad(object source) : base(source) { }
    }

    internal class IVmVolumeStrip : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkVolume.Strip);

        private PolyChunkVolume.Strip Strip
            => (PolyChunkVolume.Strip)Source;

        public bool Reversed
            => Strip.Reversed;

        public ushort[] Indices
            => Strip.Indices;

        [DisplayName("User Attributes 1")]
        public ushort[] UserAttributes1
            => Strip.UserAttributes1;

        [DisplayName("User Attributes 2")]
        public ushort[] UserAttributes2
            => Strip.UserAttributes2;

        [DisplayName("User Attributes 3")]
        public ushort[] UserAttributes3
            => Strip.UserAttributes3;

        public IVmVolumeStrip() { }

        public IVmVolumeStrip(object source) : base(source) { }
    }
}
