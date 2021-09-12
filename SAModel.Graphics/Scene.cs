using SATools.SAArchive;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SATools.SAModel.Graphics
{
    public class Scene
    {
        #region events

        public delegate void OnUpdate(double delta);

        public event OnUpdate OnUpdateEvent;

        #endregion

        #region Privates

        private readonly BufferingBridge _bufferingBridge;

        private readonly List<GameTask> _gameTasks;

        private readonly Dictionary<TextureSet, (int count, bool manual)> _textureSetCounts;

        private readonly List<TextureSet> _textureSets;

        private TextureSet _geometryTextures;

        internal bool _visualCollision;

        #endregion

        #region Properties

        /// <summary>
        /// Loaded game tasks
        /// </summary>
        public readonly ReadOnlyCollection<GameTask> GameTasks;

        /// <summary>
        /// Loaded texture sets (used in tasks and landtable)
        /// </summary>
        public ReadOnlyCollection<TextureSet> TextureSets { get; }

        /// <summary>
        ///  Geometry textures
        /// </summary>
        public TextureSet LandTextureSet
        {
            get => _geometryTextures;
            set
            {
                if(_geometryTextures == value)
                    return;
                TaskTexturesChanged(_geometryTextures, value);
                _geometryTextures = value;
            }
        }

        /// <summary>
        /// Camera used by the scene
        /// </summary>
        public Camera Cam { get; }

        /// <summary>
        /// Time passed in the scene
        /// </summary>
        public double SceneTime { get; private set; }

        #endregion

        public LandTable CurrentLandTable { get; private set; }

        public List<LandEntry> Geometry => CurrentLandTable?.Geometry;

        public LandEntry[] VisualGeometry
        {
            get => _visualCollision ? CollisionGeometry : Geometry == null ? Array.Empty<LandEntry>() : Geometry.Where(x => x.SurfaceAttributes.HasFlag(SurfaceAttributes.Visible)).ToArray();
        }

        public LandEntry[] CollisionGeometry
        {
            get => Geometry == null ? Array.Empty<LandEntry>() : Geometry.Where(x => x.SurfaceAttributes.IsCollision()).ToArray();
        }

        internal Scene(float cameraAspect, BufferingBridge bufferbridge)
        {
            Cam = new Camera(cameraAspect);

            _bufferingBridge = bufferbridge;

            _textureSetCounts = new();
            _textureSets = new();
            TextureSets = new(_textureSets);

            _gameTasks = new();
            GameTasks = new(_gameTasks);
        }

        public void Update(double delta)
        {
            OnUpdateEvent.Invoke(delta);
            SceneTime += delta;
            foreach(GameTask tsk in GameTasks)
                tsk.Update(delta, SceneTime);
        }


        #region Handling tasks

        public void AddTask(GameTask task)
        {
            if(_gameTasks.Contains(task))
                throw new ArgumentException("The added task was already part of the scene!");
            _gameTasks.Add(task);
            if(task is DisplayTask dtsk)
            {
                dtsk.OnTextureSetChanged += TaskTexturesChanged;
                TaskTexturesChanged(null, dtsk.TextureSet);
            }
        }

        public void RemoveTask(GameTask task)
        {
            if(_gameTasks.Remove(task) && task is DisplayTask dtsk)
            {
                dtsk.OnTextureSetChanged -= TaskTexturesChanged;
                TaskTexturesChanged(dtsk.TextureSet, null);
            }
        }

        public void ClearTasks()
        {
            List<GameTask> toRemove = new(_gameTasks);
            foreach(GameTask t in toRemove)
            {
                if(_gameTasks.Remove(t) && t is DisplayTask dtsk)
                {
                    dtsk.OnTextureSetChanged -= TaskTexturesChanged;
                    TaskTexturesChanged(dtsk.TextureSet, null);
                }
            }
        }

        #endregion

        public void LoadLandtable(LandTable table)
        {
            ClearLandtable();
            CurrentLandTable = table;
            CurrentLandTable.BufferLandtable();
        }

        public void ClearLandtable()
        {
            // TODO debuffer the buffered attaches here!

            CurrentLandTable = null;
        }

        #region Texture handling

        private void TaskTexturesChanged(TextureSet oldSet, TextureSet newSet)
            => TaskTexturesChanged(oldSet, newSet, false);

        private void TaskTexturesChanged(TextureSet oldSet, TextureSet newSet, bool manual)
        {
            // Removing the old texture set
            if(oldSet != null)
            {
                // this can only happen when texture was loaded manually
                if(!_textureSetCounts.TryGetValue(oldSet, out var found))
                    throw new InvalidOperationException("Texture was not manually added!");

                if(manual)
                {
                    if(found.manual)
                        found.manual = false;
                    else
                        throw new InvalidOperationException("Texture was not manually added!");
                }

                if(found.count == 1 && !found.manual)
                {
                    _textureSets.Remove(oldSet);
                    _textureSetCounts.Remove(oldSet);
                    _bufferingBridge.InternalDebufferTextureSet(oldSet);
                }
                else
                {
                    found.count--;
                    _textureSetCounts[oldSet] = found;
                }
            }

            // adding new texture set
            if(newSet != null)
            {
                if(_textureSetCounts.TryGetValue(newSet, out var found))
                {
                    if(manual)
                    {
                        if(found.manual)
                            throw new InvalidOperationException("Texture was already manually added!");
                        else
                            found.manual = true;
                    }

                    found.count++;
                    _textureSetCounts[newSet] = found;
                }
                else
                {
                    _textureSets.Add(newSet);
                    _textureSetCounts.Add(newSet, (1, manual));
                    _bufferingBridge.InternalBufferTextureSet(newSet);
                }
            }
        }

        public void AddManualTextureSet(TextureSet textureSet)
            => TaskTexturesChanged(null, textureSet, true);

        public void RemoveManualTextureSet(TextureSet textureSet)
            => TaskTexturesChanged(textureSet, null, true);

        #endregion
    }
}
