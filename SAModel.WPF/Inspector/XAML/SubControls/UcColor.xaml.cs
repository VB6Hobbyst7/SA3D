using System;
using System.Windows;
using System.Windows.Media;
using SATools.SAWPF;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcColor.xaml
    /// </summary>
    internal partial class UcColor : BaseStructUserControl<Structs.Color>
    {

        private string _hexColor;
        private bool _manual;

        public string HexColor
        {
            get => _hexColor;
            set
            {
                _hexColor = value;
                try
                {
                    _manual = true;

                    var c = Value;
                    c.Hex = value;
                    Value = c;

                    _manual = false;

                }
                catch(FormatException)
                {
                    _manual = false;
                    throw;
                }
            }
        }

        public SolidColorBrush ButtonBackground
        {
            get
            {
                var c = Value;
                return new(Color.FromArgb(c.A, c.R, c.G, c.B));
            }
        }

        public UcColor() => InitializeComponent();

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if(!_manual)
                HexColor = Value.Hex;
            OnPropertyChanged(nameof(ButtonBackground));
        }

        private void OpenColorPicker(object sender, RoutedEventArgs e)
        {
            var c = Value;
            Color col = Color.FromArgb(c.A, c.R, c.G, c.B);

            if(!WndColorPicker.ShowAsDialog(ref col))
                return;

            Value = new(col.R, col.G, col.B, col.A);
        }
    }
}
