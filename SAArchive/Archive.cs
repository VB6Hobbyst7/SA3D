using System.Collections.Generic;
using System.Drawing;
using System.IO;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAArchive
{
    /// <summary>
    /// Generic Archive class
    /// </summary>
    public abstract class Archive
    {
        /// <summary>
        /// File entries in this archive
        /// </summary>
        public List<ArchiveEntry> Entries { get; set; }
            = new List<ArchiveEntry>();

        /// <summary>
        /// Writes the archive to a file
        /// </summary>
        /// <param name="outputFile">Filepapth to write to</param>
        public void WriteToFile(string outputFile)
            => File.WriteAllBytes(outputFile, GetBytes());

        /// <summary>
        /// Returns the Archive as a byte array
        /// </summary>
        public abstract byte[] GetBytes();

        /// <summary>
        /// Creates an index/metadata file for unpacked archives
        /// </summary>
        /// <param name="path">Filepapth to write to</param>
        public abstract void CreateIndexFile(string path);

        public static Archive ReadFile(string filePath)
        {
            PushBigEndian(false);

            Archive result = null;
            byte[] data = File.ReadAllBytes(filePath);

            uint header4 = data.ToUInt32(0);
            ulong header8 = data.ToUInt64(0);
            string header16 = data.GetCString(0, System.Text.Encoding.ASCII, 16);

            if (header4 == PAK.Header)
                result = PAK.Read(data, Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant());
            else if (header16.StartsWith("archive  V2."))
                result = DAT.Read(data);
            else if (header4 == Puyo.Header_GVM || header4 == Puyo.Header_PVM)
                result = new Puyo(data);
            else if (header4 == PVMX.Header)
                result = new PVMX(data);

            PopEndian();
            return result;
        }

        public virtual TextureSet ToTextureSet()
        {
            TextureSet result = new();

            foreach (var entry in Entries)
            {
                if (!entry.Name.EndsWith(".inf"))
                    result.Textures.Add(new Texture(entry.Name, entry.GetBitmap()));
            }

            return result;
        }

        /// <summary>
        /// Single entry in the archive
        /// </summary>
        public abstract class ArchiveEntry
        {
            /// <summary>
            /// Name of the Entry
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Raw Data contained in the entry
            /// </summary>
            public byte[] Data { get; set; }

            public ArchiveEntry(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }

            public ArchiveEntry()
            {
                Name = string.Empty;
            }

            /// <summary>
            /// Returns the Entry as a texture (if it is one)
            /// </summary>
            /// <returns></returns>
            public abstract Bitmap GetBitmap();

            public override string ToString()
                => $"{Name} - [{Data?.Length ?? 0}]";

        }
    }
}
