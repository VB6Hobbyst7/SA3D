using SAModel.WPF.Inspector.Viewmodel;
using System.Windows.Controls;

namespace SAModel.WPF.Inspector.XAML
{
    /// <summary>
    /// Interaction logic for UcInspectorTable.xaml
    /// </summary>
    internal partial class UcInspectorTable : UserControl
    {
        public UcInspectorTable()
            => InitializeComponent();

        public UcInspectorTable(InspectorViewModel ivm)
        {
            DataContext = ivm;
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta * 0.5f);
            e.Handled = true;
        }
    }
}
