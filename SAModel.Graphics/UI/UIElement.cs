using SATools.SAModel.Structs;
using System;
using System.Drawing;
using System.Numerics;

namespace SATools.SAModel.Graphics.UI
{
    /// <summary>
    /// Base UI Element class
    /// </summary>
    public abstract class UIElement
    {
        private Vector2 _position;
        private Vector2 _localPivot;
        private Vector2 _globalPivot;
        private Vector2 _scale;
        private float _rotation;
        private Bitmap _bufferTexture;

        /// <summary>
        /// Used internally for re-drawing an element
        /// </summary>
        public readonly Guid ID;

        public bool UpdatedTransforms { get; private set; }

        public bool UpdatedTexture { get; private set; }

        /// <summary>
        /// Position in the Window
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                if(value == _position)
                    return;
                _position = value;
                UpdatedTransforms = true;
            }
        }

        /// <summary>
        /// Pivot Center for rotating
        /// </summary>
        public Vector2 LocalPivot
        {
            get => _localPivot;
            set
            {
                if(value == _localPivot)
                    return;
                _localPivot = value;
                UpdatedTransforms = true;
            }
        }

        /// <summary>
        /// Pivot center on the window (default is bottom left)
        /// </summary>
        public Vector2 GlobalPivot
        {
            get => _globalPivot;
            set
            {
                if(value == _globalPivot)
                    return;
                _globalPivot = value;
                UpdatedTransforms = true;
            }
        }

        /// <summary>
        /// Scale in pixels
        /// </summary>
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                if(value == _scale)
                    return;
                _scale = value;
                UpdatedTransforms = true;
            }
        }

        /// <summary>
        /// Rotation in radians around the pivot
        /// </summary>
        public float Rotation
        {
            get => _rotation;
            set
            {
                if(value == _rotation)
                    return;
                _rotation = value;
                UpdatedTransforms = true;
            }
        }

        /// <summary>
        /// used for updating the texture to render
        /// </summary>
        protected Bitmap BufferTexture
        {
            get => _bufferTexture;
            set
            {
                _bufferTexture = (Bitmap)value.Clone();
                UpdatedTexture = true;
            }
        }

        protected UIElement()
        {
            ID = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the vertex buffer for the ui element and sets <see cref="UpdatedTransforms"/> to false <br/>
        /// size: 4 * 4 (64 bytes) <br/>
        /// structure: ( PosX, PosY, UVX, UVY )
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public float[] GetTransformBuffer(float width, float height)
        {
            float left = (Position.X - Scale.X * LocalPivot.X + width * GlobalPivot.X * 2) / width - 1;
            float bottom = (Position.Y - Scale.Y * LocalPivot.Y + height * GlobalPivot.Y * 2) / height - 1;
            float right = left + Scale.X / width;
            float top = bottom + Scale.Y / height;

            UpdatedTransforms = false;

            // uv locations always stay the same
            return new float[] { left,  bottom, 0, 1,
                                 right, bottom, 1, 1,
                                 left,  top,    0, 0,
                                 right, top,    1, 0, };
        }

        /// <summary>
        /// Gets the texture and sets <see cref="UpdatedTexture"/> to false
        /// </summary>
        /// <returns></returns>
        public Bitmap GetBufferTexture()
        {
            UpdatedTexture = false;
            return _bufferTexture;
        }
    }
}
