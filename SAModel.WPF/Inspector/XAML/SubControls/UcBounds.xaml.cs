using SATools.SAModel.Structs;
using System.Numerics;
using System.Windows;

namespace SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcBounds.xaml
    /// </summary>
    internal partial class UcBounds : BaseStructUserControl<Bounds>
    {
        public Vector3 Position
        {
            get => Value.Position;
            set
            {
                Bounds bounds = Value;
                bounds.Position = value;
                Value = bounds;
            }
        }

        public float Radius
        {
            get => Value.Radius;
            set
            {
                Bounds bounds = Value;
                bounds.Radius = value;
                Value = bounds;
            }
        }


        public UcBounds()
            => InitializeComponent();

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Radius));
        }

    }
}
