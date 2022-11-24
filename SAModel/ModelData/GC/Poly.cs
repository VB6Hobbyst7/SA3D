using SATools.SACommon;
using System;
using System.Collections.Generic;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A single corner of a polygon, called loop
    /// </summary>
    public struct Corner : IEquatable<Corner>
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

        public override bool Equals(object obj)
            => obj is Corner corner
            && PositionIndex == corner.PositionIndex
            && NormalIndex == corner.NormalIndex
            && Color0Index == corner.Color0Index
            && UV0Index == corner.UV0Index;

        public override int GetHashCode() => HashCode.Combine(PositionIndex, NormalIndex, Color0Index, UV0Index);
        public override string ToString() => $"({PositionIndex}, {NormalIndex}, {Color0Index}, {UV0Index})";

        bool IEquatable<Corner>.Equals(Corner other) => Equals(other);

        public static bool operator ==(Corner left, Corner right)
            => left.Equals(right);

        public static bool operator !=(Corner left, Corner right)
            => !(left == right);
    }

    /// <summary>
    /// A collection of corners forming polygons
    /// </summary>
    public struct Poly : ICloneable
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
        /// <param name="indexAttribs">How the indices of the loops are structured</param>
        public static Poly Read(byte[] source, ref uint address, IndexAttributes indexAttribs)
        {
            PushBigEndian(true);

            PolyType type = (PolyType)source[address];
            ushort vtxCount = source.ToUInt16(address + 1);

            // checking the attributes
            bool hasFlag(IndexAttributes attrib) => indexAttribs.HasFlag(attrib);

            // position always exists
            bool hasCol = hasFlag(IndexAttributes.HasColor);
            bool hasNrm = hasFlag(IndexAttributes.HasNormal);
            bool hasUV = hasFlag(IndexAttributes.HasUV);

            //whether any of the indices use 16 bits instead of 8
            bool shortPos = hasFlag(IndexAttributes.Position16BitIndex);
            bool shortCol = hasFlag(IndexAttributes.Color16BitIndex);
            bool shortNrm = hasFlag(IndexAttributes.Normal16BitIndex);
            bool shortUV = hasFlag(IndexAttributes.UV16BitIndex);

            address += 3;

            List<Corner> corners = new();

            for (ushort i = 0; i < vtxCount; i++)
            {
                Corner l = new();

                // reading position, which should always exist
                if (shortPos)
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
                if (hasNrm)
                {
                    if (shortNrm)
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
                if (hasCol)
                {
                    if (shortCol)
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
                if (hasUV)
                {
                    if (shortUV)
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

            PopEndian();
            return new Poly(type, corners.ToArray());
        }

        /// <summary>
        /// Write the contents
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="indexAttribs">How the indices of the loops are structured</param>
        public void Write(EndianWriter writer, IndexAttributes indexAttribs)
        {
            writer.PushBigEndian(true);

            writer.Write((byte)Type);
            writer.Write((ushort)Corners.Length);

            // checking the attributes
            bool hasFlag(IndexAttributes attrib) => indexAttribs.HasFlag(attrib);

            // position always exists
            bool hasCol = hasFlag(IndexAttributes.HasColor);
            bool hasNrm = hasFlag(IndexAttributes.HasNormal);
            bool hasUV = hasFlag(IndexAttributes.HasUV);

            bool shortPos = hasFlag(IndexAttributes.Position16BitIndex);
            bool shortCol = hasFlag(IndexAttributes.Color16BitIndex);
            bool shortNrm = hasFlag(IndexAttributes.Normal16BitIndex);
            bool shortUV = hasFlag(IndexAttributes.UV16BitIndex);

            foreach (Corner v in Corners)
            {
                // Position should always exist
                if (shortPos)
                    writer.Write(v.PositionIndex);
                else
                    writer.Write((byte)v.PositionIndex);

                if (hasNrm)
                    if (shortNrm)
                        writer.Write(v.NormalIndex);
                    else
                        writer.Write((byte)v.NormalIndex);

                if (hasCol)
                    if (shortCol)
                        writer.Write(v.Color0Index);
                    else
                        writer.Write((byte)v.Color0Index);

                if (hasUV)
                    if (shortUV)
                        writer.Write(v.UV0Index);
                    else
                        writer.Write((byte)v.UV0Index);
            }

            writer.PopEndian();
        }

        public override string ToString() => $"{Type}: {Corners.Length}";

        object ICloneable.Clone() => Clone();

        public Poly Clone() => new(Type, (Corner[])Corners.Clone());
    }
}
