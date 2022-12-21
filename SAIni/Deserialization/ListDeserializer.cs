using System.ComponentModel;
using static SATools.SACommon.Ini.Deserialization.Deserialize;
using static SATools.SACommon.Ini.IniSerializeHelper;

namespace SATools.SACommon.Ini.Deserialization
{
    internal class ListDeserializer<T> : ICollectionDeserializer
    {
        public void Deserialize(object listObj, IniGroup group, string groupName, IniCollectionSettings collectionSettings, string name, IniDictionary ini, string fullname)
        {
            Type valuetype = typeof(T);
            IList<T?> list = (IList<T?>)listObj;
            int maxIndex = int.MinValue;
            TypeConverter keyconverter = collectionSettings.KeyConverter ?? new Int32Converter();

            if (!valuetype.IsComplexType(collectionSettings.ValueConverter))
            {
                if (collectionSettings.Mode == IniCollectionMode.SingleLine)
                {
                    if (group.ContainsKey(name))
                    {
                        if (!string.IsNullOrEmpty(group[name]))
                        {
                            string[] items = group[name].Split(new[] { collectionSettings.Format }, StringSplitOptions.None);
                            for (int i = 0; i < items.Length; i++)
                            {
                                T? item = (T?)valuetype.ConvertFromString(items[i], collectionSettings.ValueConverter);
                                if (item == null)
                                    throw new NullReferenceException("List item is null");
                                list.Add(item);
                            }
                        }
                        group.Remove(name);
                    }
                }
                else
                {
                    foreach (IniNameValue item in group)
                    {
                        string? key = collectionSettings.Mode.IndexFromName(name, item.Key);
                        if (key == null)
                            continue;

                        int? index = (int?)keyconverter.ConvertFromInvariantString(key);
                        maxIndex = Math.Max(index ?? throw new NullReferenceException("Converted key is null"), maxIndex);
                    }
                }
            }
            else
            {
                if (collectionSettings.Mode == IniCollectionMode.SingleLine)
                    throw new InvalidOperationException("Cannot Deserialize type " + valuetype + " with IniCollectionMode.SingleLine!");

                foreach (IniNameGroup item in ini)
                {
                    string? key = collectionSettings.Mode.IndexFromName(fullname, item.Key);
                    if (key == null)
                        continue;

                    try
                    {
                        int? index = (int?)keyconverter.ConvertFromInvariantString(key);
                        maxIndex = Math.Max(index ?? throw new NullReferenceException("Converted key is null"), maxIndex);
                    }
                    catch { }
                }
            }

            if (maxIndex == int.MinValue)
                return;

            int length = maxIndex + 1 - (collectionSettings.Mode == IniCollectionMode.SingleLine ? 0 : collectionSettings.StartIndex);

            if (!valuetype.IsComplexType(collectionSettings.ValueConverter))
            {
                for (int i = 0; i < length; i++)
                {
                    string indexString = keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex)
                        ?? throw new NullReferenceException("Key conversion returned null");
                    string keyname = collectionSettings.Mode.IndexToName(name, indexString);
                    if (group.TryGetValue(keyname, out string? value))
                    {
                        list.Add((T?)valuetype.ConvertFromString(value, collectionSettings.ValueConverter));
                        group.Remove(keyname);
                    }
                    else
                        list.Add(default);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    string indexString = keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex) ?? throw new NullReferenceException("Key conversion returned null");
                    string elementGroupName = collectionSettings.Mode.IndexToName(fullname, indexString);

                    T? element = (T?)DeserializeInternal(
                        "value",
                        valuetype,
                        valuetype.GetDefaultValue(),
                        ini,
                        elementGroupName,
                        true,
                        defaultCollectionSettings,
                        collectionSettings.ValueConverter);

                    list.Add(element);
                }
            }
        }
    }
}
