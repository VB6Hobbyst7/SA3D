using System;
using System.Collections.Generic;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ObjData.Animation
{
    /// <summary>
    /// Level geometry animation (only used in sa1)
    /// </summary>
    public class LandEntryMotion
    {
        /// <summary>
        /// Size in bytes
        /// </summary>
        public static uint Size => 18;

        /// <summary>
        /// First keyframe / Keyframe to start the animation at
        /// </summary>
        public float Frame { get; set; }

        /// <summary>
        /// Keyframes traversed per frame-update / Animation Speed
        /// </summary>
        public float Step { get; set; }

        /// <summary>
        /// Last keyframe / Length of the animation
        /// </summary>
        public float MaxFrame { get; set; }

        /// <summary>
        /// Model that is being animated
        /// </summary>
        public NJObject Model { get; set; }

        /// <summary>
        /// Animation
        /// </summary>
        public Motion Motion { get; set; }

        /// <summary>
        /// Texture list
        /// </summary>
        public uint TexListPtr { get; set; }

        /// <summary>
        /// Creates a new geometry animation
        /// </summary>
        /// <param name="frame">Start frame</param>
        /// <param name="step">Animation speed</param>
        /// <param name="maxFrame"></param>
        /// <param name="model"></param>
        /// <param name="motion"></param>
        /// <param name="texListPtr"></param>
        public LandEntryMotion(float frame, float step, float maxFrame, NJObject model, Motion motion, uint texListPtr)
        {
            Frame = frame;
            Step = step;
            MaxFrame = maxFrame;
            Model = model;
            Motion = motion;
            TexListPtr = texListPtr;
        }

        /// <summary>
        /// Reads a geometry animation from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the geometry animation is located</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="format">Attach format</param>
        /// <param name="DX">Whether the animation is for sadx</param>
        /// <param name="labels">C struct labels</param>
        /// <param name="models">Models that have already been read</param>
        /// <param name="attaches">Attaches that have already been read</param>
        /// <returns></returns>
        public static LandEntryMotion Read(byte[] source, uint address, uint imageBase, AttachFormat format, bool DX,
            Dictionary<uint, string> labels, Dictionary<uint, ModelData.Attach> attaches)
        {
            float frame = source.ToSingle(address);
            float step = source.ToSingle(address + 4);
            float maxFrame = source.ToSingle(address + 8);

            uint modelAddress = source.ToUInt32(address + 0xC) - imageBase;
            NJObject model = NJObject.Read(source, modelAddress, imageBase, format, DX, labels, attaches);

            uint motionAddress = source.ToUInt32(address + 0x10) - imageBase;
            Motion motion = Motion.Read(source, ref motionAddress, imageBase, (uint)model.Count(), labels);

            uint texListPtr = source.ToUInt32(address + 0x14);

            return new LandEntryMotion(frame, step, maxFrame, model, motion, texListPtr);
        }

        /// <summary>
        /// Writes the landentrymotion to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="labels">C struct labels</param>
        public void Write(EndianMemoryStream writer, Dictionary<string, uint> labels)
        {
            writer.WriteSingle(Frame);
            writer.WriteSingle(Step);
            writer.WriteSingle(MaxFrame);
            writer.WriteUInt32(labels.ContainsKey(Model.Name) ? labels[Model.Name] : throw new NullReferenceException($"Model \"{Model.Name}\" has not been written yet / cannot be found in labels!"));
            writer.WriteUInt32(labels.ContainsKey(Motion.Name) ? labels[Motion.Name] : throw new NullReferenceException($"Motion \"{Motion.Name}\" has not been written yet / cannot be found in labels!"));
            writer.WriteUInt32(TexListPtr);
        }
    }
}
