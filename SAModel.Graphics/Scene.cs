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
        public float timelineSpeed = 1;

        public readonly List<GameTask> objects = new List<GameTask>();
        public readonly List<LandEntry> geometry = new List<LandEntry>();
        public readonly List<ModelData.Attach> attaches = new List<ModelData.Attach>();
        public readonly List<ModelData.Attach> weightedAttaches = new List<ModelData.Attach>();

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

        public void LoadModelFile(ObjData.ModelFile file)
        {
            objects.Add(new DisplayTask(file.Model));
            NJObject[] objs = file.Model.GetObjects();
            if(file.Model.HasWeight)
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

        public void LoadModelFile(ObjData.ModelFile file, Motion motion, float animSpeed)
        {
            LoadModelFile(file);
            DisplayTask tsk = objects.Last() as DisplayTask;

            int mdlCount = tsk.obj.GetObjects().Length;
            if(motion.ModelCount > mdlCount)
                throw new ArgumentException($"Motion not compatible with model! \n Motion model count: {motion.ModelCount} \n Model count: {mdlCount}");
            tsk.motion = motion;
            tsk.animSpeed = animSpeed;
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
                tsk.Update(time);
            }
        }
    }
}
