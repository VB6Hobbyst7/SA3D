using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SAArchive;
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
        }

        protected void UpdateBuffer()
        {
            using ExtendedMemoryStream stream = new(Buffer);
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

            var matFlags = BufferMaterial.MaterialFlags;
            if(BufferTextureSet == null || BufferMaterial.TextureIndex > BufferTextureSet.Textures.Count)
                matFlags &= ~MaterialFlags.useTexture;

            int flags = (ushort)matFlags;
            writer.Write(flags);
        }

        protected virtual void ReBuffer()
        {
            UpdateBuffer();
            _bridge.BufferMaterial(this);
        }

        private static void WriteColor(LittleEndianMemoryStream writer, Color c)
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
