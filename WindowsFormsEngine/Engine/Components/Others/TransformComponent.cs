using System.Numerics;

namespace WindowsFormsApp1
{
    public class TransformComponent
    {
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = Vector2.One;
        public Vector2 Pivot = Vector2.Zero;
        public float Rotation = 0f;

        public Vector2 GetCenteredPosition()
        {
            return new Vector2(Position.X + Size.X / 2, Position.Y + Size.Y / 2);
        }
    }
}