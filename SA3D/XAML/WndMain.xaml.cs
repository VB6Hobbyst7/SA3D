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
        private VmMain Main { get; }

        public WndMain()
        {
            Main = new VmMain(App.CurrentContext);
            DataContext = Main;
            InitializeComponent();

            var c = App.CurrentContext.AsControl();

            maingrid.Children.Add(c);
        }

        private void ControlSettings_Click(object sender, RoutedEventArgs e) => new WndControlSettings().ShowDialog();

        /// <summary>
        /// Opens a dialog to import a GL transmission file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportGLTF(object sender, RoutedEventArgs e)
        {
            WndGltfImport import = new();

            if(import.ShowDialog() != true)
                return;

            Main.InsertModel(import.Imported.Root, import.InsertMode.Text == "Root", import.Imported.Textures, import.Imported.Animations);
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter = "Model File (*.*mdl, *.nj, *.gj)|*.BFMDL;*.SA1MDL;*.SA2MDL;*.SA2BMDL;*.NJ;*.GJ|Level File (*.*lvl)|*.BFLVL;*.SA1LVL;*.SA2LVL;*.SA2BLVL"
            };

            if(ofd.ShowDialog() != true)
                return;

            if(Main.OpenFile(ofd.FileName))
                return;

            _ = MessageBox.Show("File not in any valid format", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void NewModel(object sender, RoutedEventArgs e)
        {
            Main.New3DFile(Mode.Model);
        }

        private void NewLevel(object sender, RoutedEventArgs e)
        {
            Main.New3DFile(Mode.Level);
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if(Main.FilePath == null)
                SaveAs(sender, e);
            else
                Main.SaveToFile();
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            WndSave saveDialog = new(Main.ApplicationMode, Main.FilePath, Main.FileFormat, Main.FileIsNJ, Main.FileOptimize);

            if(saveDialog.ShowDialog() != true)
                return;

            Main.SaveToFile(saveDialog.Filepath, saveDialog.Format, saveDialog.NJ, saveDialog.Optimize);
        }
    }
}
