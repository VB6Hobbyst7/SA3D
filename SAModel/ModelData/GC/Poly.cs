using Reloaded.Memory.Streams.Writers;
using System;
using System.Collections.Generic;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A single corner of a polygon, called loop
    /// </summary>
    public struct Corner
    {
        /// <summary>
        /// The index of the position value
        /// </summary>
        public ushort PositionIndex { get; set; }

        /// <summary>
        /// The index of the normal value
        /// </summary>
        public ushort NormalIndex { get; set; }

        /// <summary>
        /// The index of the color value
        /// </summary>
        public ushort Color0Index { get; set; }

        /// <summary>
        /// The index of the texture coordinate value
        /// </summary>
        public ushort UV0Index { get; set; }

        public override string ToString() => $"({PositionIndex}, {NormalIndex}, {Color0Index}, {UV0Index})";
    }

    /// <summary>
    /// A collection of polygons
    /// </summary>
    [Serializable]
    public class Poly : ICloneable
    {
        /// <summary>
        /// The way in which triangles are being stored
        /// </summary>
        public PolyType Type { get; }

        /// <summary>
        /// The stored polygons
        /// </summary>
        public Corner[] Corners { get; }

        /// <summary>
        /// Create a new empty Primitive
        /// </summary>
        /// <param name="type">The type of primitive</param>
        public Poly(PolyType type, Corner[] corners)
        {
            Type = type;
            Corners = corners;
        }

        /// <summary>
        /// Read a primitive object from a file
        /// </summary>
        /// <param name="source">The files contents as a byte array</param>
        /// <param name="address">The starting address of the primitive</param>
        /// <param name="indexFlags">How the indices of the loops are structured</param>
        public static Poly Read(byte[] source, ref uint address, IndexAttributeFlags indexFlags)
        {
            bool wasBigEndian = BigEndian;
            BigEndian = true;

            PolyType type = (PolyType)source[address];
            ushort vtxCount = source.ToUInt16(address + 1);

            // checking the flags
            bool hasFlag(IndexAttributeFlags flag) => indexFlags.HasFlag(flag);

            // position always exists
            bool hasCol = hasFlag(IndexAttributeFlags.HasColor);
            bool hasNrm = hasFlag(IndexAttributeFlags.HasNormal);
            bool hasUV = hasFlag(IndexAttributeFlags.HasUV);

            //whether any of the indices use 16 bits instead of 8
            bool shortPos = hasFlag(IndexAttributeFlags.Position16BitIndex);
            bool shortCol = hasFlag(IndexAttributeFlags.Color16BitIndex);
            bool shortNrm = hasFlag(IndexAttributeFlags.Normal16BitIndex);
            bool shortUV = hasFlag(IndexAttributeFlags.UV16BitIndex);

            address += 3;

            List<Corner> corners = new();

            for(ushort i = 0; i < vtxCount; i++)
            {
                Corner l = new();

                // reading position, which should always exist
                if(shortPos)
                {
                    l.PositionIndex = source.ToUInt16(address);
                    address += 2;
                }
                else
                {
                    l.PositionIndex = source[address];
                    address++;
                }

                // reading normals
                if(hasNrm)
                {
                    if(shortNrm)
                    {
                        l.NormalIndex = source.ToUInt16(address);
                        address += 2;
                    }
                    else
                    {
                        l.NormalIndex = source[address];
                        address++;
                    }
                }

                // reading colors
                if(hasCol)
                {
                    if(shortCol)
                    {
                        l.Color0Index = source.ToUInt16(address);
                        address += 2;
                    }
                    else
                    {
                        l.Color0Index = source[address];
                        address++;
                    }
                }

                // reading uvs
                if(hasUV)
                {
                    if(shortUV)
                    {
                        l.UV0Index = source.ToUInt16(address);
                        address += 2;
                    }
                    else
                    {
                        l.UV0Index = source[address];
                        address++;
                    }
                }

                corners.Add(l);
            }

            BigEndian = wasBigEndian;
            return new Poly(type, corners.ToArray());
        }

        /// <summary>
        /// Write the contents
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="indexFlags">How the indices of the loops are structured</param>
        public void Write(EndianMemoryStream writer, IndexAttributeFlags indexFlags)
        {
            // has to be big endian
            BigEndianMemoryStream bWriter = new(writer.Stream);

            bWriter.Write((byte)Type);
            bWriter.Write((ushort)Corners.Length);

            // checking the flags
            bool hasFlag(IndexAttributeFlags flag) => indexFlags.HasFlag(flag);

            // position always exists
            bool hasCol = hasFlag(IndexAttributeFlags.HasColor);
            bool hasNrm = hasFlag(IndexAttributeFlags.HasNormal);
            bool hasUV = hasFlag(IndexAttributeFlags.HasUV);

            bool shortPos = hasFlag(IndexAttributeFlags.Position16BitIndex);
            bool shortCol = hasFlag(IndexAttributeFlags.Color16BitIndex);
            bool shortNrm = hasFlag(IndexAttributeFlags.Normal16BitIndex);
            bool shortUV = hasFlag(IndexAttributeFlags.UV16BitIndex);

            foreach(Corner v in Corners)
            {
                // Position should always exist
                if(shortPos)
                    bWriter.Write(v.PositionIndex);
                else
                    bWriter.Write((byte)v.PositionIndex);

                if(hasNrm)
                    if(shortNrm)
                        bWriter.Write(v.NormalIndex);
                    else
                        bWriter.Write((byte)v.NormalIndex);

                if(hasCol)
                    if(shortCol)
                        bWriter.Write(v.Color0Index);
                    else
                        bWriter.Write((byte)v.Color0Index);

                if(hasUV)
                    if(shortUV)
                        bWriter.Write(v.UV0Index);
                    else
                        bWriter.Write((byte)v.UV0Index);
            }

        }

        public override string ToString() => $"{Type}: {Corners.Length}";

        object ICloneable.Clone() => Clone();

        public Poly Clone() => new(Type, (Corner[])Corners.Clone());
    }
}
