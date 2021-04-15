using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SATools.SAArchive
{
    /// <summary>
    /// Texture set
    /// </summary>
    public class TextureSet
    {
        /// <summary>
        /// Textures in the texture set
        /// </summary>
        public List<Texture> Textures { get; set; }

        public TextureSet()
        {
            Textures = new List<Texture>();
        }
    }

    /// <summary>
    /// Single texture in a texture set
    /// </summary>
    public class Texture
    {
        /// <summary>
        /// Texture name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Override size
        /// </summary>
        public Size OverrideSize { get; set; }

        /// <summary>
        /// Global texture index
        /// </summary>
        public uint GlobalIndex { get; set; }

        /// <summary>
        /// Actual texture data
        /// </summary>
        public Bitmap TextureBitmap { get; set; }

        public Texture(string name, Bitmap bitmap)
        {
            Name = name;
            TextureBitmap = bitmap;
            OverrideSize = new(bitmap.Width, bitmap.Height);
        }

        public Texture(string name, Bitmap bitmap, Size overrideSize)
        {
            Name = name;
            TextureBitmap = bitmap;
            OverrideSize = overrideSize;
        }
    }
}
