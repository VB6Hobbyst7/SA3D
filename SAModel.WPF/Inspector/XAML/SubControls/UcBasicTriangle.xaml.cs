using SATools.SAModel.ModelData.BASIC;
using System.Windows;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcBasicTriangle.xaml
    /// </summary>
    internal partial class UcBasicTriangle : BaseStructUserControl<Triangle>
    {
        public ushort Index0
        {
            get => Value.Indices[0];
            set => Value.Indices[0] = value;
        }

        public ushort Index1
        {
            get => Value.Indices[1];
            set => Value.Indices[1] = value;
        }

        public ushort Index2
        {
            get => Value.Indices[2];
            set => Value.Indices[2] = value;
        }

        public UcBasicTriangle()
        {
            InitializeComponent();
        }

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Index0));
            OnPropertyChanged(nameof(Index1));
            OnPropertyChanged(nameof(Index2));
        }
    }
}
