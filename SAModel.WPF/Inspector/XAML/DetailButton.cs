using SAModel.WPF.Inspector.Viewmodel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SAModel.WPF.Inspector.XAML
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
            while(parent.GetType() != typeof(UcInspector))
                parent = VisualTreeHelper.GetParent(parent);

            UcInspector inspector = (UcInspector)parent;

            string name;
            if(DataContext is InspectorElement ie)
            {
                name = ie.Property.Name;
            }
            else
            {
                //if it is not an inspector element, then its a listInspectorElement
                int index = (int)DataContext.GetType().GetProperty("ValueIndex").GetValue(DataContext);
                name = $"{inspector.CurrentHistoryName}[{index}]";
            }

            inspector.LoadSubObject(DetailObject, name);
        }
    }
}
