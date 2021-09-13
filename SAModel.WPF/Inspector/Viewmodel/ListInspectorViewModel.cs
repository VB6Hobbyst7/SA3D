using SAWPF.BaseViewModel;
using System.Collections.Generic;

namespace SAModel.WPF.Inspector.Viewmodel
{

    internal class ListInspectorViewModel<T> : BaseViewModel
    {
        // note: this class only works under the assumption that the user is unable to modify the item count
        
        public class ListInspectorElement : BaseViewModel
        {
            private ListInspectorViewModel<T> Collection { get; }

            public int ValueIndex { get; }

            public T Value
            {
                get => Collection.SourceList[ValueIndex];
                set => Collection.SourceList[ValueIndex] = value;
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

            public ListInspectorElement(ListInspectorViewModel<T> collection, int valueIndex)
            {
                Collection = collection;
                ValueIndex = valueIndex;
            }
        }

        public int Count => SourceList.Count;

        public bool IsReadonly { get; }

        public IList<T> SourceList { get; }

        public ListInspectorElement[] ItemWrappers { get; }


        public ListInspectorViewModel(IList<T> source, bool isReadonly = false)
        {
            SourceList = source;
            IsReadonly = isReadonly;

            ItemWrappers = new ListInspectorElement[source.Count];
            for(int i = 0; i < source.Count; i++)
                ItemWrappers[i] = new(this, i);
        }
    }
}
