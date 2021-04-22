using SATools.SAModel.Graphics.UI;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.ModelData.Buffer.BufferMesh, System.Collections.Generic.List<SATools.SAModel.Graphics.RenderMatrices>>>;

namespace SATools.SAModel.Graphics.APIAccess
{
    public abstract class RenderingBridge
    {
        private bool _used;

        public abstract void InitializeGraphics(Size resolution, Structs.Color background);

        /// <summary>
        /// Runs the Context as a window
        /// </summary>
        public void AsWindow(Context context, InputBridge inputBridge)
        {
            if(_used)
                throw new System.InvalidOperationException("Access object was already used before!");
            _used = true;
            InternalAsWindow(context, inputBridge);
        }

        protected abstract void InternalAsWindow(Context context, InputBridge inputBridge);

        /// <summary>
        /// Creates a WPF control from a context
        /// </summary>
        /// <param name="windowSource">Window source to attach to</param>
        public System.Windows.FrameworkElement AsControl(Context context, InputBridge inputBridge)
        {
            if(_used)
                throw new System.InvalidOperationException("Access object was already used before!");
            _used = true;
            return InternalAsControl(context, inputBridge);
        }

        protected abstract System.Windows.FrameworkElement InternalAsControl(Context context, InputBridge inputBridge);

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

        public abstract void ToggleOpaque();

        public abstract void ToggleTransparent();

        public abstract void ChangeWireframe(WireFrameMode mode);

        public abstract void RenderMesh(BufferMesh[] mesh, RenderMatrices matrices, Material material);

        public abstract void RenderMesh(BufferMesh mesh, List<RenderMatrices> matrices);

        public abstract void RenderOverlayWireframes(LandEntryRenderBatch opaqueGeo, LandEntryRenderBatch transparentgeo, List<(DisplayTask task, List<RenderMesh> opaque, List<RenderMesh> transparent)> models);

        public abstract void RenderBounds(List<LandEntry> entries, BufferMesh sphere, Camera cam);

        public abstract void DrawModelRelationship(List<Vector3> lines, Camera cam);

        public abstract void CanvasPreDraw(int width, int height);

        public abstract void CanvasPostDraw();

        public abstract void CanvasDrawUIElement(UIElement element, float width, float height, bool forceUpdateTransforms);

    }
}
