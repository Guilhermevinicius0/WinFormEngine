using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class InputComponent
    {
        public HashSet<Keys> PressedKeys = new HashSet<Keys>();
        public HashSet<Keys> PreviewsKeys = new HashSet<Keys>();

        public HashSet<MouseButtons> PressedMouseButtons = new HashSet<MouseButtons>();
        public HashSet<MouseButtons> PreviewMouseButtons = new HashSet<MouseButtons>();

        public int MouseDelta = 0;
        public bool IsMouseOnScreen = false;

        public Vector2 CurrentMousePosition = Vector2.Zero;
        public Vector2 PreviewsMousePosition = Vector2.Zero;

        public void UpdateMouse(float x, float y)
        {
            CurrentMousePosition.X = x;
            CurrentMousePosition.Y = y;
        }

        public void UpdateMouseButtonState(MouseButtons button, bool state)
        {
            if (state)
                PressedMouseButtons.Add(button);
            else
                PressedMouseButtons.Remove(button);
        }

        public bool IsMouseButtonPressed(MouseButtons button)
        => PressedMouseButtons.Contains(button);

        public bool IsMouseButtonJustPressed(MouseButtons button)
            => PressedMouseButtons.Contains(button) && !PreviewMouseButtons.Contains(button);

        public Vector2 GetMousePositionInWorld(Camera camera)
            => camera.ScreenToWorld(CurrentMousePosition);

        public Vector2 GetMousePositionDelta()
            => CurrentMousePosition - PreviewsMousePosition;

        public float GetScrollWheelDelta()
            => MouseDelta / 120f;

        public void UpdateKeyState(Keys key, bool state)
        {
            if (state)
                PressedKeys.Add(key);
            else
                PressedKeys.Remove(key);
        }

        public bool GetKeyState(Keys key)
         => PressedKeys.Contains(key);

        public bool IsKeyJustPressed(Keys key)
            => PressedKeys.Contains(key) && !PreviewsKeys.Contains(key);

        public int GetAxis(Keys directionA, Keys directionB)
        {
            int direction = 0;

            if (GetKeyState(directionA) && !GetKeyState(directionB))
                direction -= 1;

            if (!GetKeyState(directionA) && GetKeyState(directionB))
                direction += 1;

            return direction;
        }

        public Vector2 GetVector(Keys left, Keys right, Keys up, Keys down)
        {
            Vector2 vector = new Vector2(GetAxis(left, right), GetAxis(up, down));

            if (vector.Length() > 1)
                return Vector2.Normalize(vector);

            return vector;
        }

        public void UpdatePreviewsInputs()
        {
            PreviewsKeys.Clear();
            PreviewsKeys.UnionWith(PressedKeys);

            PreviewMouseButtons.Clear();
            PreviewMouseButtons.UnionWith(PressedMouseButtons);
        }
    }
}