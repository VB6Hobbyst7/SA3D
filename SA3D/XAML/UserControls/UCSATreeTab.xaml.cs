using SATools.SA3D.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SA3D.XAML.UserControls
{
    /// <summary>
    /// Interaction logic for UCSATreeTab.xaml
    /// </summary>
    public partial class UCSATreeTab : UserControl
    {
        public UCSATreeTab()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((VMDataTree)DataContext).Selected = (VmTreeItem)e.NewValue;
        }
    }
}
