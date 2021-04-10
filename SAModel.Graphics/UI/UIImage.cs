using System;
using System.Drawing;

namespace SATools.SAModel.Graphics.UI
{
    /// <summary>
    /// Renders an Image to the UI
    /// </summary>
    public class UIImage : UIElement
    {
        private Bitmap _texture;

        /// <summary>
        /// Texture to draw
        /// </summary>
        public Bitmap Texture
        {
            get => _texture;
            set
            {
                _texture = value ?? throw new NullReferenceException("Texture cannot be null!");
                BufferTexture = value;
            }
        }

        public UIImage(Bitmap texture) : base()
        {
            Texture = texture;
        }

        public void RefreshTexture()
        {
            BufferTexture = Texture;
        }
    }
}
