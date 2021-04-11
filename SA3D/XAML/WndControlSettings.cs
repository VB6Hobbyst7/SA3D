using SATools.SAModel.Graphics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SATools.SA3D.XAML
{
    /// <summary>
    /// Interaction logic for WndControlSettings.xaml
    /// </summary>
    public class WndControlSettings : Window
    {
        private UcControlSetting _recording;

        public UcControlSetting Recording
        {
            get => _recording;
            set
            {
                if(_recording != null)
                    _recording.FinishRecording();

                _recording = value;
            }
        }

        public WndControlSettings()
        {
            // This window is being generated, so that the controls dont need to be handled manually
            // the scrollviewer and stackpanel are not worth to create an xaml file for tbh
            Title = "Control Settings";
            Width = 410;
            MinWidth = Width;
            MaxWidth = Width;
            Height = 650;
            Closing += (e, o) => DebugSettings.Global.Save("Settings");

            ScrollViewer scroll = new();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Content = scroll;

            StackPanel container = new();
            scroll.Content = container;

            UcSettingsCategory panel = null;

            var fields = typeof(DebugSettings).GetTypeInfo().GetFields();
            foreach(var field in fields)
            {
                SettingsKeyCategoryAttribute titleAttr = field.GetCustomAttribute<SettingsKeyCategoryAttribute>();
                if(titleAttr != null)
                {
                    panel = new(titleAttr.Title);
                    container.Children.Add(panel);
                }

                SettingsKeyAttribute attr = field.GetCustomAttribute<SettingsKeyAttribute>();
                if(attr != null)
                {
                    panel.Container.Children.Add(new UcControlSetting(this, field));
                }
            }

        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(_recording != null && _recording.UsesKey)
            {
                Recording.KeySelection.SelectedItem = e.Key;
                Recording = null;
            }
        }

    }
}
