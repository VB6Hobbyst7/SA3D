using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAArchive;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Graphics.OpenGL.Properties;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows;
using TKVector3 = OpenTK.Mathematics.Vector3;
using UIElement = SATools.SAModel.Graphics.UI.UIElement;

namespace SATools.SAModel.Graphics.OpenGL
{
    /// <summary>
    /// GL API Access object
    /// </summary>
    public sealed class GLAPIAccessObject : GAPIAccessObject
    {
        private Shader _defaultShader;

        private Shader _wireFrameShader;

        public override void GraphicsInit(Context context)
        {
            GL.Viewport(default, context.Resolution);
            GL.ClearColor(context.BackgroundColor.SystemCol);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);
            //GL.Enable(EnableCap.FramebufferSrgb); srgb doesnt work for glcontrol, so we'll just leave it out

            // Material
            _materialHandle = GL.GenBuffer();
            _materialTextureHandle = GL.GenTexture();

            // loading the shader
            string vertexShader = Encoding.UTF8.GetString(Resources.VertexShader);
            string fragShader = Encoding.UTF8.GetString(Resources.FragShader);
            _defaultShader = new Shader(vertexShader, fragShader);
            _defaultShader.BindUniformBlock("Material", 0, _materialHandle);

            // wireframe
            vertexShader = Encoding.UTF8.GetString(Resources.Wireframe_vert);
            fragShader = Encoding.UTF8.GetString(Resources.Wireframe_frag);
            _wireFrameShader = new Shader(vertexShader, fragShader);

            // canvas
            vertexShader = Encoding.UTF8.GetString(Resources.DefaultUI_vert);
            fragShader = Encoding.UTF8.GetString(Resources.DefaultUI_frag);
            _uiShader = new Shader(vertexShader, fragShader);

            // for debug
            if(context.GetType() == typeof(DebugContext))
            {
                ((DebugContext)context).SphereMesh.Buffer(null, false);
            }

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        protected override void InternalAsWindow(Context context)
            => new GLWindow(context, InputBridge, context.Resolution).Run();

        protected override FrameworkElement InternalAsControl(Context context)
            => new GLControl(context, InputBridge);

        public override void UpdateViewport(Rectangle screen, bool resized)
        {
            if(resized)
            {
                GL.Viewport(screen.Size);
            }
        }

        public override void UpdateBackgroundColor(Structs.Color color) => GL.ClearColor(color.SystemCol);

        public override void DebugUpdateWireframe(WireFrameMode wireframeMode)
        {
            switch(wireframeMode)
            {
                case WireFrameMode.None:
                case WireFrameMode.Overlay:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    break;
                case WireFrameMode.ReplaceLine:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    break;
                case WireFrameMode.ReplacePoint:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                    break;
                default:
                    break;
            }
        }

        public override void DebugUpdateBoundsMode(BoundsMode boundsMode) { }

        public override void DebugUpdateRenderMode(RenderMode renderMode) { }

        public override void Render(Context context)
        {
            context.Material.ViewPos = context.Camera.Realposition;
            context.Material.ViewDir = context.Camera.Orthographic ? context.Camera.ViewDir : default;

            RenderExtensions.ClearWeights();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            List<GLRenderMesh> renderMeshes = new();
            List<LandEntry> entries = new();

            foreach(LandEntry le in context.Scene.VisualGeometry)
                le.Prepare(renderMeshes, context.Scene.LandTextureSet, entries, context.Camera, _cameraViewMatrix, _cameraProjectionmatrix, null);
            foreach(GameTask tsk in context.Scene.objects)
            {
                tsk.Display();
                if(tsk is DisplayTask dtsk)
                    dtsk.Model.Prepare(renderMeshes, dtsk.TextureSet,  _cameraViewMatrix, _cameraProjectionmatrix, null, null, dtsk.Model.HasWeight);
            }

            _defaultShader.Use();
            GL.BindTexture(TextureTarget.Texture2D, _materialTextureHandle);

            // first the opaque meshes
            RenderExtensions.RenderModels(renderMeshes, false, context.Material);

            // then transparent meshes
            GL.Enable(EnableCap.Blend);
            RenderExtensions.RenderModels(renderMeshes, true, context.Material);
            GL.Disable(EnableCap.Blend);
        }

