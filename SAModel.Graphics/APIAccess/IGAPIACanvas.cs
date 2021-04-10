using SATools.SAModel.Graphics.UI;

namespace SATools.SAModel.Graphics.APIAccess
{
    public interface IGAPIACanvas
    {
        void CanvasPreDraw(int width, int height);

        void CanvasPostDraw();

        void CanvasDrawUIElement(UIElement element, float width, float height, bool forceUpdateTransforms);
    }
}
