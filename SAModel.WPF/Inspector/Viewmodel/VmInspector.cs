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

        public void LoadSubObject(object obj, string name)
        {
            Type type = obj.GetType();

            object data;
            // Check if the object implements IList
            if(type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                if(!_listViewModels.TryGetValue(obj, out data))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    Type listType = typeof(ListInspectorViewModel<>).MakeGenericType(typeArgs);
                    data = Activator.CreateInstance(listType, obj, false);
                    _listViewModels.Add(obj, data);
                }
            }
            else
                data = InspectorViewModel.GetViewModel(obj);

            int index = History.IndexOf(ActiveHistoryElement);
            if(index < History.Count - 1)
            {
                for(int i = History.Count - 1; i > index; i--)
                    History.RemoveAt(i);
            }

            VmHistoryElement newElement = new(name, data);
            History.Add(newElement);
            ActiveHistoryElement = newElement;
        }
    }
}
