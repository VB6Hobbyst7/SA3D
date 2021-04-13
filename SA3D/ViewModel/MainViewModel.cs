using Microsoft.Win32;
using SATools.SA3D.ViewModel.Base;
using SATools.SA3D.XAML;
using SATools.SAModel.Graphics;
using SATools.SAModel.Graphics.OpenGL;
using System;
using System.IO;
using System.Windows;

namespace SATools.SA3D.ViewModel
{
    internal enum Mode
    {
        Model,
        Level,
        ProjectSA1,
        ProjectSA2
    }

    /// <summary>
    /// Main view model used to control the entire application
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        // <summary>
        // Application mode
        // </summary>
        //private Mode _applicationMode;

        /// <summary>
        /// Render context being displayed
        /// </summary>
        public DebugContext RenderContext => App.Context;

        public RelayCommand OpenFileRC { get; }

        public NJObjectTreeVM NJObjectTreeVM { get; }

        public MainViewModel()
        {
            NJObjectTreeVM = new NJObjectTreeVM(this);
            OpenFileRC = new RelayCommand(OpenFile);
        }

        /// <summary>
        /// Opens and loads a model/level file
        /// </summary>
        private void OpenFile()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "SA3D File(*.*mdl, *.nj, *.*lvl)|*.SA1MDL;*.SA2MDL;*.SA2BMDL;*.NJ;*.SA1LVL;*.SA2LVL;*.SA2BLVL"
            };

            if(ofd.ShowDialog() == true)
            {
                // reading the file indicator
                byte[] file = File.ReadAllBytes(ofd.FileName);
                try
                {
                    var mdlFile = SAModel.ObjData.ModelFile.Read(file, ofd.FileName);
                    if(mdlFile != null)
                    {
                        //_applicationMode = Mode.Model;
                        RenderContext.Scene.LoadModelFile(mdlFile);
                        NJObjectTreeVM.Refresh();
                        return;
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show("Error while reading model file!\n " + e.Message, e.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                }

                try
                {
                    var ltbl = SAModel.ObjData.LandTable.ReadFile(file);
                    if(ltbl != null)
                    {
                        //_applicationMode = Mode.Level;
                        RenderContext.Scene.LoadLandtable(ltbl);
                        return;
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show("Error while reading level file!\n " + e.Message, e.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MessageBox.Show("File not in any valid format", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
