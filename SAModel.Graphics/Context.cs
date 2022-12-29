using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Graphics.UI;
using SATools.SAModel.ObjectData;
using System.Collections.Generic;
using System.Drawing;
using Color = SATools.SAModel.Structs.Color;
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.ModelData.Buffer.BufferMesh, System.Collections.Generic.List<SATools.SAModel.Graphics.RenderMatrices>>>;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Rendering context base class
    /// </summary>
    public class Context
    {
        #region Statics

        /// <summary>
        /// The focused context
        /// </summary>
        public static Context Focused { get; private set; }

        #endregion

        #region private fields

        private bool _used;

        /// <summary>
        /// Whether the context was focused in the last update
        /// </summary>
        protected bool _wasFocused;

        /// <summary>
        /// Positiona and resolution of the context
        /// </summary>
        protected Rectangle _screen;

        /// <summary>
        /// see <see cref="BackgroundColor"/>
        /// </summary>
        private Color _backgroundColor;

        /// <summary>
        /// Whether the graphics have been initialized
        /// </summary>
        internal bool _graphicsInitiated;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether this window is in focus
        /// </summary>
        public bool IsFocused
        {
            get => Focused == this;
            set
            {
                if (value)
                    Focused = this;
                else if (Focused == this)
                    Focused = null;
            }
        }

        /// <summary>
        /// Polygon display handler
        /// </summary>
        public Material Material { get; protected set; }

        /// <summary>
        /// UI handler
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// Input handler
        /// </summary>
        public Input Input { get; }

        /// <summary>
        /// The camera of the scene
        /// </summary>
        public Camera Camera
            => Scene.Cam;

        /// <summary>
        /// 3D scene to hold objects and geometry
        /// </summary>
        public Scene Scene { get; }

        /// <summary>
        /// Screen Rectangle (get only)
        /// </summary>
        public Rectangle Screen => _screen;

        /// <summary>
        /// The resolution of the context
        /// </summary>
        public Size Resolution
        {
            get => _screen.Size;
            set
            {
                _screen.Size = value;
                Camera.Aspect = _screen.Width / (float)_screen.Height;
                if (_graphicsInitiated)
                    _renderingBridge.UpdateViewport(_screen, true);
            }
        }

        /// <summary>
        /// The location of the context on the screen
        /// </summary>
        public Point Location
        {
            get => _screen.Location;
            set
            {
                _screen.Location = value;
                if (_graphicsInitiated)
                    _renderingBridge.UpdateViewport(_screen, false);
            }
        }

        /// <summary>
        /// Clearcolor
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                _renderingBridge.UpdateBackgroundColor(_backgroundColor);
            }
        }

        #endregion

        #region Rendering data

        protected readonly BufferingBridge _bufferingBridge;

        protected readonly RenderingBridge _renderingBridge;

        private readonly InputBridge _inputBridge;

        #endregion


        /// <summary>
        /// Creates a new render context
        /// </summary>
        /// <param name="inputUpdater"></param>
        public Context(Rectangle screen, RenderingBridge renderingBridge, BufferingBridge bufferingBridge)
        {
            _renderingBridge = renderingBridge;
            _bufferingBridge = bufferingBridge;
            _inputBridge = new();

            _screen = screen;
            Material = new Material(_bufferingBridge);
            Canvas = new Canvas(_renderingBridge);
            Input = new Input(_inputBridge);
            Scene = new Scene(screen.Width / (float)screen.Height, _bufferingBridge);
            _backgroundColor = new Color(0x60, 0x60, 0x60);
        }

        /// <summary>
        /// Starts the context as an independent window
        /// </summary>
        public void AsWindow()
        {
            if (_used)
                throw new System.InvalidOperationException("Context object was already used before!");
            _used = true;
            _renderingBridge.AsWindow(this, _inputBridge);
        }

        /// <summary>
        /// Returns the context as a WPF control
        /// </summary>
        /// <param name="windowSource"></param>
        /// <returns></returns>
        public System.Windows.FrameworkElement AsControl()
        {
            if (_used)
                throw new System.InvalidOperationException("Context object was already used before!");
            _used = true;
            return _renderingBridge.AsControl(this, _inputBridge);
        }

        /// <summary>
        /// Gets called when graphics are being initialized
        /// </summary>
        public void GraphicsInit()
        {
            if (!_graphicsInitiated)
                _renderingBridge.InitializeGraphics(Resolution, BackgroundColor);
            _graphicsInitiated = true;
        }

        /// <summary>
        /// Gameplay logic update
        /// </summary>
        /// <param name="delta"></param>
        public void Update(double delta)
        {
            Input.Update(Focused == this || _wasFocused);

            Scene.Update(delta);

            _wasFocused = Focused == this;
        }

        public void Render()
        {
            Material.ViewPos = Camera.Realposition;
            Material.ViewDir = Camera.Orthographic ? Camera.Forward : default;

            // get rendermeshes for the
            var (opaqueGeo, transparentGeo, all) = RenderHelper.PrepareLandEntries(Scene.VisualGeometry, Camera, _bufferingBridge);
            var models = PrepareModels();

            // First render opaque stuff
            _renderingBridge.ToggleOpaque();

            Material.BufferTextureSet = Scene.LandTextureSet;
            opaqueGeo.RenderLandentries(Material, _renderingBridge);

            foreach (var (task, opaque, transparent) in models)
            {
                Material.BufferTextureSet = task.TextureSet;
                foreach (var m in opaque)
                    _renderingBridge.RenderMesh(m.meshes, m.matrices, Material);
            }

            // Then transparent stuff
            _renderingBridge.ToggleTransparent();

            Material.BufferTextureSet = Scene.LandTextureSet;
            transparentGeo.RenderLandentries(Material, _renderingBridge);

            foreach (var (task, opaque, transparent) in models)
            {
                Material.BufferTextureSet = task.TextureSet;
                foreach (var m in transparent)
                    _renderingBridge.RenderMesh(m.meshes, m.matrices, Material);
            }

            // this is to implement debug stuff
            ExtraRenderStuff(all, opaqueGeo, transparentGeo, models);

            Canvas.Render(_screen.Width, _screen.Height);
        }

        protected virtual List<(DisplayTask task, List<RenderMesh> opaque, List<RenderMesh> transparent)> PrepareModels()
        {
            return RenderHelper.PrepareModels(Scene.GameTasks, null, Camera, _bufferingBridge);
        }

        protected internal virtual void ExtraRenderStuff(List<LandEntry> geoData, LandEntryRenderBatch opaqueGeo, LandEntryRenderBatch transparenGeo, List<(DisplayTask task, List<RenderMesh> opaque, List<RenderMesh> transparent)> models)
        {

        }
    }
}