        public override uint RenderDebug(DebugContext context)
        {
            context.Material.ViewPos = context.Camera.Realposition;
            context.Material.ViewDir = context.Camera.Orthographic ? context.Camera.ViewDir : default;
            context.Material.RenderMode = context.RenderMode;

            RenderExtensions.ClearWeights();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            List<GLRenderMesh> renderMeshes = new();
            List<LandEntry> entries = new();

            if(!context.RenderCollision)
            {
                foreach(LandEntry le in context.Scene.VisualGeometry)
                    le.Prepare(renderMeshes, context.Scene.LandTextureSet, entries, context.Camera, _cameraViewMatrix, _cameraProjectionmatrix, context.ActiveLE);
                foreach(GameTask tsk in context.Scene.objects)
                {
                    tsk.Display();
                    if(tsk is DisplayTask dtsk)
                        dtsk.Model.Prepare(renderMeshes, dtsk.TextureSet, _cameraViewMatrix, _cameraProjectionmatrix, context.ActiveNJO, null, dtsk.Model.HasWeight);
                }
            }
            else
            {
                foreach(LandEntry le in context.Scene.CollisionGeometry)
                    le.Prepare(renderMeshes, null, entries, context.Camera, _cameraViewMatrix, _cameraProjectionmatrix, context.ActiveLE);
            }

            _defaultShader.Use();
            if(context.RenderCollision)
                context.Material.RenderMode = RenderMode.FullBright;

            // first the opaque meshes
            RenderExtensions.RenderModels(renderMeshes, false, context.Material);

            // then transparent meshes
            GL.Enable(EnableCap.Blend);
            GL.Uniform1(13, 1f);

            // then the transparent meshes
            RenderExtensions.RenderModels(renderMeshes, true, context.Material);

            if(context.WireframeMode == WireFrameMode.Overlay)
            {
                _wireFrameShader.Use();
                GL.Disable(EnableCap.Blend);
                RenderExtensions.RenderModelsWireframe(renderMeshes);
                GL.Enable(EnableCap.Blend);
                _defaultShader.Use();
            }

            if(context.BoundsMode == BoundsMode.All
                || context.BoundsMode == BoundsMode.Selected && context.ActiveLE != null)
            {
                GL.Disable(EnableCap.DepthTest);
                context.Material.RenderMode = RenderMode.Falloff;
                Matrix4 normal = Matrix4.Identity;
                GL.UniformMatrix4(11, false, ref normal);

                List<LandEntry> boundObjs;

                if(context.BoundsMode == BoundsMode.All)
                {
                    boundObjs = context.Scene.geometry;
                }
                else
                {
                    boundObjs = new List<LandEntry>
                    {
                        context.ActiveLE
                    };
                }

                foreach(LandEntry le in boundObjs)
                {
                    Bounds b = le.ModelBounds;
                    Matrix4 world = Matrix4.CreateScale(b.Radius) * Matrix4.CreateTranslation(b.Position.ToGL());
                    GL.UniformMatrix4(10, false, ref world);
                    world = world * _cameraViewMatrix * _cameraProjectionmatrix;
                    GL.UniformMatrix4(12, false, ref world);
                    context.SphereMesh.Render(true, context.Material);
                }

                GL.Enable(EnableCap.DepthTest);
            }
            GL.Disable(EnableCap.Blend);
            GL.Uniform1(13, 0f);

            return (uint)renderMeshes.Count;
        }

        #region Material

        private int _materialHandle;

        private int _materialTextureHandle;

        private Texture bufferedTexture;

        public override void MaterialPreBuffer(Material material)
        {

        }

