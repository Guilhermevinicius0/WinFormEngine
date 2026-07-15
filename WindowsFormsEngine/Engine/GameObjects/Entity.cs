using System.Drawing;
using System.Numerics;

namespace WindowsFormsApp1
{
    public abstract class Entity : GameObject
    {
        public bool EnableSleepWhenAway = false;
        public bool IsAwake = true;
        public float AwakeDistance = 800f;

        public Entity(Scene scene, Color color, Vector2 size, Vector2 collisionSize, Vector2 position, bool isStatic = false) : base(scene, position, size, collisionSize, isStatic)
        {
            GraphicsComponent = new RectangleRender(color);
        }
    }
}