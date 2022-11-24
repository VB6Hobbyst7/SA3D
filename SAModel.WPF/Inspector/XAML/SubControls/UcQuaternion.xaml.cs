using PropertyChanged;
using System.Numerics;
using System.Windows;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcQuaternion.xaml
    /// </summary>
    internal partial class UcQuaternion : BaseStructUserControl<Quaternion>
    {
        [SuppressPropertyChangedWarnings]
        private float this[int i]
        {
            get
            {
                Quaternion quat = Value;

                return i switch
                {
                    1 => quat.Y,
                    2 => quat.Z,
                    3 => quat.W,
                    _ => quat.X,
                };
            }
            set
            {
                Quaternion quat = Value;

                switch (i)
                {
                    default:
                    case 0:
                        quat.X = value;
                        break;
                    case 1:
                        quat.Y = value;
                        break;
                    case 2:
                        quat.Z = value;
                        break;
                    case 3:
                        quat.W = value;
                        break;
                }

                Value = quat;
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

        public UcQuaternion() => InitializeComponent();

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FloatX));
            OnPropertyChanged(nameof(FloatY));
            OnPropertyChanged(nameof(FloatZ));
            OnPropertyChanged(nameof(FloatW));
        }
    }
}
