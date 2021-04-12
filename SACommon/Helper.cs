using System;
using System.Collections.Generic;
using System.IO;

namespace SATools.SACommon
{
    public static class Helper
    {
        public static int AddUnique<T>(this List<T> list, T item)
        {
            if(list.Contains(item))
                return list.IndexOf(item);
            int val = list.Count;
            list.Add(item);
            return val;
        }

        /// <summary>
        /// Returns a clone of an array where each field has been cloned too
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T[] ContentClone<T>(this T[] input) where T : ICloneable
        {
            T[] result = new T[input.Length];
            for(int i = 0; i < result.Length; i++)
                result[i] = (T)input[i].Clone();
            return result;
        }

        /// <summary>
        /// Adds nullbytes to align a list with the 
        /// </summary>
        /// <param name="me"></param>
        /// <param name="alignment"></param>
        public static void Align(this List<byte> me, int alignment)
        {
            int off = me.Count % alignment;
            if(off == 0)
                return;
            me.AddRange(new byte[alignment - off]);
        }

        public static byte[] ReadFully(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using MemoryStream ms = new();
            int read;
            while((read = input.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);
            return ms.ToArray();
        }
    }
}
