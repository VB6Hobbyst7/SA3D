using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAArchive;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows;
using static SATools.SAModel.Graphics.OpenGL.GlobalBuffers;
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.Graphics.OpenGL.BufferMeshHandle, System.Collections.Generic.List<SATools.SAModel.Graphics.OpenGL.RenderMatrices>>>;
using SAVector3 = SATools.SAModel.Structs.Vector3;
using TKVector3 = OpenTK.Mathematics.Vector3;
using UIElement = SATools.SAModel.Graphics.UI.UIElement;

namespace SATools.SAModel.Graphics.OpenGL
{
    /// <summary>
    /// GL API Access object
    /// </summary>
    public sealed class GLAPIAccessObject : GAPIAccessObject
    {
        public override void GraphicsInit(Context context)
        {
            GL.Viewport(default, context.Resolution);
            GL.ClearColor(context.BackgroundColor.SystemCol);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.Multisample);
            
            //GL.Enable(EnableCap.FramebufferSrgb); srgb doesnt work for glcontrol, so we'll just leave it out

            InitializeBuffers();
            InitializeShaders();

            // for debug
            if(context.GetType() == typeof(DebugContext))
            {
                ((DebugContext)context).SphereMesh.Buffer(null, false);
            }
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

            ClearWeights();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            List<GLRenderMesh> renderMeshes = new();
            var (opaque, transparent, _) = RenderExtensions.PrepareLandEntries(context.Scene.VisualGeometry, context.Camera, _cameraViewMatrix, _cameraProjectionmatrix);

            foreach(GameTask tsk in context.Scene.objects)
            {
                tsk.Display();
                if(tsk is DisplayTask dtsk)
                    dtsk.Model.Prepare(renderMeshes, dtsk.TextureSet, _cameraViewMatrix, _cameraProjectionmatrix, null, null, dtsk.Model.HasWeight);
            }

            DefaultShader.Use();

            // first the opaque meshes
            context.Material.BufferTextureSet = context.Scene.LandTextureSet;
            opaque.RenderLandentries(context.Material);

            RenderExtensions.RenderModels(renderMeshes, false, context.Material);

            // then transparent meshes
            GL.Enable(EnableCap.Blend);
            GL.Uniform1(13, 0.001f);

            context.Material.BufferTextureSet = context.Scene.LandTextureSet;
            transparent.RenderLandentries(context.Material);

            RenderExtensions.RenderModels(renderMeshes, true, context.Material);
            GL.Disable(EnableCap.Blend);
            GL.Uniform1(13, 0f);
        }

        public override uint RenderDebug(DebugContext context)
        {
            context.Material.ViewPos = context.Camera.Realposition;
            context.Material.ViewDir = context.Camera.Orthographic ? context.Camera.ViewDir : default;

            ClearWeights();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            List<NJObject> objects = new();
            List<GLRenderMesh> renderMeshes = new();
            LandEntryRenderBatch opaque;
            LandEntryRenderBatch transparent;
            List<LandEntry> landEntriesRendered;

            if(!context.RenderCollision)
            {
                DefaultShader.Use();
                (opaque, transparent, landEntriesRendered) = RenderExtensions.PrepareLandEntries(context.Scene.VisualGeometry, context.Camera, _cameraViewMatrix, _cameraProjectionmatrix);

                foreach(GameTask tsk in context.Scene.objects)
                {
                    tsk.Display();
                    if(tsk is DisplayTask dtsk)
                    {
                        objects.Add(dtsk.Model);
                        dtsk.Model.Prepare(renderMeshes, dtsk.TextureSet, _cameraViewMatrix, _cameraProjectionmatrix, context.ActiveNJO, null, dtsk.Model.HasWeight);
                    }
                }
            }
            else
            {
                CollisionShader.Use();
                (opaque, transparent, landEntriesRendered) = RenderExtensions.PrepareLandEntries(context.Scene.CollisionGeometry, context.Camera, _cameraViewMatrix, _cameraProjectionmatrix);
            }

            // first the opaque meshes
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Uniform1(13, 0f);

            context.Material.BufferTextureSet = context.Scene.LandTextureSet;
            opaque.RenderLandentries(context.Material);

            RenderExtensions.RenderModels(renderMeshes, false, context.Material);

            // then transparent meshes
            GL.Enable(EnableCap.Blend);
            GL.Uniform1(13, 1f);

            context.Material.BufferTextureSet = context.Scene.LandTextureSet;
            transparent.RenderLandentries(context.Material);

            RenderExtensions.RenderModels(renderMeshes, true, context.Material);

            // then additional stuff
            if(context.WireframeMode == WireFrameMode.Overlay)
            {
                WireFrameShader.Use();
                GL.Disable(EnableCap.Blend);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

                opaque.RenderLandentriesWireframe();
                transparent.RenderLandentriesWireframe();
                RenderExtensions.RenderModelsWireframe(renderMeshes);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }

            if(context.BoundsMode == BoundsMode.All
                || context.BoundsMode == BoundsMode.Selected && context.ActiveLE != null)
            {
                BoundsShader.SetUniform("viewPos", context.Camera.Realposition);
                BoundsShader.SetUniform("viewDir", context.Camera.Orthographic ? context.Camera.ViewDir : default);
                RenderExtensions.RenderBounds(landEntriesRendered, context.SphereMesh, _cameraViewMatrix, _cameraProjectionmatrix);
            }

            if(context.ObjectRelationsMode == ObjectRelationsMode.Lines)
                RenderExtensions.DrawModelRelationship(objects, _cameraViewMatrix, _cameraProjectionmatrix);

            return (uint)renderMeshes.Count;
        }

