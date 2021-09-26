using SATools.SAWPF.BaseViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SATools.SAModel.WPF.Inspector.Viewmodel
{

    internal class ListInspectorViewModel<T> : BaseViewModel
    {
        // note: this class only works under the assumption that the user is unable to modify the item count

        public class ListInspectorElement : BaseViewModel, IInspectorInfo
        {
            /// <summary>
            /// Collection wrapper that the element belongs to
            /// </summary>
            private ListInspectorViewModel<T> Collection { get; }

            /// <summary>
            /// index 
            /// </summary>
            public int ValueIndex { get; }

            public T Value
            {
                get => Collection.SourceList[ValueIndex];
                set => Collection.SourceList[ValueIndex] = value;
            }

            #region Interface properties

            public string BindingPath
                => "Value";

            public Type ValueType
                => Value?.GetType() ?? Collection.ContentType;

            object IInspectorInfo.Value
            {
                get => Value;
                set => Value = (T)value;
            }

            public string DetailName
            {
                get
                {
                    if(Value == null)
                        return "Null";

                    string conv = Value.ToString();
                    string type = typeof(T).ToString();

                    return conv.Equals(type) ? typeof(T).Name : conv;
                }
            }

            public bool IsCollection
                => ValueType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));

            public string DisplayName
                => ValueIndex.ToString();

            public string HistoryName
                => $"{Collection.PropertyName}[{ValueIndex}]";

            public string Tooltip
                => $"Element number {ValueIndex}";

            public HexadecimalMode Hexadecimal
                => Collection.Hexadecimal;

            public bool IsReadOnly
                => Collection.ReadonlyCollection;

            public bool SelectBackground
                => !ValueType.IsEnum;

            public bool SmoothScroll => Collection.SmoothScroll;

            #endregion

            public ListInspectorElement(ListInspectorViewModel<T> collection, int valueIndex)
            {
                Collection = collection;
                ValueIndex = valueIndex;
            }
        }

        public string PropertyName { get; }

        public bool ReadonlyCollection
            => !SourceList.GetType().IsArray && SourceList.IsReadOnly;

        public HexadecimalMode Hexadecimal { get; }

        /// <summary>
        /// Source list
        /// </summary>
        public IList<T> SourceList { get; }

        public Type ContentType
            => typeof(T);

        public bool SmoothScroll { get; }

        /// <summary>
        /// Item wrappers to allow access and replacement to source list items
        /// </summary>
        public List<ListInspectorElement> InspectorElements { get; }

        public ListInspectorViewModel(IInspectorInfo info)
        {
            SourceList = (IList<T>)info.Value;
            PropertyName = info.DisplayName;
            Hexadecimal = info.Hexadecimal;
            SmoothScroll = info.SmoothScroll;

            InspectorElements = new();
            for(int i = 0; i < SourceList.Count; i++)
                InspectorElements.Add(new(this, i));
        }
    }
}
