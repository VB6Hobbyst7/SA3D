using SATools.SA3D.ViewModel;
using System.Windows;

namespace SATools.SA3D.XAML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WndMain : Window
    {
        public WndMain()
        {
            DataContext = new MainViewModel();
            InitializeComponent();

            var c = ((MainViewModel)DataContext).RenderContext.AsControl();

            maingrid.Children.Add(c);
        }

        private void ControlSettings_Click(object sender, RoutedEventArgs e) => new WndControlSettings().ShowDialog();
    }
}
