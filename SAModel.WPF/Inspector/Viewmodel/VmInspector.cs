using SAWPF.BaseViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SAModel.WPF.Inspector.Viewmodel
{
    /// <summary>
    /// SA Model Inspector viewmodel
    /// </summary>
    internal class VmInspector : BaseViewModel
    {
        private Dictionary<object, object> _listViewModels;

        /// <summary>
        /// History of displayed objects
        /// </summary>
        public ObservableCollection<VmHistoryElement> History { get; }

        private VmHistoryElement _activeHistoryElement;

        public VmHistoryElement ActiveHistoryElement
        {
            get => _activeHistoryElement;
            set
            {
                _activeHistoryElement = value;
                OnPropertyChanged(nameof(ActiveIVM));
            }
        }

        public object ActiveIVM
            => ActiveHistoryElement.Data;

        public VmInspector()
        {
            History = new();
            _listViewModels = new();
        }

        public void LoadNewObject(object obj)
        {
            History.Clear();

            InspectorViewModel ivm = InspectorViewModel.GetViewModel(obj);
            History.Add(new(ivm.ToString(), ivm));
            ActiveHistoryElement = History[0];
        }

        public void LoadSubObject(IInspectorInfo info)
        {
            object Value = info.Value;

            object data;
            if(info.IsCollection)
            {
                if(!_listViewModels.TryGetValue(Value, out data))
                {
                    Type[] typeArgs = info.ValueType.GetGenericArguments();
                    Type listType = typeof(ListInspectorViewModel<>).MakeGenericType(typeArgs);
                    data = Activator.CreateInstance(listType, info);
                    _listViewModels.Add(Value, data);
                }
            }
            else
                data = InspectorViewModel.GetViewModel(Value);

            int index = History.IndexOf(ActiveHistoryElement);
            if(index < History.Count - 1)
            {
                for(int i = History.Count - 1; i > index; i--)
                    History.RemoveAt(i);
            }

            VmHistoryElement newElement = new(info.HistoryName, data);
            History.Add(newElement);
            ActiveHistoryElement = newElement;
        }
    }
}
