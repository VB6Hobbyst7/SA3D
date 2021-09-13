using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace SAModel.WPF.Inspector.XAML
{
    internal class EnumComboBox : ComboBox
    {
        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(EnumComboBox),
                new(null, new((d, e) =>
                {
                    EnumComboBox ecb = (EnumComboBox)d;
                    if(ecb.Items.Count == 0)
                        return; // we'll handle this again after loading

                    ComboBoxItem item;
                    if(ecb.SelectedIndex >= 0)
                    {
                        item = (ComboBoxItem)ecb.Items[ecb.SelectedIndex];
                    
                        if(item.Tag == e.NewValue)
                            return;
                    }

                    for(int i = 0; i < ecb.Items.Count; i++)
                    {
                        item = (ComboBoxItem)ecb.Items[i];
                        if(item.Tag.Equals(e.NewValue))
                            continue;

                        ecb.SelectedIndex = i;
                        return;
                    }

                    throw new InvalidOperationException($"Enum value {e.NewValue} not found");
                })));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // i have confidence in stack overflow that this way of
        // handling the changed selection works flawless.
        // If not, we are doomed

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            object value = Value;
            for(int i = 0; i < Items.Count; i++)
            {
                ComboBoxItem item = (ComboBoxItem)Items[i];
                if(!item.Tag.Equals(value))
                    continue;

                SelectedIndex = i;
                return;
            }

            throw new InvalidOperationException($"Enum value {value} not found");
        }

        private bool _handle;

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if(_handle)
                Handle();
            _handle = true;

        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            _handle = !IsDropDownOpen;
            Handle();
        }

        private void Handle()
        {
            ComboBoxItem item = (ComboBoxItem)Items[SelectedIndex];
            SetValue(ValueProperty, item.Tag);
        }
    }
}
