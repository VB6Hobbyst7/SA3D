using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
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
        public LandtableFormat Format { get; private set; }

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

            string identifier = GenerateIdentifier();
            Name = "landtable_" + identifier;
            GeoName = "collist_" + identifier;
            GeoAnimName = "animlist_" + identifier;
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

        public void BufferLandtable(bool optimize = true)
        {
            LandtableFormat format = Format;
            ConvertToFormat(LandtableFormat.Buffer, optimize, false);
            Format = format;
        }

        public void ConvertToFormat(LandtableFormat newFormat, bool optimize, bool forceUpdate)
        {
            if(newFormat == Format && !forceUpdate)
                return;

            NJObject dummyModel = new();

            void convertAttaches(AttachFormat format, HashSet<Attach> attaches, Dictionary<Attach, Attach> attachMap, HashSet<LandEntry> landentries)
            {
                foreach(Attach atc in attaches)
                {
                    dummyModel.Attach = atc;
                    dummyModel.ConvertAttachFormat(format, optimize, false, forceUpdate);
                    attachMap.Add(atc, dummyModel.Attach);
                }

                if(format == AttachFormat.Buffer)
                    return;

                foreach(LandEntry le in landentries)
                {
                    le.Attach = attachMap[le.Attach];
                }
            }

            var newAtcFormat = newFormat switch
            {
                LandtableFormat.SA1 or LandtableFormat.SADX => AttachFormat.BASIC,
                LandtableFormat.SA2 => AttachFormat.CHUNK,
                LandtableFormat.SA2B => AttachFormat.GC,
                _ => AttachFormat.Buffer,
            };

            if(newFormat <= LandtableFormat.SADX || newFormat == LandtableFormat.Buffer)
            {
                HashSet<Attach> attaches = Geometry.Select(x => x.Attach).ToHashSet();
                convertAttaches(newAtcFormat, attaches, new(), new(Geometry));
            }
            else
            {
                if(Format <= LandtableFormat.SADX || Format == LandtableFormat.Buffer)
                {
                    // attaches that are used for rendering
                    HashSet<Attach> visualAttaches = new();

                    // Attaches that are used for collision
                    HashSet<Attach> collisionAttaches = new();

                    HashSet<LandEntry> visualLandEntries = new();
                    HashSet<LandEntry> collisionLandEntries = new();

                    // For sa1/dx, of which a landentry can be used for both, collision and rendering
                    HashSet<LandEntry> hybridLandEntries = new();

                    foreach(LandEntry le in Geometry)
                    {
                        bool isCollision = le.SurfaceFlags.IsCollision();
                        bool isVisual = !isCollision || le.SurfaceFlags.HasFlag(SurfaceFlags.Visible);
                        // if its neither, we'll just keep it as an invisible visual model. just in case

                        if(isCollision)
                        {
                            collisionAttaches.Add(le.Attach);
                            collisionLandEntries.Add(le);
                        }

                        if(isVisual)
                        {
                            visualAttaches.Add(le.Attach);
                            visualLandEntries.Add(le);
                        }

                        if(isVisual && isCollision)
                            hybridLandEntries.Add(le);
                    }

                    visualLandEntries.RemoveWhere(x => hybridLandEntries.Contains(x));
                    collisionLandEntries.RemoveWhere(x => hybridLandEntries.Contains(x));

                    Dictionary<Attach, Attach> visualAttachMap = new();
                    convertAttaches(newAtcFormat, visualAttaches, visualAttachMap, visualLandEntries);

                    Dictionary<Attach, Attach> collisionAttachMap = new();
                    convertAttaches(AttachFormat.BASIC, collisionAttaches, collisionAttachMap, collisionLandEntries);

                    foreach(LandEntry le in hybridLandEntries)
                    {
                        // the copy will act as collision
                        LandEntry copy = le.ShallowCopy();
                        copy.Attach = collisionAttachMap[le.Attach];
                        Geometry.Add(copy);

                        le.Attach = visualAttachMap[le.Attach];
                    }
                    
                }
                else // when converting between sa2 formats
                {
                    // the collision format for sa2 and sa2b is the same, no conversion needed
                    HashSet<LandEntry> visualGeometry = Geometry.Where(x => x.Attach.Format != AttachFormat.BASIC).ToHashSet();
                    HashSet<Attach> attaches = visualGeometry.Select(x => x.Attach).ToHashSet();
                    convertAttaches(newAtcFormat, attaches, new(), new(visualGeometry));
                }
            }

            Format = newFormat;
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
            PushBigEndian(false);

            ulong header = source.ToUInt64(0) & HeaderMask;
            byte version = source[7];
            switch(header)
            {
                case SA1LVL:
                case SA2LVL:
                case SA2BLVL:
                    if(version > CurrentVersion)
                    {
                        PopEndian();
                        return null;
                        //throw new FormatException("Not a valid SA1LVL/SA2LVL file.");
                    }
                    break;
                default:
                    return null;
            }

            MetaData metaData = MetaData.Read(source, version, false);
            Dictionary<uint, string> labels = new(metaData.Labels);

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

            PopEndian();
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

            string identifier = GenerateIdentifier();

            List<LandEntry> geometry = new();
            string geomName;
            List<LandEntryMotion> anim = new();
            string animName;
            Dictionary<uint, Attach> attaches = new();
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
                        geomName = "collist_" + identifier;

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
                        animName = "animlist_" + identifier;

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
                        geomName = "collist_" + identifier;

                    animName = "animlist_" + identifier;

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
            using ExtendedMemoryStream stream = new();
            using EndianWriter writer = new(stream);

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

            Dictionary<string, uint> labels = new();

            uint ltblAddress = Write(writer, 0, labels);
            writer.Stream.Seek(8, SeekOrigin.Begin);
            writer.WriteUInt32(ltblAddress);
            writer.Stream.Seek(0, SeekOrigin.End);

            MetaData.Write(writer, labels);

            return stream.ToArray();
        }

        /// <summary>
        /// Writes the landtable to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public uint Write(EndianWriter writer, uint imageBase, Dictionary<string, uint> labels)
        {
            // sort the landentries
            HashSet<Attach> attaches = new();

            ushort visCount = 0;
            if(Format > LandtableFormat.SADX && Format != LandtableFormat.Buffer)
            {
                List<LandEntry> visual = new();
                List<LandEntry> basic = new();

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
            if(Format == LandtableFormat.Buffer)
            {
                foreach(var atc in attaches)
                    atc.WriteBuffer(writer, imageBase, labels);
            }
            else
            {
                foreach(var atc in attaches)
                    atc.Write(writer, imageBase, false, labels);
            }

            var t  =labels.GroupBy(x => x.Value).Where(x => x.Count() > 1).ToList();

            // write the landentry models
            foreach(LandEntry le in Geometry)
                le.WriteModel(writer, imageBase, labels);

            // write the landentry motion models
            foreach(LandEntryMotion lem in GeometryAnimations)
                lem.Model.Write(writer, imageBase, labels);

            Dictionary<Action, uint> actionAddresses = new();

            // write the landentry motion animations
            foreach(LandEntryMotion lem in GeometryAnimations)
                actionAddresses.Add(lem.MotionAction, lem.MotionAction.Write(writer, imageBase, Format == LandtableFormat.SADX, Format == LandtableFormat.Buffer, labels));

            // writing the geometry list
            uint geomAddr = 0;
            if(Geometry.Count > 0)
            {
                if(labels.ContainsKey(GeoName))
                    geomAddr = labels[GeoName];
                else
                {
                    geomAddr = writer.Position + imageBase;
                    labels.AddLabel(GeoName, geomAddr);
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
                    animAddr = writer.Position + imageBase;
                    labels.AddLabel(GeoAnimName, animAddr);
                    foreach(LandEntryMotion lem in GeometryAnimations)
                    {
                        lem.Write(writer, actionAddresses, labels);
                    }
                }
            }

            // write the texture name
            uint texNameAddr = 0;
            if(TextureFileName != null)
            {
                texNameAddr = writer.Position + imageBase;
                writer.Write(Encoding.ASCII.GetBytes(TextureFileName));
                writer.Write(new byte[1]);
            }

            // write the landtable struct itself
            uint address = writer.Position + imageBase;

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

            labels.AddLabel(Name, address);
            return address;
        }
    }
}
