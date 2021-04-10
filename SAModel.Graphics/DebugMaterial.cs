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
            _apiAccess.MaterialPreBuffer(this);

            using(ExtendedMemoryStream stream = new ExtendedMemoryStream(_buffer))
            {
                LittleEndianMemoryStream writer = new LittleEndianMemoryStream(stream);

                ViewPos.Write(writer, IOType.Float);
                writer.Write(0);

                ViewDir.Write(writer, IOType.Float);
                writer.Write(0);

                new Vector3(0, 1, 0).Write(writer, IOType.Float);
                writer.Write(0);

                writer.Write(BufferMaterial.Diffuse.RedF);
                writer.Write(BufferMaterial.Diffuse.GreenF);
                writer.Write(BufferMaterial.Diffuse.BlueF);
                writer.Write(BufferMaterial.Diffuse.AlphaF);

                writer.Write(BufferMaterial.Specular.RedF);
                writer.Write(BufferMaterial.Specular.GreenF);
                writer.Write(BufferMaterial.Specular.BlueF);
                writer.Write(BufferMaterial.Specular.AlphaF);

                writer.Write(BufferMaterial.Ambient.RedF);
                writer.Write(BufferMaterial.Ambient.GreenF);
                writer.Write(BufferMaterial.Ambient.BlueF);
                writer.Write(BufferMaterial.Ambient.AlphaF);

                writer.Write(BufferMaterial.SpecularExponent);
                int flags = (ushort)BufferMaterial.MaterialFlags | ((int)RenderMode << 24);
                writer.Write(flags);
            }

            _apiAccess.MaterialPostBuffer(this);
        }
    }
}