        public override unsafe void MaterialPostBuffer(Material material)
        {
            if(material.BufferMaterial.MaterialFlags.HasFlag(MaterialFlags.useTexture) && material.BufferTextureSet != null)
            {
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)material.BufferMaterial.TextureFiltering.ToGLMinFilter());
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)material.BufferMaterial.TextureFiltering.ToGLMagFilter());
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)material.BufferMaterial.WrapModeU());
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)material.BufferMaterial.WrapModeV());

                Texture newBufferTexture = material.BufferTextureSet.Textures[(int)material.BufferMaterial.TextureIndex];
                if(newBufferTexture != bufferedTexture)
                {
                    bufferedTexture = newBufferTexture;
                    Bitmap texture = bufferedTexture.TextureBitmap;

                    BitmapData data = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    texture.UnlockBits(data);

                }
            }

            if(material.BufferMaterial.UseAlpha)
                GL.BlendFunc(material.BufferMaterial.SourceBlendMode.ToGLBlend(), material.BufferMaterial.DestinationBlendmode.ToGLBlend());

            if(material.BufferMaterial.Culling)// && RenderMode != RenderMode.CullSide)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            GL.BindBuffer(BufferTarget.UniformBuffer, _materialHandle);
            fixed(byte* ptr = material.Buffer.ToArray())
            {
                GL.BufferData(BufferTarget.UniformBuffer, material.Buffer.Count, (IntPtr)ptr, BufferUsageHint.StreamDraw);
            }
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        #endregion

        #region Camera

        private Matrix4 _cameraViewMatrix;
        private Matrix4 _cameraProjectionmatrix;

        private Matrix4 CreateRotationMatrix(Structs.Vector3 rotation)
        {
            return Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation.Z)) *
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotation.Y)) *
                    Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotation.X));
        }

        public override void SetOrtographicMatrix(float width, float height, float zNear, float zFar) 
            => _cameraProjectionmatrix = Matrix4.CreateOrthographic(width, height, zNear, zFar);

        public override void SetPerspectiveMatrix(float fovy, float aspect, float zNear, float zFar) 
            => _cameraProjectionmatrix = Matrix4.CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar);

        public override void UpdateDirections(Structs.Vector3 rotation, out Structs.Vector3 up, out Structs.Vector3 forward, out Structs.Vector3 right)
        {
            Matrix4 mtx = CreateRotationMatrix(rotation);
            forward = new TKVector3(mtx * -Vector4.UnitZ).ToSA().Normalized();
            up = new TKVector3(mtx * Vector4.UnitY).ToSA().Normalized();
            right = new TKVector3(mtx * -Vector4.UnitX).ToSA().Normalized();
        }

        public override Structs.Vector3 ToViewPos(Structs.Vector3 position)
        {
            Vector4 viewPos = (position.ToGL4() * _cameraViewMatrix);
            return new Structs.Vector3(viewPos.X, viewPos.Y, viewPos.Z);
        }

        private Matrix4 GetViewMatrix(Structs.Vector3 position, Structs.Vector3 rotation) 
            => Matrix4.CreateTranslation(-position.ToGL()) * CreateRotationMatrix(rotation);

        public override void SetViewMatrix(Structs.Vector3 position, Structs.Vector3 rotation) 
            => _cameraViewMatrix = GetViewMatrix(position, rotation);

        public override void SetOrbitViewMatrix(Structs.Vector3 position, Structs.Vector3 rotation, Structs.Vector3 orbitOffset) 
            => _cameraViewMatrix = Matrix4.CreateTranslation(orbitOffset.ToGL()) * GetViewMatrix(position, rotation);

        #endregion

        #region Canvas

        private class UIBuffer
        {
            public int vaoHandle;
            public int vboHandle;
            public int texHandle;
            public bool used;
        }

        private Shader _uiShader;

        private PolygonMode _lastPolygonMode;

        /// <summary>
        /// Buffers that can be repurposed
        /// </summary>
        private readonly Queue<UIBuffer> _reuse = new();

        /// <summary>
        /// Buffers that were used in the last cycle
        /// </summary>
        private readonly Dictionary<Guid, UIBuffer> _buffers = new();


        public override void CanvasPreDraw(int width, int height)
        {
            _lastPolygonMode = (PolygonMode)GL.GetInteger(GetPName.PolygonMode);

            _uiShader.Use();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public override void CanvasPostDraw()
        {
            foreach(var b in _buffers.Where(x => !x.Value.used).ToArray())
            {
                _reuse.Enqueue(b.Value);
                _buffers.Remove(b.Key);
            }

            foreach(var b in _buffers)
                b.Value.used = false;

            GL.Disable(EnableCap.Blend);
            GL.PolygonMode(MaterialFace.FrontAndBack, _lastPolygonMode);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void CanvasDrawUIElement(UIElement element, float width, float height, bool forceUpdateTransforms)
        {
            if(GetUIBuffer(element.ID, out UIBuffer buffer))
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
                {
                    UpdateTransforms(element.GetTransformBuffer(width, height));

                }
                if(element.UpdatedTexture)
                {
                    UpdateTexture(element.GetBufferTexture());
                }
            }

            buffer.used = true;
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        /// <summary>
        /// Returns true if the buffer is not reused
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool GetUIBuffer(Guid id, out UIBuffer buffer)
        {
            if(!_buffers.TryGetValue(id, out buffer))
            {
                if(_reuse.Count == 0)
                {
                    buffer = GenUIBuffer();
                    _buffers.Add(id, buffer);
                    return true;
                }
                else
                {
                    buffer = _reuse.Dequeue();
                }
            }

            return false;
        }

        private static UIBuffer GenUIBuffer()
        {
            int vaoHandle = GL.GenVertexArray();
            int vboHandle = GL.GenBuffer();
            GL.BindVertexArray(vaoHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);

            // assigning attribute data
            // position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 16, 0);

            // uv
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 16, 8);

            int texHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            return new UIBuffer()
            {
                vaoHandle = vaoHandle,
                vboHandle = vboHandle,
                texHandle = texHandle
            };
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
