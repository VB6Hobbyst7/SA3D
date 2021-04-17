using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAArchive;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System;
using System.Collections.Generic;
using System.IO;
using static SATools.SAModel.Graphics.OpenGL.GlobalBuffers;
using Color = SATools.SAModel.Structs.Color;
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.Graphics.OpenGL.BufferMeshHandle, System.Collections.Generic.List<SATools.SAModel.Graphics.OpenGL.RenderMatrices>>>;

namespace SATools.SAModel.Graphics.OpenGL
{
    public static class RenderExtensions
    {
        internal static void Render(this Attach atc, bool transparent, Material material)
        {
            if(atc.MeshData == null)
                throw new InvalidOperationException($"Attach {atc.Name} has no buffer meshes");

            foreach(BufferMesh m in atc.MeshData)
            {
                if(m.Material == null || m.Material.UseAlpha != transparent)
                    continue;

                if(!MeshHandles.TryGetValue(m, out var handle))
                {
                    atc.Buffer(null, false);
                    handle = MeshHandles[m];
                }

                material.BufferMaterial = m.Material;
                GL.BindVertexArray(handle.vao);
                if(handle.eao == 0)
                    GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                else
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
                    if(obj.Attach.BufferHasOpaque || obj.Attach.BufferHasTransparent)
                        renderMeshes.Add(new GLRenderMesh(obj.Attach, textureSet, Matrix4.Identity, Matrix4.Identity, viewMatrix * projectionMatrix));
                    obj.Attach.Buffer(world, obj == activeObj);
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

        internal static void RenderModels(List<GLRenderMesh> renderMeshes, bool transparent, Material material)
        {
            for(int i = 0; i < renderMeshes.Count; i++)
            {
                GLRenderMesh m = renderMeshes[i];
                if(transparent && !m.attach.BufferHasTransparent
                    || !transparent && !m.attach.BufferHasOpaque)
                {
                    continue;
                }

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

                    var handle = MeshHandles[bm];
                    GL.BindVertexArray(handle.vao);
                    if(handle.eao == 0)
                        GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                    else
                        GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
                }
            }
        }

        #region rendering geometry

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

                    if(!MeshHandles.TryGetValue(bm, out var handle))
                    {
                        t.Key.Buffer(null, false);
                        handle = MeshHandles[bm];
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
                        if(m.eao == 0)
                            GL.DrawArrays(PrimitiveType.Triangles, 0, m.vertexCount);
                        else
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
                        if(m.eao == 0)
                            GL.DrawArrays(PrimitiveType.Triangles, 0, m.vertexCount);
                        else
                            GL.DrawElements(BeginMode.Triangles, m.vertexCount, DrawElementsType.UnsignedInt, 0);
                    }
                }
            }
        }

        internal static void RenderBounds(List<LandEntry> entries, Attach sphere, Matrix4 cameraViewMatrix, Matrix4 cameraProjectionmatrix)
        {
            BoundsShader.Use();
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);

            Matrix4 normal = Matrix4.Identity;
            GL.UniformMatrix4(11, false, ref normal);

            var handle = MeshHandles[sphere.MeshData[0]];
            GL.BindVertexArray(handle.vao);

            foreach(LandEntry le in entries)
            {
                var b = le.ModelBounds;

                Matrix4 world = Matrix4.CreateScale(b.Radius) * Matrix4.CreateTranslation(b.Position.ToGL());
                GL.UniformMatrix4(10, false, ref world);

                world = world * cameraViewMatrix * cameraProjectionmatrix;
                GL.UniformMatrix4(12, false, ref world);

                if(handle.eao == 0)
                    GL.DrawArrays(PrimitiveType.Triangles, 0, handle.vertexCount);
                else
                    GL.DrawElements(BeginMode.Triangles, handle.vertexCount, DrawElementsType.UnsignedInt, 0);
            }

        }

        unsafe internal static void DrawModelRelationship(List<NJObject> objs, Matrix4 cameraViewMatrix, Matrix4 cameraProjectionmatrix)
        {
            List<Vector3> lines = new();

            foreach(var obj in objs)
                GetModelLine(obj, lines, null);

            Matrix4 identity = Matrix4.Identity;
            Matrix4 mvp = cameraViewMatrix * cameraProjectionmatrix;

            WireFrameShader.Use();
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            GL.UniformMatrix4(10, false, ref identity);
            GL.UniformMatrix4(11, false, ref identity);
            GL.UniformMatrix4(12, false, ref mvp);

            // buffer the line data
            GL.BindBuffer(BufferTarget.ArrayBuffer, LineBufferHandle.vbo);
            Vector3[] data = lines.ToArray();
            fixed(Vector3* ptr = data)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(Vector3), (IntPtr)ptr, BufferUsageHint.StreamDraw);
            }

            GL.BindVertexArray(LineBufferHandle.vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, data.Length);

        }

        private static void GetModelLine(NJObject obj, List<Vector3> lines, Matrix4? parentWorld)
        {
            Matrix4 world = obj.LocalMatrix();
            if(parentWorld.HasValue)
            {
                world *= parentWorld.Value;

                lines.Add(new(Vector4.UnitW * parentWorld.Value));
                lines.Add(new(Vector4.UnitW * world));
            }

            for(int i = 0; i < obj.ChildCount; i++)
                GetModelLine(obj[i], lines, world);
        }

        #endregion
    }
}