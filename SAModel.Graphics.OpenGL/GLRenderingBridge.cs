using OpenTK.Graphics.OpenGL4;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Graphics.OpenGL.Properties;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows;
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.ModelData.Buffer.BufferMesh, System.Collections.Generic.List<SATools.SAModel.Graphics.RenderMatrices>>>;


namespace SATools.SAModel.Graphics.OpenGL
{
    internal class GLRenderingBridge : RenderingBridge
    {
        #region Shaders

        private readonly GLBufferingBridge _bufferBridge;

        /// <summary>
        /// For all kinds of 3D models
        /// </summary>
        internal Shader DefaultShader { get; private set; }

        /// <summary>
        /// For overlay wireframes
        /// </summary>
        internal Shader WireFrameShader { get; private set; }

        /// <summary>
        /// For landentry bounds
        /// </summary>
        internal Shader BoundsShader { get; private set; }

        /// <summary>
        /// For Rendering Collision geometry
        /// </summary>
        internal Shader CollisionShader { get; private set; }

        /// <summary>
        /// For UI textures
        /// </summary>
        internal Shader UIShader { get; private set; }

        #endregion

        #region initialization stuff

        public GLRenderingBridge(GLBufferingBridge bufferBridge) : base()
        {
            _bufferBridge = bufferBridge;
        }

        public override void InitializeGraphics(System.Drawing.Size resolution, Structs.Color background)
        {
            GL.Viewport(default, resolution);
            GL.ClearColor(background.SystemCol);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.Multisample);

            //GL.Enable(EnableCap.FramebufferSrgb); srgb doesnt work for glcontrol, so we'll just leave it out

            _bufferBridge.Initialize();
            InitializeShaders();
        }

        private void InitializeShaders()
        {
            // loading the shader
            string vertexShader = Encoding.UTF8.GetString(Resources.VertexShader);
            string fragShader = Encoding.UTF8.GetString(Resources.FragShader);
            DefaultShader = new Shader(vertexShader, fragShader);
            DefaultShader.BindUniformBlock("Material", 0, _bufferBridge.MaterialHandle);

            // wireframe
            vertexShader = Encoding.UTF8.GetString(Resources.Wireframe_vert);
            fragShader = Encoding.UTF8.GetString(Resources.Wireframe_frag);
            WireFrameShader = new Shader(vertexShader, fragShader);

            // collision
            vertexShader = Encoding.UTF8.GetString(Resources.Collision_vert);
            fragShader = Encoding.UTF8.GetString(Resources.Collision_frag);
            CollisionShader = new Shader(vertexShader, fragShader);

            // bounds
            vertexShader = Encoding.UTF8.GetString(Resources.Bounds_vert);
            fragShader = Encoding.UTF8.GetString(Resources.Bounds_frag);
            BoundsShader = new Shader(vertexShader, fragShader);

            // canvas
            vertexShader = Encoding.UTF8.GetString(Resources.DefaultUI_vert);
            fragShader = Encoding.UTF8.GetString(Resources.DefaultUI_frag);
            UIShader = new Shader(vertexShader, fragShader);
        }

        protected override void InternalAsWindow(Context context, InputBridge inputBridge)
            => new GLWindow(context, inputBridge, context.Resolution).Run();

        protected override FrameworkElement InternalAsControl(Context context, InputBridge inputBridge)
            => new GLControl(context, inputBridge);


        #endregion

        #region Setting stuff

