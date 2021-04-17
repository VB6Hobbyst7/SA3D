using SATools.SA3D.ViewModel.Base;
using SATools.SA3D.ViewModel.TreeItems;
using System.Collections.ObjectModel;

namespace SATools.SA3D.ViewModel
{
    public class VmObjectTree : BaseViewModel
    {
        private readonly VmMain _mainVM;

        private VmTreeItem _selected;

        public ObservableCollection<VmTreeItem> Objects { get; }

        public VmTreeItem Selected
        {
            get => _selected;
            set
            {
                if(_selected == value)
                    return;
                _selected = value;

                _selected.Data.Select(_selected.Parent, _mainVM);
                OnPropertyChanged(nameof(Selected));
            }
        }

        public VmObjectTree(VmMain mainVM)
        {
            _mainVM = mainVM;

            Objects = new();

            foreach(var obj in _mainVM.RenderContext.Scene.objects)
            {
                Objects.Add(new(null, new VmObject(obj, null)));
            }
        }

    }
}
