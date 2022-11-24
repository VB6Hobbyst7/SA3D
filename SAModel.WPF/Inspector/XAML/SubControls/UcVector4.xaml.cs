using PropertyChanged;
using System.Numerics;
using System.Windows;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcVector4.xaml
    /// </summary>
    internal partial class UcVector4 : BaseStructUserControl<Vector4>
    {
        [SuppressPropertyChangedWarnings]
        private float this[int i]
        {
            get
            {
                Vector4 vector = Value;

                return i switch
                {
                    1 => vector.Y,
                    2 => vector.Z,
                    3 => vector.W,
                    _ => vector.X,
                };
            }
            set
            {
                Vector4 vector = Value;

                switch (i)
                {
                    default:
                    case 0:
                        vector.X = value;
                        break;
                    case 1:
                        vector.Y = value;
                        break;
                    case 2:
                        vector.Z = value;
                        break;
                    case 3:
                        vector.W = value;
                        break;
                }

                Value = vector;
            }
        }

        public float FloatX
        {
            get => this[0];
            set => this[0] = value;
        }

        public float FloatY
        {
            get => this[1];
            set => this[1] = value;
        }

        public float FloatZ
        {
            get => this[2];
            set => this[2] = value;
        }

        public float FloatW
        {
            get => this[3];
            set => this[3] = value;
        }

        public UcVector4() => InitializeComponent();

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FloatX));
            OnPropertyChanged(nameof(FloatY));
            OnPropertyChanged(nameof(FloatZ));
            OnPropertyChanged(nameof(FloatW));
        }
    }
}
