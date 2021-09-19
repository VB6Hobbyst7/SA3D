using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SATools.SAModel.WPF.Inspector.XAML.SubControls
{
    /// <summary>
    /// Generic base class for the struct subcontrols
    /// </summary>
    internal abstract class BaseStructUserControl<T> : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Common Numeric box style property
        /// </summary>
        public static readonly DependencyProperty BaseBoxStyleProperty
            = DependencyProperty.Register(
                nameof(BaseBoxStyle),
                typeof(Style),
                typeof(BaseStructUserControl<T>)
                );

        /// <summary>
        /// Common Numeric box style
        /// </summary>
        public Style BaseBoxStyle
        {
            get => (Style)GetValue(BaseBoxStyleProperty);
            set => SetValue(BaseBoxStyleProperty, value);
        }

        /// <summary>
        /// Value property
        /// </summary>
        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(
                nameof(Value),
                typeof(T),
                typeof(BaseStructUserControl<T>),
                new(new((d, e) => ((BaseStructUserControl<T>)d).ValuePropertyChanged(e))));

        /// <summary>
        /// Value property
        /// </summary>
        public T Value
        {
            get => (T)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets called when the value changes
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        protected abstract void ValuePropertyChanged(DependencyPropertyChangedEventArgs e);

        /// <summary>
        /// Notify the <see cref="INotifyPropertyChanged"/> that a value changed
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged(string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
