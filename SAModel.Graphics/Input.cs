using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Structs;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Input;
using Point = System.Drawing.Point;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Used to receive input from keyboard and mouse
    /// </summary>
    public class Input
    {
        /// <summary>
        /// Responsible for updating the input
        /// </summary>
        private readonly InputBridge _bridge;

        /// <summary>
        /// Keys that are pressed
        /// </summary>
        internal HashSet<Key> _keyPressed;

        /// <summary>
        /// Keys that were pressed on the last update
        /// </summary>
        internal HashSet<Key> _keyWasPressed;

        /// <summary>
        /// Last state of each mouse button
        /// </summary>
        internal HashSet<MouseButton> _mouseWasPressed;

        /// <summary>
        /// Current state of each mouse button
        /// </summary>
        internal HashSet<MouseButton> _mousePressed;

        /// <summary>
        /// The last read cursor location
        /// </summary>
        public Vector2 CursorPos { get; private set; }

        /// <summary>
        /// The amount that the cursor moved
        /// </summary>
        public Vector2 CursorDif { get; private set; }

        /// <summary>
        /// The amount that the scroll was used 
        /// </summary>
        public float ScrollDif { get; private set; }

        private bool _lockCursor;

        public bool LockCursor
        {
            get => _lockCursor;
            set
            {
                if(value == _lockCursor)
                    return;
                _lockCursor = value;
                _bridge.OnSetMouselock(null, value);
            }
        }

        public Input(InputBridge apiAccess)
        {
            _bridge = apiAccess ?? throw new NotInitializedException("Inputhandler required");
            _bridge.inputHandler = this;

            _keyPressed = new();
            _keyWasPressed = new();
            _mousePressed = new();
            _mouseWasPressed = new();
        }


        /// <summary>
        /// Updates the input
        /// </summary>
        public void Update(bool focused)
        {
            _bridge.PreUpdate();

            var tKey = _keyWasPressed;
            _keyWasPressed = _keyPressed;
            _keyPressed = tKey;
            _keyPressed.Clear();

            var tMouse = _mouseWasPressed;
            _mouseWasPressed = _mousePressed;
            _mousePressed = tMouse;
            _mousePressed.Clear();


            if(focused)
            {
                _keyPressed.UnionWith(_bridge.PressedKeys);
                _mousePressed.UnionWith(_bridge.PressedButtons);
            }

            CursorDif = _bridge.CursorDelta;
            ScrollDif = _bridge.ScrollDelta;
            CursorPos = _bridge.CursorLocation;

            _bridge.PostUpdate();
        }

        /// <summary>
        /// Places the cursor in global screen space
        /// </summary>
        /// <param name="loc">The new location that the cursor should be at</param>
        public void PlaceCursor(Vector2 loc)
        {
            _bridge.SetCursorPosition(loc);
            CursorPos = loc;
        }

        /// <summary>
        /// Whether a keyboard key is being held
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsKeyDown(Key key)
            => _keyPressed.Contains(key);

        /// <summary>
        /// Whether a mouse button is being held
        /// </summary>
        /// <param name="btn"></param>
        /// <returns></returns>
        public bool IsKeyDown(MouseButton btn)
            => _mousePressed.Contains(btn);

        /// <summary>
        /// Whether a keyboard key was pressed
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool KeyPressed(Key key)
            => _keyPressed.Contains(key) && !_keyWasPressed.Contains(key);

        /// <summary>
        /// Whether a mouse button was pressed
        /// </summary>
        /// <param name="btn"></param>
        /// <returns></returns>
        public bool KeyPressed(MouseButton btn)
            => _mousePressed.Contains(btn) && !_mouseWasPressed.Contains(btn);

        /// <summary>
        /// Whether a keyboard key was released
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool KeyReleased(Key key)
            => !_keyPressed.Contains(key) && _keyWasPressed.Contains(key);

        /// <summary>
        /// Whether a mouse button was released
        /// </summary>
        /// <param name="btn"></param>
        /// <returns></returns>
        public bool KeyReleased(MouseButton btn)
            => !_mousePressed.Contains(btn) && _mouseWasPressed.Contains(btn);
    }
}
