using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.CHUNK;
using SATools.SAModel.Structs;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK.PolyChunks
{
    internal class IVmPolyChunkTextureID : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkTextureID);

        private PolyChunkTextureID Chunk
            => (PolyChunkTextureID)Source;

        [Tooltip("Chunk Type")]
        public ChunkType Type
        {
            get => Chunk.Type;
            set => _ = Chunk.Type;
        }

        [Tooltip("Whether the chunktype is TextureID2")]
        public bool Second
        {
            get => Chunk.Second;
            set
            {
                Chunk.Second = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [DisplayName("Mipmap distance adjust")]
        [Tooltip("The mipmap distance adjust\nRanges from 0 to 3.75f in 0.25-steps")]
        public float MipmapDAdjust
        {
            get => Chunk.MipmapDAdjust;
            set => Chunk.MipmapDAdjust = value;
        }

        [Tooltip("Clamps the texture v axis between 0 and 1")]
        public bool ClampV
        {
            get => Chunk.ClampV;
            set => Chunk.ClampV = value;
        }

        [Tooltip("Clamps the texture u axis between 0 and 1")]
        public bool ClampU
        {
            get => Chunk.ClampU;
            set => Chunk.ClampU = value;
        }

        [Tooltip("Mirrors the texture every second time the texture is repeated along the v axis")]
        public bool MirrorV
        {
            get => Chunk.MirrorV;
            set => Chunk.MirrorV = value;
        }

        [Tooltip("Mirrors the texture every second time the texture is repeated along the u axis")]
        public bool MirrorU
        {
            get => Chunk.MirrorU;
            set => Chunk.MirrorU = value;
        }

        [DisplayName("Texture ID")]
        [Tooltip("Texture ID to use")]
        public ushort TextureID
        {
            get => Chunk.TextureID;
            set => Chunk.TextureID = value;
        }

        [DisplayName("Super Sample")]
        [Tooltip("Whether to use super sampling (anisotropic filtering)")]
        public bool SuperSample
        {
            get => Chunk.SuperSample;
            set => Chunk.SuperSample = value;
        }

        [DisplayName("Filter Mode")]
        [Tooltip("Texture filtermode")]
        public FilterMode FilterMode
        {
            get => Chunk.FilterMode;
            set => Chunk.FilterMode = value;
        }


        public IVmPolyChunkTextureID() { }

        public IVmPolyChunkTextureID(object source) : base(source) { }
    }

    internal class IVmPolyChunkMaterial : InspectorViewModel
    {
        private Color _diffuseColor;
        private Color _ambientColor;
        private Color _specularColor;

        protected override Type ViewmodelType
            => typeof(PolyChunkMaterial);

        private PolyChunkMaterial Chunk
            => (PolyChunkMaterial)Source;

        [Tooltip("Chunk Type")]
        public ChunkType Type
        {
            get => Chunk.Type;
            set => _ = Chunk.Type;
        }

        public bool Second
        {
            get => Chunk.Second;
            set
            {
                Chunk.Second = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [DisplayName("Source Alpha")]
        public BlendMode SourceAlpha
        {
            get => Chunk.SourceAlpha;
            set => Chunk.SourceAlpha = value;
        }

        [DisplayName("Destination Alpha")]
        public BlendMode DestinationAlpha
        {
            get => Chunk.DestinationAlpha;
            set => Chunk.DestinationAlpha = value;
        }

        [DisplayName("Has Diffuse")]
        public bool HasDiffuse
        {
            get => Chunk.Diffuse.HasValue;
            set
            {
                Chunk.Diffuse = value ? _diffuseColor : null;
                OnPropertyChanged(nameof(Type));
            }
        }

        public Color Diffuse
        {
            get => _diffuseColor;
            set
            {
                _diffuseColor = value;
                if (HasDiffuse)
                    Chunk.Diffuse = _diffuseColor;
            }
        }

        public bool HasAmbient
        {
            get => Chunk.Ambient.HasValue;
            set
            {
                Chunk.Ambient = value ? _ambientColor : null;
                OnPropertyChanged(nameof(Type));
            }
        }

        public Color Ambient
        {
            get => _ambientColor;
            set
            {
                _ambientColor = value;
                if (HasAmbient)
                    Chunk.Ambient = _ambientColor;
            }
        }

        public bool HasSpecularColor
        {
            get => Chunk.Specular.HasValue;
            set
            {
                Chunk.Specular = value ? _specularColor : null;
                OnPropertyChanged(nameof(Type));
            }
        }

        public Color Specular
        {
            get => _specularColor;
            set
            {
                _specularColor = value;
                if (HasSpecularColor)
                    Chunk.Specular = _specularColor;
            }
        }

        [DisplayName("Specular Exponent")]
        public byte SpecularExponent
        {
            get => Chunk.SpecularExponent;
            set => Chunk.SpecularExponent = value;
        }

        public IVmPolyChunkMaterial() { }

        public IVmPolyChunkMaterial(object source) : base(source)
        {
            if (Chunk.Diffuse.HasValue)
                _diffuseColor = Chunk.Diffuse.Value;
            if (Chunk.Ambient.HasValue)
                _ambientColor = Chunk.Ambient.Value;
            if (Chunk.Specular.HasValue)
                _specularColor = Chunk.Specular.Value;
        }
    }

    internal class IVmPolyChunkMaterialBump : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkMaterialBump);

        private PolyChunkMaterialBump Chunk
            => (PolyChunkMaterialBump)Source;

        public ChunkType Type
        {
            get => Chunk.Type;
            set => _ = Chunk.Type;
        }

        public ushort DX
        {
            get => Chunk.DX;
            set => Chunk.DX = value;
        }

        public ushort DY
        {
            get => Chunk.DY;
            set => Chunk.DY = value;
        }

        public ushort DZ
        {
            get => Chunk.DZ;
            set => Chunk.DZ = value;
        }

        public ushort UX
        {
            get => Chunk.UX;
            set => Chunk.UX = value;
        }

        public ushort UY
        {
            get => Chunk.UY;
            set => Chunk.UY = value;
        }

        public ushort UZ
        {
            get => Chunk.UZ;
            set => Chunk.UZ = value;
        }

        public IVmPolyChunkMaterialBump() { }

        public IVmPolyChunkMaterialBump(object source) : base(source) { }
    }
}
