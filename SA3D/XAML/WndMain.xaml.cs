using SATools.SA3D.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SA3D.XAML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WndMain : Window
    {
        public WndMain()
        {
            DataContext = new VmMain(App.Context);
            InitializeComponent();

            var c = ((VmMain)DataContext).RenderContext.AsControl();

            maingrid.Children.Add(c);
        }

        private void ControlSettings_Click(object sender, RoutedEventArgs e) => new WndControlSettings().ShowDialog();

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((VmObjectTree)((TreeView)sender).DataContext).Selected = (VmTreeItem)e.NewValue;
        }
    }
}
