using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAArchive;
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
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.Graphics.OpenGL.BufferMeshHandle, System.Collections.Generic.List<SATools.SAModel.Graphics.OpenGL.RenderMatrices>>>;

namespace SATools.SAModel.Graphics.OpenGL
{
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

    internal struct CachedVertex
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
        private static readonly Dictionary<BufferMesh, BufferMeshHandle> meshHandles = new();

        //private static Random rand;

        public static void ClearWeights() => Array.Clear(weights, 0, weights.Length);

        public static Matrix4 GenMatrix(SAVector3 position, SAVector3 rotation, SAVector3 scale, bool rotateZYX)
        {
            Matrix4 rotMtx;
            var matX = Matrix4.CreateRotationX(DegToRad(rotation.X));
            var matY = Matrix4.CreateRotationY(DegToRad(rotation.Y));
            var matZ = Matrix4.CreateRotationZ(DegToRad(rotation.Z));

            if(rotateZYX)
                rotMtx = matZ * matY * matX;
            else
                rotMtx = matX * matY * matZ;

            return Matrix4.CreateScale(scale.ToGL()) * rotMtx * Matrix4.CreateTranslation(position.ToGL());
        }

        public static Matrix4 LocalMatrix(this NJObject obj) => GenMatrix(obj.Position, obj.Rotation, obj.Scale, obj.RotateZYX);

        public static Matrix4 LocalMatrix(this LandEntry obj) => GenMatrix(obj.Position, obj.Rotation, obj.Scale, obj.RotateZYX);

