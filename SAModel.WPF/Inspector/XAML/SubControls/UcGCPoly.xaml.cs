using SATools.SAModel.ModelData.GC;
using SATools.SAModel.WPF.Inspector.Viewmodel;
using SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.GC;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcGCPoly.xaml
    /// </summary>
    internal partial class UcGCPoly : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty PolyProperty
            = DependencyProperty.Register(
                nameof(Poly),
                typeof(Poly),
                typeof(UcGCPoly),
                new(new((d, e) => {
                    UcGCPoly uc = (UcGCPoly)d;
                    uc.ivm = new((Poly)e.NewValue);
                    uc.OnPropertyChanged(nameof(PolyType));
                    uc.OnPropertyChanged(nameof(Corners));
                })));

        public Poly Poly
        {
            get => (Poly)GetValue(PolyProperty);
            set => SetValue(PolyProperty, value);
        }

        public PolyType PolyType
            => Poly.Type;

        public IInspectorInfo Corners
            => ivm.InspectorElements[1];

        private IVmGCPoly ivm = new(default);

        public event PropertyChangedEventHandler PropertyChanged;

        public UcGCPoly()
        {
            Poly = new Poly(PolyType.Triangles, null);
            InitializeComponent();
        }

        private void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