        public override void OnAttachLoad(Attach attach)
        {
            if(!attach.HasWeight)
                attach.Buffer(null, false);
        }

        #region Material

        public override void BufferTextureSet(TextureSet textures)
        {
            if(textures == null)
                return;

            // Add new Buffer handles for as many texture as needed
            while(TextureHandles.Count < textures.Textures.Count)
                TextureHandles.Add((GL.GenTexture(), null));

            for(int i = 0; i < textures.Textures.Count; i++)
            {
                (int handle, Texture tex) = TextureHandles[i];
                Texture newTexture = textures.Textures[i];

                // no need to buffer the texture if the contents are still the same
                if(tex == newTexture)
                    return;

                GL.BindTexture(TextureTarget.Texture2D, handle);

                // buffer the texture data
                var texture = newTexture.TextureBitmap;
                BitmapData data = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                texture.UnlockBits(data);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                // tell the handle that this texture was buffered
                TextureHandles[i] = (handle, newTexture);
            }
        }

        public override unsafe void MaterialPostBuffer(Material material)
        {
            if(material.BufferMaterial.MaterialFlags.HasFlag(MaterialFlags.useTexture) && material.BufferTextureSet != null)
            {
                int textureIndex = (int)material.BufferMaterial.TextureIndex;
                if(textureIndex < TextureHandles.Count)
                    GL.BindTexture(TextureTarget.Texture2D, TextureHandles[textureIndex].Item1);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)material.BufferMaterial.TextureFiltering.ToGLMinFilter());
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)material.BufferMaterial.TextureFiltering.ToGLMagFilter());
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)material.BufferMaterial.WrapModeU());
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)material.BufferMaterial.WrapModeV());
                GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropyExt, material.BufferMaterial.AnisotropicFiltering ? 4 : 0);

            }

            // if the texture uses alpha, update the blend modes
            if(material.BufferMaterial.UseAlpha)
                GL.BlendFunc(material.BufferMaterial.SourceBlendMode.ToGLBlend(), material.BufferMaterial.DestinationBlendmode.ToGLBlend());

            // update the cull mode
            if(material.BufferMaterial.Culling)// && RenderMode != RenderMode.CullSide)
                GL.Enable(EnableCap.CullFace);
            else
                GL.Disable(EnableCap.CullFace);

            // update the material data buffer
            GL.BindBuffer(BufferTarget.UniformBuffer, MaterialHandle);
            fixed(byte* ptr = material.Buffer.ToArray())
            {
                GL.BufferData(BufferTarget.UniformBuffer, material.Buffer.Count, (IntPtr)ptr, BufferUsageHint.StreamDraw);
            }
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        #endregion

        #region Camera

        /// <summary>
        /// Camera view matrix
        /// </summary>
        private Matrix4 _cameraViewMatrix;

        /// <summary>
        /// Camera projection matrix
        /// </summary>
        private Matrix4 _cameraProjectionmatrix;

        public override void SetOrtographicMatrix(float width, float height, float zNear, float zFar)
            => _cameraProjectionmatrix = Matrix4.CreateOrthographic(width, height, zNear, zFar);

        public override void SetPerspectiveMatrix(float fovy, float aspect, float zNear, float zFar)
            => _cameraProjectionmatrix = Matrix4.CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar);

        public override void UpdateDirections(SAVector3 rotation, out SAVector3 up, out SAVector3 forward, out SAVector3 right)
        {
            Matrix4 mtx = rotation.CreateRotationMatrix(true);
            forward = new TKVector3(mtx * -Vector4.UnitZ).ToSA().Normalized();
            up = new TKVector3(mtx * Vector4.UnitY).ToSA().Normalized();
            right = new TKVector3(mtx * -Vector4.UnitX).ToSA().Normalized();
        }

        public override SAVector3 ToViewPos(SAVector3 position)
        {
            Vector4 viewPos = (position.ToGL4() * _cameraViewMatrix);
            return new SAVector3(viewPos.X, viewPos.Y, viewPos.Z);
        }

        public override void SetViewMatrix(SAVector3 position, SAVector3 rotation)
            => _cameraViewMatrix = Converters.CreateViewMatrix(position, rotation);

        public override void SetOrbitViewMatrix(SAVector3 position, SAVector3 rotation, SAVector3 orbitOffset)
            => _cameraViewMatrix = Matrix4.CreateTranslation(orbitOffset.ToGL()) * Converters.CreateViewMatrix(position, rotation);

        #endregion

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
            foreach(var b in UIBuffers.Where(x => !x.Value.used).ToArray())
            {
                UIReuse.Enqueue(b.Value);
                UIBuffers.Remove(b.Key);
            }

            foreach(var b in UIBuffers)
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

