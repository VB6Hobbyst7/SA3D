using OpenTK;
using OpenTK.Mathematics;

namespace SATools.SAModel.Graphics.OpenGL
{
    public struct GLRenderMesh
    {
        public ModelData.Attach attach;
        public Matrix4? realWorldMtx;
        public Matrix4 worldMtx;
        public Matrix4 normalMtx;
        public Matrix4 MVP;
        public bool active;

        public GLRenderMesh(ModelData.Attach attach, Matrix4? realWorldMtx, Matrix4 worldMtx, Matrix4 normalMtx, Matrix4 mVP, bool active)
        {
            this.attach = attach;
            this.realWorldMtx = realWorldMtx;
            this.worldMtx = worldMtx;
            this.normalMtx = normalMtx;
            MVP = mVP;
            this.active = active;
        }
    }
}
