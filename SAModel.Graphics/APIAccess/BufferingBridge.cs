using SATools.SAArchive;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SATools.SAModel.Graphics.APIAccess
{
    /// <summary>
    /// Vertex ready to be buffered
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct CacheBuffer
    {
        public readonly Vector3 position;
        public readonly Vector3 normal;
        public readonly Color color;
        public readonly Vector2 uv;
        public readonly float weightColor;

        public CacheBuffer(Vector3 position, Vector3 normal, Color color, Vector2 uv, float weightColor)
        {
            this.position = position;
            this.normal = normal;
            this.color = color;
            this.uv = uv;
            this.weightColor = weightColor;
        }
    }

    public abstract class BufferingBridge
    {
        /// <summary>
        /// Weight color array
        /// </summary>
        private static readonly Color[] weightColors;

        private bool initialized;

        public virtual void Initialize()
        {
            initialized = true;
            foreach (var ts in _textureSetUsages)
                BufferTextureSet(ts.Key);
        }

        static BufferingBridge()
        {
            // calculating colors, so that fetching colors is as fast as possible
            weightColors = new Color[64];

            for (int i = 0; i < 64; i++)
            {
                Color c = Color.Black;

                double hue = (((i / 64d) * -.666d + .666d) % 1f) * 6;
                int index = (int)hue;
                byte ff = (byte)((hue - index) * 255);
                byte q = (byte)(0xFF - ff);

                switch (index)
                {
                    case 0:
                        c.R = 0xFF;
                        c.G = ff;
                        break;
                    case 1:
                        c.R = q;
                        c.G = 0xFF;
                        break;
                    case 2:
                        c.G = 0xFF;
                        c.B = ff;
                        break;
                    case 3:
                        c.G = q;
                        c.B = 0xFF;
                        break;
                    case 4:
                        c.B = 0xFF;
                        c.R = ff;
                        break;
                    case 5:
                    default:
                        c.B = q;
                        c.R = 0xFF;
                        break;
                }
                weightColors[i] = c;
            }
        }

        /// <summary>
        /// Returns color by weight value
        /// </summary>
        /// <param name="weight">Weight (0.0 - 1.0)</param>
        /// <returns></returns>
        internal static Color GetWeightColor(float weight)
            => weightColors[(int)(weight * 255)];

        #region Vertex caching

        /// <summary>
        /// A single cached vertex
        /// </summary>
        private struct CachedVertex
        {
            public Vector4 position;
            public Vector3 normal;
            public float weightColor;

            public Vector3 V3Position => new(position.X, position.Y, position.Z);

            public CachedVertex(Vector4 position, Vector3 normal)
            {
                this.position = position;
                this.normal = normal;
                this.weightColor = 0;
            }

            public CachedVertex(BufferVertex vtx)
            {
                position = new(vtx.Position, 1);
                normal = vtx.Normal;
                weightColor = 0;
            }

            public override string ToString()
            {
                return $"({position.X:f3}, {position.Y:f3}, {position.Z:f3}, {position.W:f3}) - ({normal.X:f3}, {normal.Y:f3}, {normal.Z:f3})";
            }
        }

        /// <summary>
        /// Vertex Cache size
        /// </summary>
        private const int VertexCacheSize = 0xFFFF;

        /// <summary>
        /// Vertex cache
        /// </summary>
        private CachedVertex[] Vertices { get; }
            = new CachedVertex[VertexCacheSize];

        /// <summary>
        /// Buffers an attach
        /// </summary>
        /// <param name="attach">Attach to move into the cache</param>
        /// <param name="weightWorldMatrix"></param>
        /// <param name="active"></param>
        public void LoadToCache(BufferMesh[] meshData, Matrix4x4? weightWorldMatrix, bool active)
        {
            if (meshData.Length == 0)
                return;

            Matrix4x4 normalMtx = default;
            if (weightWorldMatrix.HasValue)
            {
                Matrix4x4.Invert(weightWorldMatrix.Value, out normalMtx);
                normalMtx = Matrix4x4.Transpose(normalMtx);
            }

            foreach (BufferMesh mesh in meshData)
            {
                LoadToCache(mesh, weightWorldMatrix, normalMtx, active);
            }
        }

        public void LoadToCache(BufferMesh mesh, Matrix4x4? weightworld = null, Matrix4x4 weightnormal = default, bool active = false)
        {
            if (mesh.Vertices != null)
            {
                if (weightworld == null)
                {
                    foreach (BufferVertex vtx in mesh.Vertices)
                        Vertices[vtx.Index + mesh.VertexWriteOffset] = new(vtx);
                }
                else
                {
                    foreach (BufferVertex vtx in mesh.Vertices)
                    {
                        Vector4 pos = Vector4.Transform(vtx.Position, weightworld.Value) * vtx.Weight;
                        Vector3 nrm = Vector3.TransformNormal(vtx.Normal, weightnormal) * vtx.Weight;

                        int index = vtx.Index + mesh.VertexWriteOffset;

                        if (mesh.ContinueWeight)
                        {
                            Vertices[index].position += pos;
                            Vertices[index].normal += nrm;
                        }
                        else
                        {
                            Vertices[index] = new(pos, nrm);
                        }

                        if (active)
                        {
                            Vertices[index].weightColor = vtx.Weight;
                        }
                    }
                }
            }

            if (mesh.Corners != null)
            {
                CacheBuffer[] toBuffer = new CacheBuffer[mesh.Corners.Length];

                for (int i = 0; i < toBuffer.Length; i++)
                {
                    BufferCorner corner = mesh.Corners[i];
                    int vOffset = corner.VertexIndex + mesh.VertexReadOffset;
                    CachedVertex vtx = Vertices[vOffset];
                    toBuffer[i] = new CacheBuffer(
                        vtx.V3Position,
                        vtx.normal,
                        corner.Color,
                        corner.Texcoord,
                        vtx.weightColor);
                }

                BufferVertexCache(mesh, toBuffer);
            }
        }

        /// <summary>
        /// Buffers a meshes vertex cache
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="cache"></param>
        public abstract void BufferVertexCache(BufferMesh mesh, CacheBuffer[] cache);

        /// <summary>
        /// Debuffers a mesh
        /// </summary>
        /// <param name="mesh"></param>
        public abstract void DebufferVertexCache(BufferMesh mesh);

        /// <summary>
        /// Used to check whether a mesh is already buffered
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public abstract bool IsBuffered(BufferMesh mesh);

        #endregion

        #region Texture Buffering

        private readonly Dictionary<TextureSet, int> _textureSetUsages
            = new();

        internal void InternalBufferTextureSet(TextureSet set)
        {
            if (!_textureSetUsages.TryGetValue(set, out _))
            {
                _textureSetUsages.Add(set, 1);
                if (initialized)
                    BufferTextureSet(set);
            }
            _textureSetUsages[set]++;
        }

        internal void InternalDebufferTextureSet(TextureSet set)
        {
            if (_textureSetUsages[set] == 1)
            {
                _textureSetUsages.Remove(set);
                if (initialized)
                    DebufferTextureSet(set);
                return;
            }
            _textureSetUsages[set]--;
        }

        /// <summary>
        /// Buffers a whole texture set
        /// </summary>
        protected abstract void BufferTextureSet(TextureSet textures);

        /// <summary>
        /// Debuffers a whole texture set
        /// </summary>
        protected abstract void DebufferTextureSet(TextureSet textures);

        #endregion

        #region Mesh and Material buffering

        /// <summary>
        /// Gets called after buffering material data
        /// </summary>
        public abstract void BufferMaterial(Material material);

        #endregion
    }
}
