using System.Drawing;

namespace SATools.SAModel.Graphics.OpenGL
{
    public static class OpenGLBridge
    {
        public static Context CreateGLContext(Rectangle rectangle)
        {
            GLBufferingBridge buffer = new();
            GLRenderingBridge render = new(buffer);
            return new Context(rectangle, render, buffer);
        }

        public static DebugContext CreateGLDebugContext(Rectangle rectangle)
        {
            GLBufferingBridge buffer = new();
            GLRenderingBridge render = new(buffer);
            return new DebugContext(rectangle, render, buffer);
        }
    }
}
