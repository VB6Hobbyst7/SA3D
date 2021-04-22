using Microsoft.Win32;
using SATools.SA3D.ViewModel.Base;
using SATools.SA3D.ViewModel.TreeItems;
using SATools.SAArchive;
using SATools.SAModel.Graphics;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace SATools.SA3D.ViewModel
{
    public enum Mode
    {
        None,
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
        public static DebugContext Context { get; private set; }

        #region Application mode Stuff

        private Mode _appMode;

        /// <summary>
        /// Application mode
        /// </summary>
        public Mode ApplicationMode
        {
            get => _appMode;
            private set
            {
                _appMode = value;
                OnPropertyChanged(nameof(ApplicationModeNotNone));
                OnPropertyChanged(nameof(EnableObjectTab));
                OnPropertyChanged(nameof(EnableGeometryTab));
            }
        }

        /// <summary>
        /// Whether the application mode hasnt been set yet
        /// </summary>
        public bool ApplicationModeNotNone
            => ApplicationMode != Mode.None;

        /// <summary>
        /// Whether to get Access to the model tab
        /// </summary>
        public bool EnableObjectTab
            => ApplicationMode is not Mode.Level and not Mode.None;

        /// <summary>
        /// Whether to get Access to the Geometry Tab
        /// </summary>
        public bool EnableGeometryTab
            => ApplicationMode is not Mode.Model and not Mode.None;

        #endregion


        /// <summary>
        /// Object treeview data
        /// </summary>
        public VMDataTree ObjectTree { get; }

        /// <summary>
        /// Geometry treeview data
        /// </summary>
        public VMDataTree GeometryTree { get; }

        public VmMain(DebugContext context)
        {
            Context = context;
            ObjectTree = new VMDataTree(this);
            GeometryTree = new VMDataTree(this);
        }

        public void New3DFile(Mode mode)
        {
            ApplicationMode = mode;
            switch(mode)
            {
                case Mode.Model:
                    
                    DebugTask task = new(null, null);
                    Context.Scene.AddTask(task);
                    break;
                case Mode.Level:
                    InitGeometryTree(null);
                    break;
                case Mode.None:
                case Mode.ProjectSA1:
                case Mode.ProjectSA2:
                default:
                    break;
            }
        }

        public void LoadMdl(DebugTask task)
        {
            
            ApplicationMode = Mode.Model;
            Context.Scene.AddTask(task);
            ObjectTree.Objects.Add(new(null, new VmObject(task, null)));
        }

        private void InitGeometryTree(LandTable ltbl)
        {
            Context.Scene.LoadLandtable(ltbl);

            GeometryTree.Objects.Clear();

            GeometryTree.Objects.Add(new VmTreeItem(null, new VmGeometryHead(Context.Scene.VisualGeometry, false)));
            GeometryTree.Objects.Add(new VmTreeItem(null, new VmGeometryHead(Context.Scene.CollisionGeometry, true)));
            GeometryTree.Objects.Add(new VmTreeItem(null, new VmTextureHead(Context.Scene.LandTextureSet)));
        }

    }
}
