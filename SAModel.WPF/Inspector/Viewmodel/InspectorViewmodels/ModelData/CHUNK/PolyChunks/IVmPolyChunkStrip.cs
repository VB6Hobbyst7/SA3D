using SATools.SAModel.ModelData.CHUNK;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK.PolyChunks
{
    internal class IVmPolyChunkStrip : InspectorViewModel
    {
        protected override Type ViewmodelType
               => typeof(PolyChunkStrip);

        private PolyChunkStrip Chunk
            => (PolyChunkStrip)Source;

        [Tooltip("Chunk Type")]
        public ChunkType Type
        {
            get => Chunk.Type;
            set => _ = Chunk.Type;
        }

        [DisplayName("Has UV")]
        [Tooltip("Whether the polygons contain uv data")]
        public bool HasUV
        {
            get => Chunk.HasUV;
            set
            {
                Chunk.HasUV = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [DisplayName("Has HD UV")]
        [Tooltip("Whether uvs repeat at 256 (normal) or 1024 (HD)")]
        public bool UVHD
        {
            get => Chunk.UVHD;
            set
            {
                Chunk.UVHD = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [DisplayName("Has Normal")]
        [Tooltip("Whether the polygons use normals")]
        public bool HasNormal
        {
            get => Chunk.HasNormal;
            set
            {
                Chunk.HasNormal = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [DisplayName("Has color")]
        [Tooltip("Whether the polygons use vertex colors")]
        public bool HasColor
        {
            get => Chunk.HasColor;
            set
            {
                Chunk.HasColor = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [DisplayName("Ignore light")]
        [Tooltip("Ignores diffuse lighting")]
        public bool IgnoreLight
        {
            get => Chunk.IgnoreLight;
            set => Chunk.IgnoreLight = value;
        }

        [DisplayName("Ignore specular")]
        [Tooltip("Ignores specular lighting")]
        public bool IgnoreSpecular
        {
            get => Chunk.IgnoreSpecular;
            set => Chunk.IgnoreSpecular = value;
        }


        [DisplayName("Ignore ambient")]
        [Tooltip("Ignores ambient lighting")]
        public bool IgnoreAmbient
        {
            get => Chunk.IgnoreAmbient;
            set => Chunk.IgnoreAmbient = value;
        }


        [DisplayName("Use alpha")]
        [Tooltip("uses alpha")]
        public bool UseAlpha
        {
            get => Chunk.UseAlpha;
            set => Chunk.UseAlpha = value;
        }


        [DisplayName("Double side")]
        [Tooltip("Ignores culling")]
        public bool DoubleSide
        {
            get => Chunk.DoubleSide;
            set => Chunk.DoubleSide = value;
        }

        [DisplayName("Flat shading")]
        [Tooltip("Uses no lighting at all (Vertex color lit?)")]
        public bool FlatShading
        {
            get => Chunk.FlatShading;
            set => Chunk.FlatShading = value;
        }

        [DisplayName("Environment mapping")]
        [Tooltip("Environment (matcap/normal) mapping")]
        public bool EnvironmentMapping
        {
            get => Chunk.EnvironmentMapping;
            set => Chunk.EnvironmentMapping = value;
        }


        [Tooltip("Unknown what it actually does, but it definitely exists (e.g. sonics light dash models use it)")]
        public bool Unknown7
        {
            get => Chunk.Unknown7;
            set => Chunk.Unknown7 = value;
        }

        [Tooltip("Polygon data of the chunk")]
        public PolyChunkStrip.Strip[] Strips
            => Chunk.Strips;

        [DisplayName("User Attributes")]
        [Tooltip("User flag count (ranges from 0 to 3)")]
        public byte UserAttributes
            => Chunk.UserAttributes;

        public IVmPolyChunkStrip() { }

        public IVmPolyChunkStrip(object source) : base(source) { }
    }
}
