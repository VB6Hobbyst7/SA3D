using PuyoTools.Modules.Archive;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VrSharp;
using VrSharp.GvrTexture;
using VrSharp.PvrTexture;

namespace SATools.SAArchive
{
    public enum PuyoArchiveType
    {
        Unknown,
        PVMFile,
        GVMFile,
    }

    public class Puyo : Archive
    {
        public const uint Header_PVM = 0x484D5650; // PVMH
        public const uint Header_GVM = 0x484D5647; // GVMH

        public bool PaletteRequired;
        public PuyoArchiveType Type;

        public override void CreateIndexFile(string path)
        {
            using(TextWriter texList = File.CreateText(Path.Combine(path, "index.txt")))
            {
                foreach(ArchiveEntry pvmentry in Entries)
                {
                    texList.WriteLine(pvmentry.Name);
                }
                texList.Flush();
                texList.Close();
            }
        }

        public static PuyoArchiveType Identify(byte[] data)
        {
            uint magic = BitConverter.ToUInt32(data, 0);
            switch(magic)
            {
                case Header_PVM:
                    return PuyoArchiveType.PVMFile;
                case Header_GVM:
                    return PuyoArchiveType.GVMFile;
                default:
                    return PuyoArchiveType.Unknown;
            }
        }

        // TODO make addpalette not use forms
        /*public void AddPalette(string startPath)
        {
            VpPalette Palette = null;
            bool gvm = Type == PuyoArchiveType.GVMFile;
            using(System.Windows.Forms.OpenFileDialog a = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = gvm ? "gvp" : "pvp",
                Filter = gvm ? "GVP Files|*.gvp" : "PVP Files|*.pvp",
                InitialDirectory = startPath,
                Title = "External palette file"
            })
            {
                if(a.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    Palette = gvm ? (VpPalette)new GvpPalette(a.FileName) : (VpPalette)new PvpPalette(a.FileName);
            }
            foreach(ArchiveEntry entry in Entries)
            {
                if(entry is PVMEntry pvme)
                {
                    PvrTexture pvrt = new PvrTexture(pvme.Data);
                    if(pvrt.NeedsExternalPalette)
                        pvme.Palette = (PvpPalette)Palette;
                }
                else if(entry is GVMEntry gvme)
                {
                    GvrTexture gvrt = new GvrTexture(gvme.Data);
                    if(gvrt.NeedsExternalPalette)
                        gvme.Palette = (GvpPalette)Palette;
                }
            }
        }*/

        public Puyo() { }

        public Puyo(byte[] pvmdata)
        {
            ArchiveBase puyobase;
            Entries = new List<ArchiveEntry>();

            Type = Identify(pvmdata);
            switch(Type)
            {
                case PuyoArchiveType.PVMFile:
                    puyobase = new PvmArchive();
                    break;
                case PuyoArchiveType.GVMFile:
                    puyobase = new GvmArchive();
                    break;
                default:
                    throw new Exception("Error: Unknown archive format");
            }

            ArchiveReader archiveReader = puyobase.Open(pvmdata);
            foreach(var puyoentry in archiveReader.Entries)
            {
                MemoryStream vrstream = (MemoryStream)(puyoentry.Open());
                switch(Type)
                {
                    case PuyoArchiveType.PVMFile:
                        PvrTexture pvrt = new PvrTexture(vrstream);
                        if(pvrt.NeedsExternalPalette)
                            PaletteRequired = true;
                        Entries.Add(new PVMEntry(vrstream.ToArray(), Path.GetFileName(puyoentry.Name)));
                        break;
                    case PuyoArchiveType.GVMFile:
                        GvrTexture gvrt = new GvrTexture(vrstream);
                        if(gvrt.NeedsExternalPalette)
                            PaletteRequired = true;
                        Entries.Add(new GVMEntry(vrstream.ToArray(), Path.GetFileName(puyoentry.Name)));
                        break;
                }
            }
        }

        public override byte[] GetBytes()
        {
            MemoryStream pvmStream = new MemoryStream();
            ArchiveBase pvmbase = new PvmArchive();
            ArchiveWriter puyoArchiveWriter = pvmbase.Create(pvmStream);
            foreach(PVMEntry tex in Entries)
            {
                MemoryStream ms = new MemoryStream(tex.Data);
                puyoArchiveWriter.CreateEntry(ms, tex.Name);
            }
            puyoArchiveWriter.Flush();
            return pvmStream.ToArray();
        }
    
        public class PVMEntry : ArchiveEntry
    {
        public uint GBIX;
        public PvpPalette Palette;

        public PVMEntry(byte[] pvrdata, string name)
        {
            Name = name;
            Data = pvrdata;
            PvrTexture pvrt = new PvrTexture(pvrdata);
            GBIX = pvrt.GlobalIndex;
        }

        public PVMEntry(string filename)
        {
            Name = Path.GetFileName(filename);
            Data = File.ReadAllBytes(filename);
            PvrTexture pvrt = new PvrTexture(Data);
            GBIX = pvrt.GlobalIndex;
        }

        public uint GetGBIX()
        {
            return GBIX;
        }

        public override Bitmap GetBitmap()
        {
            PvrTexture pvrt = new PvrTexture(Data);
            if(pvrt.NeedsExternalPalette)
                pvrt.SetPalette(Palette);
            return pvrt.ToBitmap();
        }
    }

    public class GVMEntry : ArchiveEntry
    {
        public uint GBIX;
        public GvpPalette Palette;

        public GVMEntry(byte[] gvrdata, string name)
        {
            Name = name;
            Data = gvrdata;
            GvrTexture gvrt = new GvrTexture(gvrdata);
            GBIX = gvrt.GlobalIndex;
        }

        public GVMEntry(string filename)
        {
            Name = Path.GetFileName(filename);
            Data = File.ReadAllBytes(filename);
            GvrTexture gvrt = new GvrTexture(Data);
            GBIX = gvrt.GlobalIndex;
        }

        public uint GetGBIX()
        {
            return GBIX;
        }

        public override Bitmap GetBitmap()
        {
            GvrTexture gvrt = new GvrTexture(Data);
            if(gvrt.NeedsExternalPalette)
                gvrt.SetPalette(Palette);
            return gvrt.ToBitmap();
        }
    }
    }


}
