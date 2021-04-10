using SATools.SAModel.Graphics.APIAccess;
using System.Collections.Generic;

namespace SATools.SAModel.Graphics.UI
{
    /// <summary>
    /// Responsible for drawing UI elements
    /// </summary>
    public class Canvas
    {
        private readonly IGAPIACanvas _apiAccess;

        private readonly Queue<UIElement> _renderQueue;

        private int _oldWidth;
        private int _oldHeight;

        public Canvas(IGAPIACanvas apiAccess)
        {
            _apiAccess = apiAccess;
            _renderQueue = new Queue<UIElement>();
        }

        public void Draw(UIElement element) => _renderQueue.Enqueue(element);

        /// <summary>
        /// Renders the entire canvas
        /// </summary>
        /// <param name="width">Output resolution width</param>
        /// <param name="height">Output resolution height</param>
        public void Render(int width, int height)
        {
            _apiAccess.CanvasPreDraw(width, height);

            float premWidth = width * 0.5f;
            float premHeight = height * 0.5f;

            bool forceTransformUpdate = _oldWidth != width || _oldHeight != height;
            if(forceTransformUpdate)
            {
                _oldWidth = width;
                _oldHeight = height;
            }

            while(_renderQueue.Count > 0)
            {
                UIElement element = _renderQueue.Dequeue();
                _apiAccess.CanvasDrawUIElement(element, premWidth, premHeight, forceTransformUpdate);
            }

            _renderQueue.Clear();

            _apiAccess.CanvasPostDraw();
        }

    }
}

