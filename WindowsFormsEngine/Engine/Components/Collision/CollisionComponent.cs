using System.Numerics;

namespace WindowsFormsApp1
{
    public class CollisionComponent
    {
        private TransformComponent _owner;
        public Vector2 Size;
        public bool Active = true;

        public Vector2 Position => _owner.Position;

        public CollisionComponent(TransformComponent owner, Vector2 size)
        {
            _owner = owner;
            Size = size;
        }

        public bool IsColliding(CollisionComponent target)
        {
            if (this.Size == Vector2.Zero || target.Size == Vector2.Zero || !Active)
                return false;

            return this.Position.X < target.Position.X + target.Size.X &&
              this.Position.X + this.Size.X > target.Position.X &&
              this.Position.Y < target.Position.Y + target.Size.Y &&
              this.Position.Y + this.Size.Y > target.Position.Y;
        }

        public Vector2 GetCollissionCenter()
        {
            return new Vector2((this.Position.X + this.Size.X) / 2f, (this.Position.Y + this.Size.Y) / 2f);
        }
    }
}