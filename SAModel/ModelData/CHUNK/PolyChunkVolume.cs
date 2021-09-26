using SATools.SACommon;
using System;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Volume chunk (?)
    /// </summary>
    public class PolyChunkVolume : PolyChunkSize
    {
        [Serializable]
        public sealed class Triangle : Poly
        {
            /// <summary>
            /// user attributes of the triangle
            /// </summary>
            public ushort[] UserAttributes { get; private set; }

            public override ushort Size(byte userAttributes)
            {
                return (ushort)(6u + userAttributes * 2u);
            }

            public Triangle() : base()
            {
                UserAttributes = new ushort[3];
                Indices = new ushort[3];
            }

            /// <summary>
            /// Reads a triangle from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userAttribs">Amount of user attributes to read</param>
            /// <returns></returns>
            public static Triangle Read(byte[] source, ref uint address, byte userAttribs)
            {
                Triangle tri = new();

                for(int i = 0; i < 3; i++)
                {
                    tri.Indices[i] = source.ToUInt16(address);
                    address += 2;
                }

                for(int i = 0; i < userAttribs; i++)
                {
                    tri.UserAttributes[i] = source.ToUInt16(address);
                    address += 2;
                }

                return tri;
            }

            public override void Write(EndianWriter writer, byte userAttribs)
            {
                foreach(ushort i in Indices)
                    writer.WriteUInt16(i);
                for(int i = 0; i < userAttribs; i++)
                    writer.WriteUInt16(UserAttributes[i]);
            }

            public override Poly Clone()
            {
                Triangle p = (Triangle)base.Clone();
                p.UserAttributes = (ushort[])UserAttributes.Clone();
                return p;
            }
        }

        [Serializable]
        public sealed class Quad : Poly
        {
            /// <summary>
            /// user attributes of the Quad
            /// </summary>
            public ushort[] UserAttributes { get; private set; }

            public override ushort Size(byte userAttributes)
            {
                return (ushort)(8u + userAttributes * 2u);
            }

            public Quad() : base()
            {
                UserAttributes = new ushort[4];
                Indices = new ushort[3];
            }

            /// <summary>
            /// Reads a quad from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userAttribs">Amount of user attributes to read</param>
            /// <returns></returns>
            public static Quad Read(byte[] source, ref uint address, byte userAttribs)
            {
                Quad tri = new();

                for(int i = 0; i < 4; i++)
                {
                    tri.Indices[i] = source.ToUInt16(address);
                    address += 2;
                }

                for(int i = 0; i < userAttribs; i++)
                {
                    tri.UserAttributes[i] = source.ToUInt16(address);
                    address += 2;
                }

                return tri;
            }

            public override void Write(EndianWriter writer, byte userAttributes)
            {
                foreach(ushort i in Indices)
                    writer.WriteUInt16(i);
                for(int i = 0; i < userAttributes; i++)
                    writer.WriteUInt16(UserAttributes[i]);
            }

            public override Poly Clone()
            {
                Quad p = (Quad)base.Clone();
                p.UserAttributes = (ushort[])UserAttributes.Clone();
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
            /// First user attributes of the strip
            /// </summary>
            public ushort[] UserAttributes1 { get; private set; }

            /// <summary>
            /// Second user attributes of the strip
            /// </summary>
            public ushort[] UserAttributes2 { get; private set; }

            /// <summary>
            /// Third user attributes of the strip
            /// </summary>
            public ushort[] UserAttributes3 { get; private set; }

            public override ushort Size(byte userAttributes)
            {
                return (ushort)(2u + Indices.Length * (2u + userAttributes * 2u));
            }

            public Strip(int size, bool rev) : base()
            {
                Indices = new ushort[size];
                UserAttributes1 = new ushort[size - 2];
                UserAttributes2 = new ushort[UserAttributes1.Length];
                UserAttributes3 = new ushort[UserAttributes1.Length];
                Reversed = rev;
            }

            public Strip(ushort[] indices, bool rev) : base()
            {
                Indices = indices;
                UserAttributes1 = new ushort[Indices.Length - 2];
                UserAttributes2 = new ushort[UserAttributes1.Length];
                UserAttributes3 = new ushort[UserAttributes1.Length];
                Reversed = rev;
            }

            /// <summary>
            /// Reads a strip from a byte array
            /// </summary>
            /// <param name="source">Byte source</param>
            /// <param name="address">Address at which the strip is located</param>
            /// <param name="userAttribs">Amount of user attributes to read</param>
            /// <returns></returns>
            public static Strip Read(byte[] source, ref uint address, byte userAttribs)
            {
                short header = source.ToInt16(address);
                Strip r = new(Math.Abs(header), header < 0);
                address += 2;

                bool flag1 = userAttribs > 0;
                bool flag2 = userAttribs > 1;
                bool flag3 = userAttribs > 2;

                r.Indices[0] = source.ToUInt16(address);
                r.Indices[1] = source.ToUInt16(address += 2);

                for(int i = 2; i < r.Indices.Length; i++)
                {
                    r.Indices[i] = source.ToUInt16(address += 2);
                    if(flag1)
                    {
                        int j = i - 2;
                        r.UserAttributes1[j] = source.ToUInt16(address += 2);
                        if(flag2)
                        {
                            r.UserAttributes2[j] = source.ToUInt16(address += 2);
                            if(flag3)
                            {
                                r.UserAttributes3[j] = source.ToUInt16(address += 2);
                            }
                        }
                    }
                }

                return r;
            }

            public override void Write(EndianWriter writer, byte userAttribs)
            {

                bool flag1 = userAttribs > 0;
                bool flag2 = userAttribs > 1;
                bool flag3 = userAttribs > 2;

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
                        writer.WriteUInt16(UserAttributes1[j]);
                        if(flag2)
                        {
                            writer.WriteUInt16(UserAttributes2[j]);
                            if(flag3)
                            {
                                writer.WriteUInt16(UserAttributes3[j]);
                            }
                        }
                    }
                }


            }

            public override Poly Clone()
            {
                Strip r = (Strip)base.Clone();
                r.UserAttributes1 = (ushort[])UserAttributes1.Clone();
                r.UserAttributes2 = (ushort[])UserAttributes2.Clone();
                r.UserAttributes3 = (ushort[])UserAttributes3.Clone();
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
            public abstract ushort Size(byte userAttributes);

            /// <summary>
            /// Write the polygon to a stream
            /// </summary>
            /// <param name="writer">Output stream</param>
            /// <param name="userAttribs">Userflag count (0 - 3)</param>
            public abstract void Write(EndianWriter writer, byte userAttribs);

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
        /// User attribute count (ranges from 0 to 3)
        /// </summary>
        public byte UserAttributes { get; private set; }

        /// <summary>
        /// Creates a new volume chunk
        /// </summary>
        /// <param name="polyType"> smaller than/equal to 3 is triangle, 4 is quad and more than 4 is strip</param>
        public PolyChunkVolume(uint polyType, ushort polycount, byte userFlagCount) : base(polyType > 4 ? ChunkType.Volume_Strip : polyType == 4 ? ChunkType.Volume_Polygon4 : ChunkType.Volume_Polygon3)
        {
            Polys = new Poly[polycount];
            UserAttributes = userFlagCount;
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
            byte attrib = (byte)(header >> 8);
            uint polyType = (type - ChunkType.Volume) + 3u;
            ushort polyCount = (ushort)(Header2 & 0x3FFFu);
            byte userAttribs = (byte)(Header2 >> 14);

            PolyChunkVolume cnk = new(polyType, polyCount, userAttribs)
            {
                Attributes = attrib,
                Size = size,
            };

            address += 6;

            switch(type)
            {
                case ChunkType.Volume_Polygon3:
                    for(int i = 0; i < polyCount; i++)
                        cnk.Polys[i] = Triangle.Read(source, ref address, userAttribs);
                    break;
                case ChunkType.Volume_Polygon4:
                    for(int i = 0; i < polyCount; i++)
                        cnk.Polys[i] = Quad.Read(source, ref address, userAttribs);
                    break;
                case ChunkType.Volume_Strip:
                    for(int i = 0; i < polyCount; i++)
                        cnk.Polys[i] = Strip.Read(source, ref address, userAttribs);
                    break;
                default:
                    break;
            }

            return cnk;
        }

        public override void Write(EndianWriter writer)
        {
            // updating the size
            uint size = 2;
            foreach(Poly p in Polys)
                size += p.Size(UserAttributes);
            size /= 2;
            if(size > ushort.MaxValue)
                throw new InvalidOperationException($"Volume chunk size ({size}) exceeds maximum ({ushort.MaxValue})");
            Size = (ushort)size;

            base.Write(writer);

            if(Polys.Length > 0x3FFF)
                throw new InvalidOperationException($"Poly count ({Polys.Length}) exceeds maximum ({0x3FFF})");

            writer.WriteUInt16((ushort)(Math.Min(Polys.Length, 0x3FFFu) | (ushort)(UserAttributes << 14)));

            foreach(Poly p in Polys)
            {
                p.Write(writer, UserAttributes);
            }
        }

        public override PolyChunk Clone()
        {
            PolyChunkVolume result = (PolyChunkVolume)base.Clone();
            result.Polys = Polys.ContentClone();
            return result;
        }
    }
}
