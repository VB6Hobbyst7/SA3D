using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using IniDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using IniGroup = System.Collections.Generic.Dictionary<string, string>;
using IniNameGroup = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.Dictionary<string, string>>;
using IniNameValue = System.Collections.Generic.KeyValuePair<string, string>;

namespace SATools.SACommon.Ini
{
    /// <summary>
    /// Ini serializer methods
    /// </summary>
    public static class IniSerializer
    {
        private static readonly IniCollectionSettings initialCollectionSettings 
            = new(IniCollectionMode.IndexOnly);

        private static readonly IniCollectionSettings defaultCollectionSettings 
            = new(IniCollectionMode.Normal);

        #region Serializing

        public static void Serialize(object Object, string Filename) 
            => IniFile.Write(Serialize(Object), Filename);

        public static void Serialize(object Object, TypeConverter Converter, string Filename)
            => IniFile.Write(Serialize(Object, Converter), Filename);

        public static void Serialize(object Object, IniCollectionSettings CollectionSettings, string Filename) 
            => IniFile.Write(Serialize(Object, CollectionSettings), Filename);

        public static void Serialize(object Object, IniCollectionSettings CollectionSettings, TypeConverter Converter, string Filename) 
            => IniFile.Write(Serialize(Object, CollectionSettings, Converter), Filename);

        public static IniDictionary Serialize(object Object) 
            => Serialize(Object, initialCollectionSettings, (TypeConverter)null);

        public static IniDictionary Serialize(object Object, TypeConverter Converter) 
            => Serialize(Object, initialCollectionSettings, Converter);

        public static IniDictionary Serialize(object Object, IniCollectionSettings CollectionSettings) 
            => Serialize(Object, CollectionSettings, (TypeConverter)null);

        public static IniDictionary Serialize(object Object, IniCollectionSettings CollectionSettings, TypeConverter Converter)
        {
            IniDictionary ini = new IniDictionary() { { string.Empty, new IniGroup() } };
            SerializeInternal("value", Object, ini, string.Empty, true, CollectionSettings, Converter);
            return ini;
        }

