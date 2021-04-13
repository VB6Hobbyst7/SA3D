using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Graphics.UI;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using SATools.SACommon;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Color = SATools.SAModel.Structs.Color;
using System.Runtime.InteropServices;
using SATools.SAModel.Graphics.Properties;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Context for debugging/editing the scene
    /// </summary>
    public class DebugContext : Context
    {
        #region Private Fields

        /// <summary>
        /// see <see cref="DebugMenu"/>
        /// </summary>
        private DebugMenu _debugMenu;

        /// <summary>
        /// see <see cref="RenderMode"/>
        /// </summary>
        private RenderMode _renderMode;

        /// <summary>
        /// see <see cref="WireframeMode"/>
        /// </summary>
        private WireFrameMode _wireFrameMode;

        /// <summary>
        /// see <see cref="BoundsMode"/>
        /// </summary>
        private BoundsMode _boundsMode;

        /// <summary>
        /// Used to render debug information onto
        /// </summary>
        private Bitmap _debugTexture;

        /// <summary>
        /// UI Image for the debug texture
        /// </summary>
        private UIImage _debugPanel;

        /// <summary>
        /// The private font collection for the fonts. <br/>
        /// If we dont assign it to a field, then it will auto dispose itself or something and the fonts wont work anymore, so here it stays
        /// </summary>
        private readonly PrivateFontCollection _fonts = new();

        /// <summary>
        /// Default debug font
        /// </summary>
        private readonly Font _debugFont;

        /// <summary>
        /// Bold debug font
        /// </summary>
        private readonly Font _debugFontBold;

        /// <summary>
        /// see <see cref="ActiveNJO"/>
        /// </summary>
        private NJObject _activeNJO;

        /// <summary>
        /// see <see cref="ActiveLE"/>
        /// </summary>
        private LandEntry _activeLE;

        #endregion

        #region Public properties

        /// <summary>
        /// Currently active debug overlay
        /// </summary>
        public DebugMenu DebugMenu
        {
            get => _debugMenu;
            set
            {
                _debugMenu = _debugMenu == value ? DebugMenu.Disabled : value;

                if(_debugMenu == DebugMenu.Disabled)
                    return;

                int width = 0;
                int height = 0;
                switch(_debugMenu)
                {
                    case DebugMenu.Help:
                        width = 200;
                        height = 105;
                        break;
                    case DebugMenu.Camera:
                    case DebugMenu.RenderInfo:
                        width = 350;
                        height = 180;
                        break;
                }

                _debugTexture = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }
        }

        /// <summary>
        /// How polygons should be rendered
        /// </summary>
        public RenderMode RenderMode
        {
            get => _renderMode;
            set
            {
                if(value == _renderMode)
                    return;
                _apiAccessObject.DebugUpdateRenderMode(value);
                _renderMode = value;
            }
        }

        /// <summary>
        /// How wireframes should be displayed
        /// </summary>
        public WireFrameMode WireframeMode
        {
            get => _wireFrameMode;
            set
            {
                if(value == _wireFrameMode)
                    return;
                _apiAccessObject.DebugUpdateWireframe(value);
                _wireFrameMode = value;
            }
        }

        /// <summary>
        /// Whether to draw bounds
        /// </summary>
        public BoundsMode BoundsMode
        {
            get => _boundsMode;
            set
            {
                if(value == _boundsMode)
                    return;
                _apiAccessObject.DebugUpdateBoundsMode(value);
                _boundsMode = value;
            }
        }

        /// <summary>
        /// Whether to render collision models
        /// </summary>
        public bool RenderCollision { get; set; }

        /// <summary>
        /// Camera Orbit-drag speed for the mouse
        /// </summary>
        public float CamDragSpeed { get; set; } = 0.001f;

        /// <summary>
        /// Camera first-person movement speed
        /// </summary>
        public float CamMovementSpeed { get; set; } = 30f;

        /// <summary>
        /// Basically "sprint" speed multiplier for <see cref="CamMovementSpeed"/>
        /// </summary>
        public float CamMovementModif { get; set; } = 2f;

        /// <summary>
        /// Camera first-person mouse sensitivity
        /// </summary>
        public float CamMouseSensitivity { get; set; } = 0.05f;

        public float CamOrbitSensitivity { get; set; } = 0.3f;

        /// <summary>
        /// Whether the debug camera should be enabled (useful for debuggin in the middle of a game
        /// </summary>
        public bool UseDebugCamera { get; set; } = true;

        /// <summary>
        /// Used for rendering the bounding spheres
        /// </summary>
        public ModelData.Attach SphereMesh { get; }

        /// <summary>
        /// Active NJ Object
        /// </summary>
        public NJObject ActiveNJO
        {
            get => _activeNJO;
            set
            {
                if(value == null)
                    return;
                _activeNJO = value;
                _activeLE = null;
            }
        }

        /// <summary>
        /// Active geometry object
        /// </summary>
        public LandEntry ActiveLE
        {
            get => _activeLE;
            set
            {
                if(value == null)
                    return;
                _activeLE = value;
                _activeNJO = null;
            }
        }

        public new DebugMaterial Material { get; }

        #endregion

        public DebugContext(Rectangle screen, GAPIAccessObject apiAccessObject) : base(screen, apiAccessObject)
        {
            Material = new DebugMaterial(apiAccessObject);

            LoadFonts();
            _debugFont = new Font(_fonts.Families[0], 12);
            _debugFontBold = new Font(_fonts.Families[0], 15, FontStyle.Bold);

            var stream = GetType().Assembly.GetManifestResourceStream("SATools.SAModel.Graphics.Sphere.bufmdl");
            byte[] sphere = stream.ReadFully();
            stream.Close();

            uint addr = 0;
            SphereMesh = new ModelData.Attach(new BufferMesh[] { BufferMesh.Read(sphere, ref addr) })
            {
                Name = "Debug_Sphere"
            };

            BufferMaterial mat = SphereMesh.MeshData[0].Material;
            mat.MaterialFlags = MaterialFlags.noDiffuse | MaterialFlags.noSpecular;
            mat.UseAlpha = true;
            mat.Culling = true;
            mat.SourceBlendMode = ModelData.BlendMode.SrcAlpha;
            mat.DestinationBlendmode = ModelData.BlendMode.SrcAlphaInverted;
            mat.Ambient = new Color(128, 128, 128, 64);

            RenderMode = RenderMode.Default;
            WireframeMode = WireFrameMode.None;
            BoundsMode = BoundsMode.None;

            Scene.OnUpdateEvent += DebugUpdate;
        }

        /// <summary>
        /// Used to circle through enums
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="current">Current value to change</param>
        /// <param name="back">Whether to scroll back</param>
        /// <returns></returns>
        private static T Circle<T>(T current, bool back) where T : Enum
        {
            int max = Enum.GetValues(typeof(T)).Length - 1;
            int value = Convert.ToInt32(current);
            _ = back ? value-- : value++;

            if(value < 0)
                value = max;
            else if(value > max)
                value = 0;

            return (T)Enum.ToObject(typeof(T), value);
        }

        /// <summary>
        /// Updates the debug
        /// </summary>
        /// <param name="delta"></param>
        private void DebugUpdate(double delta)
        {
            if(Focused != this)
                return;

            if(UseDebugCamera)
                UpdateCamera(delta);

            DebugSettings s = DebugSettings.Default;
            bool backward = Input.IsKeyDown(s.CircleBackward);

            if(Input.KeyPressed(s.CircleRenderMode))
            {
                RenderMode newRenderMode = Circle(RenderMode, backward);
                if(newRenderMode == RenderMode.FullBright)
                    RenderMode = RenderMode.Normals;
                else if(newRenderMode == RenderMode.FullDark)
                    RenderMode = RenderMode.Falloff;
                else
                    RenderMode = newRenderMode;
            }

            // Circle wireframe mode
            if(Input.KeyPressed(s.CircleWireframe))
                WireframeMode = Circle(WireframeMode, backward);

            // Circle displaybounds mode
            if(Input.KeyPressed(s.DisplayBounds))
                BoundsMode = Circle(BoundsMode, backward);

            // Switch collision rendering mode
            if(Input.KeyPressed(s.SwapGeometry))
                RenderCollision = !RenderCollision;

            if(Input.KeyPressed(s.DebugHelp))
                DebugMenu = DebugMenu.Help;
            else if(Input.KeyPressed(s.DebugCamera))
                DebugMenu = DebugMenu.Camera;
            else if(Input.KeyPressed(s.DebugRender))
                DebugMenu = DebugMenu.RenderInfo;

        }

        /// <summary>
        /// Camera movement
        /// </summary>
        /// <param name="delta"></param>
        private void UpdateCamera(double delta)
        {
            DebugSettings s = DebugSettings.Default;
            if(!Camera.Orbiting) // if in first-person mode
            {
                if(!_wasFocused || Input.KeyPressed(Key.Escape) || Input.KeyPressed(s.NavMode))
                {
                    Camera.Orbiting = true;
                    Input.LockCursor = false;
                }
            }
            else if(Input.KeyPressed(s.NavMode))
            {
                Camera.Orbiting = false;
                Input.LockCursor = true;
            }
            else
            {
                if(Input.KeyPressed(s.FocusObj))
                {
                    if(ActiveLE != null)
                    {
                        Camera.Position = ActiveLE.ModelBounds.Position;
                    }
                    else if(ActiveNJO != null)
                    {
                        Camera.Position = ActiveNJO.Position;
                    }
                }
            }

            if(!Camera.Orbiting)
            {
                // rotation
                Camera.Rotation = new Vector3(
                    Math.Max(-90, Math.Min(90, Camera.Rotation.X + Input.CursorDif.Y * CamMouseSensitivity)),
                    (Camera.Rotation.Y + Input.CursorDif.X * CamMouseSensitivity) % 360f,
                    0);

                // modifying movement speed 
                float dir = Input.ScrollDif < 0 ? -0.05f : 0.05f;
                for(int i = (int)Math.Abs(Input.ScrollDif); i > 0; i--)
                {
                    CamMovementSpeed += CamMovementSpeed * dir;
                    CamMovementSpeed = Math.Max(0.0001f, Math.Min(1000, CamMovementSpeed));
                }

                // movement
                Vector3 dif = default;

                if(Input.IsKeyDown(s.FpForward))
                    dif += Camera.Forward;

                if(Input.IsKeyDown(s.FpBackward))
                    dif -= Camera.Forward;

                if(Input.IsKeyDown(s.FpLeft))
                    dif += Camera.Right;

                if(Input.IsKeyDown(s.FpRight))
                    dif -= Camera.Right;

                if(Input.IsKeyDown(s.FpUp))
                    dif += Camera.Up;

                if(Input.IsKeyDown(s.FpDown))
                    dif -= Camera.Up;

                if(dif.Length == 0)
                    return;

                Camera.Position += dif.Normalized() * CamMovementSpeed * (Input.IsKeyDown(s.FpSpeedup) ? CamMovementModif : 1) * (float)delta;
            }
            else
            {
                // mouse orientation
                if(Input.IsKeyDown(s.OrbitKey))
                {
                    if(Input.IsKeyDown(s.ZoomModifier)) // zooming
                    {
                        Camera.Distance += Camera.Distance * Input.CursorDif.Y * 0.01f;
                    }
                    else if(Input.IsKeyDown(s.DragModifier)) // moving
                    {
                        Vector3 dif = default;
                        float speed = CamDragSpeed * Camera.Distance;
                        dif += Camera.Right * Input.CursorDif.X * speed;
                        dif += Camera.Up * Input.CursorDif.Y * speed;
                        Camera.Position += dif;
                    }
                    else // rotation
                    {
                        Camera.Rotation = new Vector3(Math.Max(-90, Math.Min(90, Camera.Rotation.X + Input.CursorDif.Y * CamOrbitSensitivity)), (Camera.Rotation.Y + Input.CursorDif.X * CamOrbitSensitivity) % 360f, 0);
                    }
                }
                else
                {
                    if(Input.KeyPressed(s.Perspective))
                        Camera.Orthographic = !Camera.Orthographic;

                    bool invertAxis = Input.IsKeyDown(s.AlignInvert);
                    if(Input.KeyPressed(s.AlignForward))
                        Camera.Rotation = new Vector3(0, invertAxis ? 180 : 0, 0);
                    else if(Input.KeyPressed(s.AlignSide))
                        Camera.Rotation = new Vector3(0, invertAxis ? -90 : 90, 0);
                    else if(Input.KeyPressed(s.AlignUp))
                        Camera.Rotation = new Vector3(invertAxis ? -90 : 90, 0, 0);

                    float dir = Input.ScrollDif < 0 ? 0.07f : -0.07f;
                    for(int i = (int)Math.Abs(Input.ScrollDif); i > 0; i--)
                    {
                        Camera.Distance += Camera.Distance * dir;
                    }
                }
            }
        }

        /// <summary>
        /// Renders the debug texture
        /// </summary>
        private void DrawDebug(uint meshesDrawn)
        {
            if(_debugMenu == DebugMenu.Disabled)
                return;

            using(System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(_debugTexture))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                int yOffset = 5;
                int lineHeight = _debugFont.Height;
                Brush br = Brushes.White;


                void text(string str, int xOffset)
                {
                    g.DrawString(str, _debugFont, br, xOffset, yOffset);
                    yOffset += lineHeight;
                }
                void textBold(string str, int xOffset)
                {
                    g.DrawString(str, _debugFontBold, br, xOffset, yOffset);
                    yOffset += lineHeight;
                }

                g.Clear(System.Drawing.Color.FromArgb(0x60, 0, 0, 0));
                switch(_debugMenu)
                {
                    case DebugMenu.Help:
                        textBold("== Debug == ", 30);
                        text("Debug      - F1", 10);
                        text("Camera     - F2", 10);
                        text("Renderinfo - F3", 10);
                        break;
                    case DebugMenu.Camera:
                        textBold("== Camera == ", 90);
                        text($"Location: {Camera.Position.Rounded(2)}", 10);
                        text($"Rotation: {Camera.Rotation.Rounded(2)}", 10);
                        text($"Distance: {Camera.Distance}", 10);
                        text($"View type: " + (Camera.Orthographic ? "Orthographic" : $"Perspective - FoV: {Camera.FieldOfView}"), 10);
                        text($"View Distance: {Camera.ViewDistance}", 10);
                        text($"Nav mode: " + (Camera.Orbiting ? "Orbiting" : "First Person"), 10);
                        text($"Move speed: {CamMovementSpeed}", 10);
                        text($"Mouse speed: {CamMouseSensitivity}", 10);
                        break;
                    case DebugMenu.RenderInfo:
                        textBold("== Renderinfo == ", 60);
                        text($"View Pos.: {Camera.Realposition.Rounded(2)}", 10);
                        //text($"Lighting Dir.: LIGHTDATA TODO", 10);//{RenderMaterial.LightDir.Rounded(2)}", 10);
                        text($"Meshes Drawn: {meshesDrawn}", 10);
                        text($"Render Mode: {_renderMode}", 10);
                        text($"Wireframe Mode: {_wireFrameMode}", 10);
                        text($"Display: {(RenderCollision ? "Collision" : "Visual")}", 10);
                        text($"Display Bounds: {_boundsMode}", 10);
                        if(ActiveNJO != null)
                            text($"Active object: {ActiveNJO.Name}", 10);
                        else if(ActiveLE != null)
                        {
                            text($"Active object: LandEntry {Scene.geometry.IndexOf(ActiveLE)}", 10);
                        }
                        else
                            text($"Active object: NULL", 10);
                        break;
                }

                g.Flush();
            }

            if(_debugPanel == null)
            {
                _debugPanel = new UIImage(_debugTexture)
                {
                    LocalPivot = new Vector2(0, 1),
                    GlobalPivot = new Vector2(0, 1)
                };
            }

            _debugPanel.Texture = _debugTexture;
            _debugPanel.Scale = new Vector2(_debugTexture.Width, _debugTexture.Height);
            Canvas.Draw(_debugPanel);
        }

        public override void GraphicsInit()
            => _apiAccessObject.GraphicsInit(this);

        public override void Render()
        {
            DrawDebug(_apiAccessObject.RenderDebug(this));
            Canvas.Render(Resolution.Width, Resolution.Height);
        }

        private void LoadFonts()
        { 
            using Stream fontStream = GetType().Assembly.GetManifestResourceStream("SATools.SAModel.Graphics.debugFont.ttf");
            byte[] fontBytes = fontStream.ReadFully();

            unsafe
            {
                fixed(byte* pFontData = fontBytes)
                {
                    _fonts.AddMemoryFont((IntPtr)pFontData, fontBytes.Length);
                }
            }
        }
    }
}
