﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace SATools.SAModel.Graphics.OpenGL
{
    /// <summary>
    /// A GLSL Shader
    /// </summary>
    internal class Shader
    {
        /// <summary>
        /// Used to store the different uniforms in the shader
        /// </summary>
        private struct UniformType
        {
            /// <summary>
            /// The uniform location in the shader
            /// </summary>
            public readonly int location;
            /// <summary>
            /// The uniform type
            /// </summary>
            public readonly ActiveUniformType type;

            /// <summary>
            /// The texture unit (if it is a texture)
            /// </summary>
            public readonly TextureUnit? unit;

            public UniformType(int location, ActiveUniformType type)
            {
                this.location = location;
                this.type = type;
                unit = null;
            }

            public UniformType(int location, TextureUnit unit)
            {
                this.location = location;
                type = ActiveUniformType.Sampler2D;
                this.unit = unit;
            }
        }

        /// <summary>
        /// Used to trim byte order myke
        /// </summary>
        private readonly char[] bomTrimmer = new char[] { '\uFEFF', '\u200B' };

        /// <summary>
        /// The shader program handle
        /// </summary>
        private readonly int _handle;

        /// <summary>
        /// The shaders
        /// </summary>
        private readonly Dictionary<string, UniformType> _uniformLocations;

        /// <summary>
        /// Creates a new Shader from a vertex and fragment shader
        /// </summary>
        /// <param name="vertexShaderSource">The vertex shader source</param>
        /// <param name="fragmentShaderSource">The fragment shader source</param>
        public Shader(string vertexShaderSource, string fragmentShaderSource)
        {
            vertexShaderSource = CorrectString(vertexShaderSource);
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);

            fragmentShaderSource = CorrectString(fragmentShaderSource);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);

            GL.CompileShader(vertexShader);

            string infoLogVert = GL.GetShaderInfoLog(vertexShader);
            if (!string.IsNullOrWhiteSpace(infoLogVert))
                throw new ShaderException("vertex shader couldnt compile: \n" + infoLogVert, infoLogVert.Contains("ERROR___HEXADECIMAL_CONST_OVERFLOW"));

            GL.CompileShader(fragmentShader);

            string infoLogFrag = GL.GetShaderInfoLog(fragmentShader);
            if (!string.IsNullOrWhiteSpace(infoLogFrag))
                throw new ShaderException("fragment shader couldnt compile: \n" + infoLogFrag, infoLogFrag.Contains("ERROR___HEXADECIMAL_CONST_OVERFLOW"));

            //linking the shaders

            _handle = GL.CreateProgram();

            GL.AttachShader(_handle, vertexShader);
            GL.AttachShader(_handle, fragmentShader);

            GL.LinkProgram(_handle);

            //cleanup

            GL.DetachShader(_handle, vertexShader);
            GL.DetachShader(_handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // getting the uniforms

            // First, we have to get the number of active uniforms in the shader.
            GL.GetProgram(_handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            // Next, allocate the dictionary to hold the locations.
            _uniformLocations = new Dictionary<string, UniformType>();
            int samplerInt = 0;

            // Loop over all the uniforms,
            for (var i = 0; i < numberOfUniforms; i++)
            {
                // get the name of this uniform,
                var key = GL.GetActiveUniform(_handle, i, out _, out ActiveUniformType t);

                // get the location,
                var location = GL.GetUniformLocation(_handle, key);
                if (location < 0)
                    continue;

                // and then add it to the dictionary.
                if (t == ActiveUniformType.Sampler2D)
                {
                    _uniformLocations.Add(key, new UniformType(location, TextureUnit.Texture0 + samplerInt));
                    SetUniform(key, samplerInt);
                    samplerInt++;
                }
                else
                {
                    _uniformLocations.Add(key, new UniformType(location, t));
                }
            }
        }

        private string CorrectString(string input)
            => input.Trim(bomTrimmer) + "\n\0";


        /// <summary>
        /// Binds a uniform buffer to a uniform block of a shader
        /// </summary>
        /// <param name="blockname">The name of the block</param>
        /// <param name="blockid">The block ID</param>
        /// <param name="ubo">The uniform buffer object handle</param>
        public void BindUniformBlock(string blockname, int blockid, int ubo)
        {
            int index = GL.GetUniformBlockIndex(_handle, blockname);
            if (index < 0)
                throw new ArgumentException("Block name does not exist: " + blockname);

            GL.UniformBlockBinding(_handle, index, blockid);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, blockid, ubo);
        }

        // Uniform setters
        // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
        // You use VBOs for vertex-related data, and uniforms for almost everything else.

        // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
        //     1. Bind the program you want to set the uniform on
        //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
        //     3. Use the appropriate GL.Uniform* function to set the uniform.

        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, int data)
        {
            Use();
            GL.Uniform1(_uniformLocations[name].location, data);
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, float data)
        {
            Use();
            GL.Uniform1(_uniformLocations[name].location, data);
        }

        /// <summary>
        /// Set a uniform double on this shader
        /// </summary>
        /// <param name="name">Name of the uniform</param>
        /// <param name="data">the data</param>
        public void SetUniform(string name, double data)
        {
            Use();
            GL.Uniform1(_uniformLocations[name].location, data);
        }

        /// <summary>
        /// Set a uniform boolean on this shader
        /// </summary>
        /// <param name="name">Name of the uniform</param>
        /// <param name="data">the data</param>
        public void SetUniform(string name, bool data)
        {
            Use();
            GL.Uniform1(_uniformLocations[name].location, data ? 1 : 0);
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, Matrix4 data)
        {
            Use();
            GL.UniformMatrix4(_uniformLocations[name].location, false, ref data);
        }

        /// <summary>
        /// Set a uniform Vector2 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, Vector2 data)
        {
            Use();
            GL.Uniform2(_uniformLocations[name].location, new OpenTK.Mathematics.Vector2(data.X, data.Y));
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, Vector3 data)
        {
            if (!_uniformLocations.ContainsKey(name))
                return;
            Use();
            GL.Uniform3(_uniformLocations[name].location, new OpenTK.Mathematics.Vector3(data.X, data.Y, data.Z));
        }

        /// <summary>
        /// Set a uniform Vector4 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, Vector4 data)
        {
            Use();
            GL.Uniform4(_uniformLocations[name].location, data);
        }

        /// <summary>
        /// Set a uniform color on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetUniform(string name, Color data)
        {
            Use();
            GL.Uniform4(_uniformLocations[name].location, data.SystemColor);
        }

        public void Use() => GL.UseProgram(_handle);

        public static void UnUse() => GL.UseProgram(0);
    }
}
