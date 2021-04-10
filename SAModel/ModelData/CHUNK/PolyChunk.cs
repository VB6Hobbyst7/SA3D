using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.Helper;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Polychunk base class
    /// </summary>
    [Serializable]
    public abstract class PolyChunk : ICloneable
    {
        /// <summary>
        /// Type of the polychunk
        /// </summary>
        public ChunkType Type { get; protected set; }

        /// <summary>
        /// Flag byte of the poly chunk
        /// </summary>
        public byte Flags { get; set; }

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
            byte flags = (byte)(header >> 8);

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
            chunk.Flags = flags;
            address += chunk.ByteSize;
            return chunk;
        }

        /// <summary>
        /// Write the chunk to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public virtual void Write(EndianMemoryStream writer)
        {
            writer.WriteUInt16((ushort)((byte)Type | (ushort)(Flags << 8)));
        }

        object ICloneable.Clone() => Clone();

        public virtual PolyChunk Clone() => (PolyChunk)MemberwiseClone();
    }

    /// <summary>
    /// Base class for poly chunks of the bits type
    /// </summary>
    [Serializable]
    public abstract class PolyChunkBits : PolyChunk
    {
        protected PolyChunkBits(ChunkType type) : base(type) { }

        public override uint ByteSize => 2;
    }

    /// <summary>
    /// Chunk that doesnt contain anything
    /// </summary>
    [Serializable]
    public class PolyChunkNull : PolyChunkBits
    {
        public PolyChunkNull() : base(ChunkType.Null) { }
    }

    /// <summary>
    /// Chunk to mark an end
    /// </summary>
    [Serializable]
    public class PolyChunkEnd : PolyChunkBits
    {
        public PolyChunkEnd() : base(ChunkType.End) { }
    }

    /// <summary>
    /// Sets the blendmode of the following strip chunks
    /// </summary>
    [Serializable]
    public class PolyChunkBlendAlpha : PolyChunkBits
    {
        /// <summary>
        /// Source blendmode
        /// </summary>
        public BlendMode SourceAlpha
        {
            get => (BlendMode)((Flags >> 3) & 7);
            set => Flags = (byte)((Flags & ~0x38) | ((byte)value << 3));
        }

        /// <summary>
        /// Destination blendmode
        /// </summary>
        public BlendMode DestinationAlpha
        {
            get => (BlendMode)(Flags & 7);
            set => Flags = (byte)((Flags & ~7) | (byte)value);
        }

        public PolyChunkBlendAlpha() : base(ChunkType.Bits_BlendAlpha) { }

    }

    /// <summary>
    /// Adjusts the mipmap distance of the following strip chunks
    /// </summary>
    [Serializable]
    public class PolyChunksMipmapDAdjust : PolyChunkBits
    {
        /// <summary>
        /// The mipmap distance adjust <br/>
        /// Ranges from 0 to 3.75f in 0.25-steps
        /// </summary>
        public float MipmapDAdjust
        {
            get => (Flags & 0xF) * 0.25f;
            set => Flags = (byte)((Flags & 0xF0) | (byte)Math.Max(0, Math.Min(0xF, Math.Round(value / 0.25, MidpointRounding.AwayFromZero))));
        }

        public PolyChunksMipmapDAdjust() : base(ChunkType.Bits_MipmapDAdjust) { }
    }

    /// <summary>
    /// Sets the specular exponent of the following strip chunks
    /// </summary>
    [Serializable]
    public class PolyChunkSpecularExponent : PolyChunkBits
    {
        /// <summary>
        /// Specular exponent <br/>
        /// Ranges from 0 to 16
        /// </summary>
        public byte SpecularExponent
        {
            get => (byte)(Flags & 0x1F);
            set => Flags = (byte)((Flags & ~0x1F) | Math.Min(value, (byte)16));
        }

        public PolyChunkSpecularExponent() : base(ChunkType.Bits_SpecularExponent) { }
    }

    /// <summary>
    /// Caches the following polygon chunks of the current attach into specified index
    /// </summary>
    [Serializable]
    public class PolyChunkCachePolygonList : PolyChunkBits
    {
        /// <summary>
        /// Cache ID
        /// </summary>
        public byte List
        {
            get => Flags;
            set => Flags = value;
        }

        public PolyChunkCachePolygonList() : base(ChunkType.Bits_CachePolygonList) { }
    }

    /// <summary>
    /// Draws the polygon chunks cached by a specific index
    /// </summary>
    [Serializable]
    public class PolyChunkDrawPolygonList : PolyChunkBits
    {
        /// <summary>
        /// Cache ID
        /// </summary>
        public byte List
        {
            get => Flags;
            set => Flags = value;
        }

        public PolyChunkDrawPolygonList() : base(ChunkType.Bits_DrawPolygonList) { }
    }

    /// <summary>
    /// Sets texture information of the following strip chunks
    /// </summary>
    [Serializable]
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
            get => (Flags & 0xF) * 0.25f;
            set => Flags = (byte)((Flags & 0xF0) | (byte)Math.Max(0, Math.Min(0xF, Math.Round(value / 0.25, MidpointRounding.AwayFromZero))));
        }

        /// <summary>
        /// Clamps the texture v axis between 0 and 1
        /// </summary>
        public bool ClampV
        {
            get => (Flags & 0x10) != 0;
            set => _ = value ? Flags |= 0x10 : Flags &= 0xEF;
        }

        /// <summary>
        /// Clamps the texture u axis between 0 and 1
        /// </summary>
        public bool ClampU
        {
            get => (Flags & 0x20) != 0;
            set => _ = value ? Flags |= 0x20 : Flags &= 0xDF;
        }

        /// <summary>
        /// Mirrors the texture every second time the texture is repeated along the v axis
        /// </summary>
        public bool MirrorV
        {
            get => (Flags & 0x40) != 0;
            set => _ = value ? Flags |= 0x40 : Flags &= 0xBF;
        }

        /// <summary>
        /// Mirrors the texture every second time the texture is repeated along the u axis
        /// </summary>
        public bool MirrorU
        {
            get => (Flags & 0x80) != 0;
            set => _ = value ? Flags |= 0x80 : Flags &= 0x7F;
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
            byte flags = (byte)(header >> 8);
            ushort data = source.ToUInt16(address + 2);

            return new PolyChunkTextureID(type == ChunkType.Tiny_TextureID2)
            {
                Flags = flags,
                Data = data
            };
        }

        public override void Write(EndianMemoryStream writer)
        {
            base.Write(writer);
            writer.WriteUInt16(Data);
        }
    }

    /// <summary>
    /// Base class for all chunk types that also hold a size value
    /// </summary>
    [Serializable]
    public abstract class PolyChunkSize : PolyChunk
    {
        /// <summary>
        /// Amount of shorts in the chunk
        /// </summary>
        public virtual ushort Size { get; protected set; }

        public override uint ByteSize => Size * 2u + 4u;

        public PolyChunkSize(ChunkType type) : base(type) { }

        public override void Write(EndianMemoryStream writer)
        {
            base.Write(writer);
            writer.WriteUInt16(Size);
        }
    }

    /// <summary>
    /// Material information for the following strip chunks
    /// </summary>
    [Serializable]
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
            set => TypeFlag(0x08, value);
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
            get => (BlendMode)((Flags >> 3) & 7);
            set => Flags = (byte)((Flags & ~0x38) | ((byte)value << 3));
        }

        /// <summary>
        /// Destination blendmode
        /// </summary>
        public BlendMode DestinationAlpha
        {
            get => (BlendMode)(Flags & 7);
            set => Flags = (byte)((Flags & ~7) | (byte)value);
        }

        /// <summary>
        /// Diffuse color
        /// </summary>
        public Color? Diffuse
        {
            get => _diffuse;
            set
            {
                TypeFlag(0x01, value.HasValue);
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
                TypeFlag(0x02, value.HasValue);
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
                TypeFlag(0x04, value.HasValue);
                _specular = value;
            }
        }

        /// <summary>
        /// Specular exponent <br/>
        /// Requires <see cref="Specular"/> to be set
        /// </summary>
        public byte SpecularExponent { get; set; }

        public PolyChunkMaterial() : base(ChunkType.Material) { }

        private void TypeFlag(byte val, bool state)
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
            byte flags = (byte)(header >> 8);
            address += 4;

            PolyChunkMaterial mat = new PolyChunkMaterial()
            {
                Flags = flags
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

        public override void Write(EndianMemoryStream writer)
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

    [Serializable]
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
            byte flags = (byte)(header >> 8);
            address += 4;

            return new PolyChunkMaterialBump()
            {
                Flags = flags,
                DX = source.ToUInt16(address),
                DY = source.ToUInt16(address += 2),
                DZ = source.ToUInt16(address += 2),
                UX = source.ToUInt16(address += 2),
                UY = source.ToUInt16(address += 2),
                UZ = source.ToUInt16(address += 2),
            };
        }

        public override void Write(EndianMemoryStream writer)
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

    /// <summary>
    /// Volume chunk (?)
    /// </summary>
    [Serializable]
    public class PolyChunkVolume : PolyChunkSize
    {
        [Serializable]
        public sealed class Triangle : Poly
        {
            /// <summary>
            /// Userflags of the triangle
            /// </summary>
            public ushort[] UserFlags { get; private set; }

            public override ushort Size(byte userFlags)
            {
                return (ushort)(6u + userFlags * 2u);
            }

            public Triangle() : base()
            {
                UserFlags = new ushort[3];
                Indices = new ushort[3];
            }

            /// <summary>
            /// Reads a triangle from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userflags">Amount of userflags to read</param>
            /// <returns></returns>
            public static Triangle Read(byte[] source, ref uint address, byte userflags)
            {
                Triangle tri = new Triangle();

                for(int i = 0; i < 3; i++)
                {
                    tri.Indices[i] = source.ToUInt16(address);
                    address += 2;
                }

                for(int i = 0; i < userflags; i++)
                {
                    tri.UserFlags[i] = source.ToUInt16(address);
                    address += 2;
                }

                return tri;
            }

            public override void Write(EndianMemoryStream writer, byte userFlags)
            {
                foreach(ushort i in Indices)
                    writer.WriteUInt16(i);
                for(int i = 0; i < userFlags; i++)
                    writer.WriteUInt16(UserFlags[i]);
            }

            public override Poly Clone()
            {
                Triangle p = (Triangle)base.Clone();
                p.UserFlags = (ushort[])UserFlags.Clone();
                return p;
            }
        }

        [Serializable]
        public sealed class Quad : Poly
        {
            /// <summary>
            /// Userflags of the Quad
            /// </summary>
            public ushort[] UserFlags { get; private set; }

            public override ushort Size(byte userFlags)
            {
                return (ushort)(8u + userFlags * 2u);
            }

            public Quad() : base()
            {
                UserFlags = new ushort[4];
                Indices = new ushort[3];
            }

            /// <summary>
            /// Reads a quad from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userflags">Amount of userflags to read</param>
            /// <returns></returns>
            public static Quad Read(byte[] source, ref uint address, byte userflags)
            {
                Quad tri = new Quad();

                for(int i = 0; i < 4; i++)
                {
                    tri.Indices[i] = source.ToUInt16(address);
                    address += 2;
                }

                for(int i = 0; i < userflags; i++)
                {
                    tri.UserFlags[i] = source.ToUInt16(address);
                    address += 2;
                }

                return tri;
            }

            public override void Write(EndianMemoryStream writer, byte userFlags)
            {
                foreach(ushort i in Indices)
                    writer.WriteUInt16(i);
                for(int i = 0; i < userFlags; i++)
                    writer.WriteUInt16(UserFlags[i]);
            }

            public override Poly Clone()
            {
                Quad p = (Quad)base.Clone();
                p.UserFlags = (ushort[])UserFlags.Clone();
                return p;
            }
        }

        [Serializable]
        public sealed class Strip : Poly
        {
            /// <summary>
            /// Culling direction
            /// </summary>
            public bool Reversed { get; private set; }

            /// <summary>
            /// First userflags of the strip
            /// </summary>
            public ushort[] UserFlags1 { get; private set; }

            /// <summary>
            /// Second userflags of the strip
            /// </summary>
            public ushort[] UserFlags2 { get; private set; }

            /// <summary>
            /// Third userflags of the strip
            /// </summary>
            public ushort[] UserFlags3 { get; private set; }

            public override ushort Size(byte userFlags)
            {
                return (ushort)(2u + Indices.Length * (2u + userFlags * 2u));
            }

            public Strip(int size, bool rev) : base()
            {
                Indices = new ushort[size];
                UserFlags1 = new ushort[size - 2];
                UserFlags2 = new ushort[UserFlags1.Length];
                UserFlags3 = new ushort[UserFlags1.Length];
                Reversed = rev;
            }

            public Strip(ushort[] indices, bool rev) : base()
            {
                Indices = indices;
                UserFlags1 = new ushort[Indices.Length - 2];
                UserFlags2 = new ushort[UserFlags1.Length];
                UserFlags3 = new ushort[UserFlags1.Length];
                Reversed = rev;
            }

            /// <summary>
            /// Reads a strip from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userflags">Amount of userflags to read</param>
            /// <returns></returns>
            public static Strip Read(byte[] source, ref uint address, byte userflags)
            {
                short header = source.ToInt16(address);
                Strip r = new Strip(Math.Abs(header), header < 0);
                address += 2;

                bool flag1 = userflags > 0;
                bool flag2 = userflags > 1;
                bool flag3 = userflags > 2;

                r.Indices[0] = source.ToUInt16(address);
                r.Indices[1] = source.ToUInt16(address += 2);

                for(int i = 2; i < r.Indices.Length; i++)
                {
                    r.Indices[i] = source.ToUInt16(address += 2);
                    if(flag1)
                    {
                        int j = i - 2;
                        r.UserFlags1[j] = source.ToUInt16(address += 2);
                        if(flag2)
                        {
                            r.UserFlags2[j] = source.ToUInt16(address += 2);
                            if(flag3)
                            {
                                r.UserFlags3[j] = source.ToUInt16(address += 2);
                            }
                        }
                    }
                }

                return r;
            }

            public override void Write(EndianMemoryStream writer, byte userflags)
            {

                bool flag1 = userflags > 0;
                bool flag2 = userflags > 1;
                bool flag3 = userflags > 2;

                short count = (short)Math.Min(Indices.Length, short.MaxValue);
                writer.WriteInt16(Reversed ? (short)-count : count);

                writer.WriteUInt16(Indices[0]);
                writer.WriteUInt16(Indices[1]);
                for(int i = 2; i < count; i++)
                {
                    writer.WriteUInt16(Indices[i]);
                    if(flag1)
                    {
                        int j = i - 2;
                        writer.WriteUInt16(UserFlags1[j]);
                        if(flag2)
                        {
                            writer.WriteUInt16(UserFlags2[j]);
                            if(flag3)
                            {
                                writer.WriteUInt16(UserFlags3[j]);
                            }
                        }
                    }
                }


            }

            public override Poly Clone()
            {
                Strip r = (Strip)base.Clone();
                r.UserFlags1 = (ushort[])UserFlags1.Clone();
                r.UserFlags2 = (ushort[])UserFlags2.Clone();
                r.UserFlags3 = (ushort[])UserFlags3.Clone();
                return r;
            }
        }

        /// <summary>
        /// Polygon base class for volumes
        /// </summary>
        [Serializable]
        public abstract class Poly : ICloneable
        {
            /// <summary>
            /// Indices of the polygon
            /// </summary>
            public ushort[] Indices { get; protected set; }

            internal Poly() { }

            /// <summary>
            /// Size of the polygon in bytes
            /// </summary>
            public abstract ushort Size(byte userFlags);

            /// <summary>
            /// Write the polygon to a stream
            /// </summary>
            /// <param name="writer">Output stream</param>
            /// <param name="userFlags">Userflag count (0 - 3)</param>
            public abstract void Write(EndianMemoryStream writer, byte userFlags);

            object ICloneable.Clone() => Clone();

            public virtual Poly Clone()
            {
                Poly result = (Poly)MemberwiseClone();
                Indices = (ushort[])Indices.Clone();
                return result;
            }
        }

        /// <summary>
        /// Polygons of the volume
        /// </summary>
        public Poly[] Polys { get; private set; }

        /// <summary>
        /// User flag count (ranges from 0 to 3)
        /// </summary>
        public byte UserFlags { get; private set; }

        /// <summary>
        /// Creates a new volume chunk
        /// </summary>
        /// <param name="polyType"> smaller than/equal to 3 is triangle, 4 is quad and more than 4 is strip</param>
        public PolyChunkVolume(uint polyType, ushort polycount, byte userFlagCount) : base(polyType > 4 ? ChunkType.Volume_Strip : polyType == 4 ? ChunkType.Volume_Polygon4 : ChunkType.Volume_Polygon3)
        {
            Polys = new Poly[polycount];
            UserFlags = userFlagCount;
        }

        /// <summary>
        /// Reads a volume chunk from a byte source
        /// </summary>
        /// <param name="source"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static PolyChunkVolume Read(byte[] source, uint address)
        {
            ushort header = source.ToUInt16(address);
            ushort size = source.ToUInt16(address + 2);
            ushort Header2 = source.ToUInt16(address + 4);

            ChunkType type = (ChunkType)(header & 0xFF);
            byte flags = (byte)(header >> 8);
            uint polyType = (type - ChunkType.Volume) + 3u;
            ushort polyCount = (ushort)(Header2 & 0x3FFFu);
            byte userFlags = (byte)(Header2 >> 14);

            PolyChunkVolume cnk = new PolyChunkVolume(polyType, polyCount, userFlags)
            {
                Flags = flags,
                Size = size,
            };

            address += 6;

            switch(type)
            {
                case ChunkType.Volume_Polygon3:
                    for(int i = 0; i < polyCount; i++)
                        cnk.Polys[i] = Triangle.Read(source, ref address, userFlags);
                    break;
                case ChunkType.Volume_Polygon4:
                    for(int i = 0; i < polyCount; i++)
                        cnk.Polys[i] = Quad.Read(source, ref address, userFlags);
                    break;
                case ChunkType.Volume_Strip:
                    for(int i = 0; i < polyCount; i++)
                        cnk.Polys[i] = Strip.Read(source, ref address, userFlags);
                    break;
                default:
                    break;
            }

            return cnk;
        }

        public override void Write(EndianMemoryStream writer)
        {
            // updating the size
            uint size = 2;
            foreach(Poly p in Polys)
                size += p.Size(UserFlags);
            size /= 2;
            if(size > ushort.MaxValue)
                throw new InvalidOperationException($"Volume chunk size ({size}) exceeds maximum ({ushort.MaxValue})");
            Size = (ushort)size;

            base.Write(writer);

            if(Polys.Length > 0x3FFF)
                throw new InvalidOperationException($"Poly count ({Polys.Length}) exceeds maximum ({0x3FFF})");

            writer.WriteUInt16((ushort)(Math.Min(Polys.Length, 0x3FFFu) | (ushort)(UserFlags << 14)));

            foreach(Poly p in Polys)
            {
                p.Write(writer, UserFlags);
            }
        }

        public override PolyChunk Clone()
        {
            PolyChunkVolume result = (PolyChunkVolume)base.Clone();
            result.Polys = Polys.ContentClone();
            return result;
        }
    }

    /// <summary>
    /// Chunk to hold polygon data 
    /// </summary>
    [Serializable]
    public class PolyChunkStrip : PolyChunkSize
    {
        /// <summary>
        /// Single strip in the chunk
        /// </summary>
        public class Strip : ICloneable
        {
            /// <summary>
            /// Single corner in a strip
            /// </summary>
            public struct Corner
            {
                /// <summary>
                /// Vertex Cache index
                /// </summary>
                public ushort index;

                /// <summary>
                /// Corner
                /// </summary>
                public Vector2 uv;

                /// <summary>
                /// Normal of the corner
                /// </summary>
                public Vector3 normal;

                /// <summary>
                /// Vertex color
                /// </summary>
                public Color color;

                /// <summary>
                /// First custom flag
                /// </summary>
                public ushort userFlag1;

                /// <summary>
                /// Second custom flag
                /// </summary>
                public ushort userFlag2;

                /// <summary>
                /// Third custom flag
                /// </summary>
                public ushort userFlag3;
            }

            /// <summary>
            /// Culling direction
            /// </summary>
            public bool Reversed { get; private set; }

            /// <summary>
            /// Corners in the strip <br/>
            /// The first two corners are only used for their index
            /// </summary>
            public Corner[] Corners { get; private set; }

            /// <summary>
            /// Creates a new strip from corners and cull direction
            /// </summary>
            /// <param name="corners">Corners of the strip</param>
            /// <param name="reverse">Culling direction</param>
            public Strip(Corner[] corners, bool reverse)
            {
                Reversed = reverse;
                Corners = corners;
            }

            public uint Size(byte userFlags, bool hasUV, bool hasNormal, bool hasColor)
            {
                return (uint)(2u + Corners.Length * (2 + (hasUV ? 4u : 0u) + (hasNormal ? 12u : 0u) + (hasColor ? 4u : 0u)) + ((Corners.Length - 2) * userFlags * 2));
            }

            /// <summary>
            /// Reads a strip from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userflags">Amount of userflags</param>
            /// <param name="hasUV">Whether the polygons carry uv data</param>
            /// <param name="HDUV">Whether the uv data repeats at 1024, not 256</param>
            /// <param name="hasNormal">Whether the polygons carry normal data</param>
            /// <param name="hasColor">Whether the polygons carry color data</param>
            /// <returns></returns>
            public static Strip Read(byte[] source, ref uint address, byte userflags, bool hasUV, bool HDUV, bool hasNormal, bool hasColor)
            {
                short header = source.ToInt16(address);
                bool reverse = header < 0;
                Corner[] corners = new Corner[Math.Abs(header)];

                bool flag1 = userflags > 0;
                bool flag2 = userflags > 1;
                bool flag3 = userflags > 2;

                float multiplier = HDUV ? 1f / 1024f : 1f / 256f;

                address += 2;

                for(int i = 0; i < corners.Length; i++)
                {
                    Corner c = new Corner()
                    {
                        index = source.ToUInt16(address)
                    };
                    address += 2;

                    if(hasUV)
                        c.uv = Vector2.Read(source, ref address, IOType.Short) * multiplier;
                    if(hasNormal)
                        c.normal = Vector3.Read(source, ref address, IOType.Float);
                    else if(hasColor)
                        c.color = Color.Read(source, ref address, IOType.ARGB8_16);

                    if(flag1 && i > 1)
                    {
                        c.userFlag1 = source.ToUInt16(address);
                        address += 2;
                        if(flag2)
                        {
                            c.userFlag2 = source.ToUInt16(address);
                            address += 2;
                            if(flag3)
                            {
                                c.userFlag3 = source.ToUInt16(address);
                                address += 2;
                            }
                        }
                    }

                    corners[i] = c;
                }

                return new Strip(corners, reverse);
            }

            /// <summary>
            /// Writes the strip to a byte stream
            /// </summary>
            /// <param name="writer">Output stream</param>
            /// <param name="userflags">Amount of userflags</param>
            /// <param name="hasUV">Whether the polygons carry uv data</param>
            /// <param name="HDUV">Whether the uv data repeats at 1024, not 256</param>
            /// <param name="hasNormal">Whether the polygons carry normal data</param>
            /// <param name="hasColor">Whether the polygons carry color data</param>
            public void Write(EndianMemoryStream writer, byte userflags, bool hasUV, bool HDUV, bool hasNormal, bool hasColor)
            {
                short length = (short)Math.Min(Corners.Length, short.MaxValue);
                writer.WriteInt16(Reversed ? (short)-length : length);

                bool flag1 = userflags > 0;
                bool flag2 = userflags > 1;
                bool flag3 = userflags > 2;
                float multiplier = HDUV ? 1024 : 256;

                for(int i = 0; i < length; i++)
                {
                    Corner c = Corners[i];
                    writer.WriteUInt16(c.index);
                    if(hasUV)
                        (c.uv * multiplier).Write(writer, IOType.Short);
                    if(hasNormal)
                        c.normal.Write(writer, IOType.Float);
                    else if(hasColor)
                        c.color.Write(writer, IOType.ARGB8_16);


                    if(flag1 && i > 1)
                    {
                        writer.WriteUInt16(c.userFlag1);
                        if(flag2)
                        {
                            writer.WriteUInt16(c.userFlag2);
                            if(flag3)
                            {
                                writer.WriteUInt16(c.userFlag3);
                            }
                        }
                    }
                }
            }

            object ICloneable.Clone() => Clone();

            public Strip Clone() => new Strip((Corner[])Corners.Clone(), Reversed);
        }


        /// <summary>
        /// Whether the polygons contain uv data
        /// </summary>
        public bool HasUV
        {
            get
            {
                return Type == ChunkType.Strip_StripUVN
                    || Type == ChunkType.Strip_StripUVH
                    || Type == ChunkType.Strip_StripUVNNormal
                    || Type == ChunkType.Strip_StripUVHNormal
                    || Type == ChunkType.Strip_StripUVNColor
                    || Type == ChunkType.Strip_StripUVHColor
                    || Type == ChunkType.Strip_StripUVN2
                    || Type == ChunkType.Strip_StripUVH2;
            }
            set
            {
                if(value)
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_Strip:
                            Type = ChunkType.Strip_StripUVN;
                            break;
                        case ChunkType.Strip_StripNormal:
                            Type = ChunkType.Strip_StripUVNNormal;
                            break;
                        case ChunkType.Strip_StripColor:
                            Type = ChunkType.Strip_StripUVNColor;
                            break;
                        case ChunkType.Strip_Strip2:
                            Type = ChunkType.Strip_StripUVN2;
                            break;
                    }
                }
                else
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_StripUVN:
                        case ChunkType.Strip_StripUVH:
                            Type = ChunkType.Strip_Strip;
                            break;
                        case ChunkType.Strip_StripUVNNormal:
                        case ChunkType.Strip_StripUVHNormal:
                            Type = ChunkType.Strip_StripNormal;
                            break;
                        case ChunkType.Strip_StripUVNColor:
                        case ChunkType.Strip_StripUVHColor:
                            Type = ChunkType.Strip_StripColor;
                            break;
                        case ChunkType.Strip_StripUVN2:
                        case ChunkType.Strip_StripUVH2:
                            Type = ChunkType.Strip_Strip2;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Whether uvs repeat at 256 (normal) or 1024 (HD)
        /// </summary>
        public bool UVHD
        {
            get
            {
                return Type == ChunkType.Strip_StripUVH
                    || Type == ChunkType.Strip_StripUVHNormal
                    || Type == ChunkType.Strip_StripUVHColor
                    || Type == ChunkType.Strip_StripUVH2;
            }
            set
            {
                if(!HasUV)
                    return;
                if(value)
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_StripUVN:
                            Type = ChunkType.Strip_StripUVH;
                            break;
                        case ChunkType.Strip_StripUVNNormal:
                            Type = ChunkType.Strip_StripUVHNormal;
                            break;
                        case ChunkType.Strip_StripUVNColor:
                            Type = ChunkType.Strip_StripUVHColor;
                            break;
                        case ChunkType.Strip_StripUVN2:
                            Type = ChunkType.Strip_StripUVH2;
                            break;
                    }
                }
                else
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_StripUVH:
                            Type = ChunkType.Strip_StripUVN;
                            break;
                        case ChunkType.Strip_StripUVHNormal:
                            Type = ChunkType.Strip_StripUVNNormal;
                            break;
                        case ChunkType.Strip_StripUVHColor:
                            Type = ChunkType.Strip_StripUVNColor;
                            break;
                        case ChunkType.Strip_StripUVH2:
                            Type = ChunkType.Strip_StripUVN2;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Whether the polygons use normals
        /// </summary>
        public bool HasNormal
        {
            get
            {
                return Type == ChunkType.Strip_StripNormal
                    || Type == ChunkType.Strip_StripUVNNormal
                    || Type == ChunkType.Strip_StripUVHNormal;
            }
            set
            {
                if(value)
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_Strip:
                        case ChunkType.Strip_StripColor:
                        case ChunkType.Strip_Strip2:
                            Type = ChunkType.Strip_StripNormal;
                            break;
                        case ChunkType.Strip_StripUVN:
                        case ChunkType.Strip_StripUVNColor:
                        case ChunkType.Strip_StripUVN2:
                            Type = ChunkType.Strip_StripUVNNormal;
                            break;
                        case ChunkType.Strip_StripUVH:
                        case ChunkType.Strip_StripUVHColor:
                        case ChunkType.Strip_StripUVH2:
                            Type = ChunkType.Strip_StripUVHNormal;
                            break;
                    }
                }
                else
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_StripNormal:
                            Type = ChunkType.Strip_Strip;
                            break;
                        case ChunkType.Strip_StripUVNNormal:
                            Type = ChunkType.Strip_StripUVN;
                            break;
                        case ChunkType.Strip_StripUVHNormal:
                            Type = ChunkType.Strip_StripUVH;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Whether the polygons use vertex colors
        /// </summary>
        public bool HasColor
        {
            get
            {
                return Type == ChunkType.Strip_StripColor
                    || Type == ChunkType.Strip_StripUVNColor
                    || Type == ChunkType.Strip_StripUVHColor;
            }
            set
            {
                if(value)
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_Strip:
                        case ChunkType.Strip_StripNormal:
                        case ChunkType.Strip_Strip2:
                            Type = ChunkType.Strip_StripColor;
                            break;
                        case ChunkType.Strip_StripUVN:
                        case ChunkType.Strip_StripUVNNormal:
                        case ChunkType.Strip_StripUVN2:
                            Type = ChunkType.Strip_StripUVNColor;
                            break;
                        case ChunkType.Strip_StripUVH:
                        case ChunkType.Strip_StripUVHNormal:
                        case ChunkType.Strip_StripUVH2:
                            Type = ChunkType.Strip_StripUVHColor;
                            break;
                    }
                }
                else
                {
                    switch(Type)
                    {
                        case ChunkType.Strip_StripColor:
                            Type = ChunkType.Strip_Strip;
                            break;
                        case ChunkType.Strip_StripUVNColor:
                            Type = ChunkType.Strip_StripUVN;
                            break;
                        case ChunkType.Strip_StripUVHColor:
                            Type = ChunkType.Strip_StripUVH;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Ignores diffuse lighting
        /// </summary>
        public bool IgnoreLight
        {
            get => (Flags & 1) != 0;
            set => _ = value ? Flags |= 0x01 : Flags &= 0xFE;
        }

        /// <summary>
        /// Ignores specular lighting
        /// </summary>
        public bool IgnoreSpecular
        {
            get => (Flags & 2) != 0;
            set => _ = value ? Flags |= 0x02 : Flags &= 0xFD;
        }

        /// <summary>
        /// Ignores ambient lighting
        /// </summary>
        public bool IgnoreAmbient
        {
            get => (Flags & 4) != 0;
            set => _ = value ? Flags |= 0x04 : Flags &= 0xFB;
        }

        /// <summary>
        /// uses alpha
        /// </summary>
        public bool UseAlpha
        {
            get => (Flags & 8) != 0;
            set => _ = value ? Flags |= 0x08 : Flags &= 0xF7;
        }

        /// <summary>
        /// Ignores culling
        /// </summary>
        public bool DoubleSide
        {
            get => (Flags & 0x10) != 0;
            set => _ = value ? Flags |= 0x10 : Flags &= 0xEF;
        }

        /// <summary>
        /// Uses no lighting at all (Vertex color lit?)
        /// </summary>
        public bool FlatShading
        {
            get => (Flags & 0x20) != 0;
            set => _ = value ? Flags |= 0x20 : Flags &= 0xDF;
        }

        /// <summary>
        /// Environment (matcap/normal) mapping
        /// </summary>
        public bool EnvironmentMapping
        {
            get => (Flags & 0x40) != 0;
            set => _ = value ? Flags |= 0x40 : Flags &= 0xBF;
        }

        /// <summary>
        /// Unknown what it actually does, but it definitely exists (e.g. sonics light dash models use it)
        /// </summary>
        public bool Unknown7
        {
            get => (Flags & 0x80) != 0;
            set => _ = value ? Flags |= 0x80 : Flags &= 0x7F;
        }

        /// <summary>
        /// Polygon data of the chunk
        /// </summary>
        public Strip[] Strips { get; private set; }

        /// <summary>
        /// User flag count (ranges from 0 to 3)
        /// </summary>
        public byte UserFlags { get; private set; }

        public PolyChunkStrip(ushort stripCount, byte userFlagCount) : base(ChunkType.Strip)
        {
            Strips = new Strip[stripCount];
            UserFlags = userFlagCount;
        }

        public static PolyChunkStrip Read(byte[] source, uint address)
        {
            ushort header = source.ToUInt16(address);
            ushort size = source.ToUInt16(address + 2);
            ushort Header2 = source.ToUInt16(address + 4);

            ChunkType type = (ChunkType)(header & 0xFF);
            byte flags = (byte)(header >> 8);
            ushort polyCount = (ushort)(Header2 & 0x3FFFu);
            byte userFlags = (byte)(Header2 >> 14);

            if(type >= ChunkType.Strip_Strip2)
                throw new NotImplementedException("Param2 types for strips not supported");

            PolyChunkStrip cnk = new PolyChunkStrip(polyCount, userFlags)
            {
                Type = type,
                Flags = flags,
                Size = size
            };

            address += 6;

            bool hasUV = cnk.HasUV;
            bool UVHD = cnk.UVHD;
            bool hasNormal = cnk.HasNormal;
            bool hasColor = cnk.HasColor;

            for(int i = 0; i < polyCount; i++)
            {
                cnk.Strips[i] = Strip.Read(source, ref address, userFlags, hasUV, UVHD, hasNormal, hasColor);
            }

            return cnk;
        }

        public override void Write(EndianMemoryStream writer)
        {
            bool hasUV = HasUV;
            bool uvhd = UVHD;
            bool hasNormal = HasNormal;
            bool hasColor = HasColor;

            // recalculating the size
            uint size = 2;
            foreach(Strip str in Strips)
                size += str.Size(UserFlags, hasUV, hasNormal, hasColor);
            size /= 2;

            if(size > ushort.MaxValue)
                throw new InvalidOperationException($"Strip chunk size ({size}) exceeds maximum ({ushort.MaxValue})");
            Size = (ushort)size;

            base.Write(writer);

            if(Strips.Length > 0x3FFF)
                throw new InvalidOperationException($"Strip count ({Strips.Length}) exceeds maximum ({0x3FFF})");

            writer.WriteUInt16((ushort)(Math.Min(Strips.Length, 0x3FFFu) | (ushort)(UserFlags << 14)));

            foreach(Strip s in Strips)
                s.Write(writer, UserFlags, hasUV, uvhd, hasNormal, hasColor);
        }

        public override PolyChunk Clone()
        {
            PolyChunkStrip result = (PolyChunkStrip)base.Clone();
            result.Strips = Strips.ContentClone();
            return result;
        }
    }
}
