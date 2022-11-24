using SATools.SAWPF;
using System;
using System.Windows;
using System.Windows.Media;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcColor.xaml
    /// </summary>
    internal partial class UcColor : BaseStructUserControl<Structs.Color>
    {

        private string _hexColor = "#00000000";
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
                catch (FormatException)
                {
                    _manual = false;
                    throw;
                }
            }
        }

        public UcColor() => InitializeComponent();

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!_manual)
                HexColor = Value.Hex;

            var c = Value;
            Color transparent = Color.FromArgb(c.A, c.R, c.G, c.B);
            Color opaque = Color.FromArgb(255, transparent.R, transparent.G, transparent.B);

            ColorButton.Background = new LinearGradientBrush(opaque, transparent, new(0.33d, 0), new(0.66d, 0));
        }

        private void OpenColorPicker(object sender, RoutedEventArgs e)
        {
            var c = Value;
            Color col = Color.FromArgb(c.A, c.R, c.G, c.B);

            if (!WndColorPicker.ShowAsDialog(ref col))
                return;

            Value = new(col.R, col.G, col.B, col.A);
        }
    }
}
