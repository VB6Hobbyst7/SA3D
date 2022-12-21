using System.Collections;
using System.ComponentModel;
using System.Reflection;
using static SATools.SACommon.Ini.IniSerializeHelper;

namespace SATools.SACommon.Ini.Deserialization
{
    internal static class Deserialize
    {
        internal static object? DeserializeInternal(Type type, IniDictionary ini, IniCollectionSettings CollectionSettings, TypeConverter? Converter)
        {
            object? result;
            IniDictionary iniClone = new();
            iniClone = IniFile.Combine(iniClone, ini);
            result = DeserializeInternal("value", type, type.GetDefaultValue(), iniClone, string.Empty, true, CollectionSettings, Converter);
            return result;
        }

        internal static object? DeserializeInternal(string name, Type type, object? defaultvalue, IniDictionary ini, string groupName, bool rootObject, IniCollectionSettings collectionSettings, TypeConverter? converter)
        {
            string fullname = groupName;
            if (!rootObject)
            {
                if (!string.IsNullOrEmpty(fullname))
                    fullname += '.';
                fullname += name;
            }

            if (!ini.ContainsKey(groupName))
                return defaultvalue;

            IniGroup group = ini[groupName];
            if (!type.IsComplexType(converter))
            {
                if (group.TryGetValue(name, out string? value))
                {
                    object? converted = type.ConvertFromString(value, converter);
                    group.Remove(name);
                    if (converted != null)
                        return converted;
                }
                return defaultvalue;
            }

            if (type.IsArray)
            {
                return DeserializeArray(name, fullname, type, ini, collectionSettings, group);
            }

            if (type.ImplementsGenericDefinition(typeof(IList<>), out Type? generictype))
            {
                object obj = Activator.CreateInstance(type) ?? throw new NullReferenceException("Deserialized array is null!");

                Type valuetype = generictype.GetGenericArguments()[0];
                Type listType = typeof(ListDeserializer<>).MakeGenericType(valuetype);
                ICollectionDeserializer deserializer =
                    (ICollectionDeserializer?)Activator.CreateInstance(listType)
                    ?? throw new NullReferenceException("List deserializer was null!");

                deserializer.Deserialize(obj, group, groupName, collectionSettings, name, ini, fullname);
                return obj;
            }

            if (type.ImplementsGenericDefinition(typeof(IDictionary<,>), out generictype))
            {
                object obj = Activator.CreateInstance(type) ?? throw new NullReferenceException("Dictionary array is null!");
                Type keytype = generictype.GetGenericArguments()[0];
                Type valuetype = generictype.GetGenericArguments()[1];

                if (keytype.IsComplexType(collectionSettings.KeyConverter))
                    return obj;

                Type dictionaryType = typeof(DictionaryDeserializer<,>).MakeGenericType(keytype, valuetype);
                ICollectionDeserializer deserializer =
                    (ICollectionDeserializer?)Activator.CreateInstance(dictionaryType)
                    ?? throw new NullReferenceException("Dictionary deserializer was null!");

                deserializer.Deserialize(obj, group, groupName, collectionSettings, name, ini, fullname);
                return obj;
            }

            return DeserializeObject(fullname, type, ini);
        }

        private static object? DeserializeObject(string fullname, Type type, IniDictionary ini)
        {
            object? result = Activator.CreateInstance(type);
            MemberInfo? collection = null;
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (GetAttribute<IniIgnoreAttribute>(member) != null)
                    continue;

                string membername = GetAttribute<IniNameAttribute>(member)?.Name ?? member.Name;
                IniCollectionSettings colset = GetAttribute<IniCollectionAttribute>(member)?.Settings ?? defaultCollectionSettings;
                TypeConverter? conv = GetConverterFromAttribute(member);

                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)member;
                        if (colset.Mode == IniCollectionMode.IndexOnly && typeof(ICollection).IsAssignableFrom(field.FieldType))
                        {
                            if (collection != null)
                                throw new Exception("IniCollectionMode.IndexOnly cannot be used on multiple members of a Type.");
                            collection = member;
                            continue;
                        }

                        object? defval =
                            GetAttribute<DefaultValueAttribute>(member)?.Value
                            ?? field.FieldType.GetDefaultValue();

