using System;
using System.Collections.Generic;
using System.Numerics;

namespace WindowsFormsApp1
{
    public abstract class GameObject : TransformComponent
    {
        public Scene _scene;
        public readonly bool IsStatic;

        public InputComponent Input { get => _scene.Input; }
        public CollisionComponent BoxCollider;
        public GraphicsComponent GraphicsComponent;

        public int ZIndex = 0;

        public readonly List<string> Tags = new List<string>();

        public GameObject(Scene scene, Vector2 position, Vector2 size, Vector2 collisionSize, bool isStatic = false)
        {
            this.BoxCollider = new CollisionComponent(this, collisionSize);
            _scene = scene;
            Position = position;
            Size = size;
            IsStatic = isStatic;
        }

        public abstract void OnCreation();

        public abstract void Update(float delta);

        public abstract void OnDestroy();

        public void MoveAndCollide(Vector2 velocity)
        {
            _scene.MoveAndCollide(this, velocity);
        }

        public bool IsOnFloor()
        {
            return _scene.IsOnFloor(this);
        }

        public void Destroy()
        {
            _scene.DestroyObject(this);
        }

        public bool IsCollidingWith(GameObject target)
        {
            return _scene.IsCollidingWith(this, target);
        }

        public RayCastResult RayCast(Vector2 origin, Vector2 direction, int size,
          List<GameObject> excludeList = null, List<GameObject> FilterList = null,
          Type type = null, bool selfCollide = false)
        {
            return _scene.RayCast(this, origin, direction, size, excludeList, FilterList, type, selfCollide);
        }

        public void AddTag(string tag)
        {
            Tags.Add(tag);
            _scene.AddTaggedObject(this, tag);
        }

        public void RemoveTag(string tag)
        {
            if (Tags.Contains(tag))
            {
                Tags.Remove(tag);
                _scene.RemoveTaggedObject(this, tag);
            }
        }

        public Vector2 GetDistanceTo(GameObject target)
        {
            return _scene.GetDistanceBetweenObjects(this, target);
        }

        public Vector2 GetNormalTo(GameObject target)
        {
            return _scene.GetNormalBetweenObjects(this, target);
        }
    }
}