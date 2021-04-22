using SATools.SAArchive;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System.Collections.Generic;

namespace SATools.SAModel.Graphics
{
    public abstract class GameTask
    {

        public string Name { get; }

        public GameTask(string name)
        {
            Name = name;
        }

        public virtual void Start()
        {

        }

        public virtual void Update(double delta, double time)
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
    /// The base class for when displaying a model
    /// </summary>
    public class DisplayTask : GameTask
    {
        internal delegate void TextureSetChanged(TextureSet oldSet, TextureSet newSet);
        internal event TextureSetChanged OnTextureSetChanged;

        private TextureSet _textureSet;

        public NJObject Model { get; }

        public TextureSet TextureSet
        {
            get => _textureSet;
            set
            {
                var oldSet = _textureSet;
                _textureSet = value;
                OnTextureSetChanged?.Invoke(oldSet, value);
            }
        }

        public DisplayTask(NJObject obj, TextureSet textureSet, string name = null) : base(string.IsNullOrWhiteSpace(name) ? obj.Name : name)
        {
            Model = obj;
            TextureSet = textureSet;
        }
    }

    /// <summary>
    /// Used for debugging animations
    /// </summary>
    public class DebugTask : DisplayTask
    {
        /// <summary>
        /// Loaded motions
        /// </summary>
        public List<Motion> Motions;

        /// <summary>
        /// Current running motion
        /// </summary>
        public int MotionIndex { get; set; }

        /// <summary>
        /// Frames per second
        /// </summary>
        public float AnimationSpeed { get; set; } = 1;

        public float AnimationTimestamp { get; set; }

        public DebugTask(NJObject obj, TextureSet textureSet, string name = null) : base(obj, textureSet, name)
        {
            Motions = new();
        }

        public void UpdateAnim(double delta)
        {
            if(Model == null || Motions.Count == 0)
                return;

            Motion motion = Motions[MotionIndex];

            AnimationTimestamp += (float)(delta * AnimationSpeed * motion.PlaybackSpeed);
            AnimationTimestamp %= motion.Frames - 1;

            NJObject[] models = Model.GetObjects();
            for(int i = 0; i < models.Length; i++)
            {
                if(motion.Keyframes.ContainsKey(i))
                {
                    NJObject mdl = models[i];
                    if(!mdl.Animate)
                        continue;
                    Frame frame = motion.Keyframes[i].GetFrameAt(AnimationTimestamp);
                    if(frame.position.HasValue)
                        mdl.Position = frame.position.Value;
                    if(frame.rotation.HasValue)
                        mdl.Rotation = frame.rotation.Value;
                    if(frame.scale.HasValue)
                        mdl.Scale = frame.scale.Value;
                }
            }
        }

        public override void Update(double delta, double time)
        {
            base.Update(delta, time);
            UpdateAnim(delta);
        }
    }
}
