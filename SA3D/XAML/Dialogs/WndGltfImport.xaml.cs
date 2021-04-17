using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SATools.SAModel.Convert;

namespace SATools.SA3D.XAML.Dialogs
{
    /// <summary>
    /// Interaction logic for WndGltfImport.xaml
    /// </summary>
    public partial class WndGltfImport : Window
    {
        public string FilePath { get; }

        public GLTF.Contents Imported { get; private set; }

        public WndGltfImport(string filePath)
        {
            FilePath = filePath;
            InitializeComponent();
            PathDisplay.Text = FilePath;
        }

        private void AnimFrameRate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(e.Text))
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
                Imported = GLTF.Read(FilePath, SAModel.ModelData.AttachFormat.Buffer, ImportTextures.IsChecked.Value, ImportAnims.IsChecked.Value ? playbackSpeed : null );
                DialogResult = true;
            }
            catch(Exception exc)
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
