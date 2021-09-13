using SAWPF.BaseViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace SAModel.WPF.Inspector.Viewmodel
{
    public enum HexadecimalMode
    {
        /// <summary>
        /// Don't display the number as a hexadecimal
        /// </summary>
        NoHex,

        /// <summary>
        /// Display it as both
        /// </summary>
        HybridHex,

        /// <summary>
        /// Only display as hexadecimal
        /// </summary>
        OnlyHex
    }

    /// <summary>
    /// A single inspector element
    /// </summary>
    public struct InspectorElement
    {
        public object Source { get; }

        public object Value
            => Property.GetValue(Source);

        /// <summary>
        /// Property name
        /// </summary>
        public PropertyInfo Property { get; }

        public string BindingPath 
            => Index > -1 ? "Value" : "Source." + Property.Name;

        public int Index { get; set; }

        /// <summary>
        /// The type of the property
        /// </summary>
        public Type PropertyType
            => Index > -1 ? Value.GetType() : Property.PropertyType;

        /// <summary>
        /// Whether the property points at an Array
        /// </summary>
        public bool IsCollection
            => PropertyType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; }

        public string DetailName
        {
            get
            {
                if(Source == null)
                    return "Null";

                if(IsCollection)
                {
                    IList collection = (IList)Value;
                    Type[] elementType = PropertyType.GetGenericArguments();
                    return $"{elementType[0].Name}[{collection.Count}]";
                }

                string conv = Value.ToString();
                string type = PropertyType.ToString();

                return conv.Equals(type) ? PropertyType.Name : conv;
            }
        }

        /// <summary>
        /// Property Tooltip
        /// </summary>
        public string Tooltip { get; }

        /// <summary>
        /// Whether the property is readonly
        /// </summary>
        public bool IsReadonly { get; }

        /// <summary>
        /// Whether, upon selection, the background should change
        /// </summary>
        public bool SelectBackground
            => !PropertyType.IsEnum;

        /// <summary>
        /// Wheth
        /// </summary>
        public HexadecimalMode Hexadecimal { get; }

        public InspectorElement(object source, PropertyInfo property, string name, string tooltip, bool isReadonly, HexadecimalMode hexadecimal, int index = -1)
        {
            Source = source;
            Property = property;
            DisplayName = name;
            Tooltip = tooltip;
            IsReadonly = isReadonly;
            Hexadecimal = hexadecimal;
            Index = index;
        }
    }


    /// <summary>
    /// Base viewmodel for creating an inspector window
    /// </summary>
    internal abstract class InspectorViewModel : BaseViewModel
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        protected class IgnoreAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        protected class DisplayNameAttribute : Attribute
        {
            public string DisplayName { get; }

            public DisplayNameAttribute(string displayName)
                => DisplayName = displayName;
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        protected class TooltipAttribute : Attribute
        {
            public string Tooltip { get; }

            public TooltipAttribute(string tooltip)
                => Tooltip = tooltip;
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        protected class ReadonlyAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        protected class HexadecimalAttribute : Attribute
        {
            public bool Hybrid { get; }

            public HexadecimalAttribute()
                => Hybrid = false;

            public HexadecimalAttribute(bool hybrid)
                => Hybrid = hybrid;
        }

        private static readonly Type[] _inspectorTypes;

        [Ignore]
        public List<InspectorElement> InspectorElements { get; }

        /// <summary>
        /// Data source
        /// </summary>
        protected readonly object _source;

        protected InspectorViewModel(object source)
        {
            _source = source;
            InspectorElements = GetInspectorElements();
        }

        static InspectorViewModel()
            => _inspectorTypes = typeof(InspectorViewModel).Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(InspectorViewModel))).ToArray();

        public static InspectorViewModel GetViewModel(object source)
        {
            foreach(Type t in _inspectorTypes)
            {
                if(t.Name == "IVm" + source.GetType().Name)
                    return (InspectorViewModel)Activator.CreateInstance(t, source);
            }

            return null;
        }

        protected virtual List<InspectorElement> GetInspectorElements()
        {
            List<InspectorElement> result = new();

            Type type = GetType();
            var properties = type.GetProperties();

            foreach(var p in properties)
            {
                if(!p.CanRead || p.GetCustomAttribute<IgnoreAttribute>() != null)
                    continue;

                string displayName = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name;
                string tooltip = p.GetCustomAttribute<TooltipAttribute>()?.Tooltip;
                bool isReadonly = p.GetCustomAttribute<ReadonlyAttribute>() != null;
                bool? hybridHex = p.GetCustomAttribute<HexadecimalAttribute>()?.Hybrid;
                HexadecimalMode hexMode = hybridHex.HasValue ? (hybridHex == true ? HexadecimalMode.HybridHex : HexadecimalMode.OnlyHex) : HexadecimalMode.NoHex;

                result.Add(new(this, p, displayName, tooltip, isReadonly, hexMode));
            }

            return result;
        }
    }
}
