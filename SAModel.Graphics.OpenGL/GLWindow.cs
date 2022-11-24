using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SATools.SAModel.Graphics.APIAccess;
using System;
using System.Collections.Generic;
using System.Drawing;
using Key = System.Windows.Input.Key;
using MouseButton = System.Windows.Input.MouseButton;
using TKey = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using TMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;

namespace SATools.SAModel.Graphics.OpenGL
{
    public class GLWindow : GameWindow
    {
        private static readonly Dictionary<TKey, Key> Keymap = new()
        {
            { TKey.Unknown, Key.None },
            { TKey.Space, Key.Space },
            { TKey.Apostrophe, Key.OemQuotes },
            { TKey.Comma, Key.OemComma },
            { TKey.Minus, Key.OemMinus },
            { TKey.Period, Key.OemPeriod },
            { TKey.Slash, Key.OemQuestion },
            { TKey.D0, Key.D0 },
            { TKey.D1, Key.D1 },
            { TKey.D2, Key.D2 },
            { TKey.D3, Key.D3 },
            { TKey.D4, Key.D4 },
            { TKey.D5, Key.D5 },
            { TKey.D6, Key.D6 },
            { TKey.D7, Key.D7 },
            { TKey.D8, Key.D8 },
            { TKey.D9, Key.D9 },
            { TKey.Semicolon, Key.OemSemicolon },
            { TKey.Equal, Key.OemPlus },
            { TKey.A, Key.A },
            { TKey.B, Key.B },
            { TKey.C, Key.C },
            { TKey.D, Key.D },
            { TKey.E, Key.E },
            { TKey.F, Key.F },
            { TKey.G, Key.G },
            { TKey.H, Key.H },
            { TKey.I, Key.I },
            { TKey.J, Key.J },
            { TKey.K, Key.K },
            { TKey.L, Key.L },
            { TKey.M, Key.M },
            { TKey.N, Key.N },
            { TKey.O, Key.O },
            { TKey.P, Key.P },
            { TKey.Q, Key.Q },
            { TKey.R, Key.R },
            { TKey.S, Key.S },
            { TKey.T, Key.T },
            { TKey.U, Key.U },
            { TKey.V, Key.V },
            { TKey.W, Key.W },
            { TKey.X, Key.X },
            { TKey.Y, Key.Y },
            { TKey.Z, Key.Z },
            { TKey.LeftBracket, Key.OemOpenBrackets },
            { TKey.Backslash, Key.OemBackslash },
            { TKey.RightBracket, Key.OemCloseBrackets },
            { TKey.GraveAccent, Key.OemTilde },
            { TKey.Escape, Key.Escape },
            { TKey.Enter, Key.Enter },
            { TKey.Tab, Key.Tab },
            { TKey.Backspace, Key.Back },
            { TKey.Insert, Key.Insert },
            { TKey.Delete, Key.Delete },
            { TKey.Right, Key.Right },
            { TKey.Left, Key.Left },
            { TKey.Down, Key.Down },
            { TKey.Up, Key.Up },
            { TKey.PageUp, Key.PageUp },
            { TKey.PageDown, Key.PageDown },
            { TKey.Home, Key.Home },
            { TKey.End, Key.End },
            { TKey.CapsLock, Key.CapsLock },
            { TKey.ScrollLock, Key.Scroll },
            { TKey.NumLock, Key.NumLock },
            { TKey.PrintScreen, Key.PrintScreen },
            { TKey.Pause, Key.Pause },
            { TKey.F1, Key.F1 },
            { TKey.F2, Key.F2 },
            { TKey.F3, Key.F3 },
            { TKey.F4, Key.F4 },
            { TKey.F5, Key.F5 },
            { TKey.F6, Key.F6 },
            { TKey.F7, Key.F7 },
            { TKey.F8, Key.F8 },
            { TKey.F9, Key.F9 },
            { TKey.F10, Key.F10 },
            { TKey.F11, Key.F11 },
            { TKey.F12, Key.F12 },
            { TKey.F13, Key.F13 },
            { TKey.F14, Key.F14 },
            { TKey.F15, Key.F15 },
            { TKey.F16, Key.F16 },
            { TKey.F17, Key.F17 },
            { TKey.F18, Key.F18 },
            { TKey.F19, Key.F19 },
            { TKey.F20, Key.F20 },
            { TKey.F21, Key.F21 },
            { TKey.F22, Key.F22 },
            { TKey.F23, Key.F23 },
            { TKey.F24, Key.F24 },
            { TKey.F25, Key.None },
            { TKey.KeyPad0, Key.NumPad0 },
            { TKey.KeyPad1, Key.NumPad1 },
            { TKey.KeyPad2, Key.NumPad2 },
            { TKey.KeyPad3, Key.NumPad3 },
            { TKey.KeyPad4, Key.NumPad4 },
            { TKey.KeyPad5, Key.NumPad5 },
            { TKey.KeyPad6, Key.NumPad6 },
            { TKey.KeyPad7, Key.NumPad7 },
            { TKey.KeyPad8, Key.NumPad8 },
            { TKey.KeyPad9, Key.NumPad9 },
            { TKey.KeyPadDecimal, Key.Decimal },
            { TKey.KeyPadDivide, Key.Divide },
            { TKey.KeyPadMultiply, Key.Multiply },
            { TKey.KeyPadSubtract, Key.Subtract },
            { TKey.KeyPadAdd, Key.Add },
            { TKey.KeyPadEnter, Key.Enter },
            { TKey.KeyPadEqual, Key.None }, // ?
            { TKey.LeftShift, Key.LeftShift },
            { TKey.LeftControl, Key.LeftCtrl },
            { TKey.LeftAlt, Key.LeftAlt },
            { TKey.LeftSuper, Key.LWin },
            { TKey.RightShift, Key.RightShift },
            { TKey.RightControl, Key.RightCtrl },
            { TKey.RightAlt, Key.RightAlt },
            { TKey.RightSuper, Key.RWin },
            { TKey.Menu, Key.None }
        };

