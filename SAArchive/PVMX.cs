using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace SATools.SAArchive
{
    public class PVMX : Archive
    {
        public const int Header = 0x584D5650; // 'PVMX'
        const byte Version = 1;

        public override void CreateIndexFile(string path)
        {
            using (TextWriter texList = File.CreateText(Path.Combine(path, "index.txt")))
            {
                for (int u = 0; u < Entries.Count; u++)
                {
                    byte[] tdata = Entries[u].Data;
                    string entry;
                    PVMXEntry pvmxentry = (PVMXEntry)Entries[u];
                    string dimensions = string.Join("x", pvmxentry.Width.ToString(), pvmxentry.Height.ToString());
                    if (pvmxentry.HasDimensions())
                        entry = string.Join(",", pvmxentry.GBIX.ToString(), pvmxentry.Name, dimensions);
                    else
                        entry = string.Join(",", pvmxentry.GBIX.ToString(), pvmxentry.Name);
                    texList.WriteLine(entry);
                }
                texList.Flush();
                texList.Close();
            }
        }

        public PVMX(byte[] pvmxdata)
        {
            Entries = new List<ArchiveEntry>();
            if (!(pvmxdata.Length > 4 && BitConverter.ToInt32(pvmxdata, 0) == 0x584D5650))
                throw new FormatException("File is not a PVMX archive.");
            if (pvmxdata[4] != 1)
                throw new FormatException("Incorrect PVMX archive version.");
            int off = 5;
            dictionary_field type;
            for (type = (dictionary_field)pvmxdata[off++]; type != dictionary_field.none; type = (dictionary_field)pvmxdata[off++])
            {
                string name = "";
                uint gbix = 0;
                int width = 0;
                int height = 0;
                while (type != dictionary_field.none)
                {
                    switch (type)
                    {
                        case dictionary_field.global_index:
                            gbix = BitConverter.ToUInt32(pvmxdata, off);
                            off += sizeof(uint);
                            break;

                        case dictionary_field.name:
                            int count = 0;
                            while (pvmxdata[off + count] != 0)
                                count++;
                            name = System.Text.Encoding.UTF8.GetString(pvmxdata, off, count);
                            off += count + 1;
                            break;

                        case dictionary_field.dimensions:
                            width = BitConverter.ToInt32(pvmxdata, off);
                            off += sizeof(int);
                            height = BitConverter.ToInt32(pvmxdata, off);
                            off += sizeof(int);
                            break;
                    }

                    type = (dictionary_field)pvmxdata[off++];

                }
                ulong offset = BitConverter.ToUInt64(pvmxdata, off);
                off += sizeof(ulong);
                ulong length = BitConverter.ToUInt64(pvmxdata, off);
                off += sizeof(ulong);
                byte[] texdata = new byte[(int)length];
                Array.Copy(pvmxdata, (int)offset, texdata, 0, (int)length);
                //Console.WriteLine("Added entry {0} at {1} GBIX {2} width {3} height {4}", name, off, gbix, width, height);
                Entries.Add(new PVMXEntry(name, gbix, texdata, width, height));
            }
        }

        public PVMX()
        {
            Entries = new List<ArchiveEntry>();
        }

        public void AddFile(string name, uint gbix, byte[] data, int width = 0, int height = 0)
        {
            Entries.Add(new PVMXEntry(name, gbix, data, width, height));
        }

        public override byte[] GetBytes()
        {
            MemoryStream str = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(str);
            bw.Write(Header);
            bw.Write(Version);
            List<OffData> texdata = new List<OffData>();
            foreach (PVMXEntry tex in Entries)
            {
                bw.Write((byte)dictionary_field.global_index);
                bw.Write(tex.GBIX);
                bw.Write((byte)dictionary_field.name);
                bw.Write(tex.Name.ToCharArray());
                bw.Write((byte)0);
                if (tex.HasDimensions())
                {
                    bw.Write((byte)dictionary_field.dimensions);
                    bw.Write(tex.Width);
                    bw.Write(tex.Height);
                }
                bw.Write((byte)dictionary_field.none);
                long size;
                using (MemoryStream ms = new MemoryStream(tex.Data))
                {
                    texdata.Add(new OffData(str.Position, ms.ToArray()));
                    size = ms.Length;
                }
                bw.Write(0ul);
                bw.Write(size);
            }
            bw.Write((byte)dictionary_field.none);
            foreach (OffData od in texdata)
            {
                long pos = str.Position;
                str.Position = od.off;
                bw.Write(pos);
                str.Position = pos;
                bw.Write(od.data);
            }
            return str.ToArray();
        }

        public override TextureSet ToTextureSet()
        {
            TextureSet result = new();

            foreach (PVMXEntry entry in Entries)
            {
                result.Textures.Add(new Texture(entry.Name, entry.GetBitmap(), new(entry.Width, entry.Height)));
            }

            return result;
        }

        public class PVMXEntry : ArchiveEntry
        {
            public int Width { get; set; }

            public int Height { get; set; }

            public uint GBIX { get; set; }

            public PVMXEntry(string name, uint gbix, byte[] data, int width, int height)
            {
                Name = name;
                Width = width;
                Height = height;
                Data = data;
                GBIX = gbix;
            }

            public bool HasDimensions()
            {
                if (Width != 0 || Height != 0)
                    return true;
                else
                    return false;
            }

            public override Bitmap GetBitmap()
            {
                using MemoryStream ms = new MemoryStream(Data);
                return new Bitmap(ms);
            }
        }

        struct OffData
        {
            public long off;
            public byte[] data;

            public OffData(long o, byte[] d)
            {
                off = o;
                data = d;
            }
        }

        enum dictionary_field : byte
        {
            none,
            /// <summary>
            /// 32-bit integer global index
            /// </summary>
            global_index,
            /// <summary>
            /// Null-terminated file name
            /// </summary>
            name,
            /// <summary>
            /// Two 32-bit integers defining width and height
            /// </summary>
            dimensions,
        }
    }
}
