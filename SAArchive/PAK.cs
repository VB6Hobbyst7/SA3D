using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Pfim;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using SATools.SACommon.Ini;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAArchive
{
    /// <summary>
    /// PAK  Archive class
    /// </summary>
    public class PAK : Archive
    {
        /// <summary>
        /// PAK File header (START PAK)
        /// </summary>
        public const uint Header = 0x6B617001;

        /// <summary>
        /// Folder of the pak file
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Reads a PAK archive from a file
        /// </summary>
        /// <param name="filePath">Filepath</param>
        /// <returns></returns>
        public static PAK Read(string filePath)
            => Read(File.ReadAllBytes(filePath), Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant());

        /// <summary>
        /// Reads a PAK archive from a byte array
        /// </summary>
        /// <param name="source">Byte data</param>
        /// <param name="folderName">Root folder of the PAK</param>
        /// <returns></returns>
        public static PAK Read(byte[] source, string folderName)
        {
            PushBigEndian(false);

            PAK result = new()
            {
                FolderName = folderName
            };

            if(source.ToUInt32(0) != Header)
                throw new Exception("Error: Unknown archive type");

            int numfiles = source.ToInt32(0x39);
            string[] longpaths = new string[numfiles];
            string[] names = new string[numfiles];
            uint[] lengths = new uint[numfiles];
            uint tmpaddr = 0x3D;

            for(int i = 0; i < numfiles; i++)
            {
                uint stringLength = source.ToUInt32(tmpaddr);
                longpaths[i] = source.GetCString(tmpaddr += 4, Encoding.ASCII, stringLength);

                stringLength = source.ToUInt32(tmpaddr += stringLength);
                names[i] = source.GetCString(tmpaddr += 4, Encoding.ASCII, stringLength);

                lengths[i] = source.ToUInt32(tmpaddr += stringLength);
                tmpaddr += 8; // skipping an integer here
            }

            for(int i = 0; i < numfiles; i++)
            {
                byte[] entryData = new byte[lengths[i]];
                unsafe
                {
                    fixed(byte* ptr = entryData)
                    {
                        Marshal.Copy(source, (int)tmpaddr, (IntPtr)ptr, entryData.Length);

                    }
                }

                result.Entries.Add(new PAKEntry(Path.GetFileName(names[i]), longpaths[i], entryData));
                tmpaddr += lengths[i];
            }

            PopEndian();
            return result;
        }

        /// <summary>
        /// Gets the PAK textures sorted by inf (if it exists)
        /// </summary>
        /// <param name="fileNoExt"></param>
        /// <returns></returns>
        public List<PAKEntry> GetSortedEntries(string fileNoExt)
        {
            ArchiveEntry infEntry = Entries.Find(
                x => x.Name.Equals($"{fileNoExt}\\{fileNoExt}.inf", StringComparison.OrdinalIgnoreCase));

            // Get texture names from PAK INF, if it exists
            if(infEntry != null)
            {
                byte[] inf = infEntry.Data;
                List<PAKEntry> result = new(inf.Length / 0x3C);

                for(int i = 0; i < inf.Length; i += 0x3C)
                {
                    int j = 0;
                    while(j < 0x1C)
                    {
                        if(inf[i + j] == 0)
                            break;
                    }

                    string infName = Encoding.UTF8.GetString(inf, i, j);

                    ArchiveEntry gen = Entries.First(
                        (x) => x.Name.Equals($"{fileNoExt}\\{infName}.dds", StringComparison.OrdinalIgnoreCase));
                    result.Add((PAKEntry)gen);
                }
                return result;
            }
            else
            {
                // Otherwise get the original list
                List<PAKEntry> result = new();
                // But only add files that can be converted to Bitmap
                foreach(PAKEntry entry in Entries)
                {
                    string extension = Path.GetExtension(entry.Name).ToLowerInvariant();
                    switch(extension)
                    {
                        case ".dds":
                        case ".png":
                        case ".bmp":
                        case ".gif":
                        case ".jpg":
                            result.Add(entry);
                            break;
                        default:
                            break;
                    }
                }
                return result;
            }
        }

        public override void CreateIndexFile(string path)
        {
            Dictionary<string, PAKIniItem> list = new Dictionary<string, PAKIniItem>(Entries.Count);
            foreach(PAKEntry item in Entries)
            {
                list.Add(FolderName + "\\" + item.Name, new PAKIniItem(item.LongPath));
            }
            IniSerializer.Serialize(list, Path.Combine(Path.GetFileNameWithoutExtension(path), Path.GetFileNameWithoutExtension(path) + ".ini"));
        }

        public override byte[] GetBytes()
        {
            using ExtendedMemoryStream stream = new();
            using LittleEndianMemoryStream writer = new(stream);

            writer.Write(Header);
            writer.Write(new byte[33]);
            writer.Write(Entries.Count);
            byte[] totlen = BitConverter.GetBytes(Entries.Sum((a) => a.Data.Length));
            writer.Write(totlen);
            writer.Write(totlen);
            writer.Write(new byte[8]);
            writer.Write(Entries.Count);

            foreach(PAKEntry item in Entries)
            {
                string fullname = $"{FolderName}\\{item.Name}";
                writer.Write(item.LongPath.Length);
                writer.Write(item.LongPath.ToCharArray());
                writer.Write(fullname.Length);
                writer.Write(fullname.ToCharArray());
                writer.Write(item.Data.Length);
                writer.Write(item.Data.Length);
            }
            foreach(PAKEntry item in Entries)
                writer.Write(item.Data);

            return stream.ToArray();
        }

        /// <summary>
        /// Single PAK archive entry
        /// </summary>
        public class PAKEntry : ArchiveEntry
        {
            /// <summary>
            /// Full path to the entry
            /// </summary>
            public string LongPath { get; set; }

            public PAKEntry() : base()
            {
                LongPath = string.Empty;
            }

            public PAKEntry(string name, string longPath, byte[] data) : base(name, data)
            {
                LongPath = longPath;
            }

            public override Bitmap GetBitmap()
            {
                using ExtendedMemoryStream str = new(Data);

                // If not DDS header
                if(BitConverter.ToUInt32(Data, 0) != 0x20534444)
                {
                    return new Bitmap(str);
                }


                IImage image = Pfim.Pfim.FromStream(str, new PfimConfig());
                PixelFormat pxformat = image.Format switch
                {
                    Pfim.ImageFormat.Rgba32 => PixelFormat.Format32bppArgb,
                    _ => throw new Exception("Error: Unknown image format"),
                };

                Bitmap bitmap = new(image.Width, image.Height, pxformat);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, pxformat);
                Marshal.Copy(image.Data, 0, bmpData.Scan0, image.DataLen);
                bitmap.UnlockBits(bmpData);

                return bitmap;
            }

        }

        /// <summary>
        /// Ini file container for pak files
        /// </summary>
        internal class PAKIniItem
        {
            /// <summary>
            /// Full file path
            /// </summary>
            public string LongPath { get; set; }

            public PAKIniItem(string longPath)
            {
                LongPath = longPath;
            }

            public PAKIniItem() { }
        }
    }
}
