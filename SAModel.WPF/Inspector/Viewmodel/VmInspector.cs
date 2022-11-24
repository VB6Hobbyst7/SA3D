using SATools.SAWPF.BaseViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SATools.SAModel.WPF.Inspector.Viewmodel
{
    /// <summary>
    /// SA Model Inspector viewmodel
    /// </summary>
    internal class VmInspector : BaseViewModel
    {
        private readonly Dictionary<object, object> _viewModels;

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
            _viewModels = new();
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

            if (!_viewModels.TryGetValue(Value, out object data))
            {
                if (info.IsCollection)
                {
                    Type[] typeArgs = info.ValueType.IsArray ?
                        (new Type[] { info.ValueType.GetElementType() })
                        : info.ValueType.GetGenericArguments();

                    Type listType = typeof(ListInspectorViewModel<>).MakeGenericType(typeArgs);
                    data = Activator.CreateInstance(listType, info);
                }
                else
                {
                    data = InspectorViewModel.GetViewModel(info);
                }

                _viewModels.Add(Value, data);
            }

            // check if a history element already wraps the viewmodel
            foreach (VmHistoryElement h in History)
            {
                if (h.Data == data)
                {
                    ActiveHistoryElement = h;
                    return;
                }
            }

            int index = History.IndexOf(ActiveHistoryElement);
            if (index < History.Count - 1)
            {
                for (int i = History.Count - 1; i > index; i--)
                    History.RemoveAt(i);
            }

            VmHistoryElement newElement = new(info.HistoryName, data);
            History.Add(newElement);
            ActiveHistoryElement = newElement;
        }
    }
}
