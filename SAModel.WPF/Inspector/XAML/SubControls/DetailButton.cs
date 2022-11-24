using SATools.SAModel.WPF.Inspector.Viewmodel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    internal class DetailButton : Button
    {
        public static readonly DependencyProperty DetailObjectProperty
            = DependencyProperty.Register(
                nameof(DetailObject),
                typeof(object),
                typeof(DetailButton)
                );

        public virtual object DetailObject
        {
            get => GetValue(DetailObjectProperty);
            set => SetValue(DetailObjectProperty, value);
        }

        protected override void OnClick()
        {
            base.OnClick();

            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent.GetType() != typeof(UcInspector))
                parent = VisualTreeHelper.GetParent(parent);

            UcInspector inspector = (UcInspector)parent;

            IInspectorInfo element = (IInspectorInfo)DataContext;
            inspector.LoadSubObject(element);
        }
    }
}
