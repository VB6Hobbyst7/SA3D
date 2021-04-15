using SATools.SAArchive;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SATools.SAModel.Graphics
{
    public class Scene
    {
        public delegate void OnUpdate(double delta);
        public event OnUpdate OnUpdateEvent;

        public readonly Camera cam;
        public double time = 0;

        public TextureSet LandTextureSet { get; set; }

        public readonly List<GameTask> objects = new();
        public readonly List<LandEntry> geometry = new();
        public readonly List<ModelData.Attach> attaches = new();
        public readonly List<ModelData.Attach> weightedAttaches = new();

        public LandEntry[] VisualGeometry
        {
            get => geometry.Where(x => x.SurfaceFlags.HasFlag(SurfaceFlags.Visible)).ToArray();
        }

        public LandEntry[] CollisionGeometry
        {
            get => geometry.Where(x => x.SurfaceFlags.IsCollision()).ToArray();
        }

        public Scene(Camera cam)
        {
            this.cam = cam;
        }

        public void AddDisplayTask(DisplayTask task)
        {
            objects.Add(task);
            NJObject[] objs = task.Model.GetObjects();
            if(task.Model.HasWeight)
            {
                foreach(NJObject obj in objs)
                {
                    if(obj.Attach == null)
                        continue;
                    if(!weightedAttaches.Contains(obj.Attach))
                    {
                        weightedAttaches.Add(obj.Attach);
                        obj.Attach.GenBufferMesh(true);
                    }
                }
            }
            else
            {
                foreach(NJObject obj in objs)
                {
                    if(obj.Attach == null)
                        continue;
                    if(!attaches.Contains(obj.Attach))
                    {
                        attaches.Add(obj.Attach);
                        obj.Attach.GenBufferMesh(true);
                    }
                }
            }

        }

        public void LoadLandtable(ObjData.LandTable table)
        {
            foreach(LandEntry le in table.Geometry)
            {
                geometry.Add(le);
                if(!attaches.Contains(le.Attach))
                {
                    attaches.Add(le.Attach);
                    le.Attach.GenBufferMesh(true);
                }
            }
        }

        public void Update(double delta)
        {
            OnUpdateEvent.Invoke(delta);
            time += delta;
            foreach(GameTask tsk in objects)
            {
                tsk.Update(delta, time);
            }
        }
    }
}
