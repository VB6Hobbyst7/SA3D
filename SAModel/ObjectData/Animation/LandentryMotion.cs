using SATools.SACommon;
using SATools.SAModel.ModelData;
using System;
using System.Collections.Generic;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ObjectData.Animation
{
    /// <summary>
    /// Level geometry animation (only used in sa1)
    /// </summary>
    public class LandEntryMotion
    {
        /// <summary>
        /// Size in bytes
        /// </summary>
        public static uint Size => 24;

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
        public Node Model { get; set; }

        /// <summary>
        /// Animation
        /// </summary>
        public Action MotionAction { get; set; }

        /// <summary>
        /// Texture list
        /// </summary>
        public uint TexListPtr { get; set; }

        /// <summary>
        /// Creates a new geometry animation
        /// </summary>
        /// <param name="frame">Start frame</param>
        /// <param name="step">Animation speed</param>
        public LandEntryMotion(float frame, float step, float maxFrame, Node model, Motion motion, uint texListPtr)
            : this(frame, step, maxFrame, model, new Action(model, motion), texListPtr) { }

        /// <summary>
        /// Creates a new geometry animation
        /// </summary>
        /// <param name="frame">Start frame</param>
        /// <param name="step">Animation speed</param>
        /// <param name="maxFrame"></param>
        /// <param name="model"></param>
        /// <param name="motion"></param>
        /// <param name="texListPtr"></param>
        public LandEntryMotion(float frame, float step, float maxFrame, Node model, Action action, uint texListPtr)
        {
            Frame = frame;
            Step = step;
            MaxFrame = maxFrame;
            Model = model;
            MotionAction = action;
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
            Dictionary<uint, string> labels, Dictionary<uint, Attach> attaches)
        {
            float frame = source.ToSingle(address);
            float step = source.ToSingle(address + 4);
            float maxFrame = source.ToSingle(address + 8);

            uint modelAddress = source.ToUInt32(address + 0xC) - imageBase;
            Node model = Node.Read(source, modelAddress, imageBase, format, DX, labels, attaches);

            uint motionAddress = source.ToUInt32(address + 0x10) - imageBase;
            Action action = Action.Read(source, motionAddress, imageBase, format, DX, labels, attaches);

            uint texListPtr = source.ToUInt32(address + 0x14);

            return new LandEntryMotion(frame, step, maxFrame, model, action, texListPtr);
        }

        /// <summary>
        /// Writes the landentrymotion to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="labels">C struct labels</param>
        public void Write(EndianWriter writer, uint imageBase, bool DX, bool writeBuffer, Dictionary<string, uint> labels)
        {
            uint mdlAddr = Model.Write(writer, imageBase, labels);
            uint actionAddr = MotionAction.Write(writer, imageBase, DX, writeBuffer, labels);

            writer.WriteSingle(Frame);
            writer.WriteSingle(Step);
            writer.WriteSingle(MaxFrame);
            writer.WriteUInt32(mdlAddr);
            writer.WriteUInt32(actionAddr);
            writer.WriteUInt32(TexListPtr);
        }

        public void Write(EndianWriter writer, Dictionary<Action, uint> actionAddresses, Dictionary<string, uint> labels)
        {
            writer.WriteSingle(Frame);
            writer.WriteSingle(Step);
            writer.WriteSingle(MaxFrame);

            if (!labels.TryGetValue(Model.Name, out uint mdlAddress))
                throw new NullReferenceException($"Model \"{Model.Name}\" has not been written yet / cannot be found in labels!");
            writer.WriteUInt32(mdlAddress);

            if (!actionAddresses.TryGetValue(MotionAction, out uint actionAddress))
                throw new NullReferenceException($"Model \"{Model.Name}\" has not been written yet / cannot be found in labels!");
            writer.WriteUInt32(actionAddress);

            writer.WriteUInt32(TexListPtr);
        }
    }
}
