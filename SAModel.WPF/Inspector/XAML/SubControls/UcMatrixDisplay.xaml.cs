using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcMatrixDisplay.xaml
    /// </summary>
    public partial class UcMatrixDisplay : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BaseBoxStyleProperty
            = DependencyProperty.Register(
                nameof(BaseBoxStyle),
                typeof(Style),
                typeof(UcMatrixDisplay)
                );

        public Style BaseBoxStyle
        {
            get => (Style)GetValue(BaseBoxStyleProperty);
            set => SetValue(BaseBoxStyleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(Matrix4x4),
                typeof(UcMatrixDisplay),
                new(new((d, e) =>
                {
                    UcMatrixDisplay vc = (UcMatrixDisplay)d;

                    // now update all fields :')
                    vc.OnPropertyChanged(nameof(M11));
                    vc.OnPropertyChanged(nameof(M12));
                    vc.OnPropertyChanged(nameof(M13));
                    vc.OnPropertyChanged(nameof(M14));

                    vc.OnPropertyChanged(nameof(M21));
                    vc.OnPropertyChanged(nameof(M22));
                    vc.OnPropertyChanged(nameof(M23));
                    vc.OnPropertyChanged(nameof(M24));

                    vc.OnPropertyChanged(nameof(M31));
                    vc.OnPropertyChanged(nameof(M32));
                    vc.OnPropertyChanged(nameof(M33));
                    vc.OnPropertyChanged(nameof(M34));

                    vc.OnPropertyChanged(nameof(M41));
                    vc.OnPropertyChanged(nameof(M42));
                    vc.OnPropertyChanged(nameof(M43));
                    vc.OnPropertyChanged(nameof(M44));
                })));

        public Matrix4x4 Value
            => (Matrix4x4)GetValue(ValueProperty);

        public float M11
            => Value.M11;

        public float M12
            => Value.M12;

        public float M13
            => Value.M13;

        public float M14
            => Value.M14;


        public float M21
            => Value.M21;

        public float M22
            => Value.M22;

        public float M23
            => Value.M23;

        public float M24
            => Value.M24;


        public float M31
            => Value.M31;

        public float M32
            => Value.M32;

        public float M33
            => Value.M33;

        public float M34
            => Value.M34;


        public float M41
            => Value.M41;

        public float M42
            => Value.M42;

        public float M43
            => Value.M43;

        public float M44
            => Value.M44;

        public event PropertyChangedEventHandler PropertyChanged;

        public UcMatrixDisplay() => InitializeComponent();

        protected void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
