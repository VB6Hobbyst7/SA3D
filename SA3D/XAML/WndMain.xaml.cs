using Microsoft.Win32;
using SATools.SA3D.ViewModel;
using SATools.SA3D.XAML.Dialogs;
using SATools.SA3D.XAML.UserControls;
using SATools.SAModel.Graphics;
using System;
using System.IO;
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
            Main = new VmMain(App.CurrentContext);
            DataContext = Main;
            InitializeComponent();

            var c = App.CurrentContext.AsControl();

            maingrid.Children.Add(c);
        }

        private void ControlSettings_Click(object sender, RoutedEventArgs e) => new WndControlSettings().ShowDialog();

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((VMDataTree)((TreeView)sender).DataContext).Selected = (VmTreeItem)e.NewValue;
        }

        /// <summary>
        /// Opens a dialog to import a GL transmission file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

            //Main.AddModel(import.Imported.Root, import.Imported.Textures, import.Imported.Animations);
        }

        private void Open3DFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter = "Model File (*.*mdl, *.nj, *.gj)|*.SA1MDL;*.SA2MDL;*.SA2BMDL;*.NJ;*.GJ|Level File (*.*lvl)|*.SA1LVL;*.SA2LVL;*.SA2BLVL"
            };

            if(ofd.ShowDialog() != true)
                return;

            byte[] file = File.ReadAllBytes(ofd.FileName);

            try
            {
                var mdlFile = SAModel.ObjData.ModelFile.Read(file, ofd.FileName);
                if(mdlFile != null)
                {
                    DebugTask task = new(mdlFile.Model, null, Path.GetFileNameWithoutExtension(ofd.FileName));
                    Main.LoadMdl(task);
                    return;
                }
            }
            catch(Exception exc)
            {
                MessageBox.Show("Error while reading model file!\n " + exc.Message, exc.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                var ltbl = SAModel.ObjData.LandTable.ReadFile(file);
                if(ltbl != null)
                {
                    //_applicationMode = Mode.Level;
                    //RenderContext.Scene.LoadLandtable(ltbl);
                    return;
                }
            }
            catch(Exception exc)
            {
                MessageBox.Show("Error while reading level file!\n " + exc.Message, exc.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            MessageBox.Show("File not in any valid format", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
