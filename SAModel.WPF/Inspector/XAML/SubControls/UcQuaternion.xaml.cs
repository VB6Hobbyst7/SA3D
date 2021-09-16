using System;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcQuaternion.xaml
    /// </summary>
    public partial class UcQuaternion : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BaseBoxStyleProperty
            = DependencyProperty.Register(
                nameof(BaseBoxStyle),
                typeof(Style),
                typeof(UcQuaternion)
                );

        public Style BaseBoxStyle
        {
            get => (Style)GetValue(BaseBoxStyleProperty);
            set => SetValue(BaseBoxStyleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(Quaternion),
                typeof(UcQuaternion),
                new(new((d, e) =>
                {
                    UcQuaternion vc = (UcQuaternion)d;

                    vc.OnPropertyChanged(nameof(FloatX));
                    vc.OnPropertyChanged(nameof(FloatY));
                    vc.OnPropertyChanged(nameof(FloatZ));
                    vc.OnPropertyChanged(nameof(FloatW));
                })));

        public Quaternion Value
        {
            get => (Quaternion)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private float this[int i]
        {
            get
            {
                if(i is < 0 or > 3)
                    throw new IndexOutOfRangeException();

                Quaternion vector = Value;

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

                Quaternion vector = Value;

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

        public UcQuaternion() 
            => InitializeComponent();

        protected void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
