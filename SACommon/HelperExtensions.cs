using System;
using System.Collections.Generic;
using System.IO;

namespace SATools.SACommon
{
    public static class HelperExtensions
    {
        public static int AddUnique<T>(this List<T> list, T item)
        {
            if (list.Contains(item))
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
            for (int i = 0; i < result.Length; i++)
                result[i] = (T)input[i].Clone();
            return result;
        }

        /// <summary>
        /// The linq distinct doesnt work for some reason, so here is our own method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T[] GetDistinct<T>(this IList<T> collection) where T : IEquatable<T>
        {
            T[] result = new T[collection.Count];
            int distinctCount = 0;

            foreach (T c in collection)
            {
                foreach (T r in result)
                    if (c.Equals(r))
                        goto found;
                result[distinctCount] = c;
                distinctCount++;
            found:
                ;
            }

            if (distinctCount < result.Length)
                Array.Resize(ref result, distinctCount);

            return result;
        }

        public static int[] CreateIndexMap<T>(T[] oldArray, T[] newArray) where T : IEquatable<T>
        {
            int[] result = null;
            if (oldArray.Length > newArray.Length)
            {
                result = new int[oldArray.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    T toFind = oldArray[i];
                    for (int j = 0; j < newArray.Length; j++)
                    {
                        if (newArray[j].Equals(toFind))
                        {
                            result[i] = j;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        public static (T[] distinct, int[] map) CreateDistinctMap<T>(this IList<T> collection) where T : IEquatable<T>
        {
            if (collection == null)
                return (null, null);

            int[] map = new int[collection.Count];
            T[] distinct = new T[collection.Count];
            int distinctCount = 0;

            int i = 0;
            foreach (T c in collection)
            {

                for (int j = 0; j < distinctCount; j++)
                {
                    if (distinct[j].Equals(c))
                    {
                        map[i] = j;
                        goto found;
                    }
                }

                map[i] = distinctCount;
                distinct[distinctCount] = c;
                distinctCount++;

            found:
                ;
                i++;
            }

            if (distinctCount == map.Length)
                return (distinct, null);

            Array.Resize(ref distinct, distinctCount);
            return (distinct, map);
        }

        public static void AddLabel(this Dictionary<string, uint> labels, string label, uint address)
        {
            try
            {
                labels.Add(label, address);
            }
            catch
            {
                int append = 1;
                while (labels.TryGetValue($"{label}_{append}", out _))
                    append++;
                labels.Add($"{label}_{append}", address);
            }
        }

        /// <summary>
        /// Adds nullbytes to align a list with the 
        /// </summary>
        /// <param name="me"></param>
        /// <param name="alignment"></param>
        public static void Align(this List<byte> me, int alignment)
        {
            int off = me.Count % alignment;
            if (off == 0)
                return;
            me.AddRange(new byte[alignment - off]);
        }

        public static byte[] ReadFully(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using MemoryStream ms = new();
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);
            return ms.ToArray();
        }
    }
}
