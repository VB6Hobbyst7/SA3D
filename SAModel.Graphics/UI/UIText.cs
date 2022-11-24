using System;

namespace SATools.SAModel.Graphics.UI
{
    /// <summary>
    /// Text to draw to the UI
    /// </summary>
    public class UIText : UIElement
    {
        private string _text;

        /// <summary>
        /// The text to draw
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (value == _text)
                    return;
                _text = value ?? throw new NullReferenceException("Text cannot be null");
                UpdateTexture();
            }
        }

        //TODO add font

        public UIText(string text) : base()
        {
            Text = text;
        }

        private void UpdateTexture()
        {
            // TODO add texture generation
        }
    }
}
