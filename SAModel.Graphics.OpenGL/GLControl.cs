﻿using OpenTK.Windowing.Common;
using OpenTK.Wpf;
using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SATools.SAModel.Graphics.OpenGL
{
    internal partial class NativeMethods
    {
        /// Return Type: BOOL->int  
        ///X: int  
        ///Y: int  
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);
    }

    public class GLControl : GLWpfControl
    {
        private readonly GAPIAInputBridge _inputBridge;

        private bool _mouseLocked;

        private Vector2 _center;

        private Context _context;

        public GLControl(Context context, GAPIAInputBridge inputBridge) : base()
        {
            _context = context;
            _inputBridge = inputBridge;

            _inputBridge.OnSetCursorPosition += (o, v2) =>
            {
                if(!_mouseLocked)
                {
                    var p = ToScreenPos(v2);
                    NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                }
            };

            _inputBridge.OnSetMouselock += (o, v) =>
            {
                _mouseLocked = v;
                Mouse.OverrideCursor = v ? Cursors.None : null;
            };

            Focusable = true;

            Loaded += (o, e) =>
            {
                context.Resolution = new((int)RenderSize.Width, (int)RenderSize.Height);
                _center = new((float)RenderSize.Width / 2f, (float)RenderSize.Height / 2f);
            };

            Ready += context.GraphicsInit;

            Render += (time) =>
            {
                if(context.IsFocused && !IsFocused)
                    inputBridge.ClearInputs();
                context.IsFocused = IsFocused;
                context.Update(time.TotalSeconds);

                if(_mouseLocked)
                {
                    var p = ToScreenPos(_center);
                    NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                }

                context.Render();
            };


            GLWpfControlSettings settings = new()
            {
                GraphicsContextFlags = ContextFlags.ForwardCompatible,
                MajorVersion = 4,
                MinorVersion = 6,
            };

            Start(settings);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            base.OnRenderSizeChanged(info);
            _center = new((float)RenderSize.Width / 2f, (float)RenderSize.Height / 2f);
        }



        #region Input handling

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _inputBridge.KeyPressed(e.Key);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            _inputBridge.KeyReleased(e.Key);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var pos = e.GetPosition(this);
            Vector2 posV2 = new((float)pos.X, (float)pos.Y);
            if(_mouseLocked)
                _inputBridge.UpdateCursorPos(posV2, _center);
            else
                _inputBridge.UpdateCursorPos(posV2, null);
            
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
             _inputBridge.UpdateCursorPos(null, null);
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _inputBridge.UpdateScroll(e.Delta);
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if(!IsFocused)
                Focus();
            MouseButton btn = e.ChangedButton switch
            {
                System.Windows.Input.MouseButton.Middle => MouseButton.Middle,
                System.Windows.Input.MouseButton.Right => MouseButton.Right,
                _ => (MouseButton)e.ChangedButton,
            };

            _inputBridge.MouseButtonPressed(btn);
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            MouseButton btn = e.ChangedButton switch
            {
                System.Windows.Input.MouseButton.Middle => MouseButton.Middle,
                System.Windows.Input.MouseButton.Right => MouseButton.Right,
                _ => (MouseButton)e.ChangedButton
            };

            _inputBridge.MouseButtonReleased(btn);
        }

        private Point ToScreenPos(Vector2 relative)
        {
            return PointToScreen(new Point(relative.X, relative.Y));
        }
        #endregion
    }
}