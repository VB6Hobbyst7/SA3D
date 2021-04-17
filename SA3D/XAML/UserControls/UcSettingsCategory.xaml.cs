using System.Windows.Controls;

namespace SATools.SA3D.XAML.UserControls
{
    /// <summary>
    /// Interaction logic for UcSettingsCategory.xaml
    /// </summary>
    public partial class UcSettingsCategory : UserControl
    {
        public string Title { get; }

        public UcSettingsCategory(string title)
        {
            Title = title;
            InitializeComponent();
        }
    }
}
