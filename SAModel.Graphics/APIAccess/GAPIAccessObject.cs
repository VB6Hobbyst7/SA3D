using SATools.SAArchive;
using SATools.SAModel.Graphics.UI;
using SATools.SAModel.Structs;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Interop;

namespace SATools.SAModel.Graphics.APIAccess
{
    /// <summary>
    /// Combined Graphics API Access object
    /// </summary>
    public abstract class GAPIAccessObject : IGAPIACamera, IGAPIAMaterial, IGAPIACanvas
    {
        private bool _used;

        public GAPIAInputBridge InputBridge { get; }

        public GAPIAccessObject()
        {
            InputBridge = new GAPIAInputBridge();
        }

        // Context
        /// <summary>
        /// Gets called once the graphics api is loaded
        /// </summary>
        public abstract void GraphicsInit(Context context);

        /// <summary>
        /// Runs the Context as a window
        /// </summary>
        public void AsWindow(Context context)
        {
            if(_used)
                throw new System.InvalidOperationException("Access object was already used before!");
            _used = true;
            InternalAsWindow(context);
        }

        protected abstract void InternalAsWindow(Context context);

        /// <summary>
        /// Creates a WPF control from a context
        /// </summary>
        /// <param name="windowSource">Window source to attach to</param>
        public System.Windows.FrameworkElement AsControl(Context context)
        {
            if(_used)
                throw new System.InvalidOperationException("Access object was already used before!");
            _used = true;
            return InternalAsControl(context);
        }

        protected abstract System.Windows.FrameworkElement InternalAsControl(Context context);

        /// <summary>
        /// Updates the viewport resolution
        /// </summary>
        /// <param name="screen">Screen rectangle</param>
        /// <param name="resized">Whether the screen was resized</param>
        public abstract void UpdateViewport(Rectangle screen, bool resized);

        /// <summary>
        /// Used for updating the background color in-API
        /// </summary>
        /// <param name="color"></param>
        public abstract void UpdateBackgroundColor(Structs.Color color);

        /// <summary>
        /// Renders the default context
        /// </summary>
        public abstract void Render(Context context);

        /// <summary>
        /// Renders the debug context
        /// </summary>
        public abstract uint RenderDebug(DebugContext context);

        // Debug context
        public abstract void DebugUpdateWireframe(WireFrameMode wireframeMode);
        public abstract void DebugUpdateBoundsMode(BoundsMode boundsMode);
        public abstract void DebugUpdateRenderMode(RenderMode renderMode);

        // Material
        public abstract void MaterialPreBuffer(Material material);
        public abstract void MaterialPostBuffer(Material material);

        // Camera
        public abstract void SetOrbitViewMatrix(Vector3 position, Vector3 rotation, Vector3 orbitOffset);
        public abstract void SetOrtographicMatrix(float width, float height, float zNear, float zFar);
        public abstract void SetPerspectiveMatrix(float fovy, float aspect, float zNear, float zFar);
        public abstract void SetViewMatrix(Vector3 position, Vector3 rotation);
        public abstract Vector3 ToViewPos(Vector3 position);
        public abstract void UpdateDirections(Vector3 rotation, out Vector3 up, out Vector3 forward, out Vector3 right);

        // Canvas
        public abstract void CanvasPreDraw(int width, int height);
        public abstract void CanvasPostDraw();
        public abstract void CanvasDrawUIElement(UIElement element, float width, float height, bool forceUpdateTransforms);
    }
}
