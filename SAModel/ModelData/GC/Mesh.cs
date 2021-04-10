using Reloaded.Memory.Streams.Writers;
using System;
using System.Collections.Generic;
using System.Linq;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.Helper;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A single mesh, with its own parameter and primitive data <br/>
    /// </summary>
    [Serializable]
    public class Mesh : ICloneable
    {
        /// <summary>
        /// The parameters that this mesh sets
        /// </summary>
        public Parameter[] Parameters { get; }

        /// <summary>
        /// The polygon data
        /// </summary>
        public Poly[] Polys { get; }

        /// <summary>
        /// The index attribute flags of this mesh. If it has no IndexAttribParam, it will return null
        /// </summary>
        public IndexAttributeFlags? IndexFlags
        {
            get => ((IndexAttributeParameter)Parameters.FirstOrDefault(x => x.Type == ParameterType.IndexAttributeFlags))?.IndexAttributes;
        }

        /// <summary>
        /// The location to which the parameters have been written
        /// </summary>
        private uint? _ParamAddress;

        /// <summary>
        /// The location to which the primitives have been written
        /// </summary>
        private uint? _PolyAddress;

        /// <summary>
        /// The amount of bytes which have been written for the primitives
        /// </summary>
        private uint? _PolySize;

        /// <summary>
        /// Create a new mesh from existing primitives and parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="polys"></param>
        public Mesh(Parameter[] parameters, Poly[] polys)
        {
            Parameters = parameters;
            Polys = polys;
        }

        public static Mesh Read(byte[] source, uint address, uint imageBase, ref IndexAttributeFlags indexFlags)
        {
            // getting the addresses and sizes
            uint parameters_addr = source.ToUInt32(address) - imageBase;
            int parameters_count = source.ToInt32(address + 4);

            uint primitives_addr = source.ToUInt32(address + 8) - imageBase;
            int primitives_size = source.ToInt32(address + 12);

            // reading the parameters
            List<Parameter> parameters = new List<Parameter>();
            for(int i = 0; i < parameters_count; i++)
            {
                parameters.Add(Parameter.Read(source, parameters_addr));
                parameters_addr += 8;
            }

            // getting the index attribute parameter
            IndexAttributeFlags? flags = ((IndexAttributeParameter)parameters.Find(x => x.Type == ParameterType.IndexAttributeFlags))?.IndexAttributes;
            if(flags.HasValue)
                indexFlags = flags.Value;

            // reading the primitives
            List<Poly> primitives = new List<Poly>();
            uint end_pos = (uint)(primitives_addr + primitives_size);

            while(primitives_addr < end_pos)
            {
                // if the primitive isnt valid
                if(source[primitives_addr] == 0)
                    break;
                primitives.Add(Poly.Read(source, ref primitives_addr, indexFlags));
            }

            return new Mesh(parameters.ToArray(), primitives.ToArray());
        }

        /// <summary>
        /// Writes the parameters and primitives to a stream
        /// </summary>
        /// <param name="writer">The ouput stream</param>
        /// <param name="indexFlags">The index flags</param>
        public void WriteData(EndianMemoryStream writer, IndexAttributeFlags indexFlags)
        {
            _ParamAddress = (uint)writer.Stream.Position;

            foreach(Parameter param in Parameters)
            {
                param.Write(writer);
            }

            _PolyAddress = (uint)writer.Stream.Position;

            foreach(Poly prim in Polys)
            {
                prim.Write(writer, indexFlags);
            }

            _PolySize = (uint)writer.Stream.Position - _PolyAddress;
        }

        /// <summary>
        /// Writes the location and sizes of
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="imagebase">The imagebase</param>
        public void WriteProperties(EndianMemoryStream writer, uint imagebase)
        {
            if(!_PolyAddress.HasValue)
                throw new NullReferenceException("Data has not been written yet");

            writer.WriteUInt32(_ParamAddress.Value + imagebase);
            writer.WriteUInt32((uint)Parameters.Length);
            writer.WriteUInt32(_PolyAddress.Value + imagebase);
            writer.WriteUInt32(_PolySize.Value);

            //resetting the values
            _PolyAddress = null;
            _PolySize = null;
            _ParamAddress = null;
        }

        object ICloneable.Clone() => Clone();

        public Mesh Clone() => new Mesh(Parameters.ContentClone(), Polys.ContentClone());

        public override string ToString() => (IndexFlags.HasValue ? ((uint)IndexFlags.Value).ToString() : "null") + $" - {Parameters.Length} - {Polys.Length}";
    }
}
