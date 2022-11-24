using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.GC;
using SATools.SAModel.Structs;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcGCParameter.xaml
    /// </summary>
    internal partial class UcGCParameter : BaseStructUserControl<IParameter>
    {
        private class ParameterTemplateSelector : DataTemplateSelector
        {
            private DataTemplate empty = new();

            public override DataTemplate SelectTemplate(object item, DependencyObject container)
            {
                UcGCParameter uc = (UcGCParameter)item;
                switch (uc.ParameterType)
                {
                    case ParameterType.VtxAttrFmt:
                    case ParameterType.IndexAttributes:
                    case ParameterType.Lighting:
                    case ParameterType.BlendAlpha:
                    case ParameterType.AmbientColor:
                    case ParameterType.Texture:
                    case ParameterType.TexCoordGen:
                        return (DataTemplate)uc.Resources[uc.ParameterType.ToString()];
                    default:
                        return empty;
                }
            }
        }

        public DataTemplateSelector TemplateSelector { get; }
            = new ParameterTemplateSelector();

        public ParameterType ParameterType
            => Value.Type;

        public uint Data
        {
            get => Value.Data;
            set => Value.Data = value;
        }

        #region Parameter properties

        #region vertex attribute format

        public VtxAttrFmtParameter VtxAttrFmtParameter
        {
            get => (VtxAttrFmtParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public VertexAttribute VertexAttribute
        {
            get => VtxAttrFmtParameter.VertexAttribute;
            set
            {
                VtxAttrFmtParameter t = VtxAttrFmtParameter;
                t.VertexAttribute = value;
                VtxAttrFmtParameter = t;
            }
        }

        public ushort VAFUnknown
        {
            get => VtxAttrFmtParameter.Unknown;
            set
            {
                VtxAttrFmtParameter t = VtxAttrFmtParameter;
                t.Unknown = value;
                VtxAttrFmtParameter = t;
            }
        }

        #endregion

        #region index attribute

        public IndexAttributeParameter IndexAttributeParameter
        {
            get => (IndexAttributeParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public IndexAttributes IndexAttributes
        {
            get => IndexAttributeParameter.IndexAttributes;
            set
            {
                IndexAttributeParameter t = IndexAttributeParameter;
                t.IndexAttributes = value;
                IndexAttributeParameter = t;
            }
        }

        #endregion

        #region Lighting

        public LightingParameter LightingParameter
        {
            get => (LightingParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public ushort LightingAttributes
        {
            get => LightingParameter.LightingAttributes;
            set
            {
                LightingParameter t = LightingParameter;
                t.LightingAttributes = value;
                LightingParameter = t;
            }
        }

        public byte LightingShadowStencil
        {
            get => LightingParameter.ShadowStencil;
            set
            {
                LightingParameter t = LightingParameter;
                t.ShadowStencil = value;
                LightingParameter = t;
            }
        }

        public byte LightingUnknown1
        {
            get => LightingParameter.Unknown1;
            set
            {
                LightingParameter t = LightingParameter;
                t.Unknown1 = value;
                LightingParameter = t;
            }
        }

        public byte LightingUnknown2
        {
            get => LightingParameter.Unknown2;
            set
            {
                LightingParameter t = LightingParameter;
                t.Unknown2 = value;
                LightingParameter = t;
            }
        }

        #endregion

        #region Blendalpha

        public BlendAlphaParameter BlendAlphaParameter
        {
            get => (BlendAlphaParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public BlendMode SourceAlpha
        {
            get => BlendAlphaParameter.SourceAlpha;
            set
            {
                BlendAlphaParameter t = BlendAlphaParameter;
                t.SourceAlpha = value;
                BlendAlphaParameter = t;
            }
        }

        public BlendMode DestAlpha
        {
            get => BlendAlphaParameter.DestAlpha;
            set
            {
                BlendAlphaParameter t = BlendAlphaParameter;
                t.DestAlpha = value;
                BlendAlphaParameter = t;
            }
        }

        #endregion

        #region Ambient Color

        public AmbientColorParameter AmbientColorParameter
        {
            get => (AmbientColorParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public Color AmbientColor
        {
            get => AmbientColorParameter.AmbientColor;
            set
            {
                AmbientColorParameter t = AmbientColorParameter;
                t.AmbientColor = value;
                AmbientColorParameter = t;
            }
        }

        #endregion

        #region Texture

        public TextureParameter TextureParameter
        {
            get => (TextureParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public ushort TextureID
        {
            get => TextureParameter.TextureID;
            set
            {
                TextureParameter t = TextureParameter;
                t.TextureID = value;
                TextureParameter = t;
            }
        }

        public GCTileMode TextureTiling
        {
            get => TextureParameter.Tiling;
            set
            {
                TextureParameter t = TextureParameter;
                t.Tiling = value;
                TextureParameter = t;
            }
        }

        #endregion

        #region Texture coordinate

        public TexCoordGenParameter TexCoordGenParameter
        {
            get => (TexCoordGenParameter)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public TexCoordID TexCoordID
        {
            get => TexCoordGenParameter.TexCoordID;
            set
            {
                TexCoordGenParameter t = TexCoordGenParameter;
                t.TexCoordID = value;
                TexCoordGenParameter = t;
            }
        }

        public TexGenType TexGenType
        {
            get => TexCoordGenParameter.TexGenType;
            set
            {
                TexCoordGenParameter t = TexCoordGenParameter;
                t.TexGenType = value;
                TexCoordGenParameter = t;
            }
        }

        public TexGenSrc TexGenSrc
        {
            get => TexCoordGenParameter.TexGenSrc;
            set
            {
                TexCoordGenParameter t = TexCoordGenParameter;
                t.TexGenSrc = value;
                TexCoordGenParameter = t;
            }
        }

        public TexGenMatrix MatrixID
        {
            get => TexCoordGenParameter.MatrixID;
            set
            {
                TexCoordGenParameter t = TexCoordGenParameter;
                t.MatrixID = value;
                TexCoordGenParameter = t;
            }
        }

        #endregion

        #endregion

        public UcGCParameter()
        {
            Value = new Parameter();
            InitializeComponent();
        }

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ParameterType));
            OnPropertyChanged(nameof(Data));
        }
    }
}
