using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    internal class FlagListBox : ListBox
    {
        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(FlagListBox),
                new(null, new((d, e) =>
                {
                    FlagListBox lb = (FlagListBox)d;
                    lb.UpdateCheckboxes(e.NewValue, e.OldValue);
                })));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private ulong Flag
        {
            get => Convert.ToUInt64(Value ?? 0);
            set
            {
                object newValue = Enum.ToObject(Value.GetType(), value);
                if(Value == newValue)
                    return;
                Value = newValue;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            ulong flag = Flag;
            if(flag == 0)
                return;

            foreach(FlagListItem item in Items)
            {
                ulong flagVal = Convert.ToUInt64(item.Flag);
                bool flagState = (flag & flagVal) != 0;

                item.IsSelected = flagState;
                flag &= ~flagVal;
            }

            if(flag != 0)
                throw new FormatException($"Flag had either invalid values or the listbox missed a flag value: {flag:X}");
        }

        private bool updating;

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if(updating)
                return;


            ulong curFlag = Flag;
            ulong newFlag = curFlag;
            foreach(FlagListItem i in e.AddedItems)
            {
                ulong flag = Convert.ToUInt64(i.Flag);
                if((curFlag & flag) != 0)
                    continue;

                newFlag |= flag;
            }

            foreach(FlagListItem i in e.RemovedItems)
            {
                ulong flag = Convert.ToUInt64(i.Flag);
                if((curFlag & flag) == 0)
                    continue;

                newFlag &= ~flag;
            }

            if(curFlag != newFlag)
            {
                updating = true;
                Flag = newFlag;
                updating = false;
            }

        }

        private void UpdateCheckboxes(object newValue, object oldValue)
        {
            if(updating)
                return;

            if(Items.Count == 0)
                return; // we'll handle this again after loading

            ulong flag = Convert.ToUInt64(newValue) ^ Convert.ToUInt64(oldValue);
            if(flag == 0)
                return;

            updating = true;

            foreach(FlagListItem item in Items)
            {
                ulong flagVal = Convert.ToUInt64(item.Flag);

                bool flagState = (flag & flagVal) != 0;
                if(flagState != item.IsSelected)
                    item.IsSelected = flagState;

                flag &= ~flagVal;
            }

            updating = false;

            if(flag != 0)
                throw new FormatException($"Flag had either invalid values or the listbox missed a flag value: {flag:X}");
        }
    }

    internal class FlagListItem : ListBoxItem
    {
        public static readonly DependencyProperty FlagProperty
            = DependencyProperty.Register(
                nameof(Flag),
                typeof(object),
                typeof(FlagListItem)
            );

        public virtual object Flag
        {
            get => GetValue(FlagProperty);
            set => SetValue(FlagProperty, value);
        }
    }

    internal class HexFlagListItem : FlagListItem, INotifyPropertyChanged
    {
        public override object Flag
        {
            get => base.Flag;
            set
            {
                base.Flag = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(HexValue)));
            }
        }

        public static readonly DependencyProperty HexLengthProperty
            = DependencyProperty.Register(
                nameof(HexLength),
                typeof(int),
                typeof(HexFlagListItem)
            );

        public int HexLength
        {
            get => (int)GetValue(HexLengthProperty);
            set
            {
                SetValue(HexLengthProperty, value);
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(HexValue)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string HexValue
            => string.Format($"0x{{0:X{HexLength}}}", Convert.ToUInt64(Flag));
    }
}
