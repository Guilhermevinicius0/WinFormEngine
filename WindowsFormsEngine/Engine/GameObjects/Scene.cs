using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public class Scene
    {
        public IScreen Screen;
        public Camera MainCamera;
        public RenderComponent Render = new RenderComponent();
        public InputComponent Input = new InputComponent();

        private TileMap _tileMap = new TileMap();
        public bool HasTileMap => _tileMap != null;

        private readonly List<GameObject> DynamicObjects = new List<GameObject>();
        private readonly List<GameObject> StaticObjects = new List<GameObject>();
        private readonly List<GameObject> AllObjects = new List<GameObject>();

        private Dictionary<string, HashSet<GameObject>> TaggedObjects = new Dictionary<string, HashSet<GameObject>>();

        private SpatialGrid _grid = new SpatialGrid();

        private Queue<GameObject> ToAdd = new Queue<GameObject>();
        private Queue<GameObject> ToRemove = new Queue<GameObject>();

        private int MaxQueueIterations = 1000; // use -1 to set no limit per frame, low limits drops more fps with massive operations

        public void ProcessPhysics(float delta)
        {
            ProcessQueues();
            ProcessObjects(delta);
            MainCamera.Update(delta);
            Input.UpdatePreviewsInputs();
        }

        public void LoadTileMap()
        {
            TileMapLoader.Load(this, _tileMap);
        }

        public void ProcessQueues()
        {
            if (ToAdd.Count == 0 && ToRemove.Count == 0)
                return;

            int totalToProcess = MaxQueueIterations >= 0 ? MaxQueueIterations : ToAdd.Count + ToRemove.Count;

            for (int i = 0; i < totalToProcess; i++)
            {
                int index = -1;
                if (ToAdd.Count > 0)
                {
                    var obj = ToAdd.Dequeue();

                    if (obj.IsStatic)
                    {
                        index = StaticObjects.BinarySearch(obj, Comparer<GameObject>.Create((a, b) => a.ZIndex.CompareTo(b.ZIndex)));

                        if (index < 0)
                            index = ~index;

                        StaticObjects.Insert(index, obj);
                    }
                    else
                    {
                        index = DynamicObjects.BinarySearch(obj, Comparer<GameObject>.Create((a, b) => a.ZIndex.CompareTo(b.ZIndex)));

                        if (index < 0)
                            index = ~index;

                        DynamicObjects.Insert(index, obj);
                    }

                    AllObjects.Insert(index, obj);
                    _grid.AddObject(obj);
                    obj.OnCreation();
                    DebugStats.Objects++;
                }

                else if (ToRemove.Count > 0)
                {
                    var obj = ToRemove.Dequeue();

                    if (DynamicObjects.Contains(obj))
                    {
                        DynamicObjects.Remove(obj);
                        AllObjects.Remove(obj);
                        _grid.RemoveObject(obj);
                        obj.OnDestroy();
                        DebugStats.Objects--;
                    }
                    else if (StaticObjects.Contains(obj))
                    {
                        StaticObjects.Remove(obj);
                        AllObjects.Remove(obj);
                        _grid.RemoveObject(obj);
                        obj.OnDestroy();
                        DebugStats.Objects--;
                    }
                }

                else
                    break;
            }
        }

        public void ProcessObjects(float delta)
        {
            GameObject current = null;
            Vector2 previewsPosition = Vector2.Zero;
            for (int i = 0; i < DynamicObjects.Count; i++)
            {
                current = DynamicObjects[i];
                previewsPosition = current.Position;

                current.Update(delta);
                _grid.UpdateObjectCell(current, previewsPosition);
            }
        }

        public void AddObject(GameObject obj)
        {
            ToAdd.Enqueue(obj);
        }

        public void DestroyObject(GameObject obj)
        {
            ToRemove.Enqueue(obj);
        }

        public List<GameObject> GetObjects()
        {
            return AllObjects;
        }

        public List<GameObject> GetTaggedObjects(string tag)
        {
            if (!TaggedObjects.ContainsKey(tag))
                return new List<GameObject>();

            return TaggedObjects[tag].ToList();
        }

        public void AddTaggedObject(GameObject obj, string tag)
        {
            if (!TaggedObjects.ContainsKey(tag))
                TaggedObjects.Add(tag, new HashSet<GameObject>());

            TaggedObjects[tag].Add(obj);
        }
        public void RemoveTaggedObject(GameObject obj, string tag)
        {
            if (TaggedObjects.ContainsKey(tag))
            {
                TaggedObjects[tag].Remove(obj);

                if (TaggedObjects[tag].Count == 0)
                    TaggedObjects.Remove(tag);
            }
        }

        public void DrawScene(Graphics g)
        {
            int radius = (int)(Math.Max(MainCamera.Size.X, MainCamera.Size.Y) / _grid.CellSize);
            g.Clear(Screen.BackColor);
            Render.Draw(g, MainCamera, _grid.GetNearbyObjects(MainCamera.GetCenteredPosition(), radius));
        }

        public void DrawDebugOverLay(Graphics g)
        {
            if (!DebugStats.ShowDebug) return;

            g.DrawString("DEBUG", DebugStats.Font, DebugStats.Brush, 1, 1);
            g.DrawString($"Memory: {DebugStats.Memory:0.00} MB", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 1);
            g.DrawString($"FPS {DebugStats.Show_FPS}", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 2);
            g.DrawString($"Objects {DebugStats.Show_Objects}", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 3);
            g.DrawString($"Collision Checks: {DebugStats.Show_CollisionsCheck}", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 4);
            g.DrawString($"Draw Cells: {DebugStats.Show_DrawCells}", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 5);
            g.DrawString($"Visible Cells: {DebugStats.Show_VisibileCells}", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 6);
            g.DrawString($"Invisible Cells: {DebugStats.Show_InvisibileCells}", DebugStats.Font, DebugStats.Brush, 1, (1 + DebugStats.Font.Height + DebugStats.TextMargin) * 7);

        }

        public void UpdateDebugStats()
        {
            if (!DebugStats.InitFpsMetter.HasValue)
                DebugStats.InitFpsMetter = DateTime.Now;

            DebugStats.FPS++;


            if (DateTime.Now - DebugStats.InitFpsMetter.Value > TimeSpan.FromSeconds(1))
            {
                DebugStats.Show_CollisionsCheck = DebugStats.CollisionsCheck;
                DebugStats.Show_DrawCells = DebugStats.DrawCells;
                DebugStats.Show_VisibileCells = DebugStats.VisibileCells;
                DebugStats.Show_InvisibileCells = DebugStats.InvisibileCells;
                DebugStats.Show_Objects = DebugStats.Objects;
                DebugStats.Show_FPS = DebugStats.FPS;

                DebugStats.CollisionsCheck = 0;
                DebugStats.DrawCells = 0;
                DebugStats.VisibileCells = 0;
                DebugStats.InvisibileCells = 0;
                DebugStats.FPS = 0;

                DebugStats.Memory = (GC.GetTotalMemory(false) / 1024) / 1024;

                DebugStats.InitFpsMetter = DateTime.Now;
            }
        }

        public RayCastResult RayCast(GameObject parent, Vector2 origin, Vector2 direction, int size,
          List<GameObject> excludeList = null, List<GameObject> FilterList = null,
          Type type = null, bool selfCollide = false)
        {
            var ray = new RayCast(origin, Vector2.One);

            for (int i = 0; i < size; i++)
            {
                var obj = GetCollider(ray.BoxCollider);
                if (obj != null && obj.BoxCollider.Active)
                {
                    if (FilterList != null)
                    {
                        if ((obj == parent && selfCollide && (type ?? null) == parent.GetType()) ||
                          (type != null && obj.GetType() == type && FilterList.Contains(obj)))
                            return new RayCastResult(origin, direction, i + 1, obj);
                    }
                    else if (excludeList != null)
                    {
                        if ((obj == parent && selfCollide && (type ?? null) == parent.GetType()) ||
                          (type != null && obj.GetType() == type && !excludeList.Contains(obj)))
                            return new RayCastResult(origin, direction, i + 1, obj);
                    }
                    else
                    {
                        if (type != null && obj.GetType() == type)
                            return new RayCastResult(origin, direction, i + 1, obj);
                        else
                            return new RayCastResult(origin, direction, i + 1, obj);
                    }
                }

                ray.Position += direction;
            }

            return new RayCastResult(origin, direction, size, null);
        }

        public void MoveAndCollide(GameObject obj, Vector2 velocity)
        {
            const int MaxIterations = 10;
            int iterations = 0;

            obj.Position.X += velocity.X;
            while (CheckWorldColision(obj) && iterations < MaxIterations)
            {
                var hitted = GetCollider(obj);

                if (hitted == null)
                    break;

                float direction = velocity.X != 0 ? Math.Sign(velocity.X) : 1;
                float overlap = (obj.BoxCollider.Size.X / 2f) + (hitted.BoxCollider.Size.X / 2f) -
                                     Math.Abs(obj.GetCenteredPosition().X - hitted.GetCenteredPosition().X);

                if (overlap <= 0.001f)
                {
                    obj.Position.X -= direction * 0.5f;
                    iterations++;
                    continue;
                }

                obj.Position.X -= overlap * direction;
                iterations++;
            }

            iterations = 0;

            obj.Position.Y += velocity.Y;
            while (CheckWorldColision(obj) && iterations < MaxIterations)
            {
                var hitted = GetCollider(obj);

                if (hitted == null)
                    break;

                float direction = velocity.Y != 0 ? Math.Sign(velocity.Y) : 1;
                float overlap = (obj.BoxCollider.Size.Y / 2f) + (hitted.BoxCollider.Size.Y / 2f) -
                                Math.Abs(obj.GetCenteredPosition().Y - hitted.GetCenteredPosition().Y);

                if (overlap <= 0.001f)
                {
                    obj.Position.Y -= direction * 0.5f;
                    iterations++;
                    continue;
                }

                obj.Position.Y -= overlap * direction;
                iterations++;
            }
        }

        public bool IsOnFloor(GameObject target)
        {
            var originalPosition = target.Position;
            target.Position += Vector2.UnitY;
            bool grounded = CheckWorldColision(target);
            target.Position = originalPosition;

            return grounded;
        }

        public Vector2 GetFloorNormal(GameObject target)
        {
            var ground = _grid.GetNearbyObjects(target).FirstOrDefault(o => o is Wall &&
              target.BoxCollider.IsColliding(o.BoxCollider) &&
              target.Position.Y + target.Size.Y <= o.Position.Y + 1);

            if (ground != null)
                return Vector2.Normalize((ground.GetCenteredPosition() - target.GetCenteredPosition()));

            return Vector2.Zero;
        }

        public bool CheckWorldColision(GameObject target)
        {
            var objects = _grid.GetNearbyObjects(target);
            GameObject current = null;
            for (int i = 0; i < objects.Count; i++)
            {
                current = objects[i];

                if (current == target) continue;

                if (Math.Abs(current.Position.X - target.Position.X) > MainCamera.Size.X + 10 ||
                    Math.Abs(current.Position.Y - target.Position.Y) > MainCamera.Size.Y + 10)
                    continue;


                DebugStats.CollisionsCheck++;

                if (target.BoxCollider.IsColliding(current.BoxCollider))
                    return true;
            }

            return false;
        }

        public bool IsCollidingWith(GameObject a, GameObject b)
        {
            return a.BoxCollider.IsColliding(b.BoxCollider);
        }

        public GameObject GetCollider(CollisionComponent target)
        {
            var objects = GetObjects();
            GameObject current = null;
            for (int i = 0; i < objects.Count; i++)
            {
                current = objects[i];

                if (current.BoxCollider == target) continue;

                if (Math.Abs(current.Position.X - target.Position.X) > MainCamera.Size.X + 10 ||
                    Math.Abs(current.Position.Y - target.Position.Y) > MainCamera.Size.Y + 10)
                    continue;

                DebugStats.CollisionsCheck++;

                if (target.IsColliding(current.BoxCollider))
                    return current;
            }

            return null;
        }

        public GameObject GetCollider(GameObject target)
        {
            var objects = _grid.GetNearbyObjects(target);
            GameObject current = null;
            for (int i = 0; i < objects.Count; i++)
            {
                current = objects[i];

                if (current.BoxCollider == target.BoxCollider) continue;

                if (Math.Abs(current.Position.X - target.Position.X) > MainCamera.Size.X + 10 ||
                    Math.Abs(current.Position.Y - target.Position.Y) > MainCamera.Size.Y + 10)
                    continue;

                DebugStats.CollisionsCheck++;

                if (target.BoxCollider.IsColliding(current.BoxCollider))
                    return current;
            }

            return null;
        }

        public Vector2 GetDistanceBetweenObjects(GameObject a, GameObject b)
        {
            return a.Position - b.Position;
        }

        public Vector2 GetNormalBetweenObjects(GameObject a, GameObject b)
        {
            var normal = (GetDistanceBetweenObjects(a, b));

            if (normal.Length() > 1)
                return Vector2.Normalize(normal);

            return normal;
        }
    }
}