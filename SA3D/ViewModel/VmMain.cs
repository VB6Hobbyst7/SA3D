using SATools.SA3D.ViewModel.Base;
using SATools.SA3D.ViewModel.TreeItems;
using SATools.SAArchive;
using SATools.SAModel.Graphics;
using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.IO;

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

        #region File information

        /// <summary>
        /// The path to the currently opened file
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// The attach format of the currently opened file
        /// </summary>
        public AttachFormat FileFormat { get; private set; }

        /// <summary>
        /// Whether the currently opened file is a ninja file
        /// </summary>
        public bool FileIsNJ { get; private set; }

        /// <summary>
        /// Whether the output file should be optimized
        /// </summary>
        public bool FileOptimize { get; private set; }

        /// <summary>
        /// File information for the window border
        /// </summary>
        public string WindowTitle =>
            string.IsNullOrWhiteSpace(FilePath) ? "SA3D" : $"SA3D [{FilePath}] | {FileFormat}"
                + (FileIsNJ ? " | NJ" : "") + (FileOptimize ? " | Optimized" : "");

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
            ObjectTree.Reset();
            GeometryTree.Reset();
            Context.Scene.ClearTasks();
            Context.Scene.ClearLandtable();

            FilePath = null;
            FileOptimize = true;
            FileFormat = AttachFormat.Buffer;
            FileIsNJ = false;

            switch (mode)
            {
                case Mode.Model:
                    NJObject obj = new()
                    {
                        Name = "Root"
                    };

                    DebugTask task = new(obj, null);
                    Context.Scene.AddTask(task);
                    ObjectTree.Objects.Add(new(null, new VmObject(task, null)));
                    break;
                case Mode.Level:
                    LoadLandtable(new LandTable(SAModel.ObjData.LandtableFormat.Buffer));
                    break;
                case Mode.None:
                case Mode.ProjectSA1:
                case Mode.ProjectSA2:
                default:
                    throw new NotImplementedException("Project modes not yet implemented");
            }
        }

        public bool OpenFile(string filepath)
        {
            byte[] file = File.ReadAllBytes(filepath);

            ModelFile mdlFile = ModelFile.Read(file, filepath);
            if (mdlFile != null)
            {
                DebugTask task = new(mdlFile.Model, null, Path.GetFileNameWithoutExtension(filepath));
                LoadModel(task);

                FilePath = filepath;
                FileOptimize = false;
                FileFormat = mdlFile.Format;
                FileIsNJ = mdlFile.NJFile;
                return true;
            }


            LandTable ltbl = LandTable.ReadFile(file);
            if (ltbl != null)
            {
                LoadLandtable(ltbl);

                FilePath = filepath;
                FileOptimize = false;
                FileIsNJ = false;

                FileFormat = ltbl.Format switch
                {
                    SAModel.ObjData.LandtableFormat.SA1 or SAModel.ObjData.LandtableFormat.SADX => AttachFormat.BASIC,
                    SAModel.ObjData.LandtableFormat.SA2 => AttachFormat.CHUNK,
                    SAModel.ObjData.LandtableFormat.SA2B => AttachFormat.GC,
                    _ => AttachFormat.Buffer,
                };
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the current scene and loads the model
        /// </summary>
        /// <param name="task"></param>
        private void LoadModel(DebugTask task)
        {
            ApplicationMode = Mode.Model;

            Context.Scene.ClearTasks();
            Context.Scene.ClearLandtable();
            Context.Scene.AddTask(task);

            ObjectTree.Reset();
            GeometryTree.Reset();
            ObjectTree.Objects.Add(new(null, new VmObject(task, null)));
        }

        /// <summary>
        /// Clears the current scene and loads a landtable
        /// </summary>
        /// <param name="ltbl">The landtable to load</param>
        private void LoadLandtable(LandTable ltbl)
        {
            ApplicationMode = Mode.Level;
            Context.Scene.ClearTasks();
            Context.Scene.ClearLandtable();
            Context.Scene.LoadLandtable(ltbl);

            ObjectTree.Reset();
            GeometryTree.Reset();

            GeometryTree.Objects.Add(new VmTreeItem(null, new VmGeometryHead(Context.Scene.VisualGeometry, false)));
            GeometryTree.Objects.Add(new VmTreeItem(null, new VmGeometryHead(Context.Scene.CollisionGeometry, true)));
            GeometryTree.Objects.Add(new VmTreeItem(null, new VmTextureHead(Context.Scene.LandTextureSet)));
        }

        public void InsertModel(NJObject insertRoot, bool insertAtRoot, TextureSet textures, Motion[] animations)
        {
            if (ApplicationMode == Mode.Model)
            {
                if (!insertAtRoot
                    && ObjectTree?.Selected.ItemType == TreeItemType.Model
                    && ObjectTree.Selected.Parent.ItemType != TreeItemType.ModelHead)
                {
                    ((NJObject)ObjectTree.Selected.Data).AddChild(insertRoot);
                }
                else
                {
                    DebugTask dbtsk = (DebugTask)Context.Scene.GameTasks[0];
                    if (dbtsk.Model.ChildCount == 0)
                    {
                        dbtsk.ReplaceModel(insertRoot);
                    }
                    else
                    {
                        dbtsk.Model.AddChild(insertRoot);
                        dbtsk.Model.ConvertAttachFormat(SAModel.ModelData.AttachFormat.Buffer, false, false);
                    }
                }
            }
        }

        public void SaveToFile(string filepath, AttachFormat format, bool nj, bool optimize)
        {
            FilePath = filepath;
            FileFormat = format;
            bool forceUpdate = optimize != FileOptimize;
            FileIsNJ = nj;
            FileOptimize = optimize;
            SaveToFile(forceUpdate);
        }

        public void SaveToFile()
            => SaveToFile();

        public void SaveToFile(bool forceUpdate = false)
        {
            if (ApplicationMode == Mode.Model)
            {
                DebugTask dbtsk = (DebugTask)Context.Scene.GameTasks[0];
                dbtsk.Model.ConvertAttachFormat(FileFormat, FileOptimize, false, forceUpdate);
                ModelFile.WriteToFile(FilePath, FileFormat, FileIsNJ, dbtsk.Model);
            }
            else if (ApplicationMode == Mode.Level)
            {
                var ltblFormat = FileFormat switch
                {
                    AttachFormat.BASIC => SAModel.ObjData.LandtableFormat.SA1,
                    AttachFormat.CHUNK => SAModel.ObjData.LandtableFormat.SA2,
                    AttachFormat.GC => SAModel.ObjData.LandtableFormat.SA2B,
                    _ => SAModel.ObjData.LandtableFormat.Buffer,
                };


                LandTable ltbl = Context.Scene.CurrentLandTable;
                ltbl.ConvertToFormat(ltblFormat, FileOptimize, forceUpdate);

                // conversion to sa2/b may have added geometry right at the end
                for (int i = Context.Scene.Geometry.Count; i < ltbl.Geometry.Count; i++)
                    Context.Scene.Geometry.Add(ltbl.Geometry[i]);

                ltbl.WriteFile(FilePath);
            }
            else
            {
                throw new NotImplementedException("Project saving not yet supported");
            }
        }
    }
}
