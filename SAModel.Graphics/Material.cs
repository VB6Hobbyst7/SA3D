using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SAArchive;
using SATools.SACommon;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using System.Numerics;

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
        protected readonly BufferingBridge _bridge;

        private BufferMaterial _bufferMaterial;

        private EndianWriter _bufferWriter;

        /// <summary>
        /// Texture set to use
        /// </summary>
        public TextureSet BufferTextureSet { get; set; }

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
        public byte[] Buffer { get; private set; }

        public Material(BufferingBridge bridge)
        {
            _bridge = bridge;
            Buffer = new byte[104];
            _bufferMaterial = new BufferMaterial();
            _bufferWriter = new(new(Buffer));
        }

        protected void UpdateBuffer()
        {
            _bufferWriter.Stream.Seek(0, System.IO.SeekOrigin.Begin);

            ViewPos.Write(_bufferWriter, IOType.Float);
            _bufferWriter.Write(0);

            ViewDir.Write(_bufferWriter, IOType.Float);
            _bufferWriter.Write(0);

            new Vector3(0, 1, 0).Write(_bufferWriter, IOType.Float);
            _bufferWriter.Write(0);

            WriteColor(_bufferWriter, BufferMaterial.Diffuse);
            WriteColor(_bufferWriter, BufferMaterial.Specular);
            WriteColor(_bufferWriter, BufferMaterial.Ambient);

            _bufferWriter.Write(BufferMaterial.SpecularExponent);

            var matFlags = BufferMaterial.MaterialFlags;
            if(BufferTextureSet == null || BufferMaterial.TextureIndex > BufferTextureSet.Textures.Count)
                matFlags &= ~MaterialFlags.useTexture;

            int flags = (ushort)matFlags;
            _bufferWriter.Write(flags);
        }

        protected virtual void ReBuffer()
        {
            UpdateBuffer();
            _bridge.BufferMaterial(this);
        }

        private static void WriteColor(EndianWriter writer, Color c)
        {
            writer.Write(c.RedF);
            writer.Write(c.GreenF);
            writer.Write(c.BlueF);
            writer.Write(c.AlphaF);
        }
    }

    /// <summary>
    /// Material with special debug properties
    /// </summary>
    public class DebugMaterial : Material
    {
        /// <summary>
        /// Material rendering mode
        /// </summary>
        public RenderMode RenderMode { get; set; }

        public DebugMaterial(BufferingBridge bridge) : base(bridge) { }

        protected override void ReBuffer()
        {
            UpdateBuffer();
            Buffer[^1] = (byte)RenderMode;
            _bridge.BufferMaterial(this);
        }
    }
}
