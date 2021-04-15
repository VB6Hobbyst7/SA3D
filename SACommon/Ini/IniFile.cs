using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using IniGroup = System.Collections.Generic.Dictionary<string, string>;
using IniNameGroup = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.Dictionary<string, string>>;
using IniNameValue = System.Collections.Generic.KeyValuePair<string, string>;

namespace SATools.SACommon.Ini
{
    /// <summary>
    /// Used to write and read Ini dictionaries from strings
    /// </summary>
    public static class IniFile
    {
        #region Loading
        /// <summary>
        /// Loads an Init dictionary from strings
        /// </summary>
        /// <param name="data">Ini values to load</param>
        /// <returns></returns>
        public static IniDictionary Read(params string[] data)
        {
            IniDictionary result = new();
            IniGroup current = new();

            result.Add(string.Empty, current);

            string curgroup = string.Empty;
            for(int i = 0; i < data.Length; i++)
            {
                string line = System.Text.RegularExpressions.Regex.Unescape(data[i]);

                bool startswithbracket = false;
                int firstequals = -1;
                int endbracket = -1;

                for(int c = 0; c < line.Length; c++)
                    switch(line[c])
                    {
                        case '=':
                            if(firstequals == -1)
                                firstequals = c;
                            break;
                        case '[':
                            if(c == 0)
                                startswithbracket = true;
                            break;
                        case ']':
                            endbracket = c;
                            break;
                        case ';': // comment character
                            line = line[..c];
                            break;
                    }

                if(startswithbracket & endbracket != -1)
                {
                    curgroup = line[1..endbracket];
                    current = new IniGroup();
                    try
                    {
                        result.Add(curgroup, current);
                    }
                    catch(ArgumentException ex)
                    {
                        throw new Exception("INI File error: Group \"" + curgroup + "\" already exists.\nline " + (i + 1), ex);
                    }
                }
                else if(!string.IsNullOrWhiteSpace(line))
                {
                    string key;
                    string value = string.Empty;
                    if(firstequals > -1)
                    {
                        key = line[..firstequals];
                        value = line[(firstequals + 1)..];
                    }
                    else
                        key = line;
                    try
                    {
                        current.Add(key, value);
                    }
                    catch(ArgumentException ex)
                    {
                        throw new Exception("INI File error: Value \"" + key + "\" already exists in group \"" + curgroup + "\".\nline " + (i + 1), ex);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Loads an Ini dictionary from a file
        /// </summary>
        /// <param name="filename">Path to the ini file</param>
        /// <returns></returns>
        public static IniDictionary Read(string filename)
            => Read(File.ReadAllLines(filename));

        /// <summary>
        /// Loads an Ini dictionary from a string collection
        /// </summary>
        /// <param name="data">Collection of strings</param>
        /// <returns></returns>
        public static IniDictionary Read(IEnumerable<string> data)
            => Read(data.ToArray());

        /// <summary>
        /// Loads an Ini dictionary from a stream
        /// </summary>
        /// <param name="stream">Data stream</param>
        /// <returns></returns>
        public static IniDictionary Read(Stream stream)
        {
            List<string> data = new();
            using StreamReader reader = new(stream);

            string line;
            while((line = reader.ReadLine()) != null)
                data.Add(line);

            return Read(data.ToArray());
        }

        #endregion

        #region Writing

        /// <summary>
        /// Converts an Inidictionary to a string array, ready to be written
        /// </summary>
        /// <param name="Ini">Ini dictionary to write</param>
        /// <returns></returns>
        public static string[] Write(IniDictionary Ini)
        {
            bool first = true;
            List<string> result = new List<string>();
            foreach(IniNameGroup group in Ini)
            {
                string add = "";
                if(!first)
                    add += System.Environment.NewLine;
                else
                    first = false;
                if(!string.IsNullOrEmpty(group.Key))
                {
                    add += "[" + group.Key.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("\r", @"\r").Replace(";", @"\;") + "]";
                    result.Add(add);
                }
                foreach(IniNameValue value in group.Value)
                {
                    string escapedkey = value.Key.Replace(@"\", @"\\").Replace("=", @"\=").Replace("\n", @"\n").Replace("\r", @"\r").Replace(";", @"\;");
                    if(escapedkey.StartsWith("["))
                        escapedkey = escapedkey.Insert(0, @"\");
                    result.Add(escapedkey + "=" + value.Value.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("\r", @"\r").Replace(";", @"\;"));
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Writes an Ini dictionary to a file
        /// </summary>
        /// <param name="Ini">Ini dictionary to write</param>
        /// <param name="filename">File path to write to</param>
        public static void Write(IniDictionary Ini, string filename)
            => File.WriteAllLines(filename, Write(Ini));

        /// <summary>
        /// Writes an ini dictionary to a stream
        /// </summary>
        /// <param name="Ini">Ini dictionary to write</param>
        /// <param name="stream">Stream to write to</param>
        public static void Write(IniDictionary Ini, Stream stream)
        {
            using StreamWriter writer = new(stream);
            foreach(string line in Write(Ini))
                writer.WriteLine(line);
        }

        #endregion

        /// <summary>
        /// Combines two Ini dictionaries
        /// </summary>
        /// <param name="dictA"></param>
        /// <param name="dictB"></param>
        /// <returns></returns>
        public static IniDictionary Combine(IniDictionary dictA, IniDictionary dictB)
        {
            IniDictionary result = new();

            foreach(IniNameGroup group in dictA)
                result.Add(group.Key, new(group.Value));

            foreach(IniNameGroup group in dictB)
            {
                if(result.ContainsKey(group.Key))
                    foreach(IniNameValue item in group.Value)
                        result[group.Key][item.Key] = item.Value;
                else
                    result.Add(group.Key, new IniGroup(group.Value));
            }
            return result;
        }


    }
}