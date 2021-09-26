using SATools.SAModel.ModelData.BASIC;
using System.Windows;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcBasicTriangle.xaml
    /// </summary>
    internal partial class UcBasicQuad : BaseStructUserControl<Quad>
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

        public ushort Index3
        {
            get => Value.Indices[3];
            set => Value.Indices[3] = value;
        }

        public UcBasicQuad()
        {
            InitializeComponent();
        }

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Index0));
            OnPropertyChanged(nameof(Index1));
            OnPropertyChanged(nameof(Index2));
            OnPropertyChanged(nameof(Index3));
        }
    }
}
