global using IniGroup = System.Collections.Generic.Dictionary<string, string>;
global using IniDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
global using IniNameValue = System.Collections.Generic.KeyValuePair<string, string>;
global using IniNameGroup = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.Dictionary<string, string>>;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SATools.SACommon.Ini
{
    internal static class IniSerializeHelper
    {
        internal static readonly IniCollectionSettings initialCollectionSettings
            = new(IniCollectionMode.IndexOnly);

        internal static readonly IniCollectionSettings defaultCollectionSettings
            = new(IniCollectionMode.Normal);

        /// <summary>
        /// Gets the default value of a type
        /// </summary>
        /// <param name="type">Type to get the default value of</param>
        /// <returns></returns>
        public static object? GetDefaultValue(this Type type)
            => type.IsValueType ? Activator.CreateInstance(type) : null;

        /// <summary>
        /// Checks if a type is complex
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="converter"><Typeconverter to use/param>
        /// <returns></returns>
        public static bool IsComplexType(this Type type, TypeConverter? converter)
        {
            if (Type.GetTypeCode(type) != TypeCode.Object)
                return false;

            converter ??= TypeDescriptor.GetConverter(type);

            if (converter is not ComponentConverter
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
        public static string ConvertToString(this object @object, TypeConverter? converter)
        {
            string? result = null;
            converter ??= TypeDescriptor.GetConverter(@object);

            if (@object is string @string)
                result = @string;

            else if (@object is Enum)
                result = @object.ToString();

            else if (converter is not ComponentConverter
                && converter.GetType() != typeof(TypeConverter)
                && converter.CanConvertTo(typeof(string)))
                result = converter.ConvertToInvariantString(@object);

            else if (@object is Type type)
                result = type.AssemblyQualifiedName;

            return result ?? "null";
        }

        /// <summary>
        /// Converts a string to a type
        /// </summary>
        /// <param name="type">Type to convert to</param>
        /// <param name="value">String to convert</param>
        /// <param name="converter">Converter to use</param>
        /// <returns></returns>
        public static object? ConvertFromString(this Type type, string value, TypeConverter? converter)
        {
            converter ??= TypeDescriptor.GetConverter(type);

            if (converter is not ComponentConverter
                && converter.GetType() != typeof(TypeConverter)
                && converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFromInvariantString(value);

            if (type == typeof(Type))
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
        public static bool ImplementsGenericDefinition(this Type type, Type genericInterfaceDefinition, [MaybeNullWhen(false)] out Type implementingType)
        {
            if (type.IsInterface && type.IsGenericType)
            {
                Type interfaceDefinition = type.GetGenericTypeDefinition();
                if (genericInterfaceDefinition == interfaceDefinition)
                {
                    implementingType = type;
                    return true;
                }
            }

            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType)
                {
                    Type interfaceDefinition = i.GetGenericTypeDefinition();

                    if (genericInterfaceDefinition == interfaceDefinition)
                    {
                        implementingType = i;
                        return true;
                    }
                }
            }
            implementingType = null;
            return false;
        }

        public static T? GetAttribute<T>(System.Reflection.MemberInfo member, bool inherit = true) where T : Attribute
            => (T?)Attribute.GetCustomAttribute(member, typeof(T), inherit);

        public static TypeConverter? GetConverterFromAttribute(System.Reflection.MemberInfo member)
        {
            TypeConverter? conv = null;
            TypeConverterAttribute? convattr = GetAttribute<TypeConverterAttribute>(member);
            if (convattr != null)
            {
                Type? convType = Type.GetType(convattr.ConverterTypeName);
                if (convType == null)
                    throw new NullReferenceException("Converter doesnt exist");
                conv = (TypeConverter?)Activator.CreateInstance(convType);
            }

            return conv;
        }
    }
}
