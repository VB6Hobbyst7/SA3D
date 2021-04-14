using System;
using System.Collections.Generic;
using System.Text;

namespace SATools.SACommon
{
    public static class ExeHelper
    {
        enum SectOffs : uint
        {
            VSize = 8,
            VAddr = 0xC,
            FSize = 0x10,
            FAddr = 0x14,
            Flags = 0x24,
            Size = 0x28
        }


        enum RelocTypes
        {
            /// <summary>
            /// C9h current offset += OSRel.offset
            /// </summary>
            R_DOLPHIN_NOP = 201,

            /// <summary>
            /// CAh current section = OSRel.section
            /// </summary>
            R_DOLPHIN_SECTION = 202,

            /// <summary>
            /// CBH
            /// </summary>
            R_DOLPHIN_END = 203
        }


        public static uint? SetupEXE(ref byte[] exefile)
        {
            if(exefile.ToUInt16(0) != 0x5A4D)
                return null;
            uint ptr = exefile.ToUInt32(0x3c);
            if(exefile.ToUInt32(ptr) != 0x4550) //PE\0\0
                return null;
            ptr += 4;
            ushort numsects = exefile.ToUInt16(ptr + 2);
            ptr += 0x14;
            uint imageBase = exefile.ToUInt32(ptr + 28);
            byte[] result = new byte[exefile.ToUInt32(ptr + 56)];
            Array.Copy(exefile, result, exefile.ToUInt32(ptr + 60));
            ptr += 0xe0;
            for(int i = 0; i < numsects; i++)
            {
                Array.Copy(exefile,
                    exefile.ToUInt32(ptr + (uint)SectOffs.FAddr),
                    result,
                    exefile.ToUInt32(ptr + (uint)SectOffs.VAddr),
                    exefile.ToUInt32(ptr + (uint)SectOffs.FSize));

                ptr += (int)SectOffs.Size;
            }
            exefile = result;
            return imageBase;
        }

        public static uint GetNewSectionAddress(byte[] exefile)
        {
            uint ptr = exefile.ToUInt32(0x3c);
            ptr += 4;
            ushort numsects = exefile.ToUInt16(ptr + 2);
            ptr += 0x14;
            ptr += 0xe0;
            ptr += (uint)((byte)SectOffs.Size * (numsects - 1));
            return Align(exefile.ToUInt32(ptr + (int)SectOffs.VAddr) + exefile.ToUInt32(ptr + (int)SectOffs.VSize));
        }

        public static void CreateNewSection(ref byte[] exefile, string name, byte[] data, bool isCode)
        {
            uint ptr = exefile.ToUInt32(0x3c);
            ptr += 4;
            ushort numsects = exefile.ToUInt16(ptr + 2);
            uint sectnumptr = ptr + 2;
            ptr += 0x14;
            uint PEHead = ptr;
            ptr += 0xe0;
            ptr += (uint)((byte)SectOffs.Size * numsects);
            ByteConverter.GetBytes((ushort)(numsects + 1)).CopyTo(exefile, sectnumptr);
            Array.Clear(exefile, (int)ptr, 8);
            Encoding.ASCII.GetBytes(name).CopyTo(exefile, ptr);
            UInt32 vaddr = Align(exefile.ToUInt32(ptr - (int)SectOffs.Size + (int)SectOffs.VAddr) + exefile.ToUInt32(ptr - (int)SectOffs.Size + (int)SectOffs.VSize));
            ByteConverter.GetBytes(vaddr).CopyTo(exefile, ptr + (int)SectOffs.VAddr);
            UInt32 faddr = Align(exefile.ToUInt32(ptr - (int)SectOffs.Size + (int)SectOffs.FAddr) + exefile.ToUInt32(ptr - (int)SectOffs.Size + (int)SectOffs.FSize));
            ByteConverter.GetBytes(faddr).CopyTo(exefile, ptr + (int)SectOffs.FAddr);
            ByteConverter.GetBytes(isCode ? 0x60000020 : 0xC0000040).CopyTo(exefile, ptr + (int)SectOffs.Flags);
            int diff = (int)Align((uint)data.Length);
            ByteConverter.GetBytes(diff).CopyTo(exefile, ptr + (int)SectOffs.VSize);
            ByteConverter.GetBytes(diff).CopyTo(exefile, ptr + (int)SectOffs.FSize);
            if(isCode)
                ByteConverter.GetBytes(Convert.ToUInt32(exefile.ToUInt32(PEHead + 4) + diff)).CopyTo(exefile, PEHead + 4);
            else
                ByteConverter.GetBytes(Convert.ToUInt32(exefile.ToUInt32(PEHead + 8) + diff)).CopyTo(exefile, PEHead + 8);
            ByteConverter.GetBytes(Convert.ToUInt32(exefile.ToUInt32(PEHead + 0x38) + diff)).CopyTo(exefile, PEHead + 0x38);
            Array.Resize(ref exefile, exefile.Length + diff);
            data.CopyTo(exefile, vaddr);
        }

