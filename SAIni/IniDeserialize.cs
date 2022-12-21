using System.ComponentModel;
using static SATools.SACommon.Ini.Deserialization.Deserialize;
using static SATools.SACommon.Ini.IniSerializeHelper;

namespace SATools.SACommon.Ini
{
    /// <summary>
    /// Ini deserialization methods
    /// </summary>
    public static class IniDeserialize
    {

        #region Generic Deserializing

        public static T? Deserialize<T>(string filename)
            => Deserialize<T>(IniFile.Read(filename), (TypeConverter?)null);

        public static T? Deserialize<T>(string filename, TypeConverter? Converter)
            => Deserialize<T>(IniFile.Read(filename), Converter);

        public static T? Deserialize<T>(IniDictionary ini)
            => (T?)Deserialize(typeof(T), ini, (TypeConverter?)null);

        public static T? Deserialize<T>(IniDictionary ini, TypeConverter? Converter)
            => (T?)Deserialize(typeof(T), ini, Converter);

        public static T? Deserialize<T>(string filename, IniCollectionSettings CollectionSettings)
            => Deserialize<T>(IniFile.Read(filename), CollectionSettings, null);

        public static T? Deserialize<T>(string filename, IniCollectionSettings CollectionSettings, TypeConverter? Converter)
            => Deserialize<T>(IniFile.Read(filename), CollectionSettings, Converter);

        public static T? Deserialize<T>(IniDictionary ini, IniCollectionSettings CollectionSettings)
            => (T?)DeserializeInternal(typeof(T), ini, CollectionSettings, null);

        public static T? Deserialize<T>(IniDictionary ini, IniCollectionSettings CollectionSettings, TypeConverter? Converter)
            => (T?)DeserializeInternal(typeof(T), ini, CollectionSettings, Converter);

        #endregion

        #region Deserializing

        public static object? Deserialize(Type type, string filepath)
            => Deserialize(type, IniFile.Read(filepath), (TypeConverter?)null);

        public static object? Deserialize(Type type, string Filename, TypeConverter? Converter)
            => Deserialize(type, IniFile.Read(Filename), Converter);

        public static object? Deserialize(Type type, IniDictionary ini)
            => DeserializeInternal(type, ini, initialCollectionSettings, null);

        public static object? Deserialize(Type type, IniDictionary ini, TypeConverter? Converter)
            => DeserializeInternal(type, ini, initialCollectionSettings, Converter);

        public static object? Deserialize(Type type, string Filename, IniCollectionSettings CollectionSettings)
            => DeserializeInternal(type, IniFile.Read(Filename), CollectionSettings, null);

        public static object? Deserialize(Type type, string Filename, IniCollectionSettings CollectionSettings, TypeConverter? Converter)
            => DeserializeInternal(type, IniFile.Read(Filename), CollectionSettings, Converter);

        public static object? Deserialize(Type type, IniDictionary ini, IniCollectionSettings CollectionSettings)
            => DeserializeInternal(type, ini, CollectionSettings, null);

        #endregion

    }
}
