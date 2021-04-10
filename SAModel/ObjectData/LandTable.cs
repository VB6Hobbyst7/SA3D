using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData.Animation;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// Stage geometry information
    /// </summary>
    public class LandTable
    {
        #region File headers
        /// <summary>
        /// SA1LVL file header; "SA1LVL"
        /// </summary>
        private const ulong SA1LVL = 0x4C564C314153u;

        /// <summary>
        /// SA2LVL file header; "SA2LVL"
        /// </summary>
        private const ulong SA2LVL = 0x4C564C324153u;

        /// <summary>
        /// SA2BLVL file header; "SA2BLVL"
        /// </summary>
        private const ulong SA2BLVL = 0x4C564C42324153u;

        /// <summary>
        /// Header mask 
        /// </summary>
        private const ulong HeaderMask = ~((ulong)0xFF << 56);

        /// <summary>
        /// Current file version
        /// </summary>
        private const ulong CurrentVersion = 3;

        /// <summary>
        /// <see cref="SA1LVL"/> with version integrated
        /// </summary>
        private const ulong SA1LVLVer = SA1LVL | (CurrentVersion << 56);

        /// <summary>
        /// <see cref="SA2LVL"/> with version integrated
        /// </summary>
        private const ulong SA2LVLVer = SA2LVL | (CurrentVersion << 56);

        /// <summary>
        /// <see cref="SA2BLVL"/> with version integrated
        /// </summary>
        private const ulong SA2BLVLVer = SA2BLVL | (CurrentVersion << 56);
        #endregion

        /// <summary>
        /// Landtable name / C struct label
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Level geometry
        /// </summary>
        public List<LandEntry> Geometry { get; }

        /// <summary>
        /// Geometry list c struct label
        /// </summary>
        public string GeoName { get; set; }

        /// <summary>
        /// Geometry animations (sa1)
        /// </summary>
        public List<LandEntryMotion> GeometryAnimations { get; }

        /// <summary>
        /// Geometry animation list c struct label
        /// </summary>
        public string GeoAnimName { get; set; }

        /// <summary>
        /// Landtable flags
        /// </summary>
        public uint Flags { get; set; }

        /// <summary>
        /// Draw distance
        /// </summary>
        public float DrawDistance { get; set; }

        /// <summary>
        /// Texture file name
        /// </summary>
        public string TextureFileName { get; set; }

        /// <summary>
        /// Texture list pointer
        /// </summary>
        public uint TexListPtr { get; set; }

        /// <summary>
        /// Format of the landtable
        /// </summary>
        public LandtableFormat Format { get; }

        /// <summary>
        /// MetaData of/for a LVL file
        /// </summary>
        public MetaData MetaData { get; private set; }

        /// <summary>
        /// Creates a new Landtable from existing collections
        /// </summary>
        /// <param name="geometry">Level geometry</param>
        /// <param name="geometryAnimations">Geometry animations</param>
        /// <param name="metaData">Various meta data</param>
        /// <param name="format">Landtable Format</param>
        public LandTable(List<LandEntry> geometry, List<LandEntryMotion> geometryAnimations, LandtableFormat format)
        {
            Geometry = geometry;
            GeometryAnimations = geometryAnimations;
            Format = format;
            MetaData = new MetaData();

            Name = "landtable_" + GenerateIdentifier();
            GeoName = "collist_" + GenerateIdentifier();
            GeoAnimName = "animlist_" + GenerateIdentifier();
        }

        /// <summary>
        /// Creates a new Landtable from existing collections
        /// </summary>
        /// <param name="geometry">Level geometry</param>
        /// <param name="metaData">Various meta data</param>
        /// <param name="format">Landtable Format</param>
        public LandTable(List<LandEntry> geometry, LandtableFormat format) : this(geometry, new List<LandEntryMotion>(), format)
        {

        }

        /// <summary>
        /// Creates an empty landtable
        /// </summary>
        /// <param name="format"></param>
        public LandTable(LandtableFormat format) : this(new List<LandEntry>(), new List<LandEntryMotion>(), format)
        {

        }

        /// <summary>
        /// Reads a landtable from a file <br/>
        /// Returns null if file is not valid
        /// </summary>
        /// <param name="filename">Path to the file to read</param>
        /// <returns></returns>
        public static LandTable ReadFile(string filename) => ReadFile(File.ReadAllBytes(filename));

        /// <summary>
        /// Reads a landtable from a file <br/>
        /// Returns null if file is not valid
        /// </summary>
        /// <param name="filename">Path to the file to read</param>
        /// <returns></returns>
        public static LandTable ReadFile(byte[] source)
        {
            bool be = BigEndian;
            BigEndian = false;

            ulong header = source.ToUInt64(0) & HeaderMask;
            byte version = source[7];
            switch(header)
            {
                case SA1LVL:
                case SA2LVL:
                case SA2BLVL:
                    if(version > CurrentVersion)
                    {
                        BigEndian = be;
                        return null;
                        //throw new FormatException("Not a valid SA1LVL/SA2LVL file.");
                    }
                    break;
                default:
                    return null;
            }

            MetaData metaData = MetaData.Read(source, version, false);
            Dictionary<uint, string> labels = new Dictionary<uint, string>(metaData.Labels);

            LandTable table;
            uint ltblAddress = source.ToUInt32(8);
            switch(header)
            {
                case SA1LVL:
                    table = Read(source, ltblAddress, 0, LandtableFormat.SA1, labels);
                    break;
                case SA2LVL:
                    table = Read(source, ltblAddress, 0, LandtableFormat.SA2, labels);
                    break;
                case SA2BLVL:
                    table = Read(source, ltblAddress, 0, LandtableFormat.SA2B, labels);
                    break;
                default:
                    return null;
            }
            table.MetaData = metaData;

            BigEndian = be;
            return table;
        }

        /// <summary>
        /// Reads a landtable from a byte array
        /// </summary>
        /// <param name="source"></param>
        /// <param name="address"></param>
        /// <param name="imageBase"></param>
        /// <param name="format"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static LandTable Read(byte[] source, uint address, uint imageBase, LandtableFormat format, Dictionary<uint, string> labels)
        {
            string name = labels.ContainsKey(address) ? labels[address] : "landtable_" + address.ToString("X8");
            float radius;
            uint flags = 0;

            List<LandEntry> geometry = new List<LandEntry>();
            string geomName;
            List<LandEntryMotion> anim = new List<LandEntryMotion>();
            string animName;
            Dictionary<uint, ModelData.Attach> attaches = new Dictionary<uint, ModelData.Attach>();
            string texName = "";
            uint texListPtr;

            uint tmpaddr;
            ushort geoCount = source.ToUInt16(address);
            switch(format)
            {
                case LandtableFormat.SA1:
                case LandtableFormat.SADX:
                    short anicnt = source.ToInt16(address + 2);
                    flags = source.ToUInt32(address + 4);
                    radius = source.ToSingle(address + 8);

                    tmpaddr = source.ToUInt32(address + 0xC);
                    if(tmpaddr != 0)
                    {
                        tmpaddr -= imageBase;
                        geomName = labels.ContainsKey(tmpaddr) ? labels[tmpaddr] : "collist_" + tmpaddr.ToString("X8");

                        for(int i = 0; i < geoCount; i++)
                        {
                            geometry.Add(LandEntry.Read(source, tmpaddr, imageBase, AttachFormat.BASIC, format, labels, attaches));
                            tmpaddr += 0x24;
                        }
                    }
                    else
                        geomName = "collist_" + GenerateIdentifier();

                    tmpaddr = source.ToUInt32(address + 0x10);
                    if(tmpaddr != 0)
                    {
                        tmpaddr -= imageBase;
                        animName = labels.ContainsKey(tmpaddr) ? labels[tmpaddr] : "animlist_" + tmpaddr.ToString("X8");

                        for(int i = 0; i < anicnt; i++)
                        {
                            anim.Add(LandEntryMotion.Read(source, tmpaddr, imageBase, AttachFormat.BASIC, format == LandtableFormat.SADX, labels, attaches));
                            tmpaddr += LandEntryMotion.Size;
                        }
                    }
                    else
                        animName = "animlist_" + GenerateIdentifier();

                    tmpaddr = source.ToUInt32(address + 0x14);
                    if(tmpaddr != 0)
                    {
                        tmpaddr -= imageBase;
                        texName = source.GetCString(tmpaddr, Encoding.ASCII);
                    }
                    texListPtr = source.ToUInt32(address + 0x18);

                    break;
                case LandtableFormat.SA2:
                case LandtableFormat.SA2B:
                    AttachFormat atcFmt = format == LandtableFormat.SA2 ? AttachFormat.CHUNK : AttachFormat.GC;

                    ushort visualCount = source.ToUInt16(address + 2);
                    radius = source.ToSingle(address + 0xC);

                    tmpaddr = source.ToUInt32(address + 0x10);
                    if(tmpaddr != 0)
                    {
                        tmpaddr -= imageBase;
                        geomName = labels.ContainsKey(tmpaddr) ? labels[tmpaddr] : "collist_" + tmpaddr.ToString("X8");

                        for(int i = 0; i < geoCount; i++)
                        {
                            geometry.Add(LandEntry.Read(source, tmpaddr, imageBase, i >= visualCount ? AttachFormat.BASIC : atcFmt, format, labels, attaches));
                            tmpaddr += 0x20;
                        }
                    }
                    else
                        geomName = "collist_" + GenerateIdentifier();

                    animName = "animlist_" + GenerateIdentifier();

                    tmpaddr = source.ToUInt32(address + 0x18);
                    if(tmpaddr != 0)
                    {
                        tmpaddr -= imageBase;
                        texName = source.GetCString(tmpaddr, Encoding.ASCII);
                    }
                    texListPtr = source.ToUInt32(address + 0x1C);

                    break;
                default:
                    throw new InvalidDataException("Landtable format not valid");
            }

            return new(geometry, anim, format)
            {
                Name = name,
                DrawDistance = radius,
                Flags = flags,
                GeoName = geomName,
                GeoAnimName = animName,
                TexListPtr = texListPtr,
                TextureFileName = texName
            };
        }

        /// <summary>
        /// Writes the landtable as an LVL file
        /// </summary>
        /// <param name="outputPath">Path to write the file to</param>
        public void WriteFile(string outputPath)
        {
            byte[] contents = WriteFile();
            File.WriteAllBytes(outputPath, contents);
        }

        /// <summary>
        /// Writes the landtable contents in file format and returns it as a  byte array
        /// </summary>
        /// <returns></returns>
        public byte[] WriteFile()
        {
            using(ExtendedMemoryStream stream = new ExtendedMemoryStream())
            {
                LittleEndianMemoryStream writer = new LittleEndianMemoryStream(stream);

                // writing indicator
                switch(Format)
                {
                    case LandtableFormat.SA1:
                    case LandtableFormat.SADX:
                        writer.WriteUInt64(SA1LVLVer);
                        break;
                    case LandtableFormat.SA2:
                        writer.WriteUInt64(SA2LVLVer);
                        break;
                    case LandtableFormat.SA2B:
                        writer.WriteUInt64(SA2BLVLVer);
                        break;
                }

                writer.WriteUInt64(0); // placeholders for landtable address and meta address

                Dictionary<string, uint> labels = new Dictionary<string, uint>();

                uint ltblAddress = Write(writer, 0, labels);
                writer.Stream.Seek(8, SeekOrigin.Begin);
                writer.WriteUInt32(ltblAddress);
                writer.Stream.Seek(0, SeekOrigin.End);

                MetaData.Write(writer, labels);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the landtable to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public uint Write(EndianMemoryStream writer, uint imageBase, Dictionary<string, uint> labels)
        {
            // sort the landentries
            List<ModelData.Attach> attaches = new List<ModelData.Attach>();

            ushort visCount = 0;
            if(Format > LandtableFormat.SADX)
            {
                List<LandEntry> visual = new List<LandEntry>();
                List<LandEntry> basic = new List<LandEntry>();

                foreach(LandEntry le in Geometry)
                {
                    if(le.Attach.Format == AttachFormat.BASIC)
                        basic.Add(le);
                    else
                        visual.Add(le);
                    attaches.Add(le.Attach);
                }

                Geometry.Clear();
                Geometry.AddRange(visual);
                Geometry.AddRange(basic);
                visCount = (ushort)visual.Count;
            }
            else
                foreach(LandEntry le in Geometry)
                    attaches.Add(le.Attach);

            foreach(LandEntryMotion lem in GeometryAnimations)
            {
                NJObject[] models = lem.Model.GetObjects();
                foreach(NJObject mdl in models)
                {
                    if(mdl.Attach != null)
                        attaches.Add(mdl.Attach);
                }
            }

            // writing all attaches
            foreach(var atc in attaches.Distinct())
            {
                atc.Write(writer, imageBase, false, labels);
            }

            // write the landentry models
            foreach(LandEntry le in Geometry)
                le.WriteModel(writer, imageBase, labels);

            // write the landentry motion models
            foreach(LandEntryMotion lem in GeometryAnimations)
                lem.Model.Write(writer, imageBase, labels);

            // write the landentry motion animations
            foreach(LandEntryMotion lem in GeometryAnimations)
                lem.Write(writer, labels);

            // writing the geometry list
            uint geomAddr = 0;
            if(Geometry.Count > 0)
            {
                if(labels.ContainsKey(GeoName))
                    geomAddr = labels[GeoName];
                else
                {
                    geomAddr = (uint)writer.Stream.Position + imageBase;
                    labels.Add(GeoName, geomAddr);
                    foreach(LandEntry le in Geometry)
                    {
                        le.Write(writer, Format, labels);
                    }
                }
            }

            // writing the animation list
            uint animAddr = 0;
            if(GeometryAnimations.Count > 0)
            {
                if(labels.ContainsKey(GeoAnimName))
                    animAddr = labels[GeoAnimName];
                else
                {
                    animAddr = (uint)writer.Stream.Position + imageBase;
                    labels.Add(GeoAnimName, animAddr);
                    foreach(LandEntryMotion lem in GeometryAnimations)
                    {
                        lem.Write(writer, labels);
                    }
                }
            }

            // write the texture name
            uint texNameAddr = 0;
            if(TextureFileName != null)
            {
                texNameAddr = (uint)writer.Stream.Position + imageBase;
                writer.Write(Encoding.ASCII.GetBytes(TextureFileName));
                writer.Write(new byte[1]);
            }

            // write the landtable struct itself
            uint address = (uint)writer.Stream.Position + imageBase;

            writer.WriteUInt16((ushort)Geometry.Count);
            if(Format < LandtableFormat.SA2) // sa1 + sadx
            {
                writer.WriteUInt16((ushort)GeometryAnimations.Count);
                writer.WriteUInt32(Flags);
                writer.WriteSingle(DrawDistance);
                writer.WriteUInt32(geomAddr);
                writer.WriteUInt32(animAddr);
                writer.WriteUInt32(texNameAddr);
                writer.WriteUInt32(TexListPtr);
                writer.WriteUInt64(0); // two unused pointers
            }
            else // sa2 + sa2b
            {
                writer.WriteUInt16(visCount);
                writer.WriteUInt64(0); // todo: figure out what these do
                writer.WriteSingle(DrawDistance);
                writer.WriteUInt32(geomAddr);
                writer.WriteUInt32(animAddr);
                writer.WriteUInt32(texNameAddr);
                writer.WriteUInt32(TexListPtr);
            }

            labels.Add(Name, address);
            return address;
        }
    }
}
