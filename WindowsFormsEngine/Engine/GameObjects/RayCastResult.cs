using System.Numerics;

namespace WindowsFormsApp1
{
    public struct RayCastResult
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public GameObject HittedTarget;
        public int Size;
        public bool Found;

        public RayCastResult(Vector2 origin, Vector2 direction, int size, GameObject hittedTarget)
        {
            Origin = origin;
            Direction = direction;
            Size = size;
            HittedTarget = hittedTarget;
            Found = hittedTarget != null;
        }
    }
}