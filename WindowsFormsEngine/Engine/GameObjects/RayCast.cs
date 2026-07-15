using System.Numerics;

namespace WindowsFormsApp1
{
    public class RayCast : TransformComponent
    {
        public CollisionComponent BoxCollider;


        public RayCast(Vector2 position, Vector2 size)
        {
            this.Position = position;
            this.Size = size;
            BoxCollider = new CollisionComponent(this, size);
        }
    }
}