using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SATools.SAModel.Graphics.APIAccess
{
    public class GAPIAInputBridge
    {
        internal Input inputHandler;

        public EventHandler<Vector2> OnSetCursorPosition { get; set; }

        public EventHandler<bool> OnSetMouselock { get; set; }

        internal HashSet<Key> PressedKeys { get; }

        internal HashSet<Key> ReleasedKeys { get; private set; }

        internal HashSet<MouseButton> PressedButtons { get; }

        internal HashSet<MouseButton> ReleasedButtons { get; private set; }


        private bool hadLoc;

        internal Vector2 CursorLocation { get; private set; }

        internal Vector2 CursorDelta { get; private set; }

        internal float ScrollDelta { get; private set; }

        public GAPIAInputBridge()
        {
            PressedKeys = new();
            ReleasedKeys = new();
            PressedButtons = new();
            ReleasedButtons = new();
        }

        internal void PreUpdate()
        {
            if(ReleasedKeys.Count > 0)
            {
                var keyWasPressed = inputHandler._keyPressed;
                HashSet<Key> nextKeyReleased = new();
                foreach(var k in ReleasedKeys)
                {
                    if(keyWasPressed.Contains(k))
                        PressedKeys.Remove(k);
                    else
                        nextKeyReleased.Add(k);
                }
                ReleasedKeys = nextKeyReleased;
            }

            if(ReleasedButtons.Count > 0)
            {
                var mouseWasPressed = inputHandler._mousePressed;
                HashSet<MouseButton> nextBtnReleased = new();
                foreach(var b in ReleasedButtons)
                {
                    if(mouseWasPressed.Contains(b))
                        PressedButtons.Remove(b);
                    else
                        nextBtnReleased.Add(b);
                }
                ReleasedButtons = nextBtnReleased;
            }

        }

        internal void PostUpdate()
        {
            ScrollDelta = 0;
            CursorDelta = default;
        }

        public void KeyPressed(Key key)
            => PressedKeys.Add(key);

        public void KeyReleased(Key key)
            => ReleasedKeys.Add(key);

        public void MouseButtonPressed(MouseButton button)
            => PressedButtons.Add(button);

        public void MouseButtonReleased(MouseButton button)
            => ReleasedButtons.Add(button);

        public void ClearInputs()
        {
            PressedKeys.Clear();
            ReleasedKeys.Clear();
            PressedButtons.Clear();
            PressedKeys.Clear();
            hadLoc = true;
        }

        public void UpdateCursorPos(Vector2? pos, Vector2? recenter)
        {
            if(!pos.HasValue)
            {
                hadLoc = false;
                CursorDelta = default;
                return;
            }

            if(recenter.HasValue)
            { 
                if(recenter != pos.Value)
                    CursorDelta = pos.Value - recenter.Value;
                CursorLocation = recenter.Value;
                return;
            }
            else if(hadLoc)
                CursorDelta += pos.Value - CursorLocation;
            else
                CursorDelta = default;

            hadLoc = true;
            CursorLocation = pos.Value;
        }

        public void UpdateScroll(float dif)
        {
            ScrollDelta -= dif;
        }

        public void SetCursorPosition(Vector2 newPos)
        {
            OnSetCursorPosition.Invoke(null, newPos);
            CursorLocation = newPos;
        }
    }
}
