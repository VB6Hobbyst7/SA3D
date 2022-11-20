using OpenTK.Graphics.OpenGL4;
using SATools.SAArchive;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.ModelData.Buffer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;

namespace SATools.SAModel.Graphics.OpenGL
{
    /// <summary>
    /// Contains all handles and material info of a mesh
    /// </summary>
    internal struct BufferMeshHandle
    {
        public readonly int vao;
        public readonly int vbo;
        public readonly int eao;
        public readonly int vertexCount;
        public readonly BufferMaterial material;

        public BufferMeshHandle(int vao, int vbo, int eao, int vertexCount, BufferMaterial material)
        {
            this.vao = vao;
            this.vbo = vbo;
            this.eao = eao;
            this.vertexCount = vertexCount;
            this.material = material;
        }
    }

    internal class UIBuffer
    {
        public int vaoHandle;
        public int vboHandle;
        public int texHandle;
        public bool used;
    }


    internal class GLBufferingBridge : BufferingBridge
    {

        /// <summary>
        /// Mesh handle per buffer mesh
        /// </summary>
        private readonly Dictionary<BufferMesh, BufferMeshHandle> MeshHandles
            = new();

        /// <summary>
        /// Handle for drawing lines
        /// </summary>
        public (int vao, int vbo) LineBufferHandle { get; private set; }

        /// <summary>
        /// Material buffer handle
        /// </summary>
        public int MaterialHandle { get; private set; }

        /// <summary>
        /// Texture buffer handles
        /// </summary>
        public Dictionary<TextureSet, int[]> TextureHandles { get; }
            = new();

        /// <summary>
        /// Buffers that can be repurposed
        /// </summary>
        public Queue<UIBuffer> UIReuse { get; }
            = new();

        /// <summary>
        /// Buffers that were used in the last cycle
        /// </summary>
        public Dictionary<Guid, UIBuffer> UIBuffers { get; }
            = new();

        public override void Initialize()
        {
            base.Initialize();
            MaterialHandle = GL.GenBuffer();

            // generate line buffer
            LineBufferHandle = (GL.GenVertexArray(), GL.GenBuffer());

            GL.BindVertexArray(LineBufferHandle.vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, LineBufferHandle.vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12, 0);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public BufferMeshHandle GetHandle(BufferMesh mesh)
        {
            if(!MeshHandles.TryGetValue(mesh, out var handle))
                throw new InvalidOperationException("Mesh was not buffered!");
            return handle;
        }

        internal static unsafe void BufferMatrices(RenderMatrices matrices)
        {
            GL.UniformMatrix4(10, 1, false, &matrices.worldMtx.M11);
            GL.UniformMatrix4(11, 1, false, &matrices.normalMtx.M11);
            GL.UniformMatrix4(12, 1, false, &matrices.MVP.M11);
        }

        public override unsafe void BufferVertexCache(BufferMesh mesh, CacheBuffer[] cache)
        {
            if(MeshHandles.TryGetValue(mesh, out var meshHandle))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, meshHandle.vbo);

                GL.BufferData(BufferTarget.ArrayBuffer, cache.Length * sizeof(CacheBuffer), cache, BufferUsageHint.StreamDraw);
                return;
            }

            // generating the buffers
            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int eao = 0;
            int vtxCount = mesh.TriangleList == null ? mesh.Corners.Length : mesh.TriangleList.Length;

            // Binding the buffers
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.BufferData(BufferTarget.ArrayBuffer, cache.Length * sizeof(CacheBuffer), cache, BufferUsageHint.StaticDraw);


            if(mesh.TriangleList != null)
            {
                eao = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, eao);

                GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.TriangleList.Length * sizeof(uint), mesh.TriangleList, BufferUsageHint.StaticDraw);
            }
            else
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            }


            // assigning attribute data
            // position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(CacheBuffer), 0);

            // normal
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof(CacheBuffer), 12);

            // color
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(CacheBuffer), 24);

            // uv
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, sizeof(CacheBuffer), 28);

            // weight
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(CacheBuffer), 36);

            MeshHandles.Add(mesh, new BufferMeshHandle(vao, vbo, eao, vtxCount, mesh.Material));
        }

        public override void DebufferVertexCache(BufferMesh mesh)
        {
            if(!MeshHandles.TryGetValue(mesh, out var handle))
                throw new InvalidOperationException("Mesh was not buffered");

            GL.DeleteVertexArray(handle.vao);
            GL.DeleteBuffer(handle.vbo);
            if(handle.eao != 0)
                GL.DeleteBuffer(handle.eao);
            MeshHandles.Remove(mesh);
        }

        public override bool IsBuffered(BufferMesh mesh) 
            => MeshHandles.ContainsKey(mesh);

        public override unsafe void BufferMaterial(Material material)
        {
            if(material.BufferMaterial.MaterialAttributes.HasFlag(MaterialAttributes.useTexture) && material.BufferTextureSet != null)
            {
                int textureIndex = (int)material.BufferMaterial.TextureIndex;
                GL.BindTexture(TextureTarget.Texture2D, TextureHandles[material.BufferTextureSet][textureIndex]);

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
            fixed(byte* ptr = material.Buffer)
            {
                GL.BufferData(BufferTarget.UniformBuffer, material.Buffer.Length, (IntPtr)ptr, BufferUsageHint.StreamDraw);
            }
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        protected override void BufferTextureSet(TextureSet textures)
        {
            if(textures == null || TextureHandles.ContainsKey(textures))
                return;

            int[] handles = new int[textures.Textures.Count];

            for(int i = 0; i < textures.Textures.Count; i++)
            {
                int handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, handle);

                // buffer the texture data
                var texture = textures.Textures[i].TextureBitmap;
                BitmapData data = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                texture.UnlockBits(data);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                // tell the handle that this texture was buffered
                handles[i] = handle;
            }

            TextureHandles.Add(textures, handles);
        }

        protected override void DebufferTextureSet(TextureSet textures)
        {
            if(textures == null)
                return;

            if(!TextureHandles.TryGetValue(textures, out int[] handles))
                throw new InvalidOperationException("TextureSet was not buffered!");

            GL.DeleteBuffers(handles.Length, handles);
        }

        /// <summary>
        /// Returns true if the buffer is not reused
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool GetUIBuffer(Guid id, out UIBuffer buffer)
        {
            if(!UIBuffers.TryGetValue(id, out buffer))
            {
                if(UIReuse.Count == 0)
                {
                    buffer = GenUIBuffer();
                    UIBuffers.Add(id, buffer);
                    return true;
                }
                else
                {
                    buffer = UIReuse.Dequeue();
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a ui buffer
        /// </summary>
        /// <returns></returns>
        public static UIBuffer GenUIBuffer()
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
    }
}
