using SATools.SAModel.ModelData.GC;
using System.Windows;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Interaction logic for UcGCCorner.xaml
    /// </summary>
    internal partial class UcGCCorner : BaseStructUserControl<Corner>
    {
        public ushort PositionIndex
        {
            get => Value.PositionIndex;
            set
            {
                var t = Value;
                t.PositionIndex = value;
                Value = t;
            }
        }

        public ushort NormalIndex
        {
            get => Value.NormalIndex;
            set
            {
                var t = Value;
                t.NormalIndex = value;
                Value = t;
            }
        }

        public ushort Color0Index
        {
            get => Value.Color0Index;
            set
            {
                var t = Value;
                t.Color0Index = value;
                Value = t;
            }
        }

        public ushort UV0Index
        {
            get => Value.UV0Index;
            set
            {
                var t = Value;
                t.UV0Index = value;
                Value = t;
            }
        }

        public UcGCCorner()
        {
            InitializeComponent();
        }

        protected override void ValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(PositionIndex));
            OnPropertyChanged(nameof(NormalIndex));
            OnPropertyChanged(nameof(Color0Index));
            OnPropertyChanged(nameof(UV0Index));
        }
    }
}