        public static void CompactEXE(ref byte[] exefile)
        {
            if(exefile.ToUInt16(0) != 0x5A4D)
                return;
            uint ptr = exefile.ToUInt32(0x3c);
            if(exefile.ToInt32(ptr) != 0x4550) //PE\0\0
                return;
            ptr += 4;
            ushort numsects = exefile.ToUInt16(ptr + 2);
            ptr += 0x14;
            uint PEHead = ptr;
            uint imageBase = exefile.ToUInt32(ptr + 28);
            byte[] result = new byte[exefile.ToInt32((uint)(ptr + 0xe0 + ((int)SectOffs.Size * (numsects - 1)) + (int)SectOffs.FAddr)) + exefile.ToInt32((uint)(ptr + 0xe0 + ((int)SectOffs.Size * (numsects - 1)) + (int)SectOffs.FSize))];
            Array.Copy(exefile, result, exefile.ToUInt32(ptr + 60));
            ptr += 0xe0;
            for(int i = 0; i < numsects; i++)
            {
                Array.Copy(exefile, exefile.ToInt32(ptr + (int)SectOffs.VAddr), result, exefile.ToInt32(ptr + (int)SectOffs.FAddr), exefile.ToInt32(ptr + (int)SectOffs.FSize));
                ptr += (int)SectOffs.Size;
            }
            exefile = result;
        }

        public static void FixRELPointers(byte[] file, uint imageBase = 0)
        {
            OSModuleHeader header = new(file, 0);
            OSSectionInfo[] sections = new OSSectionInfo[header.info.numSections];
            for(uint i = 0; i < header.info.numSections; i++)
                sections[i] = new OSSectionInfo(file, header.info.sectionInfoOffset + (i * 8));
            OSImportInfo[] imports = new OSImportInfo[header.impSize / 8];
            for(uint i = 0; i < imports.Length; i++)
                imports[i] = new OSImportInfo(file, header.impOffset + (i * 8));
            uint reladdr = 0;
            for(int i = 0; i < imports.Length; i++)
                if(imports[i].id == header.info.id)
                {
                    reladdr = imports[i].offset;
                    break;
                }
            OSRel rel = new(file, reladdr);
            uint dataaddr = 0;
            unchecked
            {
                while(rel.type != (byte)RelocTypes.R_DOLPHIN_END)
                {
                    dataaddr += rel.offset;
                    uint sectionbase = (uint)(sections[rel.section].offset & ~1);
                    switch(rel.type)
                    {
                        case 0x01:
                            ByteConverter.GetBytes(rel.addend + sectionbase + imageBase).CopyTo(file, dataaddr);
                            break;
                        case 0x02:
                            ByteConverter.GetBytes((file.ToUInt32(dataaddr) & 0xFC000003) | ((rel.addend + sectionbase) & 0x3FFFFFC) + imageBase).CopyTo(file, dataaddr);
                            break;
                        case 0x03:
                        case 0x04:
                            ByteConverter.GetBytes((ushort)(rel.addend + sectionbase) + imageBase).CopyTo(file, dataaddr);
                            break;
                        case 0x05:
                            ByteConverter.GetBytes((ushort)((rel.addend + sectionbase) >> 16) + imageBase).CopyTo(file, dataaddr);
                            break;
                        case 0x06:
                            ByteConverter.GetBytes((ushort)(((rel.addend + sectionbase) >> 16) + (((rel.addend + sectionbase) & 0x8000) == 0x8000 ? 1 : 0)) + imageBase).CopyTo(file, dataaddr);
                            break;
                        case 0x0A:
                            ByteConverter.GetBytes((uint)((file.ToUInt32(dataaddr) & 0xFC000003) | (((rel.addend + sectionbase) - dataaddr) & 0x3FFFFFC)) + imageBase).CopyTo(file, dataaddr);
                            break;
                        case 0x00:
                        case (byte)RelocTypes.R_DOLPHIN_NOP:
                        case (byte)RelocTypes.R_DOLPHIN_END:
                            break;
                        case (byte)RelocTypes.R_DOLPHIN_SECTION:
                            dataaddr = sectionbase;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    reladdr += 8;
                    rel = new OSRel(file, reladdr);
                }
            }
        }

        public static void AlignCode(this List<byte> me)
        {
            while(me.Count % 0x10 > 0)
                me.Add(0x90);
        }

        public static uint Align(uint address)
        {
            if(address % 0x1000 == 0)
                return address;
            return ((address / 0x1000) + 1) * 0x1000;
        }
    }

