using Microsoft.Win32;
using SATools.SA3D.ViewModel.Base;
using SATools.SA3D.ViewModel.TreeItems;
using SATools.SAModel.Graphics;
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
    public class VmMain : BaseViewModel
    {
        // <summary>
        // Application mode
        // </summary>
        //private Mode _applicationMode;

        /// <summary>
        /// Render context being displayed
        /// </summary>
        public DebugContext RenderContext { get; }

        public RelayCommand Cmd_OpenFile
            => new(Open3DFile);

        public VmObjectTree ObjectTree { get; }

        public VmMain(DebugContext renderContext)
        {
            RenderContext = renderContext;
            ObjectTree = new VmObjectTree(this);
        }

        /// <summary>
        /// Opens and loads a model/level file
        /// </summary>
        private void Open3DFile()
        {
            OpenFileDialog ofd = new()
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
                        DebugTask task = new(mdlFile.Model, null, Path.GetFileNameWithoutExtension(ofd.FileName));
                        RenderContext.Scene.AddDisplayTask(task);
                        ObjectTree.Objects.Add(new(null, new VmObject(task, mdlFile)));
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
