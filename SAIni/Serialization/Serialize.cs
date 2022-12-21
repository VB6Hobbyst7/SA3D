using System.Collections;
using System.ComponentModel;
using System.Reflection;
using static SATools.SACommon.Ini.IniSerializeHelper;

namespace SATools.SACommon.Ini.Serialization
{
    internal static class Serialize
    {
        public static void SerializeInternal(string name, object? value, IniDictionary ini, string groupName, bool rootObject, IniCollectionSettings collectionSettings, TypeConverter? converter)
        {
            IniGroup group = ini[groupName];
            if (value == null || value == DBNull.Value)
                return;
            Type valueType = value.GetType();

            if (!valueType.IsComplexType(converter))
            {
                group.Add(name, value.ConvertToString(converter));
                return;
            }

            void SerializeChild(string name, object? value)
                => SerializeInternal(name, value, ini, groupName, false, collectionSettings, collectionSettings.ValueConverter);

            if (value is IList listValue)
            {
                if (collectionSettings.Mode == IniCollectionMode.SingleLine)
                {
                    List<string> line = new();
                    foreach (object item in listValue)
                        line.Add(item.ConvertToString(collectionSettings.ValueConverter));
                    group.Add(name, string.Join(collectionSettings.Format, line.ToArray()));
                }
                else
                {
                    int i = collectionSettings.StartIndex;
                    foreach (object item in listValue)
                    {
                        string index = (i++).ConvertToString(collectionSettings.KeyConverter);
                        SerializeChild(collectionSettings.Mode.IndexToName(name, index), item);
                    }
                }
                return;
            }

            if (value is IDictionary valueDictionary)
            {
                if (collectionSettings.Mode == IniCollectionMode.SingleLine)
                    throw new InvalidOperationException("Cannot serialize IDictionary with IniCollectionMode.SingleLine!");

                foreach (DictionaryEntry item in valueDictionary)
                {
                    string key = item.Key.ConvertToString(collectionSettings.KeyConverter);
                    SerializeChild(collectionSettings.Mode.IndexToName(name, key), item.Value);
                }
                return;
            }

            string newgroup = groupName;
            if (!rootObject)
            {
                if (!string.IsNullOrEmpty(newgroup))
                    newgroup += '.';
                newgroup += name;
                ini.Add(newgroup, new());
            }

            foreach (MemberInfo member in value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (GetAttribute<IniIgnoreAttribute>(member) != null)
                    continue;

                string membername = member.Name;
                IniNameAttribute? nameAttribute = GetAttribute<IniNameAttribute>(member);
                if (nameAttribute != null)
                    membername = nameAttribute.Name;

                object? item;
                object? defval;
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)member;
                        item = field.GetValue(value);
                        defval = field.FieldType.GetDefaultValue();
                        break;
                    case MemberTypes.Property:

                        PropertyInfo property = (PropertyInfo)member;
                        defval = property.PropertyType.GetDefaultValue();
                        if (property.GetIndexParameters().Length > 0)
                            continue;

                        MethodInfo? getmethod = property.GetGetMethod();
                        if (getmethod == null)
                            continue;
                        item = getmethod.Invoke(value, null);

                        break;
                    default:
                        continue;
                }

                DefaultValueAttribute? defattr = GetAttribute<DefaultValueAttribute>(member);
                if (defattr != null)
                    defval = defattr.Value;

                if (GetAttribute<IniAlwaysIncludeAttribute>(member) != null || !object.Equals(item, defval))
                {
                    IniCollectionSettings settings = GetAttribute<IniCollectionAttribute>(member)?.Settings ?? defaultCollectionSettings;
                    TypeConverter? conv = GetConverterFromAttribute(member);

                    SerializeInternal(membername, item, ini, newgroup, false, settings, conv);
                }
            }
        }

    }
}