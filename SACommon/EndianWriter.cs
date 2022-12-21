using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SATools.SACommon
{
    public class EndianWriter : IDisposable
    {
        private readonly Stack<bool> _endianStack;
        private EndianMemoryStream _endianWriter;
        private readonly LittleEndianMemoryStream _littleEndianWriter;
        private readonly BigEndianMemoryStream _bigEndianWriter;

        public ExtendedMemoryStream Stream { get; }

        public uint Position => (uint)Stream.Position;


        public EndianWriter(ExtendedMemoryStream stream, bool bigEndian = false)
        {
            Stream = stream;
            _littleEndianWriter = new LittleEndianMemoryStream(stream);
            _bigEndianWriter = new BigEndianMemoryStream(stream);
            _endianStack = new();

            PushBigEndian(bigEndian);
        }

        /// <summary>
        /// Sets an endian. Dont forget to free it afterwards as well using <see cref="PopEndian"/>
        /// </summary>
        /// <param name="bigEndian">New bigendian mode</param>
        [MemberNotNull(nameof(_endianWriter))]
        public void PushBigEndian(bool bigEndian)
        {
            _endianStack.Push(bigEndian);
            _endianWriter = bigEndian ? _bigEndianWriter : _littleEndianWriter;
        }

        [MemberNotNull(nameof(_endianWriter))]
        public void PopEndian()
        {
            _endianStack.Pop();
            _endianWriter = BigEndian ? _bigEndianWriter : _littleEndianWriter;
        }

        /// <summary>
        /// Whether bytes should be read in big endian. Set with <see cref="PushBigEndian(bool)"/> and free afterwards with <see cref="PopEndian"/>
        /// </summary>
        public bool BigEndian
            => _endianStack.Peek();

        public void AddPadding(int alignment = 2048)
            => _endianWriter.AddPadding(alignment);

        public void AddPadding(byte value, int alignment = 2048)
            => _endianWriter.AddPadding(value, alignment);

        public void Dispose()
        {
            _littleEndianWriter.Dispose();
            _bigEndianWriter.Dispose();
            Stream.Dispose();
        }

        public byte[] ToArray()
            => _endianWriter.ToArray();

        public void Write(byte[] data)
            => _endianWriter.Write(data);

        public void Write<T>(T[] structure) where T : unmanaged
            => _endianWriter.Write(structure);

        public void Write<T>(T structure) where T : unmanaged
            => _endianWriter.Write(structure);

        public void WriteDouble(double data)
            => _endianWriter.WriteDouble(data);

        public void WriteInt16(short data)
            => _endianWriter.WriteInt16(data);

        public void WriteInt32(int data)
            => _endianWriter.WriteInt32(data);

        public void WriteInt64(long data)
            => _endianWriter.WriteInt64(data);

        public void WriteSingle(float data)
            => _endianWriter.WriteSingle(data);

        public void WriteUInt16(ushort data)
            => _endianWriter.WriteUInt16(data);

        public void WriteUInt32(uint data)
            => _endianWriter.WriteUInt32(data);

        public void WriteUInt64(ulong data)
            => _endianWriter.WriteUInt64(data);

    }
}
