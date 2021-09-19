using System.Numerics;
using System.Windows;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcVector2.xaml
    /// </summary>
    internal partial class UcVector2 : BaseStructUserControl<Vector2>
    {
        private float this[int i]
        {
            get
            {
                Vector2 vector = Value;

                return i switch
                {
                    1 => vector.Y,
                    _ => vector.X,
                };
            }
            set
            {
                Vector2 vector = Value;

                switch(i)
                {
                    default:
                    case 0:
                        vector.X = value;
                        break;
                    case 1:
                        vector.Y = value;
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

        public UcVector2() => InitializeComponent();

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FloatX));
            OnPropertyChanged(nameof(FloatY));
        }
    }
}
