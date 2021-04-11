using SATools.SAModel.Graphics;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseButton = SATools.SAModel.Graphics.MouseButton;

namespace SATools.SA3D.XAML
{
    /// <summary>
    /// Interaction logic for UcControlSetting.xaml
    /// </summary>
    public partial class UcControlSetting : UserControl
    {
        private readonly FieldInfo _field;

        private readonly SettingsKeyAttribute _attribute;

        private readonly WndControlSettings _window;

        public bool UsesKey
            => _field.FieldType == typeof(Key);

        public Key OptionKey
        {
            get => (Key)_field.GetValue(DebugSettings.Global);
            set => _field.SetValue(DebugSettings.Global, value);
        }

        public MouseButton OptionButton
        {
            get => (MouseButton)_field.GetValue(DebugSettings.Global);
            set => _field.SetValue(DebugSettings.Global, value);
        }

        public static readonly Key[] Keys;

        public static readonly MouseButton[] MouseButtons;

        static UcControlSetting()
        {
            Keys = Enum.GetValues<Key>();
            MouseButtons = Enum.GetValues<MouseButton>();
        }

        public UcControlSetting(WndControlSettings window, FieldInfo field)
        {
            _window = window;

            _field = field;
            _attribute = field.GetCustomAttribute<SettingsKeyAttribute>();
            ToolTip = _attribute.Description;
            InitializeComponent();

            OptionName.Text = _attribute.Name;

            if(UsesKey)
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
            if(UsesKey)
                KeySelection.SelectedItem = _attribute.DefaultKey;
            else
                MouseButtonSelection.SelectedItem = _attribute.DefaultMouse;
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
