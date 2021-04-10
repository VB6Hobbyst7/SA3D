using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.IO;
using static SATools.SACommon.MathHelper;
using Color = SATools.SAModel.Structs.Color;
using SAVector2 = SATools.SAModel.Structs.Vector2;
using SAVector3 = SATools.SAModel.Structs.Vector3;

namespace SATools.SAModel.Graphics.OpenGL
{
    struct BufferMeshHandle
    {
        public readonly int vao;
        public readonly int vbo;
        public readonly int eao;
        public readonly int vertexCount;

        public BufferMeshHandle(int vao, int vbo, int eao, int vertexCount)
        {
            this.vao = vao;
            this.vbo = vbo;
            this.eao = eao;
            this.vertexCount = vertexCount;
        }
    }

    struct CachedVertex
    {
        public Vector4 position;
        public Vector3 normal;

        public CachedVertex(Vector4 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }
    }

    public static class RenderExtensions
    {
        private static readonly CachedVertex[] vertices = new CachedVertex[0xFFFF];
        private static readonly float[] weights = new float[0xFFFF];
        private static readonly Dictionary<BufferMesh, BufferMeshHandle> meshHandles = new Dictionary<BufferMesh, BufferMeshHandle>();

        //private static Random rand;

        public static void ClearWeights()
        {
            Array.Clear(weights, 0, weights.Length);
        }

        public static Matrix4 GenMatrix(SAVector3 position, SAVector3 rotation, SAVector3 scale, bool rotateZYX)
        {
            Matrix4 rotMtx;
            if(rotateZYX)
            {
                rotMtx = Matrix4.CreateRotationZ(DegToRad(rotation.Z)) *
                        Matrix4.CreateRotationY(DegToRad(rotation.Y)) *
                        Matrix4.CreateRotationX(DegToRad(rotation.X));
            }
            else
            {
                rotMtx = Matrix4.CreateRotationX(DegToRad(rotation.X)) *
                        Matrix4.CreateRotationY(DegToRad(rotation.Y)) *
                        Matrix4.CreateRotationZ(DegToRad(rotation.Z));
            }


            return Matrix4.CreateScale(scale.ToGL()) * rotMtx * Matrix4.CreateTranslation(position.ToGL());
        }

        public static Matrix4 LocalMatrix(this NJObject obj) => GenMatrix(obj.Position, obj.Rotation, obj.Scale, obj.RotateZYX);

        public static Matrix4 LocalMatrix(this LandEntry obj) => GenMatrix(obj.Position, obj.Rotation, obj.Scale, obj.RotateZYX);

