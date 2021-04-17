using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAArchive;
using SATools.SAModel.Graphics.OpenGL.Properties;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Color = SATools.SAModel.Structs.Color;

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

    /// <summary>
    ///  Global buffers
    /// </summary>
    internal static class GlobalBuffers
    {
        /// <summary>
        /// A single cached vertex
        /// </summary>
        private struct CachedVertex
        {
            public Vector4 position;
            public Vector3 normal;

            public CachedVertex(Vector4 position, Vector3 normal)
            {
                this.position = position;
                this.normal = normal;
            }

            public CachedVertex(BufferVertex vtx)
            {
                position = vtx.Position.ToGL4();
                normal = vtx.Normal.ToGL();
            }

            public override string ToString()
            {
                return $"({position.X:f3}, {position.Y:f3}, {position.Z:f3}, {position.W:f3}) - ({normal.X:f3}, {normal.Y:f3}, {normal.Z:f3})";
            }
        }

        /// <summary>
        /// Vertex Cache size
        /// </summary>
        internal const int VertexCacheSize = 0xFFFF;

        /// <summary>
        /// Vertex cache
        /// </summary>
        private static CachedVertex[] Vertices { get; }
            = new CachedVertex[VertexCacheSize];

        /// <summary>
        /// weight per cached vertex to visualize on polygons (debug)
        /// </summary>
        private static float[] VertexWeights { get; }
            = new float[VertexCacheSize];

        /// <summary>
        /// Mesh handle per buffer mesh
        /// </summary>
        internal static Dictionary<BufferMesh, BufferMeshHandle> MeshHandles { get; }
            = new();

        /// <summary>
        /// Handle for drawing lines
        /// </summary>
        internal static (int vao, int vbo) LineBufferHandle { get; private set; }

        /// <summary>
        /// Material buffer handle
        /// </summary>
        internal static int MaterialHandle { get; private set; }

        /// <summary>
        /// Texture buffer handles
        /// </summary>
        internal static List<(int, Texture)> TextureHandles { get; }
            = new();


        /// <summary>
        /// Buffers that can be repurposed
        /// </summary>
        internal static Queue<UIBuffer> UIReuse { get; }
            = new();

        /// <summary>
        /// Buffers that were used in the last cycle
        /// </summary>
        internal static Dictionary<Guid, UIBuffer> UIBuffers { get; }
            = new();

        #region Shaders

        /// <summary>
        /// For all kinds of 3D models
        /// </summary>
        internal static Shader DefaultShader { get; private set; }

        /// <summary>
        /// For overlay wireframes
        /// </summary>
        internal static Shader WireFrameShader { get; private set; }

        /// <summary>
        /// For landentry bounds
        /// </summary>
        internal static Shader BoundsShader { get; private set; }

        /// <summary>
        /// For Rendering Collision geometry
        /// </summary>
        internal static Shader CollisionShader { get; private set; }

        /// <summary>
        /// For UI textures
        /// </summary>
        internal static Shader UIShader { get; private set; }

        #endregion

        internal static void InitializeBuffers()
        {
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

        internal static void InitializeShaders()
        {
            // loading the shader
            string vertexShader = Encoding.UTF8.GetString(Resources.VertexShader);
            string fragShader = Encoding.UTF8.GetString(Resources.FragShader);
            DefaultShader = new Shader(vertexShader, fragShader);
            DefaultShader.BindUniformBlock("Material", 0, MaterialHandle);

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

        /// <summary>
        /// Buffers an attach
        /// </summary>
        /// <param name="atc"></param>
        /// <param name="worldMtx"></param>
        /// <param name="active"></param>
        internal static unsafe void Buffer(this Attach atc, Matrix4? worldMtx, bool active)
        {
            if(atc.MeshData == null)
                throw new InvalidOperationException("Attach \"" + atc.Name + "\" has not been buffered");
            if(atc.MeshData.Length == 0)
                return;

            Matrix3 normalMtx = default;
            if(worldMtx.HasValue)
            {
                Matrix4 t = worldMtx.Value.Inverted();
                t.Transpose();
                normalMtx = new Matrix3(t);
            }

            foreach(BufferMesh mesh in atc.MeshData)
            {
                // Material testing
                //if(mesh.Material?.Diffuse == Color.White)
                //{
                //	if(rand == null)
                //		rand = new Random();
                //	var col = mesh.Material.Diffuse;
                //	col.RGBA = (uint)rand.Next(int.MinValue, int.MaxValue) | 0xFFu;
                //	mesh.Material.Diffuse = col;
                //}

                if(mesh.Vertices != null)
                {
                    if(worldMtx == null)
                    {
                        foreach(BufferVertex vtx in mesh.Vertices)
                            Vertices[vtx.Index + mesh.VertexWriteOffset] = new(vtx);
                    }
                    else
                    {
                        foreach(BufferVertex vtx in mesh.Vertices)
                        {
                            Vector4 pos = (vtx.Position.ToGL4() * worldMtx.Value) * vtx.Weight;
                            Vector3 nrm = (vtx.Normal.ToGL() * normalMtx) * vtx.Weight;

                            int index = vtx.Index + mesh.VertexWriteOffset;

                            if(active)
                                VertexWeights[index] = vtx.Weight;
                            else if(!mesh.ContinueWeight)
                                VertexWeights[index] = 0;

                            if(mesh.ContinueWeight)
                            {
                                Vertices[index].position += pos;
                                Vertices[index].normal += nrm;
                            }
                            else
                            {
                                Vertices[index].position = pos;
                                Vertices[index].normal = nrm;
                            }
                        }
                    }
                }

                if(mesh.Corners != null)
                {
                    int structSize = 36;
                    byte[] vertexData;
                    using(MemoryStream stream = new(mesh.Corners.Length * structSize))
                    {
                        BinaryWriter writer = new(stream);

                        foreach(BufferCorner c in mesh.Corners)
                        {
                            CachedVertex vtx = Vertices[c.VertexIndex + mesh.VertexReadOffset];

                            writer.Write(vtx.position.X);
                            writer.Write(vtx.position.Y);
                            writer.Write(vtx.position.Z);

                            writer.Write(vtx.normal.X);
                            writer.Write(vtx.normal.Y);
                            writer.Write(vtx.normal.Z);

                            Color col = worldMtx.HasValue ? Helper.GetWeightColor(VertexWeights[c.VertexIndex + mesh.VertexReadOffset]) : c.Color;
                            writer.Write(new byte[] { col.R, col.G, col.B, col.A });

                            writer.Write(c.Uv.X);
                            writer.Write(c.Uv.Y);
                        }

                        vertexData = stream.ToArray();
                    }

                    if(MeshHandles.ContainsKey(mesh))
                    {
                        if(!worldMtx.HasValue)
                            throw new InvalidOperationException("Rebuffering weighted(?) mesh without matrix");
                        BufferMeshHandle meshHandle = MeshHandles[mesh];

                        GL.BindBuffer(BufferTarget.ArrayBuffer, meshHandle.vbo);

                        fixed(byte* ptr = vertexData)
                            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length, (IntPtr)ptr, BufferUsageHint.StreamDraw);
                    }
                    else
                    {
                        // generating the buffers
                        int vao = GL.GenVertexArray();
                        int vbo = GL.GenBuffer();
                        int eao = 0;
                        int vtxCount = mesh.TriangleList == null ? mesh.Corners.Length : mesh.TriangleList.Length;

                        // Binding the buffers
                        GL.BindVertexArray(vao);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

                        fixed(byte* ptr = vertexData)
                            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length, (IntPtr)ptr, worldMtx.HasValue ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);


                        if(mesh.TriangleList != null)
                        {
                            eao = GL.GenBuffer();
                            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eao);

                            fixed(uint* ptr = mesh.TriangleList)
                                GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.TriangleList.Length * sizeof(uint), (IntPtr)ptr, BufferUsageHint.StaticDraw);
                        }
                        else
                        {
                            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                        }


                        // assigning attribute data
                        // position
                        GL.EnableVertexAttribArray(0);
                        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, structSize, 0);

                        // normal
                        GL.EnableVertexAttribArray(1);
                        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, structSize, 12);

                        // color
                        GL.EnableVertexAttribArray(2);
                        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, structSize, 24);

                        // uv
                        GL.EnableVertexAttribArray(3);
                        GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, structSize, 28);

                        MeshHandles.Add(mesh, new BufferMeshHandle(vao, vbo, eao, vtxCount, mesh.Material));
                    }
                }
            }
        }

        /// <summary>
        /// Releases a buffered attach
        /// </summary>
        /// <param name="atc"></param>
        internal static void DeBuffer(this Attach atc)
        {
            foreach(BufferMesh mesh in atc.MeshData)
            {
                if(MeshHandles.TryGetValue(mesh, out var handle))
                {
                    GL.DeleteVertexArray(handle.vao);
                    GL.DeleteBuffer(handle.vbo);
                    if(handle.eao != 0)
                        GL.DeleteBuffer(handle.eao);
                    MeshHandles.Remove(mesh);
                }
            }
        }

        /// <summary>
        /// Clears <see cref="VertexWeights"/>
        /// </summary>
        internal static void ClearWeights()
            => Array.Clear(VertexWeights, 0, VertexWeights.Length);

        /// <summary>
        /// Returns true if the buffer is not reused
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static bool GetUIBuffer(Guid id, out UIBuffer buffer)
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
        internal static UIBuffer GenUIBuffer()
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
