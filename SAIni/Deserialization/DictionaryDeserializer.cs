using static SATools.SACommon.Ini.Deserialization.Deserialize;
using static SATools.SACommon.Ini.IniSerializeHelper;

namespace SATools.SACommon.Ini.Deserialization
{
    internal class DictionaryDeserializer<TKey, TValue> : ICollectionDeserializer
    {
        public void Deserialize(object listObj, IniGroup group, string groupName, IniCollectionSettings collectionSettings, string name, IniDictionary ini, string fullname)
        {
            Type keytype = typeof(TKey);
            Type valuetype = typeof(TValue);
            IDictionary<TKey, TValue> list = (IDictionary<TKey, TValue>)listObj;

            if (collectionSettings.Mode == IniCollectionMode.SingleLine)
                throw new InvalidOperationException("Cannot deserialize IDictionary<TKey, TValue> with IniCollectionMode.SingleLine!");

            List<string> items = new();

            if (!valuetype.IsComplexType(collectionSettings.ValueConverter))
            {
                foreach (IniNameValue item in group)
                {
                    string? value = collectionSettings.Mode.IndexFromName(name, item.Key);
                    if (value == null)
                        continue;

                    items.Add(value);
                }

                foreach (string item in items)
                {
                    TKey? key = (TKey?)keytype.ConvertFromString(item, collectionSettings.KeyConverter);
                    if (key == null)
                        throw new InvalidCastException("Failed to convert key from string");

                    string valueString = collectionSettings.Mode.IndexToName(name, item);
                    TValue? value = (TValue?)valuetype.ConvertFromString(valueString, collectionSettings.ValueConverter);
                    if (value == null)
                        throw new InvalidCastException("Failed to convert value from string");

                    list.Add(key, value);
                    group.Remove(valueString);
                }
            }
            else
            {
                foreach (IniNameGroup item in ini)
                {
                    string? value = collectionSettings.Mode.IndexFromName(name, item.Key);
                    if (value == null)
                        continue;

                    items.Add(value);
                }

                foreach (string item in items)
                {
                    TKey? key = (TKey?)keytype.ConvertFromString(item, collectionSettings.KeyConverter);
                    if (key == null)
                        throw new InvalidCastException("Failed to convert key from string");

                    string valueString = collectionSettings.Mode.IndexToName(name, item);
                    TValue? value = (TValue?)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, valueString, true, defaultCollectionSettings, collectionSettings.ValueConverter);
                    if (value == null)
                        throw new InvalidCastException("Failed to convert value from string");

                    list.Add(key, value);
                    group.Remove(valueString);
                }
            }
        }

    }
}
