using SATools.SACommon;
using System;
using System.Collections.Generic;
using System.Linq;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.HelperExtensions;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A single mesh, with its own parameter and primitive data <br/>
    /// </summary>
    public struct Mesh : ICloneable
    {
        /// <summary>
        /// The parameters that this mesh sets
        /// </summary>
        public IParameter[] Parameters { get; }

        /// <summary>
        /// The polygon data
        /// </summary>
        public Poly[] Polys { get; }

        /// <summary>
        /// The index attributes of this mesh. If it has no IndexAttribParam, it will return null
        /// </summary>
        public IndexAttributes? IndexAttributes
        {
            get
            {
                IParameter p = Parameters.FirstOrDefault(x => x.Type == ParameterType.IndexAttributes);
                return p == null ? null : ((IndexAttributeParameter)p).IndexAttributes;
            }
        }

        /// <summary>
        /// Create a new mesh from existing primitives and parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="polys"></param>
        public Mesh(IParameter[] parameters, Poly[] polys)
        {
            Parameters = parameters;
            Polys = polys;
        }

        public static Mesh Read(byte[] source, uint address, uint imageBase, ref IndexAttributes indexAttribs)
        {
            // getting the addresses and sizes
            uint parameters_addr = source.ToUInt32(address) - imageBase;
            int parameters_count = source.ToInt32(address + 4);

            uint primitives_addr = source.ToUInt32(address + 8) - imageBase;
            int primitives_size = source.ToInt32(address + 12);

            // reading the parameters
            List<IParameter> parameters = new();
            for (int i = 0; i < parameters_count; i++)
            {
                parameters.Add(ParameterExtensions.Read(source, parameters_addr));
                parameters_addr += 8;
            }

            // getting the index attribute parameter
            var p = parameters.FirstOrDefault(x => x.Type == ParameterType.IndexAttributes);
            if (p != null)
                indexAttribs = ((IndexAttributeParameter)p).IndexAttributes;

            // reading the primitives
            List<Poly> primitives = new();
            uint end_pos = (uint)(primitives_addr + primitives_size);

            while (primitives_addr < end_pos)
            {
                // if the primitive isnt valid
                if (source[primitives_addr] == 0)
                    break;
                primitives.Add(Poly.Read(source, ref primitives_addr, indexAttribs));
            }

            return new Mesh(parameters.ToArray(), primitives.ToArray());
        }

        object ICloneable.Clone() => Clone();

        public Mesh Clone() => new((IParameter[])Parameters.Clone(), Polys.ContentClone());

        public override string ToString() => (IndexAttributes.HasValue ? ((uint)IndexAttributes.Value).ToString() : "null") + $" - {Parameters.Length} - {Polys.Length}";
    }
}