        private static void SerializeInternal(string name, object value, IniDictionary ini, string groupName, bool rootObject, IniCollectionSettings collectionSettings, TypeConverter converter)
        {
            IniGroup group = ini[groupName];
            if(value == null || value == DBNull.Value)
                return;
            if(!value.GetType().IsComplexType(converter))
            {
                group.Add(name, value.ConvertToString(converter));
                return;
            }
            if(value is IList)
            {
                int i = collectionSettings.StartIndex;
                switch(collectionSettings.Mode)
                {
                    case IniCollectionMode.Normal:
                        foreach(object item in (IList)value)
                            SerializeInternal(name + "[" + (i++).ConvertToString(collectionSettings.KeyConverter) + "]", item, ini, groupName, false, defaultCollectionSettings, collectionSettings.ValueConverter);
                        return;
                    case IniCollectionMode.IndexOnly:
                        foreach(object item in (IList)value)
                            SerializeInternal((i++).ConvertToString(collectionSettings.KeyConverter), item, ini, groupName, false, defaultCollectionSettings, collectionSettings.ValueConverter);
                        return;
                    case IniCollectionMode.NoSquareBrackets:
                        foreach(object item in (IList)value)
                            SerializeInternal(name + (i++).ConvertToString(collectionSettings.KeyConverter), item, ini, groupName, false, defaultCollectionSettings, collectionSettings.ValueConverter);
                        return;
                    case IniCollectionMode.SingleLine:
                        List<string> line = new List<string>();
                        foreach(object item in (IList)value)
                            line.Add(item.ConvertToString(collectionSettings.ValueConverter));
                        group.Add(name, string.Join(collectionSettings.Format, line.ToArray()));
                        return;
                }
            }
            if(value is IDictionary)
            {
                switch(collectionSettings.Mode)
                {
                    case IniCollectionMode.Normal:
                        foreach(DictionaryEntry item in (IDictionary)value)
                            SerializeInternal(name + "[" + item.Key.ConvertToString(collectionSettings.KeyConverter) + "]", item.Value, ini, groupName, false, defaultCollectionSettings, collectionSettings.ValueConverter);
                        return;
                    case IniCollectionMode.IndexOnly:
                        foreach(DictionaryEntry item in (IDictionary)value)
                            SerializeInternal(item.Key.ConvertToString(collectionSettings.KeyConverter), item.Value, ini, groupName, false, defaultCollectionSettings, collectionSettings.ValueConverter);
                        return;
                    case IniCollectionMode.NoSquareBrackets:
                        foreach(DictionaryEntry item in (IDictionary)value)
                            SerializeInternal(name + item.Key.ConvertToString(collectionSettings.KeyConverter), item.Value, ini, groupName, false, defaultCollectionSettings, collectionSettings.ValueConverter);
                        return;
                    case IniCollectionMode.SingleLine:
                        throw new InvalidOperationException("Cannot serialize IDictionary with IniCollectionMode.SingleLine!");
                }
            }
            string newgroup = groupName;
            if(!rootObject)
            {
                if(!string.IsNullOrEmpty(newgroup))
                    newgroup += '.';
                newgroup += name;
                ini.Add(newgroup, new Dictionary<string, string>());
            }
            foreach(MemberInfo member in value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if(Attribute.GetCustomAttribute(member, typeof(IniIgnoreAttribute), true) != null)
                    continue;
                string membername = member.Name;
                if(Attribute.GetCustomAttribute(member, typeof(IniNameAttribute), true) != null)
                    membername = ((IniNameAttribute)Attribute.GetCustomAttribute(member, typeof(IniNameAttribute), true)).Name;
                object item;
                object defval;
                switch(member.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)member;
                        item = field.GetValue(value);
                        defval = field.FieldType.GetDefaultValue();
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo)member;
                        defval = property.PropertyType.GetDefaultValue();
                        if(property.GetIndexParameters().Length > 0)
                            continue;
                        MethodInfo getmethod = property.GetGetMethod();
                        if(getmethod == null)
                            continue;
                        item = getmethod.Invoke(value, null);
                        break;
                    default:
                        continue;
                }
                DefaultValueAttribute defattr = (DefaultValueAttribute)Attribute.GetCustomAttribute(member, typeof(DefaultValueAttribute), true);
                if(defattr != null)
                    defval = defattr.Value;
                if(Attribute.GetCustomAttribute(member, typeof(IniAlwaysIncludeAttribute), true) != null || !object.Equals(item, defval))
                {
                    IniCollectionSettings settings = defaultCollectionSettings;
                    IniCollectionAttribute collattr = (IniCollectionAttribute)Attribute.GetCustomAttribute(member, typeof(IniCollectionAttribute));
                    if(collattr != null)
                        settings = collattr.Settings;
                    TypeConverter conv = null;
                    TypeConverterAttribute convattr = (TypeConverterAttribute)Attribute.GetCustomAttribute(member, typeof(TypeConverterAttribute));
                    if(convattr != null)
                        conv = (TypeConverter)Activator.CreateInstance(Type.GetType(convattr.ConverterTypeName));
                    SerializeInternal(membername, item, ini, newgroup, false, settings, conv);
                }
            }
        }

        #endregion

        #region Generic Deserializing

        public static T Deserialize<T>(string filename) 
            => Deserialize<T>(IniFile.Read(filename), (TypeConverter)null);

        public static T Deserialize<T>(string filename, TypeConverter Converter) 
            => Deserialize<T>(IniFile.Read(filename), Converter);

        public static T Deserialize<T>(IniDictionary INI) 
            => (T)Deserialize(typeof(T), INI, (TypeConverter)null);

        public static T Deserialize<T>(IniDictionary INI, TypeConverter Converter) 
            => (T)Deserialize(typeof(T), INI, Converter);

        public static T Deserialize<T>(string filename, IniCollectionSettings CollectionSettings) 
            => Deserialize<T>(IniFile.Read(filename), CollectionSettings, null);

        public static T Deserialize<T>(string filename, IniCollectionSettings CollectionSettings, TypeConverter Converter) 
            => Deserialize<T>(IniFile.Read(filename), CollectionSettings, Converter);

        public static T Deserialize<T>(IniDictionary INI, IniCollectionSettings CollectionSettings) 
            => (T)Deserialize(typeof(T), INI, CollectionSettings, null);

        public static T Deserialize<T>(IniDictionary INI, IniCollectionSettings CollectionSettings, TypeConverter Converter) 
            => (T)Deserialize(typeof(T), INI, CollectionSettings, Converter);

        #endregion

        #region Deserializing

        public static object Deserialize(Type type, string filepath) 
            => Deserialize(type, IniFile.Read(filepath), (TypeConverter)null);

        public static object Deserialize(Type type, string Filename, TypeConverter Converter) 
            => Deserialize(type, IniFile.Read(Filename), Converter);

        public static object Deserialize(Type type, IniDictionary INI) 
            => Deserialize(type, INI, initialCollectionSettings, null);

        public static object Deserialize(Type type, IniDictionary INI, TypeConverter Converter) 
            => Deserialize(type, INI, initialCollectionSettings, Converter);

        public static object Deserialize(Type type, string Filename, IniCollectionSettings CollectionSettings) 
            => Deserialize(type, IniFile.Read(Filename), CollectionSettings, null);

        public static object Deserialize(Type type, string Filename, IniCollectionSettings CollectionSettings, TypeConverter Converter) 
            => Deserialize(type, IniFile.Read(Filename), CollectionSettings, Converter);

        public static object Deserialize(Type type, IniDictionary INI, IniCollectionSettings CollectionSettings) 
            => Deserialize(type, INI, CollectionSettings, null);

        public static object Deserialize(Type type, IniDictionary INI, IniCollectionSettings CollectionSettings, TypeConverter Converter)
        {
            object Object;
            IniDictionary ini = new();
            ini = IniFile.Combine(ini, INI);
            Object = DeserializeInternal("value", type, type.GetDefaultValue(), ini, string.Empty, true, CollectionSettings, Converter);
            return Object;
        }

        private static object DeserializeInternal(string name, Type type, object defaultvalue, IniDictionary ini, string groupName, bool rootObject, IniCollectionSettings collectionSettings, TypeConverter converter)
        {
            string fullname = groupName;
            if(!rootObject)
            {
                if(!string.IsNullOrEmpty(fullname))
                    fullname += '.';
                fullname += name;
            }
            if(!ini.ContainsKey(groupName))
                return defaultvalue;
            Dictionary<string, string> group = ini[groupName];
            if(!type.IsComplexType(converter))
            {
                if(group.ContainsKey(name))
                {
                    object converted = type.ConvertFromString(group[name], converter);
                    group.Remove(name);
                    if(converted != null)
                        return converted;
                }
                return defaultvalue;
            }
            if(type.IsArray)
            {
                Type valuetype = type.GetElementType();
                int maxind = int.MinValue;
                TypeConverter keyconverter = collectionSettings.KeyConverter ?? new Int32Converter();
                if(!IsComplexType(valuetype, collectionSettings.ValueConverter))
                {
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(IniNameValue item in group)
                                if(item.Key.StartsWith(name + "["))
                                {
                                    int key = (int)keyconverter.ConvertFromInvariantString(item.Key.Substring(name.Length + 1, item.Key.Length - (name.Length + 2)));
                                    maxind = Math.Max(key, maxind);
                                }
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(IniNameValue item in group)
                                try
                                {
                                    maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key), maxind);
                                }
                                catch { }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(IniNameValue item in group)
                                if(item.Key.StartsWith(name))
                                    try
                                    {
                                        maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key.Substring(name.Length)), maxind);
                                    }
                                    catch { }
                            break;
                        case IniCollectionMode.SingleLine:
                            if(group.ContainsKey(name))
                            {
                                string[] items;
                                if(string.IsNullOrEmpty(group[name]))
                                    items = new string[0];
                                else
                                    items = group[name].Split(new[] { collectionSettings.Format }, StringSplitOptions.None);
                                Array _obj = Array.CreateInstance(valuetype, items.Length);
                                for(int i = 0; i < items.Length; i++)
                                    _obj.SetValue(valuetype.ConvertFromString(items[i], collectionSettings.ValueConverter), i);
                                group.Remove(name);
                                return _obj;
                            }
                            else
                                return null;
                    }
                }
                else
                {
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(IniNameGroup item in ini)
                                if(item.Key.StartsWith(fullname + "["))
                                {
                                    int key = (int)keyconverter.ConvertFromInvariantString(item.Key.Substring(fullname.Length + 1, item.Key.Length - (fullname.Length + 2)));
                                    maxind = Math.Max(key, maxind);
                                }
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(IniNameGroup item in ini)
                                if(!string.IsNullOrEmpty(item.Key))
                                    try
                                    {
                                        maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key), maxind);
                                    }
                                    catch { }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(IniNameGroup item in ini)
                                if(item.Key.StartsWith(fullname))
                                    try
                                    {
                                        maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key.Substring(fullname.Length)), maxind);
                                    }
                                    catch { }
                            break;
                        case IniCollectionMode.SingleLine:
                            throw new InvalidOperationException("Cannot deserialize type " + valuetype + " with IniCollectionMode.SingleLine!");
                    }
                }
                if(maxind == int.MinValue)
                    return Array.CreateInstance(valuetype, 0);
                int length = maxind + 1 - (collectionSettings.Mode == IniCollectionMode.SingleLine ? 0 : collectionSettings.StartIndex);
                Array obj = Array.CreateInstance(valuetype, length);
                if(!IsComplexType(valuetype, collectionSettings.ValueConverter))
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            for(int i = 0; i < length; i++)
                            {
                                string keyname = name + "[" + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex) + "]";
                                if(group.ContainsKey(keyname))
                                {
                                    obj.SetValue(valuetype.ConvertFromString(group[keyname], collectionSettings.ValueConverter), i);
                                    group.Remove(keyname);
                                }
                                else
                                    obj.SetValue(valuetype.GetDefaultValue(), i);
                            }
                            break;
                        case IniCollectionMode.IndexOnly:
                            for(int i = 0; i < length; i++)
                            {
                                string keyname = keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex);
                                if(group.ContainsKey(keyname))
                                {
                                    obj.SetValue(valuetype.ConvertFromString(group[keyname], collectionSettings.ValueConverter), i);
                                    group.Remove(keyname);
                                }
                                else
                                    obj.SetValue(valuetype.GetDefaultValue(), i);
                            }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            for(int i = 0; i < length; i++)
                            {
                                string keyname = name + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex);
                                if(group.ContainsKey(keyname))
                                {
                                    obj.SetValue(valuetype.ConvertFromString(group[keyname], collectionSettings.ValueConverter), i);
                                    group.Remove(keyname);
                                }
                                else
                                    obj.SetValue(valuetype.GetDefaultValue(), i);
                            }
                            break;
                    }
                else
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            for(int i = 0; i < length; i++)
                                obj.SetValue(DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, fullname + "[" + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex) + "]", true, defaultCollectionSettings, collectionSettings.ValueConverter), i);
                            break;
                        case IniCollectionMode.IndexOnly:
                            for(int i = 0; i < length; i++)
                                obj.SetValue(DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex), true, defaultCollectionSettings, collectionSettings.ValueConverter), i);
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            for(int i = 0; i < length; i++)
                                obj.SetValue(DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, fullname + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex), true, defaultCollectionSettings, collectionSettings.ValueConverter), i);
                            break;
                    }
                return obj;
            }
            if(ImplementsGenericDefinition(type, typeof(IList<>), out Type generictype))
            {
                object obj = Activator.CreateInstance(type);
                Type valuetype = generictype.GetGenericArguments()[0];
                CollectionDeserializer deserializer = (CollectionDeserializer)Activator.CreateInstance(typeof(ListDeserializer<>).MakeGenericType(valuetype));
                deserializer.Deserialize(obj, group, groupName, collectionSettings, name, ini, fullname);
                return obj;
            }
            if(type.ImplementsGenericDefinition(typeof(IDictionary<,>), out generictype))
            {
                object obj = Activator.CreateInstance(type);
                Type keytype = generictype.GetGenericArguments()[0];
                Type valuetype = generictype.GetGenericArguments()[1];
                if(keytype.IsComplexType(collectionSettings.KeyConverter))
                    return obj;
                CollectionDeserializer deserializer = (CollectionDeserializer)Activator.CreateInstance(typeof(DictionaryDeserializer<,>).MakeGenericType(keytype, valuetype));
                deserializer.Deserialize(obj, group, groupName, collectionSettings, name, ini, fullname);
                return obj;
            }
            object result = Activator.CreateInstance(type);
            MemberInfo collection = null;
            foreach(MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if(Attribute.GetCustomAttribute(member, typeof(IniIgnoreAttribute), true) != null)
                    continue;
                string membername = member.Name;
                if(Attribute.GetCustomAttribute(member, typeof(IniNameAttribute), true) != null)
                    membername = ((IniNameAttribute)Attribute.GetCustomAttribute(member, typeof(IniNameAttribute), true)).Name;
                IniCollectionSettings colset = defaultCollectionSettings;
                IniCollectionAttribute colattr = (IniCollectionAttribute)Attribute.GetCustomAttribute(member, typeof(IniCollectionAttribute), true);
                if(colattr != null)
                    colset = colattr.Settings;
                TypeConverter conv = null;
                TypeConverterAttribute convattr = (TypeConverterAttribute)Attribute.GetCustomAttribute(member, typeof(TypeConverterAttribute), true);
                if(convattr != null)
                    conv = (TypeConverter)Activator.CreateInstance(Type.GetType(convattr.ConverterTypeName));
                switch(member.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)member;
                        if(colset.Mode == IniCollectionMode.IndexOnly && typeof(ICollection).IsAssignableFrom(field.FieldType))
                        {
                            if(collection != null)
                                throw new Exception("IniCollectionMode.IndexOnly cannot be used on multiple members of a Type.");
                            collection = member;
                            continue;
                        }
                        object defval = field.FieldType.GetDefaultValue();
                        DefaultValueAttribute defattr = (DefaultValueAttribute)Attribute.GetCustomAttribute(member, typeof(DefaultValueAttribute), true);
                        if(defattr != null)
                            defval = defattr.Value;
                        field.SetValue(result, DeserializeInternal(membername, field.FieldType, defval, ini, fullname, false, colset, conv));
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo)member;
                        if(property.GetIndexParameters().Length > 0)
                            continue;
                        if(colset.Mode == IniCollectionMode.IndexOnly && typeof(ICollection).IsAssignableFrom(property.PropertyType))
                        {
                            if(collection != null)
                                throw new Exception("IniCollectionMode.IndexOnly cannot be used on multiple members of a Type.");
                            collection = member;
                            continue;
                        }
                        defval = property.PropertyType.GetDefaultValue();
                        defattr = (DefaultValueAttribute)Attribute.GetCustomAttribute(member, typeof(DefaultValueAttribute), true);
                        if(defattr != null)
                            defval = defattr.Value;
                        object propval = DeserializeInternal(membername, property.PropertyType, defval, ini, fullname, false, colset, conv);
                        MethodInfo setmethod = property.GetSetMethod();
                        if(setmethod == null)
                            continue;
                        setmethod.Invoke(result, new object[] { propval });
                        break;
                }
            }
            if(collection != null)
                switch(collection.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo field = (FieldInfo)collection;
                        field.SetValue(result, DeserializeInternal(collection.Name, field.FieldType, field.FieldType.GetDefaultValue(), ini, fullname, false, ((IniCollectionAttribute)Attribute.GetCustomAttribute(collection, typeof(IniCollectionAttribute), true)).Settings, null));
                        break;
                    case MemberTypes.Property:
                        PropertyInfo property = (PropertyInfo)collection;
                        object propval = DeserializeInternal(collection.Name, property.PropertyType, property.PropertyType.GetDefaultValue(), ini, fullname, false, ((IniCollectionAttribute)Attribute.GetCustomAttribute(collection, typeof(IniCollectionAttribute), true)).Settings, null);
                        MethodInfo setmethod = property.GetSetMethod();
                        if(setmethod == null)
                            break;
                        setmethod.Invoke(result, new object[] { propval });
                        break;
                }
            ini.Remove(fullname);
            return result;
        }

        #endregion

        #region Helper stuff

        /// <summary>
        /// Gets the default value of a type
        /// </summary>
        /// <param name="type">Type to get the default value of</param>
        /// <returns></returns>
        private static object GetDefaultValue(this Type type) 
            => type.IsValueType ? Activator.CreateInstance(type) : null;

        /// <summary>
        /// Checks if a type is complex
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="converter"><Typeconverter to use/param>
        /// <returns></returns>
        private static bool IsComplexType(this Type type, TypeConverter converter)
        {
            if(Type.GetTypeCode(type) != TypeCode.Object)
                return false;

            if(converter == null)
                converter = TypeDescriptor.GetConverter(type);

            if(converter is not ComponentConverter 
                && converter.GetType() != typeof(TypeConverter)
                && converter.CanConvertTo(typeof(string)) & converter.CanConvertFrom(typeof(string)))
                return false;

            return type.GetType() != typeof(Type);
        }

        /// <summary>
        /// Converts an object to a string
        /// </summary>
        /// <param name="object">Object to convert</param>
        /// <param name="converter">Typeconverter to use</param>
        /// <returns></returns>
        private static string ConvertToString(this object @object, TypeConverter converter)
        {
            if(@object is string)
                return (string)@object;

            if(@object is Enum)
                return @object.ToString();

            if(converter == null)
                converter = TypeDescriptor.GetConverter(@object);

            if(!(converter is ComponentConverter) 
                && converter.GetType() != typeof(TypeConverter)
                && converter.CanConvertTo(typeof(string)))
                return converter.ConvertToInvariantString(@object);

            if(@object is Type type)
                return type.AssemblyQualifiedName;

            return null;
        }

        /// <summary>
        /// Converts a string to a type
        /// </summary>
        /// <param name="type">Type to convert to</param>
        /// <param name="value">String to convert</param>
        /// <param name="converter">Converter to use</param>
        /// <returns></returns>
        private static object ConvertFromString(this Type type, string value, TypeConverter converter)
        {
            if(converter == null)
                converter = TypeDescriptor.GetConverter(type);

            if(!(converter is ComponentConverter) 
                && converter.GetType() != typeof(TypeConverter)
                && converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFromInvariantString(value);

            if(type == typeof(Type))
                return Type.GetType(value);

            return type.GetDefaultValue();
        }

        /// <summary>
        /// Checks if a type implements a generic definition
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="genericInterfaceDefinition">Interface to check inside type</param>
        /// <param name="implementingType">Type implemented</param>
        /// <returns></returns>
        private static bool ImplementsGenericDefinition(this Type type, Type genericInterfaceDefinition, out Type implementingType)
        {
            if(type.IsInterface && type.IsGenericType)
            {
                Type interfaceDefinition = type.GetGenericTypeDefinition();
                if(genericInterfaceDefinition == interfaceDefinition)
                {
                    implementingType = type;
                    return true;
                }
            }

            foreach(Type i in type.GetInterfaces())
            {
                if(i.IsGenericType)
                {
                    Type interfaceDefinition = i.GetGenericTypeDefinition();

                    if(genericInterfaceDefinition == interfaceDefinition)
                    {
                        implementingType = i;
                        return true;
                    }
                }
            }
            implementingType = null;
            return false;
        }

        private abstract class CollectionDeserializer
        {
            public abstract void Deserialize(object listObj, IniGroup group, string groupName, IniCollectionSettings collectionSettings, string name, IniDictionary ini, string fullname);
        }

        private sealed class ListDeserializer<T> : CollectionDeserializer
        {
            public override void Deserialize(object listObj, IniGroup group, string groupName, IniCollectionSettings collectionSettings, string name, IniDictionary ini, string fullname)
            {
                Type valuetype = typeof(T);
                IList<T> list = (IList<T>)listObj;
                int maxind = int.MinValue;
                TypeConverter keyconverter = collectionSettings.KeyConverter ?? new Int32Converter();
                if(!IsComplexType(valuetype, collectionSettings.ValueConverter))
                {
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(IniNameValue item in group)
                                if(item.Key.StartsWith(name + "["))
                                {
                                    int key = (int)keyconverter.ConvertFromInvariantString(item.Key.Substring(name.Length + 1, item.Key.Length - (name.Length + 2)));
                                    maxind = Math.Max(key, maxind);
                                }
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(IniNameValue item in group)
                                try
                                {
                                    maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key), maxind);
                                }
                                catch { }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(IniNameValue item in group)
                                if(item.Key.StartsWith(name))
                                    try
                                    {
                                        maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key.Substring(name.Length)), maxind);
                                    }
                                    catch { }
                            break;
                        case IniCollectionMode.SingleLine:
                            if(group.ContainsKey(name))
                            {
                                if(!string.IsNullOrEmpty(group[name]))
                                {
                                    string[] items = group[name].Split(new[] { collectionSettings.Format }, StringSplitOptions.None);
                                    for(int i = 0; i < items.Length; i++)
                                        list.Add((T)valuetype.ConvertFromString(items[i], collectionSettings.ValueConverter));
                                }
                                group.Remove(name);
                            }
                            break;
                    }
                }
                else
                {
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(IniNameGroup item in ini)
                                if(item.Key.StartsWith(fullname + "["))
                                {
                                    int key = (int)keyconverter.ConvertFromInvariantString(item.Key.Substring(fullname.Length + 1, item.Key.Length - (fullname.Length + 2)));
                                    maxind = Math.Max(key, maxind);
                                }
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(IniNameGroup item in ini)
                                if(!string.IsNullOrEmpty(item.Key))
                                    try
                                    {
                                        maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key), maxind);
                                    }
                                    catch { }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(IniNameGroup item in ini)
                                if(item.Key.StartsWith(fullname))
                                    try
                                    {
                                        maxind = Math.Max((int)keyconverter.ConvertFromInvariantString(item.Key.Substring(fullname.Length)), maxind);
                                    }
                                    catch { }
                            break;
                        case IniCollectionMode.SingleLine:
                            throw new InvalidOperationException("Cannot deserialize type " + valuetype + " with IniCollectionMode.SingleLine!");
                    }
                }
                if(maxind == int.MinValue)
                    return;
                int length = maxind + 1 - (collectionSettings.Mode == IniCollectionMode.SingleLine ? 0 : collectionSettings.StartIndex);
                if(!IsComplexType(valuetype, collectionSettings.ValueConverter))
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            for(int i = 0; i < length; i++)
                            {
                                string keyname = name + "[" + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex) + "]";
                                if(group.ContainsKey(keyname))
                                {
                                    list.Add((T)valuetype.ConvertFromString(group[keyname], collectionSettings.ValueConverter));
                                    group.Remove(keyname);
                                }
                                else
                                    list.Add((T)valuetype.GetDefaultValue());
                            }
                            break;
                        case IniCollectionMode.IndexOnly:
                            for(int i = 0; i < length; i++)
                            {
                                string keyname = keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex);
                                if(group.ContainsKey(keyname))
                                {
                                    list.Add((T)valuetype.ConvertFromString(group[keyname], collectionSettings.ValueConverter));
                                    group.Remove(keyname);
                                }
                                else
                                    list.Add((T)valuetype.GetDefaultValue());
                            }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            for(int i = 0; i < length; i++)
                            {
                                string keyname = name + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex);
                                if(group.ContainsKey(keyname))
                                {
                                    list.Add((T)valuetype.ConvertFromString(group[keyname], collectionSettings.ValueConverter));
                                    group.Remove(keyname);
                                }
                                else
                                    list.Add(default);
                            }
                            break;
                    }
                else
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            for(int i = 0; i < length; i++)
                                list.Add((T)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, fullname + "[" + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex) + "]", true, defaultCollectionSettings, collectionSettings.ValueConverter));
                            break;
                        case IniCollectionMode.IndexOnly:
                            for(int i = 0; i < length; i++)
                                list.Add((T)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex), true, defaultCollectionSettings, collectionSettings.ValueConverter));
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            for(int i = 0; i < length; i++)
                                list.Add((T)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, fullname + keyconverter.ConvertToInvariantString(i + collectionSettings.StartIndex).ToString(), true, defaultCollectionSettings, collectionSettings.ValueConverter));
                            break;
                    }
            }
        }

        private sealed class DictionaryDeserializer<TKey, TValue> : CollectionDeserializer
        {
            public override void Deserialize(object listObj, IniGroup group, string groupName, IniCollectionSettings collectionSettings, string name, IniDictionary ini, string fullname)
            {
                Type keytype = typeof(TKey);
                Type valuetype = typeof(TValue);
                IDictionary<TKey, TValue> list = (IDictionary<TKey, TValue>)listObj;
                if(!valuetype.IsComplexType(collectionSettings.ValueConverter))
                {
                    List<string> items = new List<string>();
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(IniNameValue item in group)
                                if(item.Key.StartsWith(name + "["))
                                    items.Add(item.Key.Substring(name.Length + 1, item.Key.Length - (name.Length + 2)));
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(IniNameValue item in group)
                                items.Add(item.Key);
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(IniNameValue item in group)
                                if(item.Key.StartsWith(name))
                                    items.Add(item.Key.Substring(name.Length));
                            break;
                        case IniCollectionMode.SingleLine:
                            throw new InvalidOperationException("Cannot deserialize IDictionary<TKey, TValue> with IniCollectionMode.SingleLine!");
                    }
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(string item in items)
                            {
                                list.Add((TKey)keytype.ConvertFromString(item, collectionSettings.KeyConverter), (TValue)valuetype.ConvertFromString(group[name + "[" + item + "]"], collectionSettings.ValueConverter));
                                group.Remove(name + "[" + item + "]");
                            }
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(string item in items)
                            {
                                list.Add((TKey)keytype.ConvertFromString(item, collectionSettings.KeyConverter), (TValue)valuetype.ConvertFromString(group[item], collectionSettings.ValueConverter));
                                group.Remove(item);
                            }
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(string item in items)
                            {
                                list.Add((TKey)keytype.ConvertFromString(item, collectionSettings.KeyConverter), (TValue)valuetype.ConvertFromString(group[name + item], collectionSettings.ValueConverter));
                                group.Remove(name + item);
                            }
                            break;
                    }
                }
                else
                {
                    List<string> items = new List<string>();
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(IniNameGroup item in ini)
                                if(item.Key.StartsWith(name + "["))
                                    items.Add(item.Key.Substring(name.Length + 1, item.Key.Length - (name.Length + 2)));
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(IniNameGroup item in ini)
                                if(item.Key != groupName)
                                    items.Add(item.Key);
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(IniNameGroup item in ini)
                                if(item.Key.StartsWith(name))
                                    items.Add(item.Key.Substring(name.Length));
                            break;
                        case IniCollectionMode.SingleLine:
                            throw new InvalidOperationException("Cannot deserialize IDictionary<TKey, TValue> with IniCollectionMode.SingleLine!");
                    }
                    switch(collectionSettings.Mode)
                    {
                        case IniCollectionMode.Normal:
                            foreach(string item in items)
                                list.Add((TKey)keytype.ConvertFromString(item, collectionSettings.KeyConverter), (TValue)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, name + "[" + item + "]", true, defaultCollectionSettings, collectionSettings.ValueConverter));
                            break;
                        case IniCollectionMode.IndexOnly:
                            foreach(string item in items)
                                list.Add((TKey)keytype.ConvertFromString(item, collectionSettings.KeyConverter), (TValue)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, item, true, defaultCollectionSettings, collectionSettings.ValueConverter));
                            break;
                        case IniCollectionMode.NoSquareBrackets:
                            foreach(string item in items)
                                list.Add((TKey)keytype.ConvertFromString(item, collectionSettings.KeyConverter), (TValue)DeserializeInternal("value", valuetype, valuetype.GetDefaultValue(), ini, name + item, true, defaultCollectionSettings, collectionSettings.ValueConverter));
                            break;
                    }
                }
            }
        }

        #endregion
    }

}