    class OSModuleLink
    {
        public uint next;
        public uint prev;

        public OSModuleLink(byte[] file, uint address)
        {
            next = file.ToUInt32(address);
            prev = file.ToUInt32(address + 4);
        }
    }

    class OSModuleInfo
    {
        public uint id;              // unique identifier for the module
        public OSModuleLink link;              // doubly linked list of modules
        public uint numSections;        // # of sections
        public uint sectionInfoOffset;  // offset to section info table
        public uint nameOffset;      // offset to module name
        public uint nameSize;          // size of module name
        public uint version;            // version number

        public OSModuleInfo(byte[] file, uint address)
        {
            id = file.ToUInt32(address);
            address += 4;
            link = new OSModuleLink(file, address);
            address += 8;
            numSections = file.ToUInt32(address);
            address += 4;
            sectionInfoOffset = file.ToUInt32(address);
            address += 4;
            nameOffset = file.ToUInt32(address);
            address += 4;
            nameSize = file.ToUInt32(address);
            address += 4;
            version = file.ToUInt32(address);
        }
    }

    class OSModuleHeader
    {
        // CAUTION: info must be the 1st member
        public OSModuleInfo info;

        // OS_MODULE_VERSION == 1
        public uint bssSize;            // total size of bss sections in bytes
        public uint relOffset;
        public uint impOffset;
        public uint impSize;            // size in bytes
        public byte prologSection;    // section # for prolog function
        public byte epilogSection;    // section # for epilog function
        public byte unresolvedSection;  // section # for unresolved function
        public byte padding0;
        public uint prolog;          // prolog function offset
        public uint epilog;          // epilog function offset
        public uint unresolved;      // unresolved function offset

        // OS_MODULE_VERSION == 2
        public uint align;            // module alignment constraint
        public uint bssAlign;          // bss alignment constraint

        public OSModuleHeader(byte[] file, uint address)
        {
            info = new OSModuleInfo(file, address);
            address += 0x20;
            bssSize = file.ToUInt32(address);
            address += 4;
            relOffset = file.ToUInt32(address);
            address += 4;
            impOffset = file.ToUInt32(address);
            address += 4;
            impSize = file.ToUInt32(address);
            address += 4;
            prologSection = file[address++];
            epilogSection = file[address++];
            unresolvedSection = file[address++];
            padding0 = file[address++];
            prolog = file.ToUInt32(address);
            address += 4;
            epilog = file.ToUInt32(address);
            address += 4;
            unresolved = file.ToUInt32(address);
            address += 4;
            align = file.ToUInt32(address);
            address += 4;
            bssAlign = file.ToUInt32(address);
        }
    }

    class OSSectionInfo
    {
        public uint offset;
        public uint size;

        public OSSectionInfo(byte[] file, uint address)
        {
            offset = file.ToUInt32(address);
            size = file.ToUInt32(address + 4);
        }
    }

    class OSImportInfo
    {
        public uint id;              // external module id
        public uint offset;          // offset to OSRel instructions

        public OSImportInfo(byte[] file, uint address)
        {
            id = file.ToUInt32(address);
            offset = file.ToUInt32(address + 4);
        }
    }

    class OSRel
    {
        public ushort offset;            // byte offset from the previous entry
        public byte type;
        public byte section;
        public uint addend;

        public OSRel(byte[] file, uint address)
        {
            offset = file.ToUInt16(address);
            type = file[address + 2];
            section = file[address + 3];
            addend = file.ToUInt32(address + 4);
        }
    }

}
