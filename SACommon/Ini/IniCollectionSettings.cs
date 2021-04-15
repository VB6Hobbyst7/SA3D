using System.ComponentModel;

namespace SATools.SACommon.Ini
{
    /// <summary>
    /// How an ini collection is serialized
    /// </summary>
    public enum IniCollectionMode
    {
        /// <summary>
        /// The collection is serialized normally.
        /// </summary>
        Normal,

        /// <summary>
        /// The collection is serialized using only the index in the ini entry's key.
        /// </summary>
        IndexOnly,

        /// <summary>
        /// The collection is serialized using the collection's name and index in the ini entry's key, with no square brackets.
        /// </summary>
        NoSquareBrackets,

        /// <summary>
        /// The <paramref name="Format"/> property is used with <seealso cref="string.Join"/> to create the ini entry's value. The key is the collection's name.
        /// </summary>
        SingleLine
    }

    /// <summary>
    /// Ini Settings for a collection
    /// </summary>
    public class IniCollectionSettings
    {
        /// <param name="mode">Serializer mode of the ini collection</param>
        public IniCollectionSettings(IniCollectionMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// Serializer mode of the ini collection
        /// </summary>
        public IniCollectionMode Mode { get; }

        /// <summary>
        /// Format of the collection
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// The index of the first item in the collection. Does not apply to Dictionary objects or <see cref="IniCollectionMode.SingleLine"/>.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// A <see cref="TypeConverter"/> used to convert indexes/keys to and from <see cref="string"/>.
        /// </summary>
        public TypeConverter KeyConverter { get; set; }

        /// <summary>
        /// A <see cref="TypeConverter"/> used to convert values to and from <see cref="string"/>.
        /// </summary>
        public TypeConverter ValueConverter { get; set; }
    }


}