                        field.SetValue(result, DeserializeInternal(membername, field.FieldType, defval, ini, fullname, false, colset, conv));
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo)member;

                        if (property.GetIndexParameters().Length > 0)
                            continue;

                        if (colset.Mode == IniCollectionMode.IndexOnly && typeof(ICollection).IsAssignableFrom(property.PropertyType))
                        {
                            if (collection != null)
                                throw new Exception("IniCollectionMode.IndexOnly cannot be used on multiple members of a Type.");
                            collection = member;
                            continue;
                        }

                        defval =
                            GetAttribute<DefaultValueAttribute>(member)?.Value
                            ?? property.PropertyType.GetDefaultValue();

                        object? propval = DeserializeInternal(membername, property.PropertyType, defval, ini, fullname, false, colset, conv);

                        property.GetSetMethod()?.Invoke(result, new[] { propval });
                        break;
                }
            }

            if (collection != null)
            {
                IniCollectionSettings settings = GetAttribute<IniCollectionAttribute>(collection)?.Settings ?? defaultCollectionSettings;

                switch (collection.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)collection;

                        object? fieldVal = DeserializeInternal(collection.Name, field.FieldType, field.FieldType.GetDefaultValue(), ini, fullname, false, settings, null);

                        field.SetValue(result, fieldVal);
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo)collection;

                        object? propval = DeserializeInternal(collection.Name, property.PropertyType, property.PropertyType.GetDefaultValue(), ini, fullname, false, settings, null);

                        property.GetSetMethod()?.Invoke(result, new[] { propval });
                        break;
                }
            }

            ini.Remove(fullname);
            return result;
        }

        private static object? DeserializeArray(string name, string fullname, Type type, IniDictionary ini, IniCollectionSettings collectionSettings, IniGroup group)
        {
            Type valuetype = type.GetElementType() ?? throw new NullReferenceException("Element type is null");
            TypeConverter keyconverter = collectionSettings.KeyConverter ?? new Int32Converter();
            int maxIndex = int.MinValue;

            if (!valuetype.IsComplexType(collectionSettings.ValueConverter))
            {
                if (collectionSettings.Mode == IniCollectionMode.SingleLine)
                {
                    if (group.ContainsKey(name))
                    {
                        string[] items;
                        if (string.IsNullOrEmpty(group[name]))
                            items = Array.Empty<string>();
                        else
                            items = group[name].Split(new[] { collectionSettings.Format }, StringSplitOptions.None);

                        Array _obj = Array.CreateInstance(valuetype, items.Length);
                        for (int i = 0; i < items.Length; i++)
                            _obj.SetValue(valuetype.ConvertFromString(items[i], collectionSettings.ValueConverter), i);
                        group.Remove(name);
                        return _obj;
                    }
                    else
                        return null;
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
                return Array.CreateInstance(valuetype, 0);

            int length = maxIndex + 1 - (collectionSettings.Mode == IniCollectionMode.SingleLine ? 0 : collectionSettings.StartIndex);
            Array obj = Array.CreateInstance(valuetype, length);

            if (!valuetype.IsComplexType(collectionSettings.ValueConverter))
            {
                for (int i = 0; i < length; i++)
                {
                    string indexString = keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex)
                        ?? throw new NullReferenceException("Key conversion returned null");
                    string keyname = collectionSettings.Mode.IndexToName(name, indexString);
                    if (group.TryGetValue(keyname, out string? value))
                    {
                        obj.SetValue(valuetype.ConvertFromString(value, collectionSettings.ValueConverter), i);
                        group.Remove(keyname);
                    }
                    else
                        obj.SetValue(valuetype.GetDefaultValue(), i);
                }

            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    string indexString = keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex)
                        ?? throw new NullReferenceException("Key conversion returned null");
                    string groupName = collectionSettings.Mode.IndexToName(fullname, indexString);

                    object? element = DeserializeInternal(
                        "value",
                        valuetype,
                        valuetype.GetDefaultValue(),
                        ini,
                        groupName,
                        true,
                        defaultCollectionSettings,
                        collectionSettings.ValueConverter);

                    obj.SetValue(element, i);
                }
            }
            return obj;
        }
    }
}