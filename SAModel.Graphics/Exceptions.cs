using System;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Gets thrown when something was not initialized
    /// </summary>
    sealed class NotInitializedException : Exception
    {
        public NotInitializedException(string message) : base(message)
        {
        }
    }

    public class ShaderException : Exception
    {
        private const string integratedGraphicsMessage = "OpenGL Shader compilation faulty on Integrated graphics! Please use your GPU! \n\n";

        public bool IntegratedGraphics { get; }

        public ShaderException(string message, bool integratedGraphics = false) : base(integratedGraphics ? integratedGraphicsMessage + message : message)
        {
            IntegratedGraphics = integratedGraphics;
        }
    }
}
