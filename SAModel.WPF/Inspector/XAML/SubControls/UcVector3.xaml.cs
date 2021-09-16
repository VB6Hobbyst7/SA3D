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
    public partial class UcVector3 : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BaseBoxStyleProperty
            = DependencyProperty.Register(
                nameof(BaseBoxStyle),
                typeof(Style),
                typeof(UcVector3)
                );

        public Style BaseBoxStyle
        {
            get => (Style)GetValue(BaseBoxStyleProperty);
            set => SetValue(BaseBoxStyleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(Vector3),
                typeof(UcVector3),
                new(new((d, e) =>
                {
                    UcVector3 vc = (UcVector3)d;

                    vc.OnPropertyChanged(nameof(FloatX));
                    vc.OnPropertyChanged(nameof(FloatY));
                    vc.OnPropertyChanged(nameof(FloatZ));
                })));

        public Vector3 Value
        {
            get => (Vector3)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private float this[int i]
        {
            get
            {
                if(i < 0 || i > 3)
                    throw new IndexOutOfRangeException();

                Vector3 vector = Value;

                return i switch
                {
                    1 => vector.Y,
                    2 => vector.Z,
                    _ => vector.X,
                };
            }
            set
            {
                if(i < 0 || i > 3)
                    throw new IndexOutOfRangeException();

                Vector3 vector = Value;

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

        public event PropertyChangedEventHandler PropertyChanged;

        public UcVector3()
        {
            InitializeComponent();
        }

        protected void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
