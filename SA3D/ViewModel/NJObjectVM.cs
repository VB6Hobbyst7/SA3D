using SATools.SA3D.ViewModel.Base;
using SATools.SAModel.ObjData;
using System.Collections.ObjectModel;

namespace SATools.SA3D.ViewModel
{
    /// <summary>
    /// NJObject tree item view model
    /// </summary>
    public class NJObjectVM : BaseViewModel
    {
        public NJObject NJObject { get; }

        public ObservableCollection<NJObjectVM> Children { get; }

        /// <summary>
        /// The name of the item
        /// </summary>
        public string Name
        {
            get => NJObject.Name;
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    return;
                }

                NJObject.Name = Name;
            }
        }

        /// <summary>
        /// Command for expanding this object
        /// </summary>
        public RelayCommand ExpandCommand { get; }

        public RelayCommand<NJObjectVM> SelectItem { get; }

        /// <summary>
        /// If there are any children, this item can expand
        /// </summary>
        public bool CanExpand => NJObject.ChildCount > 0;

        public bool IsExpanded
        {
            get => Children.Count > 0 && Children?[0] != null;
            set
            {
                if(value)
                {
                    Expand();
                }
                else
                {
                    Collapse();
                }
            }
        }

        public bool IsSelected
        {
            set
            {
                if(value)
                    SelectItem.Execute(this);
            }
        }

        public NJObjectVM(NJObject njObject, RelayCommand<NJObjectVM> selectItem)
        {
            NJObject = njObject;
            ExpandCommand = new RelayCommand(Expand);
            SelectItem = selectItem;
            Children = new ObservableCollection<NJObjectVM>();
            if(CanExpand)
            {
                Children.Add(null);
            }
        }

        private void Expand()
        {
            if(!CanExpand)
            {
                return;
            }

            Children.Clear();
            for(int i = 0; i < NJObject.ChildCount; i++)
            {
                Children.Add(new NJObjectVM(NJObject[i], SelectItem));
            }

        }

        private void Collapse()
        {
            Children.Clear();
            if(CanExpand)
            {
                Children.Add(null);
            }
        }
    }
}
