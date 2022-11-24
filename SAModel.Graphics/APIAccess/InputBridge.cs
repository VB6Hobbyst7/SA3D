using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Input;

namespace SATools.SAModel.Graphics.APIAccess
{
    /// <summary>
    /// Input bridge between window/control and the api
    /// </summary>
    public class InputBridge
    {
        /// <summary>
        /// Input handler to get he last pressed keys of
        /// </summary>
        internal Input inputHandler;

        /// <summary>
        /// Gets called when the API places the cursor (local position)
        /// </summary>
        public EventHandler<Vector2> OnSetCursorPosition { get; set; }

        /// <summary>
        /// Gets called when the API changes the cursor lock
        /// </summary>
        public EventHandler<bool> OnSetMouselock { get; set; }

        /// <summary>
        /// Pressed keys
        /// </summary>
        internal HashSet<Key> PressedKeys { get; } = new();

        /// <summary>
        /// released keys for the next frame
        /// </summary>
        private readonly HashSet<Key> _releasedKeys = new();

        /// <summary>
        /// pressed buttons
        /// </summary>
        internal HashSet<MouseButton> PressedButtons { get; } = new();

        /// <summary>
        /// released buttons for the next frame
        /// </summary>
        private readonly HashSet<MouseButton> _releasedButtons = new();

        /// <summary>
        /// whether the mouse had a valid location before
        /// </summary>
        private bool hadLoc;

        internal Vector2 CursorLocation { get; private set; }

        internal Vector2 CursorDelta { get; private set; }

        internal float ScrollDelta { get; private set; }

        /// <summary>
        /// Called before the input update; <br/>
        /// Releases keys
        /// </summary>
        internal void PreUpdate()
        {
            if (_releasedKeys.Count > 0)
            {
                var keyWasPressed = inputHandler._keyPressed;
                foreach (var k in _releasedKeys)
                    if (keyWasPressed.Contains(k))
                        PressedKeys.Remove(k);
                _releasedKeys.RemoveWhere(x => !PressedKeys.Contains(x));
            }

            if (_releasedButtons.Count > 0)
            {
                var mouseWasPressed = inputHandler._mousePressed;
                foreach (var b in _releasedButtons)
                    if (mouseWasPressed.Contains(b))
                        PressedButtons.Remove(b);
                _releasedButtons.RemoveWhere(x => !PressedButtons.Contains(x));
            }

        }

        /// <summary>
        /// Called after the input update; <br/>
        /// Clears deltas
        /// </summary>
        internal void PostUpdate()
        {
            ScrollDelta = 0;
            CursorDelta = default;
        }

        /// <summary>
        /// Called when a key was pressed
        /// </summary>
        public void KeyPressed(Key key)
            => PressedKeys.Add(key);

        /// <summary>
        /// Called when a key was released
        /// </summary>
        public void KeyReleased(Key key)
            => _releasedKeys.Add(key);

        /// <summary>
        /// Called when a mouse button was pressed
        /// </summary>
        public void MouseButtonPressed(MouseButton button)
            => PressedButtons.Add(button);

        /// <summary>
        /// Called when a mouse button was released
        /// </summary>
        public void MouseButtonReleased(MouseButton button)
            => _releasedButtons.Add(button);

        /// <summary>
        /// Clears inputs
        /// </summary>
        public void ClearInputs()
        {
            PressedKeys.Clear();
            _releasedKeys.Clear();
            PressedButtons.Clear();
            _releasedButtons.Clear();
            UpdateCursorPos(null, null);
        }

        /// <summary>
        /// Updates the cursor position
        /// </summary>
        /// <param name="pos">The new position; null if its outside the window</param>
        /// <param name="recenter">Recentering position; null if mouse isnt locked to center</param>
        public void UpdateCursorPos(Vector2? pos, Vector2? recenter)
        {
            if (!pos.HasValue)
            {
                hadLoc = false;
                CursorDelta = default;
                return;
            }

            if (recenter.HasValue)
            {
                if (recenter != pos.Value)
                    CursorDelta = pos.Value - recenter.Value;
                CursorLocation = recenter.Value;
                return;
            }
            else if (hadLoc)
                CursorDelta += pos.Value - CursorLocation;
            else
                CursorDelta = default;

            hadLoc = true;
            CursorLocation = pos.Value;
        }

        /// <summary>
        /// Updates the scroll delta
        /// </summary>
        public void UpdateScroll(float delta)
            => ScrollDelta += delta;


        /// <summary>
        /// Places the cursor
        /// </summary>
        /// <param name="newPos"></param>
        internal void SetCursorPosition(Vector2 newPos)
        {
            OnSetCursorPosition.Invoke(null, newPos);
            CursorLocation = newPos;
        }
    }
}
