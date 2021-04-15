using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAArchive
{
    public class DAT : Archive
    {
        /// <summary>
        /// Whether the archive is of the steam version
        /// </summary>
        public bool Steam { get; set; }

        public override void CreateIndexFile(string path)
        {
            using TextWriter tw = File.CreateText(Path.Combine(path, "index.txt"));

            Entries.Sort((f1, f2) => StringComparer.OrdinalIgnoreCase.Compare(f1.Name, f2.Name));
            for(int i = 0; i < Entries.Count; i++)
            {
                tw.WriteLine(Entries[i].Name);
            }
            tw.Flush();
            tw.Close();
        }
        
        /// <summary>
        /// Reads a dat archive from a file
        /// </summary>
        /// <param name="filepath">Filepath to the dat file</param>
        /// <returns></returns>
        public static DAT Read(string filepath)
            => Read(File.ReadAllBytes(filepath));

        public static DAT Read(byte[] source)
        {
            PushBigEndian(false);

            DAT result = new();

            result.Steam = source.GetCString(0, 0x10) switch
            {
                "archive  V2.2\0\0\0" => false,
                "archive  V2.DMZ\0" => true,
                _ => throw new Exception("Error: Unknown archive type"),
            };

            int count = source.ToInt32(0x10);

            for(int i = 0; i < count; i++)
            {
                result.Entries.Add(DATEntry.Read(source, (uint)(0x14 + (i * 0xC))));
            }

            PopEndian();
            return result;
        }

        /// <summary>
        /// Processes and return an entries file data
        /// </summary>
        /// <param name="index">File index</param>
        public byte[] GetFile(int index)
            => CompressDAT.ProcessBuffer(Entries[index].Data);

        /// <summary>
        /// Checks whether a file is compressed
        /// </summary>
        /// <param name="index">File index</param>
        /// <returns></returns>
        public bool IsFileCompressed(int index)
            => CompressDAT.isFileCompressed(Entries[index].Data);

        /// <summary>
        /// Replaces an entry
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        public void ReplaceFile(string path, int index)
            => Entries[index] = new DATEntry(path);

        public override byte[] GetBytes()
        {
            int fsize = 0x14;
            int hloc = fsize;
            fsize += Entries.Count * 0xC;
            int tloc = fsize;

            foreach(DATEntry item in Entries)
                fsize += item.Name.Length + 1;

            int floc = fsize;
            foreach(DATEntry item in Entries)
                fsize += item.Data.Length;

            byte[] file = new byte[fsize];

            Encoding.ASCII.GetBytes(Steam ? "archive  V2.DMZ" : "archive  V2.2").CopyTo(file, 0);
            BitConverter.GetBytes(Entries.Count).CopyTo(file, 0x10);
            foreach(DATEntry item in Entries)
            {
                BitConverter.GetBytes(tloc).CopyTo(file, hloc);
                hloc += 4;
                System.Text.Encoding.ASCII.GetBytes(item.Name).CopyTo(file, tloc);
                tloc += item.Name.Length + 1;
                BitConverter.GetBytes(floc).CopyTo(file, hloc);
                hloc += 4;
                item.Data.CopyTo(file, floc);
                floc += item.Data.Length;
                BitConverter.GetBytes(item.Data.Length).CopyTo(file, hloc);
                hloc += 4;
            }

            return file;
        }

        public class DATEntry : ArchiveEntry
        {

            public DATEntry() : base() { }

            public DATEntry(string fileName)
            {
                Name = Path.GetFileName(fileName);
                Data = File.ReadAllBytes(fileName);
            }

            /// <summary>
            /// Reads a Dat entrx from a byte array
            /// </summary>
            /// <param name="source"></param>
            /// <param name="address"></param>
            /// <returns></returns>
            public static DATEntry Read(byte[] source, uint address)
            {
                string name = source.GetCString(source.ToUInt32(address));
                byte[] data = new byte[source.ToInt32(address + 8)];
                int dataOffset = source.ToInt32(address + 4);

                unsafe
                {
                    fixed(byte* ptr = data)
                    {
                        Marshal.Copy(source, (int)dataOffset, (IntPtr)ptr, data.Length);

                    }
                }


                return new DATEntry()
                {
                    Name = name,
                    Data = data
                };
            }

            public override Bitmap GetBitmap()
            {
                using MemoryStream str = new(Data);
                return new Bitmap(str);
            }
        }

        /// <summary>
        /// Dat compresser class
        /// </summary>
        public static class CompressDAT
        {
            const uint SLIDING_LEN = 0x1000;
            const uint SLIDING_MASK = 0xFFF;

            const byte NIBBLE_HIGH = 0xF0;
            const byte NIBBLE_LOW = 0x0F;

            //TODO: Documentation for OffsetLengthPair
            struct OffsetLengthPair
            {
                public byte highByte, lowByte;

                //TODO: Write Setter for Offset
                public int Offset
                {
                    get => ((lowByte & NIBBLE_HIGH) << 4) | highByte;
                }

                //TODO: Write Setter for Length
                public int Length
                {
                    get => (lowByte & NIBBLE_LOW) + 3;
                }
            }

            //TODO: Documentation for ChunkHeader
            struct ChunkHeader
            {
                private byte flags;
                private byte mask;

                // TODO: Documentation for ReadFlag method
                public bool ReadFlag(out bool flag)
                {
                    bool endOfHeader = mask != 0x00;

                    flag = (flags & mask) != 0;

                    mask <<= 1;
                    return endOfHeader;
                }

                public ChunkHeader(byte flags)
                {
                    this.flags = flags;
                    this.mask = 0x01;
                }
            }

            //TODO: Write CompressBuffer method
            private static void CompressBuffer(byte[] compBuf, byte[] decompBuf /*Starting at + 20*/)
            {

            }

            // TODO: Add documentation for DecompressBuffer method
            /// <summary>
            /// Decompresses a Lempel-Ziv buffer
            /// </summary>
            /// <param name="decompBuf"></param>
            /// <param name="compBuf"></param>
            private static void DecompressBuffer(byte[] decompBuf, byte[] compBuf /*Starting at + 20*/)
            {
                OffsetLengthPair olPair = new OffsetLengthPair();

                int compBufPtr = 0;
                int decompBufPtr = 0;

                //Create sliding dictionary buffer and clear first 4078 bytes of dictionary buffer to 0
                byte[] slidingDict = new byte[SLIDING_LEN];

                //Set an offset to the dictionary insertion point
                uint dictInsertionOffset = SLIDING_LEN - 18;

                // Current chunk header
                ChunkHeader chunkHeader = new ChunkHeader();

                while(decompBufPtr < decompBuf.Length)
                {
                    // At the start of each chunk...
                    if(!chunkHeader.ReadFlag(out bool flag))
                    {
                        // Load the chunk header
                        chunkHeader = new ChunkHeader(compBuf[compBufPtr++]);
                        chunkHeader.ReadFlag(out flag);
                    }

                    // Each chunk header is a byte and is a collection of 8 flags

                    // If the flag is set, load a character
                    if(flag)
                    {
                        // Copy the character
                        byte rawByte = compBuf[compBufPtr++];
                        decompBuf[decompBufPtr++] = rawByte;

                        // Add the character to the dictionary, and slide the dictionary
                        slidingDict[dictInsertionOffset++] = rawByte;
                        dictInsertionOffset &= SLIDING_MASK;

                    }
                    // If the flag is clear, load an offset/length pair
                    else
                    {
                        // Load the offset/length pair
                        olPair.highByte = compBuf[compBufPtr++];
                        olPair.lowByte = compBuf[compBufPtr++];

                        // Get the offset from the offset/length pair
                        int offset = olPair.Offset;

                        // Get the length from the offset/length pair
                        int length = olPair.Length;

                        for(int i = 0; i < length; i++)
                        {
                            byte rawByte = slidingDict[(offset + i) & SLIDING_MASK];
                            decompBuf[decompBufPtr++] = rawByte;

                            if(decompBufPtr >= decompBuf.Length)
                                return;

                            // Add the character to the dictionary, and slide the dictionary
                            slidingDict[dictInsertionOffset++] = rawByte;
                            dictInsertionOffset &= SLIDING_MASK;
                        }
                    }
                }
            }

            public static bool isFileCompressed(byte[] CompressedBuffer)
            {
                return Encoding.ASCII.GetString(CompressedBuffer, 0, 13) == "compress v1.0";
            }

            public static byte[] ProcessBuffer(byte[] CompressedBuffer)
            {
                if(isFileCompressed(CompressedBuffer))
                {
                    uint DecompressedSize = BitConverter.ToUInt32(CompressedBuffer, 16);
                    byte[] DecompressedBuffer = new byte[DecompressedSize];
                    //Xor Decrypt the whole buffer
                    byte XorEncryptionValue = CompressedBuffer[15];

                    byte[] CompBuf = new byte[CompressedBuffer.Length - 20];
                    for(int i = 20; i < CompressedBuffer.Length; i++)
                    {
                        CompBuf[i - 20] = (byte)(CompressedBuffer[i] ^ XorEncryptionValue);
                    }

                    //Decompress the whole buffer
                    DecompressBuffer(DecompressedBuffer, CompBuf);

                    //Switch the buffers around so the decompressed one gets saved instead
                    return DecompressedBuffer;
                }
                else
                {
                    return CompressedBuffer;
                }
            }
        }
    }
}
