using SAWPF.BaseViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAModel.WPF.Inspector.Viewmodel
{

    /// <summary>
    /// A single inspector element
    /// </summary>
    public struct InspectorElement : IInspectorInfo
    {
        public object Source { get; }

        public PropertyInfo Property { get; }

        #region Interface properties
        public bool SelectBackground
            => !ValueType.IsEnum;

        public string BindingPath
            => $"Source.{Property.Name}";

        public object Value
            => Property.GetValue(Source);

        public Type ValueType
            => Value.GetType();

        public bool IsCollection
            => ValueType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));

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
                    Type[] elementType = ValueType.GetGenericArguments();
                    return $"{elementType[0].Name}[{collection.Count}]";
                }

                string conv = Value.ToString();
                string type = ValueType.ToString();

                return conv.Equals(type) ? ValueType.Name : conv;
            }
        }

        public string Tooltip { get; }

        public HexadecimalMode Hexadecimal { get; }

        public string HistoryName
            => Property.Name;

        public bool IsReadOnly { get; }

        #endregion

        public InspectorElement(object source, PropertyInfo property, string name, string tooltip, bool propertyReadonly, HexadecimalMode hexadecimal)
        {
            Source = source;
            Property = property;
            DisplayName = name;
            Tooltip = tooltip;
            IsReadOnly = propertyReadonly;
            Hexadecimal = hexadecimal;
        }
    }


    /// <summary>
    /// Base viewmodel for creating an inspector window
    /// </summary>
    internal abstract class InspectorViewModel : BaseViewModel
    {
        #region protected Inspector Viewmodel Attributes

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
        protected class HexadecimalAttribute : Attribute
        {
            public bool Hybrid { get; }

            public HexadecimalAttribute()
                => Hybrid = false;

            public HexadecimalAttribute(bool hybrid)
                => Hybrid = hybrid;
        }

        #endregion

        private static readonly Dictionary<Type, Type> _viewmodelTypes;

        [Ignore]
        public List<InspectorElement> InspectorElements { get; }

        protected abstract Type ViewmodelType { get; }

        /// <summary>
        /// Data source
        /// </summary>
        protected readonly object _source;

        protected InspectorViewModel() { }

        protected InspectorViewModel(object source)
        {
            _source = source;
            InspectorElements = GetInspectorElements();
        }

        static InspectorViewModel()
        {
            _viewmodelTypes = new Dictionary<Type, Type>();
            foreach(Type t in typeof(InspectorViewModel).Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(InspectorViewModel))))
            {
                InspectorViewModel ivm = (InspectorViewModel)Activator.CreateInstance(t);
                _viewmodelTypes.Add(ivm.ViewmodelType, t);
            }
        }

        public static bool CheckViewmodelExists(object source) 
            => _viewmodelTypes.ContainsKey(source.GetType());

        /// <summary>
        /// Creates a corresponding viewmodel for an object
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static InspectorViewModel GetViewModel(object source)
        {
            if(!_viewmodelTypes.TryGetValue(source.GetType(), out Type ivmType))
                throw new InvalidInspectorTypeException(source.GetType());

            return (InspectorViewModel)Activator.CreateInstance(ivmType, source);
        }

        /// <summary>
        /// Generates a list of inspector info
        /// </summary>
        /// <returns></returns>
        private List<InspectorElement> GetInspectorElements()
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
                bool? hybridHex = p.GetCustomAttribute<HexadecimalAttribute>()?.Hybrid;
                HexadecimalMode hexMode = hybridHex.HasValue ? (hybridHex == true ? HexadecimalMode.HybridHex : HexadecimalMode.OnlyHex) : HexadecimalMode.NoHex;

                result.Add(new(this, p, displayName, tooltip, !p.CanWrite, hexMode));
            }

            return result;
        }
    }
}
