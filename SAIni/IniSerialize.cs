using System.ComponentModel;
using static SATools.SACommon.Ini.IniSerializeHelper;
using static SATools.SACommon.Ini.Serialization.Serialize;

namespace SATools.SACommon.Ini
{
    public static class IniSerialize
    {
        public static void Serialize(object? Object, string Filename)
            => IniFile.Write(Serialize(Object), Filename);

        public static void Serialize(object? Object, TypeConverter? Converter, string Filename)
            => IniFile.Write(Serialize(Object, Converter), Filename);

        public static void Serialize(object? Object, IniCollectionSettings CollectionSettings, string Filename)
            => IniFile.Write(Serialize(Object, CollectionSettings), Filename);

        public static void Serialize(object? Object, IniCollectionSettings CollectionSettings, TypeConverter? Converter, string Filename)
            => IniFile.Write(Serialize(Object, CollectionSettings, Converter), Filename);

        public static IniDictionary Serialize(object? Object)
        => Serialize(Object, initialCollectionSettings, (TypeConverter?)null);

        public static IniDictionary Serialize(object? Object, TypeConverter? Converter)
        => Serialize(Object, initialCollectionSettings, Converter);

        public static IniDictionary Serialize(object? Object, IniCollectionSettings CollectionSettings)
        => Serialize(Object, CollectionSettings, (TypeConverter?)null);

        public static IniDictionary Serialize(object? Object, IniCollectionSettings CollectionSettings, TypeConverter? Converter)
        {
            IniDictionary ini = new() { { string.Empty, new IniGroup() } };
            SerializeInternal("value", Object, ini, string.Empty, true, CollectionSettings, Converter);
            return ini;
        }

    }
}
