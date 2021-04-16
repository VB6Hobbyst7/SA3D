using SATools.SAArchive;

namespace SATools.SAModel.Graphics.APIAccess
{
    /// <summary>
    /// Graphics API Access for <see cref="Material"/>
    /// </summary>
    public interface IGAPIAMaterial
    {

        /// <summary>
        /// Gets called after buffering material data
        /// </summary>
        void MaterialPostBuffer(Material material);

        /// <summary>
        /// Buffers all textures in a texture set
        /// </summary>
        /// <param name="textures"></param>
        void BufferTextureSet(TextureSet textures);
    }
}
