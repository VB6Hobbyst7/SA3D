using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System.Collections.Generic;
using System.Numerics;
using LandEntryRenderBatch = System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<SATools.SAModel.ModelData.Buffer.BufferMesh, System.Collections.Generic.List<SATools.SAModel.Graphics.RenderMatrices>>>;
using Matrix4 = System.Numerics.Matrix4x4;

namespace SATools.SAModel.Graphics
{
    public struct RenderMatrices
    {
        public Matrix4 worldMtx;
        public Matrix4 normalMtx;
        public Matrix4 MVP;

        public RenderMatrices(Matrix4 mvp)
        {
            worldMtx = Matrix4.Identity;
            normalMtx = Matrix4.Identity;
            MVP = mvp;
        }

        public RenderMatrices(Matrix4 worldMtx, Matrix4 mvp)
        {
            this.worldMtx = worldMtx;
            normalMtx = worldMtx.GetNormalMatrix();
            MVP = mvp;
        }
    }

    public struct RenderMesh
    {
        public BufferMesh[] meshes;
        public RenderMatrices matrices;

        public RenderMesh(BufferMesh[] meshes, RenderMatrices matrices)
        {
            this.meshes = meshes;
            this.matrices = matrices;
        }
    }

    internal static class RenderHelper
    {
        #region Preparing render meshes

        public static List<(DisplayTask task, List<RenderMesh> opaque, List<RenderMesh> transparent)> PrepareModels(IReadOnlyCollection<GameTask> tasks, NJObject active, Camera cam, BufferingBridge buffer)
        {
            List<(DisplayTask task, List<RenderMesh> opaque, List<RenderMesh> transparent)> result = new();

            foreach (GameTask t in tasks)
            {
                t.Display();

                if (t is DisplayTask dtsk && dtsk.Model != null)
                {
                    List<RenderMesh> opaque = new();
                    List<RenderMesh> transparent = new();
                    dtsk.Model.PrepareModel(opaque, transparent, buffer, cam, active, null, dtsk.Model.HasWeight);
                    result.Add((dtsk, opaque, transparent));
                }

            }

            return result;
        }

        private static void PrepareModel(
            this NJObject obj,
            List<RenderMesh> opaque,
            List<RenderMesh> transparent,
            BufferingBridge buffer,
            Camera cam,
            NJObject activeObj,
            Matrix4? parentWorld,
            bool weighted)
        {
            Matrix4 world = obj.LocalMatrix;
            if (parentWorld.HasValue)
                world *= parentWorld.Value;

            if (obj.Attach != null && obj.Attach.MeshData.Length > 0)
            {
                // if a model is weighted, then the buffered vertex positions/normals will have to be set to world space, which means that world and normal matrix should be identities
                if (weighted)
                    buffer.LoadToCache(obj.Attach.MeshData, world, obj == activeObj);
                else if (!buffer.IsBuffered(obj.Attach.MeshData[0]))
                    buffer.LoadToCache(obj.Attach.MeshData, null, false);

                RenderMatrices matrices = weighted ? new(cam.ViewMatrix * cam.ProjectionMatrix) : new(world, world * cam.ViewMatrix * cam.ProjectionMatrix);

                var meshes = obj.Attach.GetDisplayMeshes();

                if (meshes.opaque.Length > 0)
                    opaque.Add(new RenderMesh(meshes.opaque, matrices));

                if (meshes.transparent.Length > 0)
                    transparent.Add(new RenderMesh(meshes.transparent, matrices));
            }

            for (int i = 0; i < obj.ChildCount; i++)
                obj[i].PrepareModel(opaque, transparent, buffer, cam, activeObj, world, weighted);
        }


        internal static void GetModelLine(NJObject obj, List<Vector3> lines, Matrix4? parentWorld)
        {
            Matrix4 world = obj.LocalMatrix;
            if (parentWorld.HasValue)
            {
                world *= parentWorld.Value;

                lines.Add(Vector3.Transform(Vector3.Zero, parentWorld.Value));
                lines.Add(Vector3.Transform(Vector3.Zero, world));
            }

            for (int i = 0; i < obj.ChildCount; i++)
                GetModelLine(obj[i], lines, world);
        }

        internal static (LandEntryRenderBatch opaque, LandEntryRenderBatch transparent, List<LandEntry> rendered) PrepareLandEntries(LandEntry[] entries, Camera camera, BufferingBridge bufferBridge)
        {
            // the output list. This contains all landentries that need to be rendered
            List<LandEntry> rendered = new();

            // the landentries to render are grouped by attach
            Dictionary<Attach, List<LandEntry>> toRender = new();

            for (int i = 0; i < entries.Length; i++)
            {
                LandEntry le = entries[i];
                // check if the entry can be rendered at all
                if (!camera.CanRender(le.ModelBounds))
                    continue;

                if (toRender.TryGetValue(le.Attach, out List<LandEntry> list))
                    list.Add(le);
                else
                {
                    // check if the attach is already buffered
                    if (!bufferBridge.IsBuffered(le.Attach.MeshData[0]))
                        bufferBridge.LoadToCache(le.Attach.MeshData, null, false);
                    toRender.Add(le.Attach, new() { le });
                }
                rendered.Add(le);
            }

            // Landentry Renderbatch structure:
            // <TextureIndex <Buffermesh, List<Matrices>>
            // So, each buffermesh has multiple render matrices to be rendered multiple times
            // and each of those lists belongs to a texture. That way textures dont need
            // to be swapped out so often

            LandEntryRenderBatch opaque = new();
            LandEntryRenderBatch transparent = new();

            foreach (var t in toRender)
            {
                List<RenderMatrices> matrices = new();
                foreach (LandEntry le in t.Value)
                {
                    Matrix4 world = le.WorldMatrix;
                    RenderMatrices rm = new(world, world * camera.ViewMatrix * camera.ProjectionMatrix);
                    matrices.Add(rm);
                }

                // check if attach is buffered


                foreach (BufferMesh bm in t.Key.MeshData)
                {
                    if (bm.Material == null)
                        continue;

                    int index = bm.Material.HasAttribute(MaterialAttributes.useTexture) ? (int)bm.Material.TextureIndex : -1;

                    Dictionary<BufferMesh, List<RenderMatrices>> buffers;
                    if (bm.Material.UseAlpha)
                    {
                        if (!transparent.TryGetValue(index, out buffers))
                        {
                            buffers = new();
                            transparent.Add(index, buffers);
                        }
                    }
                    else
                    {
                        if (!opaque.TryGetValue(index, out buffers))
                        {
                            buffers = new();
                            opaque.Add(index, buffers);
                        }
                    }
                    buffers.Add(bm, matrices);


                }
            }

            return (opaque, transparent, rendered);
        }


        #endregion

        #region Rendering

        internal static void RenderLandentries(this LandEntryRenderBatch geometry, Material material, RenderingBridge renderingBridge)
        {
            foreach (var g in geometry)
            {
                foreach (var t in g.Value)
                {
                    BufferMesh m = t.Key;
                    material.BufferMaterial = m.Material;
                    material.BufferMaterial = m.Material;
                    renderingBridge.RenderMesh(m, t.Value);
                }
            }
        }

        #endregion
    }
}
