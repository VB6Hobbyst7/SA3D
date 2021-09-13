using Microsoft.Win32;
using SATools.SAModel.ObjData;
using System.IO;
using System.Windows;

namespace SAModelInspector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WndMain : Window
    {
        public WndMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Reads a file and loads it into the Inspector
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter = "All Supported Formats (*.*mdl, *.nj, *.gj, *.*lvl)|*.BFMDL;*.SA1MDL;*.SA2MDL;*.SA2BMDL;*.NJ;*.GJ;*.BFLVL;*.SA1LVL;*.SA2LVL;*.SA2BLVL"
                       + "|Model File (*.*mdl, *.nj, *.gj)|*.BFMDL;*.SA1MDL;*.SA2MDL;*.SA2BMDL;*.NJ;*.GJ"
                       + "|Level File (*.*lvl)|*.BFLVL;*.SA1LVL;*.SA2LVL;*.SA2BLVL"
            };

            if(ofd.ShowDialog() != true)
                return;

            byte[] file = File.ReadAllBytes(ofd.FileName);

            ModelFile mdlFile = ModelFile.Read(file, ofd.FileName);
            if(mdlFile != null)
            {
                Inspector.LoadNewObject(mdlFile);
                return;
            }


            LandTable ltbl = LandTable.ReadFile(file);
            if(ltbl != null)
            {
                Inspector.LoadNewObject(ltbl);
                return;
            }
        }
    }
}
