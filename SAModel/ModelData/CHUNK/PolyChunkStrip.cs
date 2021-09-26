using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Numerics;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Chunk to hold polygon data 
    /// </summary>
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

            public uint Size(byte userAttributes, bool hasUV, bool hasNormal, bool hasColor)
            {
                return (uint)(2u + Corners.Length * (2 + (hasUV ? 4u : 0u) + (hasNormal ? 12u : 0u) + (hasColor ? 4u : 0u)) + ((Corners.Length - 2) * userAttributes * 2));
            }

            /// <summary>
            /// Reads a strip from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userAttributes">Amount of user attributes</param>
            /// <param name="hasUV">Whether the polygons carry uv data</param>
            /// <param name="HDUV">Whether the uv data repeats at 1024, not 256</param>
            /// <param name="hasNormal">Whether the polygons carry normal data</param>
            /// <param name="hasColor">Whether the polygons carry color data</param>
            /// <returns></returns>
            public static Strip Read(byte[] source, ref uint address, byte userAttributes, bool hasUV, bool HDUV, bool hasNormal, bool hasColor)
            {
                short header = source.ToInt16(address);
                bool reverse = header < 0;
                Corner[] corners = new Corner[Math.Abs(header)];

                bool flag1 = userAttributes > 0;
                bool flag2 = userAttributes > 1;
                bool flag3 = userAttributes > 2;

                float multiplier = HDUV ? 1f / 1024f : 1f / 256f;

                address += 2;

                for(int i = 0; i < corners.Length; i++)
                {
                    Corner c = new()
                    {
                        index = source.ToUInt16(address)
                    };
                    address += 2;

                    if(hasUV)
                        c.uv = Vector2Extensions.Read(source, ref address, IOType.Short) * multiplier;
                    if(hasNormal)
                        c.normal = Vector3Extensions.Read(source, ref address, IOType.Float);
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
            /// <param name="userAttribs">Amount of user attributes</param>
            /// <param name="hasUV">Whether the polygons carry uv data</param>
            /// <param name="HDUV">Whether the uv data repeats at 1024, not 256</param>
            /// <param name="hasNormal">Whether the polygons carry normal data</param>
            /// <param name="hasColor">Whether the polygons carry color data</param>
            public void Write(EndianWriter writer, byte userAttribs, bool hasUV, bool HDUV, bool hasNormal, bool hasColor)
            {
                short length = (short)Math.Min(Corners.Length, short.MaxValue);
                writer.WriteInt16(Reversed ? (short)-length : length);

                bool flag1 = userAttribs > 0;
                bool flag2 = userAttribs > 1;
                bool flag3 = userAttribs > 2;
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

            public Strip Clone() => new((Corner[])Corners.Clone(), Reversed);
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
            get => (Attributes & 1) != 0;
            set => _ = value ? Attributes |= 0x01 : Attributes &= 0xFE;
        }

        /// <summary>
        /// Ignores specular lighting
        /// </summary>
        public bool IgnoreSpecular
        {
            get => (Attributes & 2) != 0;
            set => _ = value ? Attributes |= 0x02 : Attributes &= 0xFD;
        }

        /// <summary>
        /// Ignores ambient lighting
        /// </summary>
        public bool IgnoreAmbient
        {
            get => (Attributes & 4) != 0;
            set => _ = value ? Attributes |= 0x04 : Attributes &= 0xFB;
        }

        /// <summary>
        /// uses alpha
        /// </summary>
        public bool UseAlpha
        {
            get => (Attributes & 8) != 0;
            set => _ = value ? Attributes |= 0x08 : Attributes &= 0xF7;
        }

        /// <summary>
        /// Ignores culling
        /// </summary>
        public bool DoubleSide
        {
            get => (Attributes & 0x10) != 0;
            set => _ = value ? Attributes |= 0x10 : Attributes &= 0xEF;
        }

        /// <summary>
        /// Uses no lighting at all (Vertex color lit?)
        /// </summary>
        public bool FlatShading
        {
            get => (Attributes & 0x20) != 0;
            set => _ = value ? Attributes |= 0x20 : Attributes &= 0xDF;
        }

        /// <summary>
        /// Environment (matcap/normal) mapping
        /// </summary>
        public bool EnvironmentMapping
        {
            get => (Attributes & 0x40) != 0;
            set => _ = value ? Attributes |= 0x40 : Attributes &= 0xBF;
        }

        /// <summary>
        /// Unknown what it actually does, but it definitely exists (e.g. sonics light dash models use it)
        /// </summary>
        public bool Unknown7
        {
            get => (Attributes & 0x80) != 0;
            set => _ = value ? Attributes |= 0x80 : Attributes &= 0x7F;
        }

        /// <summary>
        /// Polygon data of the chunk
        /// </summary>
        public Strip[] Strips { get; private set; }

        /// <summary>
        /// User flag count (ranges from 0 to 3)
        /// </summary>
        public byte UserAttributes { get; private set; }

        public PolyChunkStrip(ushort stripCount, byte userFlagCount) : base(ChunkType.Strip)
        {
            Strips = new Strip[stripCount];
            UserAttributes = userFlagCount;
        }

        public static PolyChunkStrip Read(byte[] source, uint address)
        {
            ushort header = source.ToUInt16(address);
            ushort size = source.ToUInt16(address + 2);
            ushort Header2 = source.ToUInt16(address + 4);

            ChunkType type = (ChunkType)(header & 0xFF);
            byte attribs = (byte)(header >> 8);
            ushort polyCount = (ushort)(Header2 & 0x3FFFu);
            byte userAttribs = (byte)(Header2 >> 14);

            if(type >= ChunkType.Strip_Strip2)
                throw new NotImplementedException("Param2 types for strips not supported");

            PolyChunkStrip cnk = new(polyCount, userAttribs)
            {
                Type = type,
                Attributes = attribs,
                Size = size
            };

            address += 6;

            bool hasUV = cnk.HasUV;
            bool UVHD = cnk.UVHD;
            bool hasNormal = cnk.HasNormal;
            bool hasColor = cnk.HasColor;

            for(int i = 0; i < polyCount; i++)
            {
                cnk.Strips[i] = Strip.Read(source, ref address, userAttribs, hasUV, UVHD, hasNormal, hasColor);
            }

            return cnk;
        }

        public override void Write(EndianWriter writer)
        {
            bool hasUV = HasUV;
            bool uvhd = UVHD;
            bool hasNormal = HasNormal;
            bool hasColor = HasColor;

            // recalculating the size
            uint size = 2;
            foreach(Strip str in Strips)
                size += str.Size(UserAttributes, hasUV, hasNormal, hasColor);
            size /= 2;

            if(size > ushort.MaxValue)
                throw new InvalidOperationException($"Strip chunk size ({size}) exceeds maximum ({ushort.MaxValue})");
            Size = (ushort)size;

            base.Write(writer);

            if(Strips.Length > 0x3FFF)
                throw new InvalidOperationException($"Strip count ({Strips.Length}) exceeds maximum ({0x3FFF})");

            writer.WriteUInt16((ushort)(Math.Min(Strips.Length, 0x3FFFu) | (ushort)(UserAttributes << 14)));

            foreach(Strip s in Strips)
                s.Write(writer, UserAttributes, hasUV, uvhd, hasNormal, hasColor);
        }

        public override PolyChunk Clone()
        {
            PolyChunkStrip result = (PolyChunkStrip)base.Clone();
            result.Strips = Strips.ContentClone();
            return result;
        }
    }
}
