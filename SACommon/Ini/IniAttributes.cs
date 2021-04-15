using System;
using System.ComponentModel;

namespace SATools.SACommon.Ini
{
    /// <summary>
    /// Defines the Ini collection mode of property or field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class IniCollectionAttribute : Attribute
    {
        /// <summary>
        /// Collection settings
        /// </summary>
        public IniCollectionSettings Settings { get; }

        /// <summary>
        /// Format of the collection
        /// </summary>
        public string Format
        {
            get => Settings.Format; 
            set => Settings.Format = value;
        }

        /// <summary>
        /// The index of the first item in the collection. Does not apply to Dictionary objects or <see cref="IniCollectionMode.SingleLine"/>.
        /// </summary>
        public int StartIndex
        {
            get => Settings.StartIndex;
            set => Settings.StartIndex = value;
        }

        /// <summary>
        /// The <see cref="Type"/> of a <see cref="TypeConverter"/> used to convert indexes/keys to and from <see cref="string"/>.
        /// </summary>
        public Type KeyConverter
        {
            get => Settings.KeyConverter?.GetType();
            set => Settings.KeyConverter = (TypeConverter)Activator.CreateInstance(value);
        }

        /// <summary>
        /// The <see cref="Type"/> of a <see cref="TypeConverter"/> used to convert values to and from <see cref="string"/>.
        /// </summary>
        public Type ValueConverter
        {
            get => Settings.ValueConverter?.GetType();
            set => Settings.ValueConverter = (TypeConverter)Activator.CreateInstance(value);
        }

        /// <param name="mode">Collection mode to use</param>
        public IniCollectionAttribute(IniCollectionMode mode)
        {
            Settings = new IniCollectionSettings(mode);
        }
    }

    /// <summary>
    /// Defines a custom Ini name for a property or field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class IniNameAttribute : Attribute
    {
        /// <param name="name">The custom ini name to use</param>
        public IniNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Name of the property
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Marks a property or field to always be included
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class IniAlwaysIncludeAttribute : Attribute { }

    /// <summary>
    /// Marks a property or field to never be included
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class IniIgnoreAttribute : Attribute { }
}
