using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAArchive;

namespace SATools.SAModel.Graphics.OpenGL
{
    internal struct RenderMatrices
    {
        public Matrix4 worldMtx;
        public Matrix4 normalMtx;
        public Matrix4 MVP;

        public RenderMatrices(Matrix4 worldMtx, Matrix4 normalMtx, Matrix4 mvp)
        {
            this.worldMtx = worldMtx;
            this.normalMtx = normalMtx;
            MVP = mvp;
        }

        public void BufferMatrices()
        {
            GL.UniformMatrix4(10, false, ref worldMtx);
            GL.UniformMatrix4(11, false, ref normalMtx);
            GL.UniformMatrix4(12, false, ref MVP);
        }
    }

    internal struct GLRenderMesh
    {
        public ModelData.Attach attach;
        public TextureSet textureSet;
        public RenderMatrices matrices;

        public GLRenderMesh(ModelData.Attach attach, TextureSet textureSet, Matrix4 worldMtx, Matrix4 normalMtx, Matrix4 mvp)
        {
            this.attach = attach;
            this.textureSet = textureSet;
            matrices = new(worldMtx, normalMtx, mvp);
        }

        public void BufferMatrices() 
            => matrices.BufferMatrices();
    }
}
