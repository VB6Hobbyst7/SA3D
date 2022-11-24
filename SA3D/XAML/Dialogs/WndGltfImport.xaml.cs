using Microsoft.Win32;
using SATools.SAModel.Convert;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SATools.SA3D.XAML.Dialogs
{
    /// <summary>
    /// Interaction logic for WndGltfImport.xaml
    /// </summary>
    public partial class WndGltfImport : Window
    {
        public GLTF.Contents Imported { get; private set; }

        public WndGltfImport()
        {
            InitializeComponent();

            filepath.Dialog = new OpenFileDialog()
            {
                Title = "Select GLTF file to import",
                Filter = "GLTF file (*.glb; *.gltf)|*.glb;*.gltf"
            };
        }

        private void AnimFrameRate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Text))
            {
                AnimFrameRate.Text = "0";
                return;
            }

            e.Handled = !float.TryParse(e.Text, out _);
        }

        private void Import(object sender, RoutedEventArgs e)
        {
            float playbackSpeed = float.Parse(AnimFrameRate.Text);

            try
            {
                if (!File.Exists(filepath.FilePath))
                {
                    _ = MessageBox.Show("Filepath doesnt exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Imported = GLTF.Read(filepath.FilePath, ImportTextures.IsChecked.Value, ImportAnims.IsChecked.Value ? playbackSpeed : null);
                DialogResult = true;
            }
            catch (Exception exc)
            {
                _ = new SAWPF.ErrorDialog("GLTF Import", "GLTF Import failed!", exc.ToString()).ShowDialog();
                DialogResult = false;
            }
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
            Close();
        }
    }
}
