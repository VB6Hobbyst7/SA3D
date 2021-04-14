using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using System;
using System.Collections.ObjectModel;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Buffers and activates a material
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Graphics API Access for buffering Materials
        /// </summary>
        protected readonly IGAPIAMaterial _apiAccess;

        protected BufferMaterial _bufferMaterial;

        /// <summary>
        /// Active material
        /// </summary>
        public BufferMaterial BufferMaterial
        {
            get => _bufferMaterial;
            set
            {
                if(value == null)
                    return;
                _bufferMaterial = value.Clone();
                ReBuffer();
            }
        }

        /// <summary>
        /// Viewing position
        /// </summary>
        public Vector3 ViewPos { get; set; }

        /// <summary>
        /// Camera viewing direction
        /// </summary>
        public Vector3 ViewDir { get; set; }

        /// <summary>
        /// Base buffer data
        /// </summary>
        public ReadOnlyCollection<byte> Buffer { get; private set; }

        protected byte[] _buffer;

        public Material(IGAPIAMaterial apiAccess)
        {
            _apiAccess = apiAccess;
            _buffer = new byte[104];
            Buffer = Array.AsReadOnly(_buffer);
            _bufferMaterial = new BufferMaterial();
        }

        /// <summary>
        /// Sets the buffer material to a new instance and buffers it
        /// </summary>
        public void ResetBufferMaterial() => BufferMaterial = new BufferMaterial();

        /// <summary>
        /// Buffers the material into the byte buffer
        /// </summary>
        public virtual void ReBuffer()
        {
            _apiAccess.MaterialPreBuffer(this);

            using(ExtendedMemoryStream stream = new(_buffer))
            {
                LittleEndianMemoryStream writer = new(stream);

                ViewPos.Write(writer, IOType.Float);
                writer.Write(0);

                ViewDir.Write(writer, IOType.Float);
                writer.Write(0);

                new Vector3(0, 1, 0).Write(writer, IOType.Float);
                writer.Write(0);

                WriteColor(writer, BufferMaterial.Diffuse);
                WriteColor(writer, BufferMaterial.Specular);
                WriteColor(writer, BufferMaterial.Ambient);

                writer.Write(BufferMaterial.SpecularExponent);
                int flags = (ushort)BufferMaterial.MaterialFlags;
                writer.Write(flags);
            }

            _apiAccess.MaterialPostBuffer(this);
        }

        protected static void WriteColor(LittleEndianMemoryStream writer, Color c)
        {
            writer.Write(c.RedF);
            writer.Write(c.GreenF);
            writer.Write(c.BlueF);
            writer.Write(c.AlphaF);
        }
    }
}