        private static readonly Dictionary<TMouseButton, MouseButton> MouseButtonMap = new()
        {
            { TMouseButton.Left, MouseButton.Left },
            { TMouseButton.Middle, MouseButton.Middle },
            { TMouseButton.Right, MouseButton.Right },
            { TMouseButton.Button4, MouseButton.XButton1 },
            { TMouseButton.Button5, MouseButton.XButton2 }
        };

        private readonly Context _context;

        private readonly InputBridge _inputBridge;

        private bool _mouseLocked;

        private Vector2 _center;

        public GLWindow(Context context, InputBridge inputBridge, Size resolution, Point? location = null, bool UseVsync = true) :
            base(
                new(),
                new()
                {
                    API = ContextAPI.OpenGL,
                    APIVersion = new Version(4, 6),
                    Title = "SA3D",
                    Size = new(resolution.Width, resolution.Height),
                    NumberOfSamples = 4,
                    Flags = ContextFlags.ForwardCompatible,
                    WindowBorder = WindowBorder.Resizable,
                })
        {
            if (!UseVsync)
                VSync = VSyncMode.Off;
            _context = context;
            _inputBridge = inputBridge;

            _inputBridge.OnSetCursorPosition += (o, v2) =>
            {
                if (!_mouseLocked)
                    MousePosition = new(v2.X, v2.Y);
            };

            _inputBridge.OnSetMouselock += (o, v) =>
            {
                _mouseLocked = v;
                CursorVisible = !v;
            };

            if (location.HasValue)
                Location = new(location.Value.X, location.Value.Y);

            _center = Size / 2;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            _context.GraphicsInit();
            _context.Location = new(Location.X, Location.Y);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            _context.Resolution = new(ClientSize.X, ClientSize.Y);
            _center = Size / 2;
        }

        protected override void OnMove(WindowPositionEventArgs e)
        {
            base.OnMove(e);
            _context.Location = new(Location.X, Location.Y);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            _context.IsFocused = IsFocused;
            _context.Update(e.Time);
            if (_mouseLocked)
            {
                MousePosition = _center;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _context.Render();
            Context.SwapBuffers();
        }

        #region Input handling

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Keymap.TryGetValue(e.Key, out Key key))
                _inputBridge.KeyPressed(key);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (Keymap.TryGetValue(e.Key, out Key key))
                _inputBridge.KeyReleased(key);
        }

        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
        {
            base.OnFocusedChanged(e);
            if (!e.IsFocused)
                _inputBridge.ClearInputs();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            var pos = e.Position;
            if (_mouseLocked)
                _inputBridge.UpdateCursorPos(new(pos.X, pos.Y), new(_center.X, _center.Y));
            else
                _inputBridge.UpdateCursorPos(new(pos.X, pos.Y), null);
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            _inputBridge.ClearInputs();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _inputBridge.UpdateScroll(e.Offset.Y);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (MouseButtonMap.TryGetValue(e.Button, out MouseButton m))
                _inputBridge.MouseButtonPressed(m);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (MouseButtonMap.TryGetValue(e.Button, out MouseButton m))
                _inputBridge.MouseButtonReleased(m);
        }


        #endregion

    }
}
