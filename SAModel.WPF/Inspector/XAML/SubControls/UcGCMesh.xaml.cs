using SATools.SAModel.ModelData.GC;
using SATools.SAModel.WPF.Inspector.Viewmodel;
using SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.GC;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcGCMesh.xaml
    /// </summary>
    internal partial class UcGCMesh : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MeshProperty
            = DependencyProperty.Register(
                nameof(Mesh),
                typeof(Mesh),
                typeof(UcGCMesh),
                new(new((d, e) =>
                {
                    UcGCMesh uc = (UcGCMesh)d;
                    uc.ivm = new((Mesh)e.NewValue);
                    uc.OnPropertyChanged(nameof(Parameters));
                    uc.OnPropertyChanged(nameof(Polys));
                })));

        public Mesh Mesh
        {
            get => (Mesh)GetValue(MeshProperty);
            set => SetValue(MeshProperty, value);
        }

        public IInspectorInfo Parameters
            => ivm.InspectorElements[0];

        public IInspectorInfo Polys
            => ivm.InspectorElements[1];

        private IVmMesh ivm = new(default);

        public event PropertyChangedEventHandler PropertyChanged;

        public UcGCMesh()
        {
            InitializeComponent();
        }

        private void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