        public override void ToggleOpaque()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            DefaultShader.Use();
            GL.Disable(EnableCap.Blend);
            GL.Uniform1(13, 0f);
        }

        public override void ToggleTransparent()
        {
            GL.Enable(EnableCap.Blend);
            GL.Uniform1(13, 1f);
        }

        public override void ChangeWireframe(WireFrameMode mode)
        {
            switch(mode)
            {
                case WireFrameMode.ReplaceLine:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    break;
                case WireFrameMode.ReplacePoint:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                    break;
                default:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    break;
            }
        }

        public override void UpdateViewport(Rectangle screen, bool resized)
        {
            if(resized)
            {
                GL.Viewport(screen.Size);
            }
        }

        public override void UpdateBackgroundColor(Structs.Color color) => GL.ClearColor(color.SystemCol);

        #endregion

        public override void RenderMesh(BufferMesh[] mesh, RenderMatrices matrices, Material material)
        {
            GLBufferingBridge.BufferMatrices(matrices);

            for(int i = 0; i < mesh.Length; i++)
            {
                if(material != null)
                    material.BufferMaterial = mesh[i].Material;
                var handle = _bufferBridge.GetHandle(mesh[i]);

                GL.BindVertexArray(handle.vao);
                if(handle.eao == 0)
                    GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                else
                    GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
            }
        }

        public override void RenderMesh(BufferMesh mesh, List<RenderMatrices> matrices)
        {
            var handle = _bufferBridge.GetHandle(mesh);
            GL.BindVertexArray(handle.vao);

            if(handle.eao == 0)
                foreach(var m in matrices)
                {
                    GLBufferingBridge.BufferMatrices(m);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                }
            else
                foreach(var m in matrices)
                {
                    GLBufferingBridge.BufferMatrices(m);
                    GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
                }
        }

        public override void RenderOverlayWireframes(LandEntryRenderBatch opaqueGeo, LandEntryRenderBatch transparentgeo, List<(DisplayTask task, List<RenderMesh> opaque, List<RenderMesh> transparent)> models)
        {
            WireFrameShader.Use();
            GL.Disable(EnableCap.Blend);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            RenderLandentriesWireframe(opaqueGeo);
            RenderLandentriesWireframe(transparentgeo);

            foreach(var (_, opaque, transparent) in models)
            {
                RenderModelsWireframe(opaque);
                RenderModelsWireframe(transparent);
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        private void RenderModelsWireframe(List<RenderMesh> renderMeshes)
        {
            for(int i = 0; i < renderMeshes.Count; i++)
            {
                var m = renderMeshes[i];
                RenderMesh(m.meshes, m.matrices, null);
            }
        }

        private void RenderLandentriesWireframe(LandEntryRenderBatch geometry)
        {
            foreach(var g in geometry)
                foreach(var t in g.Value)
                    RenderMesh(t.Key, t.Value);
        }

        public override void RenderBounds(List<LandEntry> entries, BufferMesh sphere, Camera cam)
        {
            BoundsShader.Use();
            BoundsShader.SetUniform("viewPos", cam.Realposition);
            BoundsShader.SetUniform("viewDir", cam.Orthographic ? cam.Forward : default);

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);

            if(!_bufferBridge.IsBuffered(sphere))
                _bufferBridge.LoadToCache(sphere);
            var handle = _bufferBridge.GetHandle(sphere);
            GL.BindVertexArray(handle.vao);

            foreach(LandEntry le in entries)
            {
                var b = le.ModelBounds;
                GLBufferingBridge.BufferMatrices(new(b.Matrix, b.Matrix * cam.ViewMatrix * cam.ProjectionMatrix));

                if(handle.eao == 0)
                    GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                else
                    GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
            }

            GL.Enable(EnableCap.DepthTest);
        }

        unsafe public override void DrawModelRelationship(List<Vector3> lines, Camera cam)
        {
            RenderMatrices rm = new(cam.ViewMatrix * cam.ProjectionMatrix);

            WireFrameShader.Use();
            GLBufferingBridge.BufferMatrices(rm);

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            // buffer the line data
            GL.BindVertexArray(_bufferBridge.LineBufferHandle.vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _bufferBridge.LineBufferHandle.vbo);

            Vector3[] data = lines.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(Vector3), data, BufferUsageHint.StreamDraw);

            GL.DrawArrays(PrimitiveType.Lines, 0, data.Length);
            GL.Enable(EnableCap.DepthTest);
        }


        #region Canvas

        private PolygonMode _lastPolygonMode;

        public override void CanvasPreDraw(int width, int height)
        {
            _lastPolygonMode = (PolygonMode)GL.GetInteger(GetPName.PolygonMode);

            UIShader.Use();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public override void CanvasPostDraw()
        {
            foreach(var b in _bufferBridge.UIBuffers.Where(x => !x.Value.used).ToArray())
            {
                _bufferBridge.UIReuse.Enqueue(b.Value);
                _bufferBridge.UIBuffers.Remove(b.Key);
            }

            foreach(var b in _bufferBridge.UIBuffers)
                b.Value.used = false;

            GL.Disable(EnableCap.Blend);
            GL.PolygonMode(MaterialFace.FrontAndBack, _lastPolygonMode);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void CanvasDrawUIElement(UI.UIElement element, float width, float height, bool forceUpdateTransforms)
        {
            if(_bufferBridge.GetUIBuffer(element.ID, out UIBuffer buffer))
            {
                UpdateTransforms(element.GetTransformBuffer(width, height));
                UpdateTexture(element.GetBufferTexture());
            }
            else
            {
                GL.BindVertexArray(buffer.vaoHandle);
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer.vboHandle);
                GL.BindTexture(TextureTarget.Texture2D, buffer.texHandle);

                if(element.UpdatedTransforms || forceUpdateTransforms)
                    UpdateTransforms(element.GetTransformBuffer(width, height));

                if(element.UpdatedTexture)
                    UpdateTexture(element.GetBufferTexture());
            }

            buffer.used = true;
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        private static unsafe void UpdateTransforms(float[] transformBuffer)
        {
            fixed(float* ptr = transformBuffer)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, 64, (IntPtr)ptr, BufferUsageHint.DynamicDraw);
            }
        }

        private static void UpdateTexture(Bitmap texture)
        {
            BitmapData data = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            texture.UnlockBits(data);
        }

        #endregion
    }
}
