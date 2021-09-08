using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.HelperExtensions;


namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// Meta data storage for both mdl and lvl files
    /// </summary>
    [Serializable]
    public class MetaData
    {
        /// <summary>
        /// Author of the file
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Description of the files contents
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// C struct labels (only for reading)
        /// </summary>
        public ReadOnlyDictionary<uint, string> Labels { get; private set; }

        /// <summary>
        /// Animation file paths
        /// </summary>
        public List<string> AnimFiles { get; }

        /// <summary>
        /// Morph file path
        /// </summary>
        public List<string> MorphFiles { get; }

        /// <summary>
        /// Other chunk blocks that have no mappings (for this library)
        /// </summary>
        public Dictionary<uint, byte[]> Other { get; set; }

        /// <summary>
        /// Creates a new empty set of meta data
        /// </summary>
        public MetaData()
        {
            AnimFiles = new List<string>();
            MorphFiles = new List<string>();
            Other = new Dictionary<uint, byte[]>();
            Labels = new ReadOnlyDictionary<uint, string>(new Dictionary<uint, string>());
        }

        /// <summary>
        /// Reads a set of meta data from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="version">File version</param>
        /// <param name="mdl">Whether the meta data is coming from an mdl file</param>
        /// <returns></returns>
        public static MetaData Read(byte[] source, int version, bool mdl)
        {
            MetaData result = new();

            uint tmpAddr = source.ToUInt32(0xC);
            Dictionary<uint, string> labels = new();
            switch(version)
            {
                case 0:
                    if(!mdl)
                        goto case 1;

                    // reading animation locations
                    if(tmpAddr != 0)
                    {
                        uint pathAddr = source.ToUInt32(tmpAddr);
                        while(pathAddr != uint.MaxValue)
                        {
                            result.AnimFiles.Add(source.GetCString(pathAddr));
                            tmpAddr += 4;
                            pathAddr = source.ToUInt32(tmpAddr);
                        }
                    }

                    tmpAddr = source.ToUInt32(0x10);
                    if(tmpAddr != 0)
                    {
                        uint pathAddr = source.ToUInt32(tmpAddr);
                        while(pathAddr != uint.MaxValue)
                        {
                            result.MorphFiles.Add(source.GetCString(pathAddr));
                            tmpAddr += 4;
                            pathAddr = source.ToUInt32(tmpAddr);
                        }
                    }

                    goto case 1;
                case 1:
                    if(mdl)
                        tmpAddr = source.ToUInt32(0x14);
                    if(tmpAddr == 0)
                        break;

                    // version 1 added labels
                    uint addr = source.ToUInt32(tmpAddr);
                    while(addr != uint.MaxValue)
                    {
                        labels.Add(addr, source.GetCString(source.ToUInt32(tmpAddr + 4)));
                        tmpAddr += 8;
                        addr = source.ToUInt32(tmpAddr);
                    }
                    break;
                case 2:
                case 3:
                    // version 2 onwards used "blocks" of meta data, 
                    // where version 3 refined the concept to make 
                    // a block use local addresses to that block

                    if(tmpAddr == 0)
                        break;
                    MetaType type = (MetaType)source.ToUInt32(tmpAddr);

                    while(type != MetaType.End)
                    {
                        uint blockSize = source.ToUInt32(tmpAddr + 4);
                        tmpAddr += 8;
                        uint nextMetaBlock = tmpAddr + blockSize;
                        uint pathAddr;

                        if(version == 2)
                        {

                            switch(type)
                            {
                                case MetaType.Label:
                                    while(source.ToInt64(tmpAddr) != -1)
                                    {
                                        labels.Add(source.ToUInt32(tmpAddr), source.GetCString(source.ToUInt32(tmpAddr + 4)));
                                        tmpAddr += 8;
                                    }
                                    break;
                                case MetaType.Animation:
                                    pathAddr = source.ToUInt32(tmpAddr);
                                    while(pathAddr != uint.MaxValue)
                                    {
                                        result.AnimFiles.Add(source.GetCString(pathAddr));
                                        tmpAddr += 4;
                                        pathAddr = source.ToUInt32(tmpAddr);
                                    }
                                    break;
                                case MetaType.Morph:
                                    pathAddr = source.ToUInt32(tmpAddr);
                                    while(pathAddr != uint.MaxValue)
                                    {
                                        result.MorphFiles.Add(source.GetCString(pathAddr));
                                        tmpAddr += 4;
                                        pathAddr = source.ToUInt32(tmpAddr);
                                    }
                                    break;
                                case MetaType.Author:
                                    result.Author = source.GetCString(tmpAddr);
                                    break;
                                case MetaType.Description:
                                    result.Description = source.GetCString(tmpAddr);
                                    break;
                            }
                        }
                        else
                        {
                            byte[] block = new byte[blockSize];
                            Array.Copy(source, tmpAddr, block, 0, blockSize);
                            uint blockAddr = 0;
                            switch(type)
                            {
                                case MetaType.Label:
                                    while(block.ToInt64(blockAddr) != -1)
                                    {
                                        labels.Add(block.ToUInt32(blockAddr),
                                            block.GetCString(block.ToUInt32(blockAddr + 4)));
                                        blockAddr += 8;
                                    }
                                    break;
                                case MetaType.Animation:
                                    pathAddr = block.ToUInt32(blockAddr);
                                    while(pathAddr != uint.MaxValue)
                                    {
                                        result.AnimFiles.Add(block.GetCString(pathAddr));
                                        blockAddr += 4;
                                        pathAddr = block.ToUInt32(blockAddr);
                                    }
                                    break;
                                case MetaType.Morph:
                                    pathAddr = block.ToUInt32(blockAddr);
                                    while(pathAddr != uint.MaxValue)
                                    {
                                        result.MorphFiles.Add(block.GetCString(pathAddr));
                                        blockAddr += 4;
                                        pathAddr = block.ToUInt32(blockAddr);
                                    }
                                    break;
                                case MetaType.Author:
                                    result.Author = block.GetCString(0);
                                    break;
                                case MetaType.Description:
                                    result.Description = block.GetCString(0);
                                    break;
                                default:
                                    result.Other.Add((uint)type, block);
                                    break;
                            }
                        }

                        tmpAddr = nextMetaBlock;
                        type = (MetaType)source.ToUInt32(tmpAddr);
                    }
                    break;
            }
            result.Labels = new ReadOnlyDictionary<uint, string>(labels);

            return result;
        }

        /// <summary>
        /// Writes the meta data to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="labels">New set of labels</param>
        public void Write(EndianWriter writer, Dictionary<string, uint> labels)
        {
            // write meta data
            uint metaAddr = writer.Position;
            writer.Stream.Seek(0xC, SeekOrigin.Begin);
            writer.WriteUInt32(metaAddr);
            writer.Stream.Seek(0, SeekOrigin.End);

            void MetaHeader(MetaType type, List<byte> metaBytes)
            {
                writer.WriteUInt32((uint)type);
                writer.WriteUInt32((uint)metaBytes.Count);
                writer.Write(metaBytes.ToArray());
            }

            // labels
            if(labels.Count > 0)
            {
                List<byte> meta = new((labels.Count * 8) + 8);
                int straddr = (labels.Count * 8) + 8;
                List<byte> strbytes = new();
                foreach(KeyValuePair<string, uint> label in labels)
                {
                    meta.AddRange((label.Value).GetBytes());
                    meta.AddRange((straddr + strbytes.Count).GetBytes());
                    strbytes.AddRange(Encoding.UTF8.GetBytes(label.Key));
                    strbytes.Add(0);
                    strbytes.Align(4);
                }
                meta.AddRange((-1L).GetBytes());
                meta.AddRange(strbytes);
                MetaHeader(MetaType.Label, meta);
            }

            // animation files
            if(AnimFiles != null && AnimFiles.Count > 0)
            {
                List<byte> meta = new((AnimFiles.Count + 1) * 4);
                int straddr = (AnimFiles.Count + 1) * 4;
                List<byte> strbytes = new();
                for(int i = 0; i < AnimFiles.Count; i++)
                {
                    meta.AddRange((straddr + strbytes.Count).GetBytes());
                    strbytes.AddRange(Encoding.UTF8.GetBytes(AnimFiles[i]));
                    strbytes.Add(0);
                    strbytes.Align(4);
                }
                meta.AddRange((-1).GetBytes());
                meta.AddRange(strbytes);
                MetaHeader(MetaType.Animation, meta);
            }

            // morph files
            if(MorphFiles != null && MorphFiles.Count > 0)
            {
                List<byte> meta = new((MorphFiles.Count + 1) * 4);
                int straddr = (MorphFiles.Count + 1) * 4;
                List<byte> strbytes = new();
                for(int i = 0; i < MorphFiles.Count; i++)
                {
                    meta.AddRange((straddr + strbytes.Count).GetBytes());
                    strbytes.AddRange(Encoding.UTF8.GetBytes(MorphFiles[i]));
                    strbytes.Add(0);
                    strbytes.Align(4);
                }
                meta.AddRange((-1).GetBytes());
                meta.AddRange(strbytes);
                MetaHeader(MetaType.Morph, meta);
            }

            // author
            if(!string.IsNullOrEmpty(Author))
            {
                List<byte> meta = new(Author.Length + 1);
                meta.AddRange(Encoding.UTF8.GetBytes(Author));
                meta.Add(0);
                meta.Align(4);
                MetaHeader(MetaType.Author, meta);
            }

            // description
            if(!string.IsNullOrEmpty(Description))
            {
                List<byte> meta = new(Description.Length + 1);
                meta.AddRange(Encoding.UTF8.GetBytes(Description));
                meta.Add(0);
                meta.Align(4);
                MetaHeader(MetaType.Description, meta);
            }

            // other metadata
            foreach(var item in Other)
            {
                writer.WriteUInt32(item.Key);
                writer.WriteUInt32((uint)item.Value.Length);
                writer.Write(item.Value);
            }

            writer.WriteUInt32((uint)MetaType.End);
            writer.WriteUInt32(0);
        }
    }
}
