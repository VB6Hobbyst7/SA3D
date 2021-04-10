using SATools.SA3D.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SATools.SA3D
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new MainViewModel();
            InitializeComponent();

            var c = ((MainViewModel)DataContext).RenderContext.AsControl();

            maingrid.Children.Add(c);
        }
    }
}
