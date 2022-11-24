using SATools.SAModel.Graphics.Properties;
using System;
using System.Configuration;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SATools.SA3D.XAML.UserControls
{
    /// <summary>
    /// Interaction logic for UcControlSetting.xaml
    /// </summary>
    public partial class UcControlSetting : UserControl
    {
        private readonly PropertyInfo _property;

        private string Default => _property.GetCustomAttribute<DefaultSettingValueAttribute>().Value;

        private readonly SettingsKeyAttribute _attribute;

        private readonly WndControlSettings _window;

        public bool UsesKey
            => _property.PropertyType == typeof(Key);

        public Key OptionKey
        {
            get => (Key)_property.GetValue(DebugSettings.Default);
            set => _property.SetValue(DebugSettings.Default, value);
        }

        public MouseButton OptionButton
        {
            get => (MouseButton)_property.GetValue(DebugSettings.Default);
            set => _property.SetValue(DebugSettings.Default, value);
        }

        public static readonly Key[] Keys;

        public static readonly MouseButton[] MouseButtons;

        static UcControlSetting()
        {
            Keys = Enum.GetValues<Key>();
            MouseButtons = Enum.GetValues<MouseButton>();
        }

        public UcControlSetting(WndControlSettings window, PropertyInfo field)
        {
            _window = window;

            _property = field;
            _attribute = field.GetCustomAttribute<SettingsKeyAttribute>();
            ToolTip = _attribute.Description;
            InitializeComponent();

            OptionName.Text = _attribute.Name;

            if (UsesKey)
            {
                MouseButtonSelection.Visibility = Visibility.Collapsed;
            }
            else
            {
                KeySelection.Visibility = Visibility.Collapsed;
                RecordButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (UsesKey)
                KeySelection.SelectedItem = Default.Substring(5, Default.Length - 11);
            else
                MouseButtonSelection.SelectedItem = Default;
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            RecordButton.Visibility = Visibility.Collapsed;
            RecordText.Visibility = Visibility.Visible;
            _window.Recording = this;
        }

        public void FinishRecording()
        {
            RecordButton.Visibility = Visibility.Visible;
            RecordText.Visibility = Visibility.Collapsed;
        }

    }
}
