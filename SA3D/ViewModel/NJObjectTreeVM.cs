using SATools.SA3D.ViewModel.Base;
using System.Collections.ObjectModel;

namespace SATools.SA3D.ViewModel
{
    public class NJObjectTreeVM : BaseViewModel
    {
        private readonly MainViewModel _mainVM;

        public ObservableCollection<NJObjectVM> NJObjects { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if(value == _isSelected)
                {
                    return;
                }

                if(value)
                    Display();

                _isSelected = value && IsEnabled;
            }
        }

        public bool IsEnabled => NJObjects.Count > 0;

        private readonly RelayCommand<NJObjectVM> SelectItem;

        public NJObjectTreeVM(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            SelectItem = new RelayCommand<NJObjectVM>(Select);

            NJObjects = new ObservableCollection<NJObjectVM>();
            Display();
        }

        private void Select(NJObjectVM item) => _mainVM.RenderContext.ActiveNJO = item.NJObject;

        public void Refresh()
        {
            Display();
            OnPropertyChanged(nameof(IsEnabled));
        }

        private void Display()
        {
            NJObjects.Clear();
            foreach(var obj in _mainVM.RenderContext.Scene.objects)
            {
                NJObjects.Add(new NJObjectVM(obj.obj, SelectItem));
            }
        }
    }
}
