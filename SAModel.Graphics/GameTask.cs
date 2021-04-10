using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;

namespace SATools.SAModel.Graphics
{
    public abstract class GameTask
    {
        public NJObject obj;
        // texture list
        public virtual void Start()
        {

        }

        public virtual void Update(double time)
        {

        }

        public virtual void Display()
        {

        }

        public virtual void End()
        {

        }

    }

    /// <summary>
    /// An empty task, which is just used to display a model
    /// </summary>
    public class DisplayTask : GameTask
    {
        public Motion motion;
        public float animSpeed;

        public DisplayTask(NJObject obj)
        {
            this.obj = obj;
        }

        public void UpdateAnim(double time)
        {
            if(motion == null)
                return;

            float f = (float)(time % (motion.Frames - 1));

            NJObject[] models = obj.GetObjects();
            for(int i = 0; i < models.Length; i++)
            {
                if(motion.Keyframes.ContainsKey(i))
                {
                    NJObject mdl = models[i];
                    if(!mdl.Animate)
                        continue;
                    Frame frame = motion.Keyframes[i].GetFrameAt(f);
                    if(frame.position.HasValue)
                        mdl.Position = frame.position.Value;
                    if(frame.rotation.HasValue)
                        mdl.Rotation = frame.rotation.Value;
                    if(frame.scale.HasValue)
                        mdl.Scale = frame.scale.Value;
                }
            }
        }

        public override void Update(double time)
        {
            base.Update(time);
            UpdateAnim(time * animSpeed);
        }
    }
}
