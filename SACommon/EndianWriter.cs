using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using System;
using System.Collections.Generic;

namespace SATools.SACommon
{
    public class EndianWriter : IDisposable
    {
        public ExtendedMemoryStream Stream { get; }

        public uint Position => (uint)Stream.Position;

        private EndianMemoryStream endianWriter;
        private readonly LittleEndianMemoryStream littleEndianWriter;
        private readonly BigEndianMemoryStream bigEndianWriter;

        private readonly Stack<bool> _endianStack = new();

        public EndianWriter(ExtendedMemoryStream stream, bool bigEndian = false)
        {
            Stream = stream;
            littleEndianWriter = new LittleEndianMemoryStream(stream);
            bigEndianWriter = new BigEndianMemoryStream(stream);

            PushBigEndian(bigEndian);
        }

        /// <summary>
        /// Sets an endian. Dont forget to free it afterwards as well using <see cref="PopEndian"/>
        /// </summary>
        /// <param name="bigEndian">New bigendian mode</param>
        public void PushBigEndian(bool bigEndian)
        {
            _endianStack.Push(bigEndian);
            endianWriter = bigEndian ? bigEndianWriter : littleEndianWriter;
        }

        public void PopEndian()
        {
            _endianStack.Pop();
            endianWriter = BigEndian ? bigEndianWriter : littleEndianWriter;
        }

        /// <summary>
        /// Whether bytes should be read in big endian. Set with <see cref="PushBigEndian(bool)"/> and free afterwards with <see cref="PopEndian"/>
        /// </summary>
        public bool BigEndian
            => _endianStack.Peek();

        public void AddPadding(int alignment = 2048)
            => endianWriter.AddPadding(alignment);

        public void AddPadding(byte value, int alignment = 2048)
            => endianWriter.AddPadding(value, alignment);

        public void Dispose()
        {
            littleEndianWriter.Dispose();
            bigEndianWriter.Dispose();
            Stream.Dispose();
        }

        public byte[] ToArray()
            => endianWriter.ToArray();

        public void Write(byte[] data)
            => endianWriter.Write(data);

        public void Write<T>(T[] structure) where T : unmanaged
            => endianWriter.Write(structure);

        public void Write<T>(T structure) where T : unmanaged
            => endianWriter.Write(structure);

        public void WriteDouble(double data)
            => endianWriter.WriteDouble(data);

        public void WriteInt16(short data)
            => endianWriter.WriteInt16(data);

        public void WriteInt32(int data)
            => endianWriter.WriteInt32(data);

        public void WriteInt64(long data)
            => endianWriter.WriteInt64(data);

        public void WriteSingle(float data)
            => endianWriter.WriteSingle(data);

        public void WriteUInt16(ushort data)
            => endianWriter.WriteUInt16(data);

        public void WriteUInt32(uint data)
            => endianWriter.WriteUInt32(data);

        public void WriteUInt64(ulong data)
            => endianWriter.WriteUInt64(data);

    }
}