        public static unsafe void Buffer(this Attach atc, Matrix4? worldMtx, bool active)
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
                    using(MemoryStream stream = new(mesh.Corners.Length * structSize))
                    {
                        BinaryWriter writer = new(stream);

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

                        meshHandles.Add(mesh, new BufferMeshHandle(vao, vbo, eao, vtxCount, mesh.Material));
                    }
                }
            }
        }

        public static void DeBuffer(this Attach atc)
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

        public static void Render(this Attach atc, bool transparent, Material material)
        {
            if(atc.MeshData == null)
                throw new InvalidOperationException($"Attach {atc.Name} has no buffer meshes");

            foreach(BufferMesh m in atc.MeshData)
            {
                if(m.Material == null || m.Material.UseAlpha != transparent)
                    continue;

                if(!meshHandles.TryGetValue(m, out var handle))
                {
                    atc.Buffer(null, false);
                    handle = meshHandles[m];
                }

                material.BufferMaterial = m.Material;
                GL.BindVertexArray(handle.vao);
                GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
            }
        }

        internal static void Prepare(this NJObject obj, List<GLRenderMesh> renderMeshes, TextureSet textureSet, Matrix4 viewMatrix, Matrix4 projectionMatrix, NJObject activeObj, Matrix4? parentWorld, bool weighted)
        {
            Matrix4 world = obj.LocalMatrix();
            if(parentWorld.HasValue)
                world *= parentWorld.Value;

            if(obj.Attach != null && obj.Attach.MeshData.Length > 0)
            {
                // if a model is weighted, then the buffered vertex positions/normals will have to be set to world space, which means that world and normal matrix should be identities
                if(weighted)
                {
                    obj.Attach.Buffer(world, obj == activeObj);
                    if(obj.Attach.BufferHasOpaque || obj.Attach.BufferHasTransparent)
                        renderMeshes.Add(new GLRenderMesh(obj.Attach, textureSet, Matrix4.Identity, Matrix4.Identity, viewMatrix * projectionMatrix));
                }
                else
                {
                    Matrix4 normalMtx = world.Inverted();
                    normalMtx.Transpose();
                    renderMeshes.Add(new GLRenderMesh(obj.Attach, textureSet, world, normalMtx, world * viewMatrix * projectionMatrix));
                }
            }

            for(int i = 0; i < obj.ChildCount; i++)
                obj[i].Prepare(renderMeshes, textureSet, viewMatrix, projectionMatrix, activeObj, world, weighted);
        }

        internal static void Prepare(this LandEntry le, List<GLRenderMesh> renderMeshes, TextureSet textureSet, List<LandEntry> entries, Camera camera, Matrix4 viewMatrix, Matrix4 projectionMatrix, LandEntry activeLE)
        {
            if(!camera.CanRender(le.ModelBounds) || entries.Contains(le))
                return;
            entries.Add(le);
            Matrix4 world = le.LocalMatrix();
            Matrix4 normalMtx = world.Inverted();
            normalMtx.Transpose();
            renderMeshes.Add(new GLRenderMesh(le.Attach, textureSet, world, normalMtx, world * viewMatrix * projectionMatrix));
        }

        internal static void RenderModels(List<GLRenderMesh> renderMeshes, bool transparent, Material material)
        {
            for(int i = 0; i < renderMeshes.Count; i++)
            {
                GLRenderMesh m = renderMeshes[i];
                if(transparent && !m.attach.BufferHasTransparent
                    || !transparent && !m.attach.BufferHasOpaque)
                    continue;
                material.BufferTextureSet = m.textureSet;
                m.BufferMatrices();
                m.attach.Render(transparent, material);
            }
        }

        internal static void RenderModelsWireframe(List<GLRenderMesh> renderMeshes)
        {
            for(int i = 0; i < renderMeshes.Count; i++)
            {
                GLRenderMesh m = renderMeshes[i];

                if(!m.attach.BufferHasOpaque && !m.attach.BufferHasTransparent)
                    continue;

                m.BufferMatrices();

                foreach(BufferMesh bm in m.attach.MeshData)
                {
                    if(bm.Material == null)
                        continue;

                    var handle = meshHandles[bm];
                    GL.BindVertexArray(handle.vao);
                    GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
                }
            }
        }

        internal static (LandEntryRenderBatch opaque, LandEntryRenderBatch transparent, List<LandEntry> rendered) PrepareLandEntries(LandEntry[] entries, Camera camera, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            List<LandEntry> rendered = new();
            Dictionary<Attach, List<LandEntry>> toRender = new();

            foreach(LandEntry le in entries)
            {
                if(!camera.CanRender(le.ModelBounds))
                    continue;
                if(toRender.TryGetValue(le.Attach, out List<LandEntry> list))
                {
                    list.Add(le);
                }
                else
                {
                    toRender.Add(le.Attach, new() { le });
                }
                rendered.Add(le);
            }

            LandEntryRenderBatch opaque = new();
            LandEntryRenderBatch transparent = new();

            foreach(var t in toRender)
            {
                List<RenderMatrices> matrices = new();
                foreach(LandEntry le in t.Value)
                {
                    Matrix4 world = le.LocalMatrix();
                    Matrix4 normalMtx = world.Inverted();
                    normalMtx.Transpose();

                    RenderMatrices rm = new(world, normalMtx, world * viewMatrix * projectionMatrix);
                    matrices.Add(rm);
                }

                foreach(BufferMesh bm in t.Key.MeshData)
                {
                    if(bm.Material == null)
                        continue;

                    if(!meshHandles.TryGetValue(bm, out var handle))
                    {
                        t.Key.Buffer(null, false);
                        handle = meshHandles[bm];
                    }

                    int index = bm.Material.HasFlag(MaterialFlags.useTexture) ? (int)bm.Material.TextureIndex : -1;

                    Dictionary<BufferMeshHandle, List<RenderMatrices>> buffers;
                    if(bm.Material.UseAlpha)
                    {
                        if(!transparent.TryGetValue(index, out buffers))
                        {
                            buffers = new();
                            transparent.Add(index, buffers);
                        }
                    }
                    else
                    {
                        if(!opaque.TryGetValue(index, out buffers))
                        {
                            buffers = new();
                            opaque.Add(index, buffers);
                        }
                    }
                    buffers.Add(handle, matrices);                    


                }
            }

            return (opaque, transparent, rendered);
        }

        internal static void RenderLandentries(this LandEntryRenderBatch geometry, Material material)
        {
            foreach(var g in geometry)
            {
                foreach(var t in g.Value)
                {
                    BufferMeshHandle m = t.Key;
                    material.BufferMaterial = m.material;
                    GL.BindVertexArray(m.vao);

                    foreach(var mtx in t.Value)
                    {
                        mtx.BufferMatrices();
                        GL.DrawElements(BeginMode.Triangles, m.vertexCount, DrawElementsType.UnsignedInt, 0);
                    }
                }
            }
        }

        internal static void RenderLandentriesWireframe(this LandEntryRenderBatch geometry)
        {
            foreach(var g in geometry)
            {
                foreach(var t in g.Value)
                {
                    BufferMeshHandle m = t.Key;
                    GL.BindVertexArray(m.vao);

                    foreach(var mtx in t.Value)
                    {
                        mtx.BufferMatrices();
                        GL.DrawElements(BeginMode.Triangles, m.vertexCount, DrawElementsType.UnsignedInt, 0);
                    }
                }
            }
        }

        internal static void RenderBounds(List<LandEntry> entries, Attach sphere, Matrix4 cameraViewMatrix, Matrix4 cameraProjectionmatrix)
        {
            GL.Disable(EnableCap.DepthTest);
            Matrix4 normal = Matrix4.Identity;
            GL.UniformMatrix4(11, false, ref normal);

            var handle = meshHandles[sphere.MeshData[0]];
            GL.BindVertexArray(handle.vao);

            foreach(LandEntry le in entries)
            {
                var b = le.ModelBounds;

                Matrix4 world = Matrix4.CreateScale(b.Radius) * Matrix4.CreateTranslation(b.Position.ToGL());
                GL.UniformMatrix4(10, false, ref world);

                world = world * cameraViewMatrix * cameraProjectionmatrix;
                GL.UniformMatrix4(12, false, ref world);

                GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
            }

            GL.Enable(EnableCap.DepthTest);
        }

        #region Conversion extensions

        private static CachedVertex ToCache(this BufferVertex vtx) => new(vtx.position.ToGL4(), vtx.normal.ToGL());

        public static Vector3 ToGL(this SAVector3 vec3) => new(vec3.X, vec3.Y, vec3.Z);

        public static Vector4 ToGL4(this SAVector3 vec3) => new(vec3.X, vec3.Y, vec3.Z, 1);

        public static SAVector3 ToSA(this Vector3 vec3) => new(vec3.X, vec3.Y, vec3.Z);

        public static Vector2 ToGL(this SAVector2 vec2) => new(vec2.X, vec2.Y);

        public static SAVector2 ToSA(this Vector2 vec2) => new(vec2.X, vec2.Y);


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

        public static TextureMinFilter ToGLMinFilter(this FilterMode filter)
        {
            return filter switch
            {
                FilterMode.PointSampled => TextureMinFilter.NearestMipmapNearest,
                FilterMode.Bilinear => TextureMinFilter.NearestMipmapLinear,
                FilterMode.Trilinear => TextureMinFilter.LinearMipmapLinear,
                _ => throw new InvalidCastException($"{filter} has no corresponding OpenGL filter"),
            };
        }

        public static TextureMagFilter ToGLMagFilter(this FilterMode filter)
        {
            return filter switch
            {
                FilterMode.PointSampled => TextureMagFilter.Nearest,
                FilterMode.Bilinear or FilterMode.Trilinear => TextureMagFilter.Linear,
                _ => throw new InvalidCastException($"{filter} has no corresponding OpenGL filter"),
            };
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