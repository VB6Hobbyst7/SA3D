using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.CHUNK;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK.PolyChunks
{
    internal class IVmPolyChunkNull : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkNull);

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => ChunkType.Null;

        public IVmPolyChunkNull() { }

        public IVmPolyChunkNull(object source) : base(source) { }
    }

    internal class IVmPolyChunkEnd : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkEnd);

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => ChunkType.End;

        public IVmPolyChunkEnd() { }

        public IVmPolyChunkEnd(object source) : base(source) { }
    }

    internal class IVmPolyChunkBlend : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkBlendAlpha);

        private PolyChunkBlendAlpha Chunk
            => (PolyChunkBlendAlpha)Source;

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => Chunk.Type;

        [DisplayName("Source Alpha")]
        public BlendMode SourceAlpha
        {
            get => Chunk.SourceAlpha;
            set => Chunk.SourceAlpha = value;
        }

        [DisplayName("Destination Alpha")]
        public BlendMode DestinationAlpha
        {
            get => Chunk.DestinationAlpha;
            set => Chunk.DestinationAlpha = value;
        }

        public IVmPolyChunkBlend() { }

        public IVmPolyChunkBlend(object source) : base(source) { }
    }

    internal class IVmPolyChunksMipmapDAdjust : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunksMipmapDAdjust);

        private PolyChunksMipmapDAdjust Chunk
            => (PolyChunksMipmapDAdjust)Source;

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => Chunk.Type;

        [DisplayName("Mipmap Distance Adjust")]
        public float MipmapDAdjust
        {
            get => Chunk.MipmapDAdjust;
            set => Chunk.MipmapDAdjust = value;
        }

        public IVmPolyChunksMipmapDAdjust() { }

        public IVmPolyChunksMipmapDAdjust(object source) : base(source) { }
    }

    internal class IVmPolyChunkSpecularExponent : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkSpecularExponent);

        private PolyChunkSpecularExponent Chunk
            => (PolyChunkSpecularExponent)Source;

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => Chunk.Type;

        [DisplayName("Specular Exponent")]
        public byte SpecularExponent
        {
            get => Chunk.SpecularExponent;
            set => Chunk.SpecularExponent = value;
        }

        public IVmPolyChunkSpecularExponent() { }

        public IVmPolyChunkSpecularExponent(object source) : base(source) { }
    }

    internal class IVmPolyChunkCachePolygonList : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkCachePolygonList);

        private PolyChunkCachePolygonList Chunk
            => (PolyChunkCachePolygonList)Source;

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => Chunk.Type;

        [DisplayName("List Index")]
        public byte List
        {
            get => Chunk.List;
            set => Chunk.List = value;
        }

        public IVmPolyChunkCachePolygonList() { }

        public IVmPolyChunkCachePolygonList(object source) : base(source) { }
    }

    internal class IVmPolyChunkDrawPolygonList : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkDrawPolygonList);

        private PolyChunkDrawPolygonList Chunk
            => (PolyChunkDrawPolygonList)Source;

        [DisplayName("Chunk Type")]
        public ChunkType ChunkType
            => Chunk.Type;

        [DisplayName("List Index")]
        public byte List
        {
            get => Chunk.List;
            set => Chunk.List = value;
        }

        public IVmPolyChunkDrawPolygonList() { }

        public IVmPolyChunkDrawPolygonList(object source) : base(source) { }
    }
}
