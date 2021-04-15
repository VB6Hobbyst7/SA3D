using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAArchive
{
    public class NjArchive : Archive
    {
        public override void CreateIndexFile(string path) 
            => throw new NotImplementedException();
        public override byte[] GetBytes() 
            => throw new NotImplementedException();

        public static NjArchive Read(byte[] source)
        {
            PushBigEndian(source[0] == 0);

            NjArchive result = new();

            int count = source.ToInt32(0) - 1;
            List<int> sizehdrs = new();

            for(uint i = 0; i < count; i++)
            {
                uint sizeaddr = 4 + i * 4;
                int size = source.ToInt32(sizeaddr);
                //Console.WriteLine("Entry size data {0} at offset {1}: size {2}", i, sizeaddr, size);
                sizehdrs.Add(size);
            }

            int[] sizes = sizehdrs.ToArray();
            int offset = 0x20;
            for(int i = 0; i < sizes.Length; i++)
            {
                byte[] data = new byte[sizes[i]];
                unsafe
                {
                    fixed(byte* ptr = data)
                    {
                        Marshal.Copy(source, offset, (IntPtr)ptr, data.Length);
                    }
                }

                result.Entries.Add(new NjArchiveEntry(data));
                offset += sizes[i];
            }

            PopEndian();
            return result;
        }

        public class NjArchiveEntry : ArchiveEntry
        {
            public NjArchiveEntry(byte[] data) : base()
            {
                Data = data;
            }

            public override Bitmap GetBitmap()
            {
                throw new NotImplementedException();
            }
        }
    }
}
