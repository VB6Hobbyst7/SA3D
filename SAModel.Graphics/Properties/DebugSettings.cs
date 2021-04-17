using System;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Input;

namespace SATools.SAModel.Graphics.Properties {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SettingsKeyCategoryAttribute : Attribute
    {
        public string Title { get; }

        public SettingsKeyCategoryAttribute(string title)
        {
            Title = title;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SettingsKeyAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public SettingsKeyAttribute(string name, string description)
        {
            Name = name;
            Description = description;

        }
    }

    [DebuggerNonUserCode()]
    public sealed class DebugSettings : ApplicationSettingsBase {
        
        private static DebugSettings defaultInstance = (DebugSettings)Synchronized(new DebugSettings());
        
        public static DebugSettings Default =>  defaultInstance;

        [UserScopedSetting()]
        [SettingsKeyCategory("Switching the camera")]
        [SettingsKey("Navigation Mode", "Switches between Orbiting and FPS movement")]
        [DefaultSettingValue("<Key>O</Key>")]
        public Key NavMode
        {
            get => (Key)this[nameof(NavMode)];
            set => this[nameof(NavMode)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Perspective", "Switches between Perspective and Orthographic")]
        [DefaultSettingValue("<Key>NumPad5</Key>")]
        public Key Perspective
        {
            get => (Key)this[nameof(Perspective)];
            set => this[nameof(Perspective)] = value;
        }

        [UserScopedSetting()]
        [SettingsKeyCategory("First Person Controls")]
        [SettingsKey("Forward", "Key used to move forward in first person")]
        [DefaultSettingValue("<Key>W</Key>")]
        public Key FpForward
        {
            get => (Key)this[nameof(FpForward)];
            set => this[nameof(FpForward)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Backward", "Key used to move backward in first person")]
        [DefaultSettingValue("<Key>S</Key>")]
        public Key FpBackward
        {
            get => (Key)this[nameof(FpBackward)];
            set => this[nameof(FpBackward)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Right", "Key used to move right in first person")]
        [DefaultSettingValue("<Key>D</Key>")]
        public Key FpRight
        {
            get => (Key)this[nameof(FpRight)];
            set => this[nameof(FpRight)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Left", "Key used to move left in first person")]
        [DefaultSettingValue("<Key>A</Key>")]
        public Key FpLeft
        {
            get => (Key)this[nameof(FpLeft)];
            set => this[nameof(FpLeft)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Up", "Key used to move up in first person")]
        [DefaultSettingValue("<Key>Space</Key>")]
        public Key FpUp
        {
            get => (Key)this[nameof(FpUp)];
            set => this[nameof(FpUp)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Down", "Key used to move down in first person")]
        [DefaultSettingValue("<Key>LeftCtrl</Key>")]
        public Key FpDown
        {
            get => (Key)this[nameof(FpDown)];
            set => this[nameof(FpDown)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Speed up", "Movement modifier used to speed up a bit")]
        [DefaultSettingValue("<Key>LeftShift</Key>")]
        public Key FpSpeedup
        {
            get => (Key)this[nameof(FpSpeedup)];
            set => this[nameof(FpSpeedup)] = value;
        }


        [UserScopedSetting()]
        [SettingsKeyCategory("Orbiting Controls")]
        [SettingsKey("Orbiting Key", "Mouse button used for navigating in orbit mode")]
        [DefaultSettingValue("Middle")]
        public MouseButton OrbitKey
        {
            get => (MouseButton)this[nameof(OrbitKey)];
            set => this[nameof(OrbitKey)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Drag Modifier", "Modifier used to move camera when pressing the orbit key")]
        [DefaultSettingValue("<Key>LeftShift</Key>")]
        public Key DragModifier
        {
            get => (Key)this[nameof(DragModifier)];
            set => this[nameof(DragModifier)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Zoom Modifier", "Modifier used to zoom camera when pressing the orbit key")]
        [DefaultSettingValue("<Key>LeftCtrl</Key>")]
        public Key ZoomModifier
        {
            get => (Key)this[nameof(ZoomModifier)];
            set => this[nameof(ZoomModifier)] = value;
        }


        [UserScopedSetting()]
        [SettingsKeyCategory("Camera Snapping")]
        [SettingsKey("Align Forward", "Aligns camera with the -Z axis")]
        [DefaultSettingValue("<Key>NumPad1</Key>")]
        public Key AlignForward
        {
            get => (Key)this[nameof(AlignForward)];
            set => this[nameof(AlignForward)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Align Up", "Aligns camera with the -Y axis")]
        [DefaultSettingValue("<Key>NumPad7</Key>")]
        public Key AlignUp
        {
            get => (Key)this[nameof(AlignUp)];
            set => this[nameof(AlignUp)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Align Side", "Aligns camera with the -X axis")]
        [DefaultSettingValue("<Key>NumPad3</Key>")]
        public Key AlignSide
        {
            get => (Key)this[nameof(AlignSide)];
            set => this[nameof(AlignSide)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Align Invert", "Inverts the axis of the selected axis to align with")]
        [DefaultSettingValue("<Key>LeftCtrl</Key>")]
        public Key AlignInvert
        {
            get => (Key)this[nameof(AlignInvert)];
            set => this[nameof(AlignInvert)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Resets Camera", "Resets camera properties to default values")]
        [DefaultSettingValue("<Key>R</Key>")]
        public Key ResetCamera
        {
            get => (Key)this[nameof(ResetCamera)];
            set => this[nameof(ResetCamera)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Focus Object", "Focuses camera to selected object when in orbit mode")]
        [DefaultSettingValue("<Key>F</Key>")]
        public Key FocusObj
        {
            get => (Key)this[nameof(FocusObj)];
            set => this[nameof(FocusObj)] = value;
        }


        [UserScopedSetting()]
        [SettingsKeyCategory("Debug Menu Keys")]
        [SettingsKey("Debug Help", "Displays the debug help menu")]
        [DefaultSettingValue("<Key>F1</Key>")]
        public Key DebugHelp
        {
            get => (Key)this[nameof(DebugHelp)];
            set => this[nameof(DebugHelp)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Debug Camera", "Displays the debug camera menu")]
        [DefaultSettingValue("<Key>F2</Key>")]
        public Key DebugCamera
        {
            get => (Key)this[nameof(DebugCamera)];
            set => this[nameof(DebugCamera)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Debug Render", "Displays the debug render menu")]
        [DefaultSettingValue("<Key>F3</Key>")]
        public Key DebugRender
        {
            get => (Key)this[nameof(DebugRender)];
            set => this[nameof(DebugRender)] = value;
        }


        [UserScopedSetting()]
        [SettingsKeyCategory("Debug Option Keys")]
        [SettingsKey("Circle Render mode", "Circles between the various render modes")]
        [DefaultSettingValue("<Key>F5</Key>")]
        public Key CircleRenderMode
        {
            get => (Key)this[nameof(CircleRenderMode)];
            set => this[nameof(CircleRenderMode)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Circle Wireframe Mode", "Circles between the various wireframe modes")]
        [DefaultSettingValue("<Key>F6</Key>")]
        public Key CircleWireframe
        {
            get => (Key)this[nameof(CircleWireframe)];
            set => this[nameof(CircleWireframe)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Swap Geometry", "Changes between rendering visual and collision geometry")]
        [DefaultSettingValue("<Key>F7</Key>")]
        public Key SwapGeometry
        {
            get => (Key)this[nameof(SwapGeometry)];
            set => this[nameof(SwapGeometry)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Circle Object Relations", "Draws lines between object locations")]
        [DefaultSettingValue("<Key>F8</Key>")]
        public Key CircleObjectRelations
        {
            get => (Key)this[nameof(CircleObjectRelations)];
            set => this[nameof(CircleObjectRelations)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Display Bounds", "Displays geometry bounds")]
        [DefaultSettingValue("<Key>F9</Key>")]
        public Key DisplayBounds
        {
            get => (Key)this[nameof(DisplayBounds)];
            set => this[nameof(DisplayBounds)] = value;
        }

        [UserScopedSetting()]
        [SettingsKey("Circle Backwards", "Hold this button when using a hotkey for circling options to circle in the other direction")]
        [DefaultSettingValue("<Key>RightShift</Key>")]
        public Key CircleBackward
        {
            get => (Key)this[nameof(CircleBackward)];
            set => this[nameof(CircleBackward)] = value;
        }

    }
}
