using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ObjData.Animation
{
    /// <summary>
    /// Animation data of a model
    /// </summary>
    public class Motion
    {
        #region headers

        /// <summary>
        /// SAANIM file header; "SAANIM"
        /// </summary>
        private const ulong SAANIM = 0x4D494E414153u;

        /// <summary>
        /// Header mask
        /// </summary>
        private const ulong HeaderMask = ~((ulong)0xFF << 56);

        /// <summary>
        /// Current file version
        /// </summary>
        private const ulong CurrentVersion = 1;

        /// <summary>
        /// SAANIM header with integrated version number
        /// </summary>
        private const ulong SAANIMVer = SAANIM | (CurrentVersion << 56);

        /// <summary>
        /// Ninja motion file header
        /// </summary>
        private const uint NMDM = 0x4D444D4Eu;

        #endregion

        /// <summary>
        /// Motion name / C struct label
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Maximum number of frames
        /// </summary>
        public uint Frames { get; private set; }

        /// <summary>
        /// Size of the model hierarchy to animate
        /// </summary>
        public uint ModelCount { get; }

        /// <summary>
        /// Intepolation mode between keyframes
        /// </summary>
        public InterpolationMode InterpolationMode { get; set; }

        /// <summary>
        /// Whether to use 16 bit BAMS for the rotation keyframes
        /// </summary>
        public bool ShortRot { get; set; }

        /// <summary>
        /// Keyframes based on their model id
        /// </summary>
        public Dictionary<int, Keyframes> Keyframes = new();

        /// <summary>
        /// Creates a new empty motion
        /// </summary>
        /// <param name="frameCount"></param>
        /// <param name="modelCount"></param>
        /// <param name="interpolationMode"></param>
        public Motion(uint frameCount, uint modelCount, InterpolationMode interpolationMode)
        {
            Frames = frameCount;
            ModelCount = modelCount;
            InterpolationMode = interpolationMode;
            Keyframes = new Dictionary<int, Keyframes>();

            Name = "animation_" + GenerateIdentifier();
        }

        /// <summary>
        /// Updates the frame count
        /// </summary>
        public void UpdateFrameCount()
        {
            Frames = 0;
            foreach(var k in Keyframes.Values)
            {
                uint count = k.KeyframeCount;
                if(count > Frames)
                    Frames = count;
            }
        }

        /// <summary>
        /// Reads a motion from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the motion is located</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="modelCount">Model count / hierarchy size</param>
        /// <param name="labels">C struct labels</param>
        /// <param name="shortrot">Whether the rotations are 16bit</param>
        /// <returns></returns>
        public static Motion Read(byte[] source, ref uint address, uint imageBase, uint modelCount, Dictionary<uint, string> labels, bool shortrot = false)
        {
            string name = labels?.ContainsKey(address) == true ? labels[address] : "animation_" + address.ToString("X8");
            uint Frames = source.ToUInt32(address + 4);
            AnimFlags animtype = (AnimFlags)source.ToUInt16(address + 8);

            ushort tmp = source.ToUInt16(address + 10);
            InterpolationMode mode = (InterpolationMode)((tmp >> 6) & 0x3);
            int channels = (tmp & 0xF);

            Motion result = new(Frames, modelCount, mode)
            {
                Name = name,
                ShortRot = shortrot
            };

            uint tmpAddr = source.ToUInt32(address) - imageBase;
            for(int i = 0; i < modelCount; i++)
            {
                Keyframes kf = Animation.Keyframes.Read(source, ref tmpAddr, imageBase, channels, animtype, shortrot);

                if(kf.HasKeyframes)
                    result.Keyframes.Add(i, kf);
            }

            return result;
        }

        /// <summary>
        /// Reads a motion from a file <br/>
        /// Returns null if file is not valid
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <param name="modelCount">Model count (can be left untouched, unless file version is 0)</param>
        /// <returns></returns>
        public static Motion ReadFile(string path, int modelCount = -1)
            => ReadFile(File.ReadAllBytes(path), modelCount);

        /// <summary>
        /// Reads a motion off a byte array (from a file) <br/>
        /// Returns null if file is not valid
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="modelCount">Model count (can be left untouched, unless file version is 0)</param>
        /// <returns></returns>
        public static Motion ReadFile(byte[] source, int modelCount = -1)
        {
            PushBigEndian(false);
            Motion result = null;

            if(source.ToUInt32(0) == NMDM)
            {
                if(modelCount < 0)
                    throw new ArgumentException("Cannot open NJM animations without a model!");

                PushBigEndian(source.CheckBigEndianInt32(0xC));
                // framecount. as long as that one is not bigger than 65,535 or 18 minutes of animation at 60fps, we good
                uint aniaddr = 8;
                result = Read(source, ref aniaddr, ~7u, (uint)modelCount, null, true);
                PopEndian();
            }
            else if((source.ToUInt64(0) & HeaderMask) == SAANIM)
            {

                byte version = source[7];
                if(version > CurrentVersion)
                {
                    PopEndian();
                    throw new FormatException("Not a valid SAANIM file.");
                }

                uint aniaddr = source.ToUInt32(8);
                Dictionary<uint, string> labels = new();
                uint tmpaddr = BitConverter.ToUInt32(source, 0xC);
                if(tmpaddr != 0)
                    labels.Add(aniaddr, source.GetCString(tmpaddr));
                if(version > 0)
                    modelCount = BitConverter.ToInt32(source, 0x10);
                else if(modelCount == -1)
                {
                    PopEndian();
                    throw new NotImplementedException("Cannot open version 0 animations without a model!");
                }

                result = Read(source, ref aniaddr, 0, (uint)(modelCount), labels, false);

            }

            PopEndian();
            return result;
        }

        /// <summary>
        /// Writes the motion to a stream
        /// </summary>
        /// <param name="writer">Ouput stream</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="labels">C struct label</param>
        public uint Write(EndianMemoryStream writer, uint imageBase, Dictionary<string, uint> labels)
        {
            AnimFlags type = 0;
            foreach(Keyframes kf in Keyframes.Values)
                type |= kf.Type;

            int channels = type.ChannelCount();

            (uint addr, uint count)[][] keyFrameLocations = new (uint addr, uint count)[ModelCount][];

            for(int i = 0; i < ModelCount; i++)
            {
                if(!Keyframes.ContainsKey(i))
                {
                    keyFrameLocations[i] = new (uint addr, uint count)[channels];
                }
                else
                {
                    keyFrameLocations[i] = Keyframes[i].Write(writer, imageBase, channels, type, ShortRot);
                }
            }

            uint keyframesAddr = (uint)writer.Stream.Position + imageBase;

            foreach(var kf in keyFrameLocations)
            {
                for(int i = 0; i < kf.Length; i++)
                    writer.WriteUInt32(kf[i].addr);
                for(int i = 0; i < kf.Length; i++)
                    writer.WriteUInt32(kf[i].count);
            }

            UpdateFrameCount();

            uint address = (uint)writer.Stream.Position + imageBase;
            labels.Add(Name, address);

            writer.WriteUInt32(keyframesAddr);
            writer.WriteUInt32(Frames);
            writer.WriteUInt16((ushort)type);
            writer.WriteUInt16((ushort)((channels & 0xF) | (int)InterpolationMode << 6));
            return address;
        }

        /// <summary>
        /// Writes the motion to a file
        /// </summary>
        /// <param name="outputPath">Path to write the file to</param>
        public void WriteFile(string outputPath)
        {
            File.WriteAllBytes(outputPath, WriteFile());
        }

        /// <summary>
        /// Writes the motion to a byte array in format of a motion file
        /// </summary>
        /// <returns></returns>
        public byte[] WriteFile()
        {
            using(ExtendedMemoryStream stream = new())
            {
                LittleEndianMemoryStream writer = new(stream);

                writer.WriteUInt64(SAANIMVer);
                writer.WriteUInt32(0); // placeholders for motion address and name address
                writer.WriteUInt32(0x14);
                writer.WriteInt32((int)ModelCount | (ShortRot ? int.MinValue : 0));
                writer.Write(Encoding.UTF8.GetBytes(Name));
                writer.Write(new byte[1]);

                uint aniAddr = Write(writer, 0, new Dictionary<string, uint>());
                writer.Stream.Seek(8, SeekOrigin.Begin);
                writer.WriteUInt32(aniAddr);
                writer.Stream.Seek(0, SeekOrigin.End);

                return stream.ToArray();
            }
        }

        public override string ToString() => $"{Name} - {Frames}, {ModelCount} - {Keyframes.Count}";
    }
}
