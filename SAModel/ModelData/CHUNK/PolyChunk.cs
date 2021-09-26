using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Polychunk base class
    /// </summary>
    public abstract class PolyChunk : ICloneable
    {
        /// <summary>
        /// Type of the polychunk
        /// </summary>
        public ChunkType Type { get; protected set; }

        /// <summary>
        /// Attribute byte of the poly chunk
        /// </summary>
        public byte Attributes { get; set; }

        /// <summary>
        /// Size of the chunk in bytes
        /// </summary>
        public abstract uint ByteSize { get; }

        protected PolyChunk(ChunkType type)
        {
            if(type.IsVertex())
                throw new ArgumentException("Chunktype has to be ");
            Type = type;
        }

        /// <summary>
        /// Reads a poly chunk from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the poly chunk is located</param>
        /// <returns></returns>
        public static PolyChunk Read(byte[] source, ref uint address)
        {
            ushort header = source.ToUInt16(address);
            ChunkType type = (ChunkType)(header & 0xFF);
            byte attribs = (byte)(header >> 8);

            PolyChunk chunk;
            switch(type)
            {
                case ChunkType.Null:
                    chunk = new PolyChunkNull();
                    break;
                case ChunkType.End:
                    chunk = new PolyChunkEnd();
                    break;
                case ChunkType.Bits_BlendAlpha:
                    chunk = new PolyChunkBlendAlpha();
                    break;
                case ChunkType.Bits_MipmapDAdjust:
                    chunk = new PolyChunksMipmapDAdjust();
                    break;
                case ChunkType.Bits_SpecularExponent:
                    chunk = new PolyChunkSpecularExponent();
                    break;
                case ChunkType.Bits_CachePolygonList:
                    chunk = new PolyChunkCachePolygonList();
                    break;
                case ChunkType.Bits_DrawPolygonList:
                    chunk = new PolyChunkDrawPolygonList();
                    break;
                case ChunkType.Tiny_TextureID:
                case ChunkType.Tiny_TextureID2:
                    chunk = PolyChunkTextureID.Read(source, address);
                    break;
                case ChunkType.Material:
                case ChunkType.Material_Diffuse:
                case ChunkType.Material_Ambient:
                case ChunkType.Material_DiffuseAmbient:
                case ChunkType.Material_Specular:
                case ChunkType.Material_DiffuseSpecular:
                case ChunkType.Material_AmbientSpecular:
                case ChunkType.Material_DiffuseAmbientSpecular:
                case ChunkType.Material_Diffuse2:
                case ChunkType.Material_Ambient2:
                case ChunkType.Material_DiffuseAmbient2:
                case ChunkType.Material_Specular2:
                case ChunkType.Material_DiffuseSpecular2:
                case ChunkType.Material_AmbientSpecular2:
                case ChunkType.Material_DiffuseAmbientSpecular2:
                    chunk = PolyChunkMaterial.Read(source, address);
                    break;
                case ChunkType.Material_Bump:
                    chunk = PolyChunkMaterialBump.Read(source, address);
                    break;
                case ChunkType.Volume_Polygon3:
                case ChunkType.Volume_Polygon4:
                case ChunkType.Volume_Strip:
                    chunk = PolyChunkVolume.Read(source, address);
                    break;
                case ChunkType.Strip_Strip:
                case ChunkType.Strip_StripUVN:
                case ChunkType.Strip_StripUVH:
                case ChunkType.Strip_StripNormal:
                case ChunkType.Strip_StripUVNNormal:
                case ChunkType.Strip_StripUVHNormal:
                case ChunkType.Strip_StripColor:
                case ChunkType.Strip_StripUVNColor:
                case ChunkType.Strip_StripUVHColor:
                case ChunkType.Strip_Strip2:
                case ChunkType.Strip_StripUVN2:
                case ChunkType.Strip_StripUVH2:
                    chunk = PolyChunkStrip.Read(source, address);
                    break;
                default:
                    throw new InvalidOperationException($"Chunk {type} is not a valid poly chunk type at {address.ToString("X8")}");
            }
            chunk.Attributes = attribs;
            address += chunk.ByteSize;
            return chunk;
        }

        /// <summary>
        /// Write the chunk to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public virtual void Write(EndianWriter writer)
        {
            writer.WriteUInt16((ushort)((byte)Type | (ushort)(Attributes << 8)));
        }

        object ICloneable.Clone() => Clone();

        public virtual PolyChunk Clone() => (PolyChunk)MemberwiseClone();

        public override string ToString()
            => Type.ToString();
    }

    /// <summary>
    /// Sets texture information of the following strip chunks
    /// </summary>
    public class PolyChunkTextureID : PolyChunk
    {
        public override uint ByteSize => 4;

        /// <summary>
        /// Whether the chunktype is TextureID2
        /// </summary>
        public bool Second
        {
            get => Type == ChunkType.Tiny_TextureID2;
            set => Type = value ? ChunkType.Tiny_TextureID2 : ChunkType.Tiny_TextureID;
        }

        /// <summary>
        /// The mipmap distance adjust <br/>
        /// Ranges from 0 to 3.75f in 0.25-steps
        /// </summary>
        public float MipmapDAdjust
        {
            get => (Attributes & 0xF) * 0.25f;
            set => Attributes = (byte)((Attributes & 0xF0) | (byte)Math.Max(0, Math.Min(0xF, Math.Round(value / 0.25, MidpointRounding.AwayFromZero))));
        }

        /// <summary>
        /// Clamps the texture v axis between 0 and 1
        /// </summary>
        public bool ClampV
        {
            get => (Attributes & 0x10) != 0;
            set => _ = value ? Attributes |= 0x10 : Attributes &= 0xEF;
        }

        /// <summary>
        /// Clamps the texture u axis between 0 and 1
        /// </summary>
        public bool ClampU
        {
            get => (Attributes & 0x20) != 0;
            set => _ = value ? Attributes |= 0x20 : Attributes &= 0xDF;
        }

        /// <summary>
        /// Mirrors the texture every second time the texture is repeated along the v axis
        /// </summary>
        public bool MirrorV
        {
            get => (Attributes & 0x40) != 0;
            set => _ = value ? Attributes |= 0x40 : Attributes &= 0xBF;
        }

        /// <summary>
        /// Mirrors the texture every second time the texture is repeated along the u axis
        /// </summary>
        public bool MirrorU
        {
            get => (Attributes & 0x80) != 0;
            set => _ = value ? Attributes |= 0x80 : Attributes &= 0x7F;
        }


        /// <summary>
        /// Second data short for the texture chunk
        /// </summary>
        public ushort Data { get; private set; }

        /// <summary>
        /// Texture ID to use
        /// </summary>
        public ushort TextureID
        {
            get => (ushort)(Data & 0x1FFFu);
            set => Data = (ushort)((Data & ~0x1FFF) | Math.Min(value, (ushort)0x1FFF));
        }

        /// <summary>
        /// Whether to use super sampling (anisotropic filtering)
        /// </summary>
        public bool SuperSample
        {
            get => (Data & 0x2000) != 0;
            set => _ = value ? Data |= 0x2000 : Data &= 0xDFFF;
        }

        /// <summary>
        /// Texture filtermode 
        /// </summary>
        public FilterMode FilterMode
        {
            get => (FilterMode)(Data >> 14);
            set => Data = (ushort)((Data & ~0xC000) | ((ushort)value << 14));
        }

        public PolyChunkTextureID(bool second) : base(second ? ChunkType.Tiny_TextureID2 : ChunkType.Tiny_TextureID) { }

        /// <summary>
        /// Reads a texture chunk from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the chunk is located</param>
        /// <returns></returns>
        public static PolyChunkTextureID Read(byte[] source, uint address)
        {
            ushort header = source.ToUInt16(address);
            ChunkType type = (ChunkType)(header & 0xFF);
            byte attribs = (byte)(header >> 8);
            ushort data = source.ToUInt16(address + 2);

            return new PolyChunkTextureID(type == ChunkType.Tiny_TextureID2)
            {
                Attributes = attribs,
                Data = data
            };
        }

        public override void Write(EndianWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt16(Data);
        }

        public override string ToString()
            => $"{Type} - {TextureID}";
    }

    /// <summary>
    /// Base class for all chunk types that also hold a size value
    /// </summary>
    public abstract class PolyChunkSize : PolyChunk
    {
        /// <summary>
        /// Amount of shorts in the chunk
        /// </summary>
        public virtual ushort Size { get; protected set; }

        public override uint ByteSize => Size * 2u + 4u;

        public PolyChunkSize(ChunkType type) : base(type) { }

        public override void Write(EndianWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt16(Size);
        }
    }

    /// <summary>
    /// Material information for the following strip chunks
    /// </summary>
    public class PolyChunkMaterial : PolyChunkSize
    {
        private Color? _diffuse;
        private Color? _ambient;
        private Color? _specular;

        /// <summary>
        /// Whether the material type is a second type
        /// </summary>
        public bool Second
        {
            get => ((byte)Type & 0x08) != 0;
            set => TypeAttribute(0x08, value);
        }

        public override ushort Size
        {
            get
            {
                ushort val = 0;
                if(((byte)Type & 0x01) != 0)
                    val += 2;
                if(((byte)Type & 0x02) != 0)
                    val += 2;
                if(((byte)Type & 0x04) != 0)
                    val += 2;
                return val;
            }
        }

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

        /// <summary>
        /// Diffuse color
        /// </summary>
        public Color? Diffuse
        {
            get => _diffuse;
            set
            {
                TypeAttribute(0x01, value.HasValue);
                _diffuse = value;
            }
        }

        /// <summary>
        /// Ambient color
        /// </summary>
        public Color? Ambient
        {
            get => _ambient;
            set
            {
                TypeAttribute(0x02, value.HasValue);
                _ambient = value;
            }
        }

        /// <summary>
        /// Specular color
        /// </summary>
        public Color? Specular
        {
            get => _specular;
            set
            {
                TypeAttribute(0x04, value.HasValue);
                _specular = value;
            }
        }

        /// <summary>
        /// Specular exponent <br/>
        /// Requires <see cref="Specular"/> to be set
        /// </summary>
        public byte SpecularExponent { get; set; }

        public PolyChunkMaterial() : base(ChunkType.Material) { }

        private void TypeAttribute(byte val, bool state)
        {
            byte type = (byte)Type;
            if(state)
                Type = (ChunkType)(byte)(type | val);
            else
                Type = (ChunkType)(byte)(type & ~val);
        }

        /// <summary>
        /// Reads a material chunk from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the chunk is located</param>
        /// <returns></returns>
        public static PolyChunkMaterial Read(byte[] source, uint address)
        {
            ushort header = source.ToUInt16(address);
            ChunkType type = (ChunkType)(header & 0xFF);
            byte attribs = (byte)(header >> 8);
            address += 4;

            PolyChunkMaterial mat = new()
            {
                Attributes = attribs
            };

            if(((byte)type & 0x01) != 0)
            {
                mat.Diffuse = Color.Read(source, ref address, IOType.ARGB8_16);
            }

            if(((byte)type & 0x02) != 0)
            {
                mat.Ambient = Color.Read(source, ref address, IOType.ARGB8_16);
            }

            if(((byte)type & 0x04) != 0)
            {
                Color spec = Color.Read(source, ref address, IOType.ARGB8_16);
                mat.SpecularExponent = spec.A;
                spec.A = 255;
                mat.Specular = spec;
            }

            mat.Second = ((byte)type & 0x08) != 0;

            return mat;
        }

        public override void Write(EndianWriter writer)
        {
            base.Write(writer);
            if(_diffuse.HasValue)
                _diffuse.Value.Write(writer, IOType.ARGB8_16);
            if(_ambient.HasValue)
                _ambient.Value.Write(writer, IOType.ARGB8_16);
            if(_specular.HasValue)
            {
                Color wSpecular = _specular.Value;
                wSpecular.A = SpecularExponent;
                wSpecular.Write(writer, IOType.ARGB8_16);
            }
        }
    }

    public class PolyChunkMaterialBump : PolyChunkSize
    {
        public override ushort Size => 6;

        public ushort DX { get; set; }
        public ushort DY { get; set; }
        public ushort DZ { get; set; }
        public ushort UX { get; set; }
        public ushort UY { get; set; }
        public ushort UZ { get; set; }

        public PolyChunkMaterialBump() : base(ChunkType.Material_Bump) { }

        public static PolyChunkMaterialBump Read(byte[] source, uint address)
        {
            ushort header = source.ToUInt16(address);
            byte attrib = (byte)(header >> 8);
            address += 4;

            return new PolyChunkMaterialBump()
            {
                Attributes = attrib,
                DX = source.ToUInt16(address),
                DY = source.ToUInt16(address += 2),
                DZ = source.ToUInt16(address += 2),
                UX = source.ToUInt16(address += 2),
                UY = source.ToUInt16(address += 2),
                UZ = source.ToUInt16(address += 2),
            };
        }

        public override void Write(EndianWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt16(DX);
            writer.WriteUInt16(DY);
            writer.WriteUInt16(DZ);
            writer.WriteUInt16(UX);
            writer.WriteUInt16(UY);
            writer.WriteUInt16(UZ);
        }
    }

}
