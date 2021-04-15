using SATools.SAArchive;

namespace SATools.SAModel.Graphics.APIAccess
{
    /// <summary>
    /// Graphics API Access for <see cref="Material"/>
    /// </summary>
    public interface IGAPIAMaterial
    {
        /// <summary>
        /// Gets called before buffering material data
        /// </summary>
        void MaterialPreBuffer(Material material);

        /// <summary>
        /// Gets called after buffering material data
        /// </summary>
        void MaterialPostBuffer(Material material);
    }
}
