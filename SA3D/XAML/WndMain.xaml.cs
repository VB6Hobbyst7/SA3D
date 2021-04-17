using Microsoft.Win32;
using SATools.SA3D.ViewModel;
using SATools.SA3D.XAML.Dialogs;
using SATools.SA3D.XAML.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SA3D.XAML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WndMain : Window
    {
        VmMain Main { get; }

        public WndMain()
        {
            Main = new VmMain(App.Context);
            DataContext = Main;
            InitializeComponent();

            var c = App.Context.AsControl();

            maingrid.Children.Add(c);
        }

        private void ControlSettings_Click(object sender, RoutedEventArgs e) => new WndControlSettings().ShowDialog();

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((VmObjectTree)((TreeView)sender).DataContext).Selected = (VmTreeItem)e.NewValue;
        }

        private void ImportGLTF(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter = "GLTF file (*.glb; *.gltf)|*.glb;*.gltf"
            };

            if(ofd.ShowDialog() != true)
                return;

            WndGltfImport import = new (ofd.FileName);

            if(import.ShowDialog() != true)
                return;

            Main.AddModel(import.Imported.Root, import.Imported.Textures, import.Imported.Animations);
        }
    }
}