        unsafe public static void Buffer(this ModelData.Attach atc, Matrix4? worldMtx, bool active)
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
                            vertices[vtx.index] = vtx.ToCache();
                    }
                    else
                    {
                        foreach(BufferVertex vtx in mesh.Vertices)
                        {
                            Vector4 pos = (vtx.position.ToGL4() * worldMtx.Value) * vtx.weight;
                            Vector3 nrm = (vtx.normal.ToGL() * normalMtx) * vtx.weight;
                            if(active)
                                weights[vtx.index] = vtx.weight;
                            else if(weights[vtx.index] > 0 && !mesh.ContinueWeight)
                                weights[vtx.index] = 0;

                            if(mesh.ContinueWeight)
                            {
                                vertices[vtx.index].position += pos;
                                vertices[vtx.index].normal += nrm;
                            }
                            else
                            {
                                vertices[vtx.index].position = pos;
                                vertices[vtx.index].normal = nrm;
                            }
                        }
                    }
                }

                if(mesh.Corners != null)
                {
                    int structSize = 36;
                    byte[] vertexData;
                    using(MemoryStream stream = new MemoryStream(mesh.Corners.Length * structSize))
                    {
                        BinaryWriter writer = new BinaryWriter(stream);

                        foreach(BufferCorner c in mesh.Corners)
                        {
                            CachedVertex vtx = vertices[c.vertexIndex];

                            writer.Write(vtx.position.X);
                            writer.Write(vtx.position.Y);
                            writer.Write(vtx.position.Z);

                            writer.Write(vtx.normal.X);
                            writer.Write(vtx.normal.Y);
                            writer.Write(vtx.normal.Z);

                            Color col = worldMtx.HasValue ? Helper.GetWeightColor(weights[c.vertexIndex]) : c.color;
                            writer.Write(new byte[] { col.R, col.G, col.B, col.A });

                            writer.Write(c.uv.X);
                            writer.Write(c.uv.Y);
                        }

                        vertexData = stream.ToArray();
                    }

                    if(meshHandles.ContainsKey(mesh))
                    {
                        if(!worldMtx.HasValue)
                            throw new InvalidOperationException("Rebuffering weighted(?) mesh without matrix");
                        BufferMeshHandle meshHandle = meshHandles[mesh];

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
                            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


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

                        meshHandles.Add(mesh, new BufferMeshHandle(vao, vbo, eao, vtxCount));
                    }
                }
            }
        }

        public static void DeBuffer(this ModelData.Attach atc)
        {
            foreach(BufferMesh mesh in atc.MeshData)
            {
                if(meshHandles.TryGetValue(mesh, out var handle))
                {
                    GL.DeleteVertexArray(handle.vao);
                    GL.DeleteBuffer(handle.vbo);
                    if(handle.eao != 0)
                        GL.DeleteBuffer(handle.eao);
                    meshHandles.Remove(mesh);
                }
            }
        }

        public static void Render(this ModelData.Attach atc, Matrix4? weightMtx, bool transparent, bool active, Material material)
        {
            if(atc.MeshData == null)
                throw new InvalidOperationException($"Attach {atc.Name} has no buffer meshes");

            // rebuffer weighted models
            if(weightMtx.HasValue && !transparent)
            {
                atc.Buffer(weightMtx, active);
            }

            foreach(BufferMesh m in atc.MeshData)
            {
                if(m.Material == null || m.Material.UseAlpha != transparent)
                    continue;

                if(!meshHandles.TryGetValue(m, out var handle))
                {
                    atc.Buffer(null, active);
                    handle = meshHandles[m];
                }
                material.BufferMaterial = m.Material;
                GL.BindVertexArray(handle.vao);
                GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
            }
        }

        public static void Prepare(this NJObject obj, List<GLRenderMesh> renderMeshes, Matrix4 viewMatrix, Matrix4 projectionMatrix, NJObject activeObj, Matrix4? parentWorld, bool weighted)
        {
            Matrix4 world = obj.LocalMatrix();
            if(parentWorld.HasValue)
                world *= parentWorld.Value;

            if(obj.Attach != null)
            {
                // if a model is weighted, then the buffered vertex positions/normals will have to be set to world space, which means that world and normal matrix should be identities
                if(weighted)
                {
                    renderMeshes.Add(new GLRenderMesh(obj.Attach, world, Matrix4.Identity, Matrix4.Identity, viewMatrix * projectionMatrix, obj == activeObj));
                }
                else
                {
                    Matrix4 normalMtx = world.Inverted();
                    normalMtx.Transpose();
                    renderMeshes.Add(new GLRenderMesh(obj.Attach, null, world, normalMtx, world * viewMatrix * projectionMatrix, obj == activeObj));
                }
            }

            for(int i = 0; i < obj.ChildCount; i++)
                obj[i].Prepare(renderMeshes, viewMatrix, projectionMatrix, activeObj, world, weighted);
        }

        public static void Prepare(this LandEntry le, List<GLRenderMesh> renderMeshes, List<LandEntry> entries, Camera camera, Matrix4 viewMatrix, Matrix4 projectionMatrix, LandEntry activeLE)
        {
            if(!camera.CanRender(le.ModelBounds) || entries.Contains(le))
                return;
            entries.Add(le);
            Matrix4 world = le.LocalMatrix();
            Matrix4 normalMtx = world.Inverted();
            normalMtx.Transpose();
            renderMeshes.Add(new GLRenderMesh(le.Attach, null, world, normalMtx, world * viewMatrix * projectionMatrix, le == activeLE));
        }

        public static void RenderModels(List<GLRenderMesh> renderMeshes, bool transparent, Material material)
        {
            for(int i = 0; i < renderMeshes.Count; i++)
            {
                GLRenderMesh m = renderMeshes[i];
                GL.UniformMatrix4(10, false, ref m.worldMtx);
                GL.UniformMatrix4(11, false, ref m.normalMtx);
                GL.UniformMatrix4(12, false, ref m.MVP);
                m.attach.Render(m.realWorldMtx, transparent, m.active, material);
            }
        }

        public static void RenderModelsWireframe(List<GLRenderMesh> renderMeshes, bool transparent, DebugMaterial material)
        {
            GL.Uniform1(13, 0.001f); // setting normal offset for wireframe
            RenderMode old = material.RenderMode;
            material.RenderMode = RenderMode.FullDark; // drawing the lines black
            material.BufferMaterial = new BufferMaterial();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            for(int i = 0; i < renderMeshes.Count; i++)
            {
                GLRenderMesh m = renderMeshes[i];

                if(m.attach.MeshData == null)
                    throw new InvalidOperationException($"Attach {m.attach.Name} has no buffer meshes");

                GL.UniformMatrix4(10, false, ref m.worldMtx);
                GL.UniformMatrix4(11, false, ref m.normalMtx);
                GL.UniformMatrix4(12, false, ref m.MVP);

                foreach(BufferMesh bm in m.attach.MeshData)
                {
                    if(bm.Material == null || bm.Material.UseAlpha != transparent)
                        continue;

                    if(!meshHandles.TryGetValue(bm, out var handle))
                        throw new InvalidOperationException($"Mesh in {m.attach.Name} not buffered");

                    if(bm.Material.Culling)
                        GL.Enable(EnableCap.CullFace);
                    else
                        GL.Disable(EnableCap.CullFace);

                    GL.BindVertexArray(handle.vao);

                    if(handle.eao == 0)
                        GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                    else
                        GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
                }
            }

            // reset
            GL.Uniform1(13, 0f);
            material.RenderMode = old;
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }


        #region Conversion extensions

        private static CachedVertex ToCache(this BufferVertex vtx)
        {
            return new CachedVertex(vtx.position.ToGL4(), vtx.normal.ToGL());
        }

        public static Vector3 ToGL(this SAVector3 vec3)
        {
            return new Vector3(vec3.X, vec3.Y, vec3.Z);
        }

        public static Vector4 ToGL4(this SAVector3 vec3)
        {
            return new Vector4(vec3.X, vec3.Y, vec3.Z, 1);
        }

        public static SAVector3 ToSA(this Vector3 vec3)
        {
            return new Structs.Vector3(vec3.X, vec3.Y, vec3.Z);
        }

        public static Vector2 ToGL(this SAVector2 vec2)
        {
            return new Vector2(vec2.X, vec2.Y);
        }

        public static SAVector2 ToSA(this Vector2 vec2)
        {
            return new Structs.Vector2(vec2.X, vec2.Y);
        }


        public static BlendingFactor ToGLBlend(this BlendMode instr)
        {
            switch(instr)
            {
                default:
                case BlendMode.Zero:
                    return BlendingFactor.Zero;
                case BlendMode.One:
                    return BlendingFactor.One;
                case BlendMode.Other:
                    return BlendingFactor.SrcColor;
                case BlendMode.OtherInverted:
                    return BlendingFactor.OneMinusSrcColor;
                case BlendMode.SrcAlpha:
                    return BlendingFactor.SrcAlpha;
                case BlendMode.SrcAlphaInverted:
                    return BlendingFactor.OneMinusSrcAlpha;
                case BlendMode.DstAlpha:
                    return BlendingFactor.DstAlpha;
                case BlendMode.DstAlphaInverted:
                    return BlendingFactor.OneMinusDstAlpha;
            }
        }

        public static TextureMinFilter ToGLMinFilter(this ModelData.FilterMode filter)
        {
            switch(filter)
            {
                case ModelData.FilterMode.PointSampled:
                    return TextureMinFilter.Nearest;
                case ModelData.FilterMode.Bilinear:
                case ModelData.FilterMode.Trilinear:
                    return TextureMinFilter.Linear;
                default:
                    throw new InvalidCastException($"{filter} has no corresponding OpenGL filter");
            }
        }

        public static TextureMagFilter ToGLMagFilter(this ModelData.FilterMode filter)
        {
            switch(filter)
            {
                case ModelData.FilterMode.PointSampled:
                    return TextureMagFilter.Nearest;
                case ModelData.FilterMode.Bilinear:
                case ModelData.FilterMode.Trilinear:
                    return TextureMagFilter.Linear;
                default:
                    throw new InvalidCastException($"{filter} has no corresponding OpenGL filter");
            }
        }

        public static TextureWrapMode WrapModeU(this BufferMaterial mat)
        {
            if(mat.ClampU)
                return TextureWrapMode.ClampToEdge;
            else
                return mat.MirrorU ? TextureWrapMode.MirroredRepeat : TextureWrapMode.Repeat;
        }

        public static TextureWrapMode WrapModeV(this BufferMaterial mat)
        {
            if(mat.ClampV)
                return TextureWrapMode.ClampToEdge;
            else
                return mat.MirrorV ? TextureWrapMode.MirroredRepeat : TextureWrapMode.Repeat;
        }

        #endregion
    }
}