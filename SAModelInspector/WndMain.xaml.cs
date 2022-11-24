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
        object loaded = null;

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

            if (ofd.ShowDialog() != true)
                return;

            byte[] file = File.ReadAllBytes(ofd.FileName);

            ModelFile mdlFile = ModelFile.Read(file, ofd.FileName);
            if (mdlFile != null)
            {
                loaded = mdlFile;
                Inspector.LoadNewObject(mdlFile);
                return;
            }


            LandTable ltbl = LandTable.ReadFile(file);
            if (ltbl != null)
            {
                loaded = ltbl;

                Inspector.LoadNewObject(ltbl);
                return;
            }
        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            if (loaded == null)
                return;


            if (loaded is ModelFile mdlfile)
            {
                SaveFileDialog sfd = new()
                {
                    Filter = "Model File (*.*mdl, *.nj, *.gj)|*.BFMDL;*.SA1MDL;*.SA2MDL;*.SA2BMDL;*.NJ;*.GJ"
                };

                if (sfd.ShowDialog() != true)
                    return;

                mdlfile.SaveToFile(sfd.FileName, false);
            }

            if (loaded is LandTable ltbl)
            {
                SaveFileDialog sfd = new()
                {
                    Filter = "Level File (*.*lvl)|*.BFLVL;*.SA1LVL;*.SA2LVL;*.SA2BLVL"
                };

                if (sfd.ShowDialog() != true)
                    return;

                ltbl.WriteFile(sfd.FileName);
            }
        }
    }
}
