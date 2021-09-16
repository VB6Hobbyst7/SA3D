using System;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcVector4.xaml
    /// </summary>
    public partial class UcVector4 : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BaseBoxStyleProperty
            = DependencyProperty.Register(
                nameof(BaseBoxStyle),
                typeof(Style),
                typeof(UcVector4)
                );

        public Style BaseBoxStyle
        {
            get => (Style)GetValue(BaseBoxStyleProperty);
            set => SetValue(BaseBoxStyleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(Vector4),
                typeof(UcVector4),
                new(new((d, e) =>
                {
                    UcVector4 vc = (UcVector4)d;

                    vc.OnPropertyChanged(nameof(FloatX));
                    vc.OnPropertyChanged(nameof(FloatY));
                    vc.OnPropertyChanged(nameof(FloatZ));
                    vc.OnPropertyChanged(nameof(FloatW));
                })));

        public Vector4 Value
        {
            get => (Vector4)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private float this[int i]
        {
            get
            {
                if(i is < 0 or > 3)
                    throw new IndexOutOfRangeException();

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
                if(i is < 0 or > 3)
                    throw new IndexOutOfRangeException();

                Vector4 vector = Value;

                switch(i)
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

        public event PropertyChangedEventHandler PropertyChanged;

        public UcVector4()
        {
            InitializeComponent();
        }

        protected void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
