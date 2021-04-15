﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VrSharp.PvrTexture;

namespace SATools.SAArchive
{
    public class PB : Archive
    {
        public override void CreateIndexFile(string path)
        {
            using(TextWriter texList = File.CreateText(Path.Combine(path, "index.txt")))
            {
                for(int u = 0; u < Entries.Count; u++)
                {
                    texList.WriteLine(u.ToString("D3") + ".pvr");
                }
                texList.Flush();
                texList.Close();
            }
        }

        public PB(byte[] pbdata)
        {
            Entries = new List<ArchiveEntry>();
            int numtextures = pbdata[4];
            for(int u = 0; u < numtextures; u++)
            {
                Entries.Add(new PBEntry(pbdata, 8 + 16 * u, u.ToString("D3") + ".pvr"));
                //Console.WriteLine("Added header {0}: offset {1}, pixel format {2}, data format {3}, GBIX {4}, width {5}, height {6}", u, hdr.Offset, hdr.PixelFormat, hdr.DataFormat, hdr.GBIX, hdr.Width, hdr.Height);
            }
            for(int u = 0; u < numtextures; u++)
            {
                PBEntry pbentry = (PBEntry)Entries[u];
                int chunksize;
                if(u == numtextures - 1)
                    chunksize = pbdata.Length - pbentry.Offset;
                else
                {
                    PBEntry pbentry_1 = (PBEntry)Entries[u + 1];
                    chunksize = pbentry_1.Offset - pbentry.Offset;
                }
                byte[] headerless = new byte[chunksize];
                Array.Copy(pbdata, pbentry.Offset, headerless, 0, chunksize);
                pbentry.Data = pbentry.GetPVR(headerless);
                //Console.WriteLine("Added data: offset {0}, length {1}", headers[u].Offset, pbchunk.Length);
            }
        }

        public PB(int count)
        {
            Entries = new List<ArchiveEntry>(count);
        }

        public PB() { }

        public int GetCurrentOffset(int index, int total)
        {
            int offset_base = 8 + 16 * total;
            if(index == 0)
                return offset_base;
            for(int u = 0; u < index; u++)
            {
                PBEntry entry = (PBEntry)Entries[u];
                offset_base += entry.GetHeaderless().Length;
            }
            return offset_base;
        }


        public override byte[] GetBytes()
        {
            List<byte> result = new List<byte>();
            result.Add(0x50); // P
            result.Add(0x56); // B
            result.Add(0x42); // V
            result.Add(0x02); // Version ID
            result.AddRange(BitConverter.GetBytes((uint)Entries.Count));
            for(int u = 0; u < Entries.Count; u++)
            {
                PBEntry entry = (PBEntry)Entries[u];
                result.AddRange(entry.GetHeader());
            }
            for(int u = 0; u < Entries.Count; u++)
            {
                PBEntry entry = (PBEntry)Entries[u];
                result.AddRange(entry.GetHeaderless());
            }
            return result.ToArray();
        }

        public class PBEntry : ArchiveEntry
        {
            public int Offset { get; set; }
            public PvrPixelFormat PixelFormat { get; set; }
            public PvrDataFormat DataFormat { get; set; }
            public uint GBIX { get; set; }
            public ushort Width { get; set; }
            public ushort Height { get; set; }

            public byte[] GetHeader()
            {
                List<byte> result = new List<byte>();
                result.AddRange(BitConverter.GetBytes(Offset));
                result.Add((byte)PixelFormat);
                result.Add((byte)DataFormat);
                result.Add(0);
                result.Add(0);
                result.AddRange(BitConverter.GetBytes(GBIX));
                result.AddRange(BitConverter.GetBytes(Width));
                result.AddRange(BitConverter.GetBytes(Height));
                return result.ToArray();
            }

            public byte[] GetHeaderless()
            {
                int length = BitConverter.ToInt32(Data, 20) - 8;
                byte[] result = new byte[length];
                Array.Copy(Data, 32, result, 0, length);
                return result;
            }

            public PBEntry(byte[] pbdata, int tempaddr, string name)
            {
                Name = name;
                Offset = BitConverter.ToInt32(pbdata, tempaddr);
                PixelFormat = (PvrPixelFormat)pbdata[tempaddr + 4];
                DataFormat = (PvrDataFormat)pbdata[tempaddr + 5];
                GBIX = BitConverter.ToUInt32(pbdata, tempaddr + 8);
                Width = BitConverter.ToUInt16(pbdata, tempaddr + 12);
                Height = BitConverter.ToUInt16(pbdata, tempaddr + 14);
            }

            public PBEntry(string filename, int offset)
            {
                Data = File.ReadAllBytes(filename);
                PvrTexture pvrt = new PvrTexture(Data);
                byte[] data = GetHeaderless();
                Name = Path.GetFileNameWithoutExtension(filename);
                Offset = offset;
                PixelFormat = pvrt.PixelFormat;
                DataFormat = pvrt.DataFormat;
                GBIX = pvrt.GlobalIndex;
                Width = pvrt.TextureWidth;
                Height = pvrt.TextureHeight;
            }

            public PBEntry(int offset, PvrPixelFormat pxformat, PvrDataFormat dataformat, uint gbix, ushort width, ushort height)
            {
                Offset = offset;
                PixelFormat = pxformat;
                DataFormat = dataformat;
                GBIX = gbix;
                Width = width;
                Height = height;
            }

            public override Bitmap GetBitmap()
            {
                return new PvrTexture(Data).ToBitmap();
            }

            public byte[] GetPVR(byte[] data)
            {
                List<byte> result = new List<byte>();
                int chunksize_file = data.Length;
                // Make chunk size divisible by 16 because it crashes otherwise
                if(chunksize_file % 16 != 0)
                {
                    do
                    {
                        chunksize_file++;
                    }
                    while(chunksize_file % 16 != 0);
                }
                byte[] gbixheader = { 0x47, 0x42, 0x49, 0x58 };
                byte[] pvrtheader = { 0x50, 0x56, 0x52, 0x54 };
                byte[] padding = { 0x20, 0x20, 0x20, 0x20 };
                result.AddRange(gbixheader);
                result.AddRange(BitConverter.GetBytes(8));
                result.AddRange(BitConverter.GetBytes(GBIX));
                result.AddRange(padding);
                result.AddRange(pvrtheader);
                result.AddRange(BitConverter.GetBytes(chunksize_file + 8));
                result.Add((byte)PixelFormat);
                result.Add((byte)DataFormat);
                result.Add(0);
                result.Add(0);
                result.AddRange(BitConverter.GetBytes(Width));
                result.AddRange(BitConverter.GetBytes(Height));
                result.AddRange(data);
                int pd = 0;
                // Make file size divisible by 16 because it crashes otherwise
                if(result.Count % 16 != 0)
                {
                    do
                    {
                        result.Add(0);
                        pd++;
                    }
                    while(result.Count % 16 != 0);
                }
                return result.ToArray();
            }
        }
    }


}
