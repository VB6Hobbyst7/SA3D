using System;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Base class for poly chunks of the bits type
    /// </summary>
    public abstract class PolyChunkBits : PolyChunk
    {
        protected PolyChunkBits(ChunkType type) : base(type) { }

        public override uint ByteSize => 2;
    }

    /// <summary>
    /// Chunk that doesnt contain anything
    /// </summary>
    public class PolyChunkNull : PolyChunkBits
    {
        public PolyChunkNull() : base(ChunkType.Null) { }
    }

    /// <summary>
    /// Chunk to mark an end
    /// </summary>
    public class PolyChunkEnd : PolyChunkBits
    {
        public PolyChunkEnd() : base(ChunkType.End) { }
    }

    /// <summary>
    /// Sets the blendmode of the following strip chunks
    /// </summary>
    public class PolyChunkBlendAlpha : PolyChunkBits
    {
        /// <summary>
        /// Source blendmode
        /// </summary>
        public BlendMode SourceAlpha
        {
            get => (BlendMode)((Attributes >> 3) & 7);
            set => Attributes = (byte)((Attributes & ~0x38) | ((byte)value << 3));
        }

        /// <summary>
        /// Destination blendmode
        /// </summary>
        public BlendMode DestinationAlpha
        {
            get => (BlendMode)(Attributes & 7);
            set => Attributes = (byte)((Attributes & ~7) | (byte)value);
        }

        public PolyChunkBlendAlpha() : base(ChunkType.Bits_BlendAlpha) { }

        public override string ToString()
            => $"BlendAlpha - {SourceAlpha} -> {DestinationAlpha}";

    }

    /// <summary>
    /// Adjusts the mipmap distance of the following strip chunks
    /// </summary>
    public class PolyChunksMipmapDAdjust : PolyChunkBits
    {
        /// <summary>
        /// The mipmap distance adjust <br/>
        /// Ranges from 0 to 3.75f in 0.25-steps
        /// </summary>
        public float MipmapDAdjust
        {
            get => (Attributes & 0xF) * 0.25f;
            set => Attributes = (byte)((Attributes & 0xF0) | (byte)Math.Max(0, Math.Min(0xF, Math.Round(value / 0.25, MidpointRounding.AwayFromZero))));
        }

        public PolyChunksMipmapDAdjust() : base(ChunkType.Bits_MipmapDAdjust) { }

        public override string ToString()
            => $"{Type} - {MipmapDAdjust}";
    }

    /// <summary>
    /// Sets the specular exponent of the following strip chunks
    /// </summary>
    public class PolyChunkSpecularExponent : PolyChunkBits
    {
        /// <summary>
        /// Specular exponent <br/>
        /// Ranges from 0 to 16
        /// </summary>
        public byte SpecularExponent
        {
            get => (byte)(Attributes & 0x1F);
            set => Attributes = (byte)((Attributes & ~0x1F) | Math.Min(value, (byte)16));
        }

        public PolyChunkSpecularExponent() : base(ChunkType.Bits_SpecularExponent) { }

        public override string ToString()
            => $"{Type} - {SpecularExponent}";
    }

    /// <summary>
    /// Caches the following polygon chunks of the current attach into specified index
    /// </summary>
    public class PolyChunkCachePolygonList : PolyChunkBits
    {
        /// <summary>
        /// Cache ID
        /// </summary>
        public byte List
        {
            get => Attributes;
            set => Attributes = value;
        }

        public PolyChunkCachePolygonList() : base(ChunkType.Bits_CachePolygonList) { }

        public override string ToString()
            => $"{Type} - {List}";
    }

    /// <summary>
    /// Draws the polygon chunks cached by a specific index
    /// </summary>
    public class PolyChunkDrawPolygonList : PolyChunkBits
    {
        /// <summary>
        /// Cache ID
        /// </summary>
        public byte List
        {
            get => Attributes;
            set => Attributes = value;
        }

        public PolyChunkDrawPolygonList() : base(ChunkType.Bits_DrawPolygonList) { }

        public override string ToString()
            => $"{Type} - {List}";
    }
}
