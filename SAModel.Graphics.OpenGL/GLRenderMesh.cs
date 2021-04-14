using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SATools.SAModel.Graphics.OpenGL
{
    public struct GLRenderMesh
    {
        public ModelData.Attach attach;
        public Matrix4 worldMtx;
        public Matrix4 normalMtx;
        public Matrix4 MVP;

        public GLRenderMesh(ModelData.Attach attach, Matrix4 worldMtx, Matrix4 normalMtx, Matrix4 mVP)
        {
            this.attach = attach;
            this.worldMtx = worldMtx;
            this.normalMtx = normalMtx;
            MVP = mVP;
        }

        public void BufferMatrices()
        {
            GL.UniformMatrix4(10, false, ref worldMtx);
            GL.UniformMatrix4(11, false, ref normalMtx);
            GL.UniformMatrix4(12, false, ref MVP);
        }
    }
}
