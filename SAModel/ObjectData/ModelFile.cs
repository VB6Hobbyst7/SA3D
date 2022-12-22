using Reloaded.Memory.Streams;
using SATools.SACommon;
using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// Stores a hierarchy of NJS_Objects and Attaches
    /// </summary>
    public class ModelFile
    {
        #region File Headers
        /// <summary>
        /// SA1MDL file header; "SA1MDL"
        /// </summary>
        private const ulong SA1MDL = 0x4C444D314153u;

        /// <summary>
        /// SA2MDL file header; "SA2MDL"
        /// </summary>
        private const ulong SA2MDL = 0x4C444D324153u;

        /// <summary>
        /// SA2BMDL file header; "SA2BMDL"
        /// </summary>
        private const ulong SA2BMDL = 0x4C444D42324153u;

        /// <summary>
        /// BFMDL file header; "BFMDL"
        /// </summary>
        private const ulong BFMDL = 0x4C444D4642u;

        /// <summary>
        /// Header mask 
        /// </summary>
        private const ulong HeaderMask = ~((ulong)0xFF << 56);

        /// <summary>
        /// Current file version
        /// </summary>
        private const ulong CurrentVersion = 3;

        /// <summary>
        /// <see cref="SA1MDL"/> with version integrated
        /// </summary>
        private const ulong SA1MDLVer = SA1MDL | (CurrentVersion << 56);

        /// <summary>
        /// <see cref="SA2MDL"/> with version integrated
        /// </summary>
        private const ulong SA2MDLVer = SA2MDL | (CurrentVersion << 56);

        /// <summary>
        /// <see cref="SA2BMDL"/> with version integrated
        /// </summary>
        private const ulong SA2BMDLVer = SA2BMDL | (CurrentVersion << 56);

        /// <summary>
        /// BFMDL with version integrated
        /// </summary>
        private const ulong BFMDLVer = BFMDL | (CurrentVersion << 56);

        /// <summary>
        /// NJ header
        /// </summary>
        private const ushort NJ = (ushort)0x4A4Eu;

        /// <summary>
        /// GJ Header
        /// </summary>
        private const ushort GJ = (ushort)0x4A47u;

        /// <summary>
        /// NJ/GJ Chunk model header part
        /// </summary>
        private const ushort CM = (ushort)0x4D43u;

        /// <summary>
        /// NJ/GJ basic model header part
        /// </summary>
        private const ushort BM = (ushort)0x4D42u;

        /// <summary>
        /// NJ/GJ texture list header part
        /// </summary>
        private const ushort TL = (ushort)0x4C54u;

        public const string FileFilter = "Ninja Model (*.sa1mdl;*.sa2mdl;*.sa2bmdl;*.nj;*.gj)|*.sa1mdl;*.sa2mdl;*.sa2bmdl;*.nj;*.gj";

        #endregion

        /// <summary>
        /// Whether the file is an NJ binary
        /// </summary>
        public bool NJFile { get; }

        /// <summary>
        /// Attach format of the file
        /// </summary>
        public AttachFormat Format { get; }

        /// <summary>
        /// Hierarchy tip of the file
        /// </summary>
        public ObjectNode Model { get; }

        /// <summary>
        /// Animations from files references in the meta data
        /// </summary>
        public ReadOnlyCollection<Motion> Animations { get; }

        /// <summary>
        /// Meta data of the file
        /// </summary>
        public MetaData MetaData { get; }

        private ModelFile(AttachFormat format, ObjectNode model, Motion[] animations, MetaData metaData, bool nj)
        {
            Format = format;
            Model = model;
            Animations = new ReadOnlyCollection<Motion>(animations);
            MetaData = metaData;
            NJFile = nj;
        }

        /// <summary>
        /// Reads a model file from 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ModelFile? Read(string filename) 
            => Read(File.ReadAllBytes(filename), filename);

        /// <summary>
        /// Reads a model file from a byte source and its file path (for relative located files) <br/>
        /// Returns null if the modelfile couldnt be read
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="filename">File path of the read file. Used if the model uses more files outside of the byte source</param>
        /// <returns></returns>
        public static ModelFile? Read(byte[] source, string? filename = null)
        {
            PushBigEndian(false);

            AttachFormat? format = null;
            ObjectNode model;
            Dictionary<uint, Attach> attaches = new();
            List<Motion> Animations = new();
            MetaData metaData = new();
            bool nj = false;

            ushort NJMagic = source.ToUInt16(0);
            if (NJMagic == NJ || NJMagic == GJ)
            {
                NJMagic = source.ToUInt16(0x2);

                uint ninjaOffset = 8u;
                bool fileEndian = source.CheckBigEndianInt32(0x8u);
            texlistRetry:

                switch (NJMagic)
                {
                    case BM:
                        format = AttachFormat.BASIC;
                        break;
                    case CM:
                        format = AttachFormat.CHUNK;
                        break;
                    case TL:
                        uint POF0Offset = source.ToUInt32(0x4) + 0x8;
                        uint POF0Size = source.ToUInt32(POF0Offset + 0x4);
                        uint texListOffset = POF0Offset + POF0Size + 0x8;
                        ninjaOffset = texListOffset + 0x8;

                        NJMagic = source.ToUInt16(texListOffset + 0x2);

                        PushBigEndian(fileEndian);

                        //Get Texture Listings for if that is ever implemented
                        uint texCount = source.ToUInt32(0xC);
                        uint texOffset = 0;
                        List<string> texNames = new();

                        for (int i = 0; i < texCount; i++)
                        {
                            uint textAddress = source.ToUInt32(texOffset + 0x10) + 0x8;
                            texNames.Add(source.GetCString(textAddress, System.Text.Encoding.UTF8));
                            texOffset += 0xC;
                        }
                        PopEndian();

                        goto texlistRetry;
                }

                if (format == null)
                    throw new NullReferenceException("Attach format is null");

                PushBigEndian(fileEndian);

                // the addresses start 8 bytes ahead, and since we always subtract the image base from the addresses,
                // we have to add them this time, so we invert the 8 to add 8 by subtracting
                model = ObjectNode.Read(source, ninjaOffset, ~(ninjaOffset - 1), format.Value, false, new(), attaches);
                nj = true;
            }
            else
            {
                // checking for mdl format
                ulong header8 = source.ToUInt64(0) & HeaderMask;
                switch (header8)
                {
                    case SA1MDL:
                        format = AttachFormat.BASIC;
                        break;
                    case SA2MDL:
                        format = AttachFormat.CHUNK;
                        break;
                    case SA2BMDL:
                        format = AttachFormat.GC;
                        break;
                    case BFMDL:
                        format = AttachFormat.Buffer;
                        break;
                    default:
                        return null;
                        //throw new InvalidDataException("File is not a valid model file");
                }

                // checking the version
                byte version = source[7];
                if (version > CurrentVersion)
                {
                    PopEndian();
                    return null;
                    //throw new FormatException("Not a valid SA1LVL/SA2LVL file.");
                }

                metaData = MetaData.Read(source, version, true);
                Dictionary<uint, string> labels = new(metaData.Labels);

                model = ObjectNode.Read(source, source.ToUInt32(8), 0, format.Value, false, labels, attaches);

                // reading animations
                if (filename != null)
                {
                    string path = Path.GetDirectoryName(filename) ?? throw new InvalidOperationException("File path invalid");
                    try
                    {
                        foreach (string item in metaData.AnimFiles)
                        {
                            Motion? motion = Motion.ReadFile(Path.Combine(path, item), model.CountAnimated());
                            if (motion == null)
                                continue;
                            Animations.Add(motion);
                        }
                    }
                    catch
                    {
                        Animations.Clear();
                    }
                }

            }

            PopEndian();
            return new(format.Value, model, Animations.ToArray(), metaData, nj);
        }

        /// <summary>
        /// Saves the model file to a selected path
        /// </summary>
        /// <param name="outputPath">The path of the file</param>
        /// <param name="DX">Whether the file is for sadx</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        public void SaveToFile(string outputPath, bool NJ)
        {
            byte[] data = Write(Format, NJ, Model, MetaData);
            File.WriteAllBytes(outputPath, data);
        }

        /// <summary>
        /// Writes a model hierarchy (without meta data) to a binary file
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="outputPath">The path of the file</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        public static void WriteToFile(string outputPath, AttachFormat format, bool NJ, ObjectNode model)
            => WriteToFile(outputPath, format, NJ, model, new MetaData());

        /// <summary>
        /// Writes a model hierarchy to a binary file
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="outputPath">The path of the file</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        /// <param name="author">Author of the file</param>
        /// <param name="description">Description of the files contents</param>
        /// <param name="metadata">Other meta data</param>
        /// <param name="animFiles">Animation file paths</param>
        public static void WriteToFile(string outputPath, AttachFormat format, bool NJ, ObjectNode model, MetaData metaData)
        {
            File.WriteAllBytes(outputPath, Write(format, NJ, model, metaData));
        }

        /// <summary>
        /// Writes a model hierarchy (without meta data) to a stream and returns the contents
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="NJFile">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        public static byte[] Write(AttachFormat format, bool NJFile, ObjectNode model)
            => Write(format, NJFile, model, new MetaData());

        /// <summary>
        /// Writes a model hierarchy to stream and returns the contents
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="NJFile">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        /// <param name="author">Author of the file</param>
        /// <param name="description">Description of the files contents</param>
        /// <param name="metadata">Other meta data</param>
        /// <param name="animFiles">Animation file paths</param>
        public static byte[] Write(AttachFormat format, bool NJFile, ObjectNode model, MetaData metaData)
        {
            using ExtendedMemoryStream stream = new();
            EndianWriter writer = new(stream);
            uint imageBase = 0;

            if (NJFile)
            {
                writer.WriteUInt16(NJ);
                switch (format)
                {
                    case AttachFormat.BASIC:
                        writer.WriteUInt32(BM);
                        break;
                    case AttachFormat.CHUNK:
                        writer.WriteUInt32(CM);
                        break;
                    default:
                        throw new ArgumentException($"Attach format {format} not supported for NJ binaries");
                }
                writer.WriteUInt32(0); // file length placeholder
                imageBase = ~(8u);
            }
            else
            {
                ulong header = 0;
                header = format switch
                {
                    AttachFormat.BASIC => SA1MDLVer,
                    AttachFormat.CHUNK => SA2MDLVer,
                    AttachFormat.GC => SA2BMDLVer,
                    AttachFormat.Buffer => BFMDLVer,
                    _ => throw new ArgumentException($"Attach format {format} not supported for SAMDL files"),
                };
                writer.WriteUInt64(header);
                writer.WriteUInt32(0x10);
                writer.WriteUInt32(0); // labels placeholder
            }

            Dictionary<string, uint> labels = new();
            model.WriteHierarchy(writer, imageBase, false, format == AttachFormat.Buffer, labels);

            if (NJFile)
            {
                // replace size
                writer.Stream.Seek(4, SeekOrigin.Begin);
                writer.WriteUInt32((uint)writer.Stream.Length);
                writer.Stream.Seek(0, SeekOrigin.End);
            }
            else
            {
                metaData.Write(writer, labels);
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Writes a model as an NJA file
        /// </summary>
        /// <param name="outputPath">Path to write the file to (extension will be forced to .NJA)</param>
        /// <param name="DX">Whether the file is for SADX</param>
        /// <param name="model">Top level object to write</param>
        /// <param name="textures">Texture list</param>
        public static void WriteNJA(string outputPath, bool DX, ObjectNode model, string[]? textures = null)
        {
            ObjectNode[] objects = model.GetObjects();
            Attach[] attaches = model.GetAttaches();

            AttachFormat fmt;
            if (attaches.Length > 0)
            {
                fmt = attaches[0].Format;
                foreach (Attach atc in attaches)
                    if (fmt != atc.Format)
                        throw new InvalidCastException("Not all attaches are of the same type!");
                if (fmt == AttachFormat.Buffer)
                    throw new InvalidCastException("All attaches are of buffer format! Can't decide what format to write");
            }

            outputPath = Path.ChangeExtension(outputPath, ".NJA");
            using TextWriter writer = File.CreateText(outputPath);
            List<string> labels = new();
            foreach (var atc in attaches)
            {
                atc.WriteNJA(writer, DX, labels, textures);
            }

            writer.WriteLine("OBJECT_START");
            writer.WriteLine();

            foreach (ObjectNode obj in objects.Reverse())
            {
                obj.WriteNJA(writer, labels);
            }

            writer.WriteLine("OBJECT_END");
            writer.WriteLine();

            writer.WriteLine();
            writer.WriteLine("DEFAULT_START");
            writer.WriteLine();

            writer.WriteLine("#ifndef DEFAULT_OBJECT_NAME");
            writer.Write("#define DEFAULT_OBJECT_NAME ");
            writer.WriteLine(model.Name);
            writer.WriteLine("#endif");

            writer.WriteLine();
            writer.WriteLine("DEFAULT_END");
            writer.WriteLine();
        }

        public override string ToString()
            => $"{(NJFile ? "" : "NJ")} Modelfile - {Format}";
    }
}