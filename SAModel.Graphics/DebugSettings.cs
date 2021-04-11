using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace SATools.SAModel.Graphics
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SettingsKeyCategoryAttribute : Attribute
    {
        public string Title { get; }

        public SettingsKeyCategoryAttribute(string title)
        {
            Title = title;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SettingsKeyAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public Key DefaultKey { get; }
        public MouseButton DefaultMouse { get; }

        public SettingsKeyAttribute(string name, string description, Key defaultKey)
        {
            Name = name;
            Description = description;
            DefaultKey = defaultKey;

        }

        public SettingsKeyAttribute(string name, string description, MouseButton defaultKey)
        {
            Name = name;
            Description = description;
            DefaultMouse = defaultKey;
        }
    }

    /// <summary>
    /// Holds all different settings values for the Debug Scene
    /// </summary>
    public class DebugSettings
    {
        /// <summary>
        /// Settings accessible for everything
        /// </summary>
        public static DebugSettings Global { get; private set; }

        [SettingsKeyCategory("Switching the camera")]
        [SettingsKey("Navigation Mode", "Switches between Orbiting and FPS movement", Key.O)]
        public Key navMode;
        [SettingsKey("Perspective", "Switches between Perspective and Orthographic", Key.NumPad5)]
        public Key perspective;

        [SettingsKeyCategory("First Person Controls")]
        [SettingsKey("Forward", "Key used to move forward in first person", Key.W)]
        public Key fpForward;
        [SettingsKey("Backward", "Key used to move backward in first person", Key.S)]
        public Key fpBackward;
        [SettingsKey("Right", "Key used to move right in first person", Key.D)]
        public Key fpRight;
        [SettingsKey("Left", "Key used to move left in first person", Key.A)]
        public Key fpLeft;
        [SettingsKey("Up", "Key used to move up in first person", Key.Space)]
        public Key fpUp;
        [SettingsKey("Down", "Key used to move down in first person", Key.LeftCtrl)]
        public Key fpDown;
        [SettingsKey("Speed up", "Movement modifier used to speed up a bit", Key.LeftShift)]
        public Key fpSpeedup;

        [SettingsKeyCategory("Orbiting Controls")]
        [SettingsKey("Orbiting Key", "Mouse button used for navigating in orbit mode", MouseButton.Middle)]
        public MouseButton OrbitKey;
        [SettingsKey("Drag Modifier", "Modifier used to move camera when pressing the orbit key", Key.LeftShift)]
        public Key dragModifier;
        [SettingsKey("Zoom Modifier", "Modifier used to zoom camera when pressing the orbit key", Key.LeftCtrl)]
        public Key zoomModifier;

        [SettingsKeyCategory("Camera Snapping")]
        [SettingsKey("Align Forward", "Aligns camera with the -Z axis", Key.NumPad1)]
        public Key alignForward;
        [SettingsKey("Align Up", "Aligns camera with the -Y axis", Key.NumPad7)]
        public Key alignUp;
        [SettingsKey("Align Side", "Aligns camera with the -X axis", Key.NumPad3)]
        public Key alignSide;
        [SettingsKey("Align Invert", "Inverts the axis of the selected axis to align with", Key.LeftCtrl)]
        public Key alignInvert;
        [SettingsKey("Resets Camera", "Resets camera properties to default values", Key.R)]
        public Key resetCamera;
        [SettingsKey("Focus Object", "Focuses camera to selected object when in orbit mode", Key.F)]
        public Key focusObj;

        [SettingsKeyCategory("Debug Menu Keys")]
        [SettingsKey("Debug Help", "Displays the debug help menu", Key.F1)]
        public Key DebugHelp;
        [SettingsKey("Debug Camera", "Displays the debug camera menu", Key.F2)]
        public Key DebugCamera;
        [SettingsKey("Debug Render", "Displays the debug render menu", Key.F3)]
        public Key DebugRender;

        [SettingsKeyCategory("Debug Option Keys")]
        [SettingsKey("Circle Render mode", "Circles between the various render modes", Key.F5)]
        public Key circleRenderMode;
        [SettingsKey("Circle Wireframe Mode", "Circles between the various wireframe modes", Key.F6)]
        public Key circleWireframe;
        [SettingsKey("Swap Geometry", "Changes between rendering visual and collision geometry", Key.F7)]
        public Key swapGeometry;
        [SettingsKey("Display Bounds", "Displays geometry bounds", Key.F8)]
        public Key displayBounds;
        [SettingsKey("Circle Backwards", "Hold this button when using a hotkey for circling options to circle in the other direction", Key.RightAlt)]
        public Key circleBackward;



        public DebugSettings()
        {
            var fields = GetType().GetTypeInfo().GetFields();
            foreach(var field in fields)
            {
                SettingsKeyAttribute attr = field.GetCustomAttribute<SettingsKeyAttribute>();
                if(attr != null)
                {
                    if(field.FieldType == typeof(Key))
                        field.SetValue(this, attr.DefaultKey);
                    else if(field.FieldType == typeof(MouseButton))
                        field.SetValue(this, attr.DefaultMouse);
                }
            }
        }

        static DebugSettings()
        {
            if(File.Exists("Settings.json"))
                Load("Settings.json");
            else
            {
                Global = new DebugSettings();
                Global.Save("Settings");
            }
        }

        /// <summary>
        /// Saves the settings to a file
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            JsonSerializer js = new JsonSerializer() { Culture = System.Globalization.CultureInfo.InvariantCulture };
            using TextWriter tw = File.CreateText(Path.ChangeExtension(path, ".json"));
            using JsonTextWriter jtw = new JsonTextWriter(tw) { Formatting = Formatting.Indented };
            js.Serialize(jtw, this);
        }

        /// <summary>
        /// Loads a settings file into the global settings
        /// </summary>
        /// <param name="path"></param>
        public static void Load(string path)
        {
            DebugSettings settings = null;
            JsonSerializer js = new JsonSerializer() { Culture = System.Globalization.CultureInfo.InvariantCulture };
            try
            {
                using(TextReader tr = File.OpenText(path))
                using(JsonTextReader jtr = new JsonTextReader(tr))
                    settings = js.Deserialize<DebugSettings>(jtr);
            }
            catch(Exception)
            {
                return;
            }
            Global = settings;
        }
    }
}
