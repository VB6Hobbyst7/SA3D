using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.BASIC;
using SATools.SAModel.Structs;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.BASIC
{
    internal class IVmMaterial : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(Material);

        private Material Material
        {
            get => (Material)Source;
            set => Source = value;
        }

        [DisplayName("Diffuce Color")]
        public Color DiffuseColor
        {
            get => Material.DiffuseColor;
            set
            {
                var m = Material;
                m.DiffuseColor = value;
                Material = m;
            }
        }

        [DisplayName("Specular Color")]
        public Color SpecularColor
        {
            get => Material.SpecularColor;
            set
            {
                var m = Material;
                m.SpecularColor = value;
                Material = m;
            }
        }

        [DisplayName("Specular Exponent")]
        public float Exponent
        {
            get => Material.Exponent;
            set
            {
                var m = Material;
                m.Exponent = value;
                Material = m;
            }
        }

        [DisplayName("Texture ID")]
        public uint TextureID
        {
            get => Material.TextureID;
            set
            {
                var m = Material;
                m.TextureID = value;
                Material = m;
            }
        }

        [DisplayName("User Attributes")]
        [Hexadecimal]
        public byte UserAttributes
        {
            get => Material.UserAttributes;
            set
            {
                var m = Material;
                m.UserAttributes = value;
                Material = m;
            }
        }

        [DisplayName("Pick Status")]
        public bool PickStatus
        {
            get => Material.PickStatus;
            set
            {
                var m = Material;
                m.PickStatus = value;
                Material = m;
            }
        }

        [DisplayName("Mipmap Distance Adjust")]
        public float MipmapDAdjust
        {
            get => Material.MipmapDAdjust;
            set
            {
                var m = Material;
                m.MipmapDAdjust = value;
                Material = m;
            }
        }

        [DisplayName("Super Sampling")]
        public bool SuperSample
        {
            get => Material.SuperSample;
            set
            {
                var m = Material;
                m.SuperSample = value;
                Material = m;
            }
        }

        [DisplayName("Filter mode")]
        public FilterMode FilterMode
        {
            get => Material.FilterMode;
            set
            {
                var m = Material;
                m.FilterMode = value;
                Material = m;
            }
        }

        [DisplayName("Clamp V")]
        public bool ClampV
        {
            get => Material.ClampV;
            set
            {
                var m = Material;
                m.ClampV = value;
                Material = m;
            }
        }

        [DisplayName("Clamp U")]
        public bool ClampU
        {
            get => Material.ClampU;
            set
            {
                var m = Material;
                m.ClampU = value;
                Material = m;
            }
        }

        [DisplayName("Mirror V")]
        public bool MirrorV
        {
            get => Material.MirrorV;
            set
            {
                var m = Material;
                m.MirrorV = value;
                Material = m;
            }
        }

        [DisplayName("Mirror U")]
        public bool MirrorU
        {
            get => Material.MirrorU;
            set
            {
                var m = Material;
                m.MirrorU = value;
                Material = m;
            }
        }

        [DisplayName("Ignore Specular")]
        public bool IgnoreSpecular
        {
            get => Material.IgnoreSpecular;
            set
            {
                var m = Material;
                m.IgnoreSpecular = value;
                Material = m;
            }
        }

        [DisplayName("Use Alpha")]
        public bool UseAlpha
        {
            get => Material.UseAlpha;
            set
            {
                var m = Material;
                m.UseAlpha = value;
                Material = m;
            }
        }

        [DisplayName("Use Texture")]
        public bool UseTexture
        {
            get => Material.UseTexture;
            set
            {
                var m = Material;
                m.UseTexture = value;
                Material = m;
            }
        }

        [DisplayName("Environment Map")]
        public bool EnvironmentMap
        {
            get => Material.EnvironmentMap;
            set
            {
                var m = Material;
                m.EnvironmentMap = value;
                Material = m;
            }
        }

        [DisplayName("Double Sided")]
        public bool DoubleSided
        {
            get => Material.DoubleSided;
            set
            {
                var m = Material;
                m.DoubleSided = value;
                Material = m;
            }
        }

        [DisplayName("Flat Shading")]
        public bool FlatShading
        {
            get => Material.FlatShading;
            set
            {
                var m = Material;
                m.FlatShading = value;
                Material = m;
            }
        }

        [DisplayName("Ignore Lighting")]
        public bool IgnoreLighting
        {
            get => Material.IgnoreLighting;
            set
            {
                var m = Material;
                m.IgnoreLighting = value;
                Material = m;
            }
        }

        [DisplayName("Destination Alpha")]
        public BlendMode DestinationAlpha
        {
            get => Material.DestinationAlpha;
            set
            {
                var m = Material;
                m.DestinationAlpha = value;
                Material = m;
            }
        }

        [DisplayName("Source Alpha")]
        public BlendMode SourceAlpha
        {
            get => Material.SourceAlpha;
            set
            {
                var m = Material;
                m.SourceAlpha = value;
                Material = m;
            }
        }


        public IVmMaterial() : base() { }

        public IVmMaterial(object source) : base(source) { }
    }
}
