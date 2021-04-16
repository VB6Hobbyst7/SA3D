using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Structs;

namespace SATools.SAModel.Graphics
{
    public class DebugMaterial : Material
    {
        public RenderMode RenderMode { get; set; }

        public DebugMaterial(IGAPIAMaterial apiAccess) : base(apiAccess)
        {
        }

        public override void ReBuffer()
        {
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

                var matFlags = BufferMaterial.MaterialFlags;
                if(BufferTextureSet == null)
                    matFlags &= ~ModelData.Buffer.MaterialFlags.useTexture;

                int flags = (ushort)matFlags | ((int)RenderMode << 24);
                writer.Write(flags);
            }

            _apiAccess.MaterialPostBuffer(this);
        }
    }
}
