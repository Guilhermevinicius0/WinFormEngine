using physics.Engine.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form, IScreen
    {
        private Scene _scene = new Scene();
        private Graphics gfxBuffer;
        private Bitmap bmpBuffer;
        private readonly FastLoop _fastLoop;

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            GenerateGfxBuffer();

            this.BackColor = Color.SkyBlue;
            this.KeyPreview = true;

            _scene.Screen = this;
            _scene.MainCamera = new Camera(new Vector2(this.ClientRectangle.X, this.ClientRectangle.Y),
                                            new Vector2(this.ClientRectangle.Width, this.ClientRectangle.Height));

            this.KeyDown += (s, e) => _scene.Input.UpdateKeyState(e.KeyCode, true);
            this.KeyUp += (s, e) => _scene.Input.UpdateKeyState(e.KeyCode, false);

            this.MouseEnter += (s, e) => _scene.Input.IsMouseOnScreen = true;
            this.MouseLeave += (s, e) => _scene.Input.IsMouseOnScreen = false;

            this.MouseMove += (s, e) => _scene.Input.UpdateMouse(e.X, e.Y);
            this.MouseDown += (s, e) => _scene.Input.UpdateMouseButtonState(e.Button, true);
            this.MouseUp += (s, e) => _scene.Input.UpdateMouseButtonState(e.Button, false);
            this.MouseWheel += (s, e) => _scene.Input.MouseDelta += e.Delta;

            if (_scene.HasTileMap)
                _scene.LoadTileMap();

            _fastLoop = new FastLoop(WorldUpdate);
        }

        public void WorldUpdate(double deltaTime)
        {
            CheckWorldInputs();
            _scene.ProcessPhysics((float)deltaTime);
            Draw();
            UpdateDebugStats();
        }

        public void Draw()
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _scene.DrawScene(gfxBuffer);
            e.Graphics.DrawImage(bmpBuffer, new Point(0, 0));
            _scene.DrawDebugOverLay(e.Graphics);
        }

        private void CheckWorldInputs()
        {
            if (_scene.Input.IsKeyJustPressed(Keys.F1))
                DebugStats.ShowDebug = !DebugStats.ShowDebug;

            // test to stress the engine
            if (_scene.Input.IsKeyJustPressed(Keys.F))
            {
                for (int y = 0; y < 500; y++)
                {
                    for (int x = 0; x < 500; x++)
                    {
                        var wall = new Wall(
                                        _scene,
                                        Color.Gray,
                                        new Vector2(34, 34),
                                        new Vector2(34, 34),
                                        new Vector2(x * 34, y * 34));

                        wall.ZIndex = 1;
                        _scene.AddObject(wall);
                    }
                }
            }
        }

        private void UpdateDebugStats()
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

        private void GenerateGfxBuffer()
        {
            bmpBuffer = new Bitmap(Size.Width, Size.Height);
            gfxBuffer = Graphics.FromImage(bmpBuffer);

            gfxBuffer.CompositingMode = CompositingMode.SourceCopy;
            gfxBuffer.CompositingQuality = CompositingQuality.HighSpeed;
            gfxBuffer.InterpolationMode = InterpolationMode.NearestNeighbor;
            gfxBuffer.PixelOffsetMode = PixelOffsetMode.Half;
            gfxBuffer.SmoothingMode = SmoothingMode.HighSpeed;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GenerateGfxBuffer();

            if (_scene?.MainCamera != null)
                _scene.MainCamera.Size = new Vector2(this.ClientRectangle.Width, this.ClientRectangle.Height);
        }

        public Vector2 GetSize()
        {
            return new Vector2(this.ClientRectangle.Width, this.ClientRectangle.Height);
        }
    }

    public interface IScreen
    {
        Vector2 GetSize();
        void WorldUpdate(double deltaTime);
        void Draw();
        Color BackColor { get; set; }
    }

    public class Player : Entity
    {
        public Keys Up = Keys.W;
        public Keys Left = Keys.A;
        public Keys Down = Keys.S;
        public Keys Right = Keys.D;

        private float _speed = 200f;

        public Player(Scene scene, Color color, Vector2 size, Vector2 collisionSize, Vector2 position, bool isStatic = false)
          : base(scene, color, size, collisionSize, position, isStatic)
        {
        }

        public override void OnCreation()
        {
        }

        public override void Update(float delta)
        {
            Vector2 moviment = Input.GetVector(Left, Right, Up, Down);
            moviment *= _speed * delta;

            MoveAndCollide(moviment);
        }

        public override void OnDestroy()
        {
        }
    }

    public class Wall : Entity
    {
        public Wall(Scene scene, Color color, Vector2 size, Vector2 collisionSize, Vector2 position, bool isStatic = true) : base(scene, color, size, collisionSize, position, isStatic)
        {
        }

        public override void OnCreation()
        {
        }

        public override void Update(float delta)
        {
        }

        public override void OnDestroy()
        {
        }
    }

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

    public static class DebugStats
    {
        public static bool ShowDebug = false;

        public static int CollisionsCheck = 0;
        public static int DrawCells = 0;
        public static int VisibileCells = 0;
        public static int InvisibileCells = 0;
        public static int Objects = 0;
        public static int FPS = 0;

        public static int Show_CollisionsCheck = 0;
        public static int Show_DrawCells = 0;
        public static int Show_VisibileCells = 0;
        public static int Show_InvisibileCells = 0;
        public static int Show_Objects = 0;
        public static int Show_FPS = 0;

        public static long Memory = 0;

        public static DateTime? InitFpsMetter;

        public static Brush Brush = Brushes.Black;
        public static Font Font = new Font(FontFamily.GenericSansSerif, 7f, FontStyle.Regular);
        public static int TextMargin = 3;
    }

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

    public class Camera : TransformComponent
    {
        public GameObject Target;
        public bool AllowFollow = false;
        public float Zoom = 1f;
        public float Smothness = 5f;
        public float Offset_X = 0f;
        public float Offset_Y = -100f;

        public Camera(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }

        public Vector2 WorldToScreen(Vector2 worldPos)
        {
            return (worldPos - Position) * Zoom;
        }

        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return screenPos / Zoom + Position;
        }

        public void Update(float delta)
        {
            if (AllowFollow && Target != null)
            {
                Vector2 targetPosition = Target.GetCenteredPosition();

                targetPosition.X += Offset_X;
                targetPosition.Y += Offset_Y;

                Position = Vector2.Lerp(Position, targetPosition - (Size / 2f) / Zoom, Smothness * delta);
            }
        }
    }

    public class RectangleRender : GraphicsComponent
    {
        public RectangleRender(Color color) : base(color) { }

        public override void Draw(Graphics g, Camera camera, GameObject owner)
        {
            if (owner == null || !IsVisible)
                return;

            DebugStats.VisibileCells++;

            bool visible = (owner.Position.X + owner.Size.X >= camera.Position.X &&
                          owner.Position.X < camera.Position.X + camera.Size.X) &&
                         (owner.Position.Y + owner.Size.Y >= camera.Position.Y &&
                          owner.Position.Y < camera.Position.Y + camera.Size.Y);

            if (visible)
            {
                var pos = camera.WorldToScreen(owner.Position);
                var size = owner.Size * camera.Zoom;
                g.FillRectangle(Brush, pos.X, pos.Y, size.X, size.Y);
                DebugStats.DrawCells++;
            }
            else
            {
                DebugStats.InvisibileCells++;
            }
        }
    }

    public abstract class GraphicsComponent
    {
        public bool IsVisible = true;
        public Brush Brush;

        public GraphicsComponent(Color color)
        {
            Brush = new SolidBrush(color);
        }

        public abstract void Draw(Graphics g, Camera camera, GameObject Owner);
    }

    public class RenderComponent
    {
        public void Draw(Graphics g, Camera camera, List<GameObject> objects)
        {
            GameObject current = null;
            for (int i = 0; i < objects.Count; i++)
            {
                current = objects[i];

                if (current.GraphicsComponent != null)
                    current.GraphicsComponent.Draw(g, camera, current);
            }
        }
    }

    public class SpatialGrid
    {
        public float CellSize;
        private int _searchRadius;
        private Dictionary<Vector2, List<GameObject>> _grid;
        private List<GameObject> _nearbyCache = new List<GameObject>();

        public SpatialGrid(float cellSize = 64, int searchReadius = 2)
        {
            CellSize = cellSize;
            _searchRadius = searchReadius;
            _grid = new Dictionary<Vector2, List<GameObject>>();
        }

        public Vector2 GetCellKey(Vector2 postion)
        {
            return new Vector2((int)Math.Floor(postion.X / CellSize),
                               (int)Math.Floor(postion.Y / CellSize));
        }

        public void AddObject(GameObject obj)
        {
            var key = GetCellKey(obj.Position);

            if (!_grid.ContainsKey(key))
                _grid.Add(key, new List<GameObject>());

            _grid[key].Add(obj);
        }

        public void RemoveObject(GameObject obj)
        {
            var key = GetCellKey(obj.Position);

            if (!_grid.ContainsKey(key))
                return;

            var list = _grid[key];

            if (list.Contains(obj))
            {
                list.Remove(obj);

                if (list.Count == 0)
                    _grid.Remove(key);
            }
        }

        public void UpdateObjectCell(GameObject obj, Vector2 previewsPosition)
        {
            if (obj.Position == previewsPosition)
                return;

            var oldKey = GetCellKey(previewsPosition);
            var newKey = GetCellKey(obj.Position);

            if (oldKey == newKey)
                return;

            if (!_grid.ContainsKey(newKey))
                _grid.Add(newKey, new List<GameObject>());

            _grid[newKey].Add(obj);

            if (_grid.TryGetValue(oldKey, out var oldList))
            {
                if (oldList.Contains(obj))
                    oldList.Remove(obj);

                if (oldList.Count == 0)
                    _grid.Remove(oldKey);
            }
        }

        public List<GameObject> GetNearbyObjects(GameObject target, int? customRadius = null)
            => CalculateNearbyObjects(target.Position, customRadius);

        public List<GameObject> GetNearbyObjects(Vector2 position, int? customRadius = null)
            => CalculateNearbyObjects(position, customRadius);

        private List<GameObject> CalculateNearbyObjects(Vector2 position, int? customRadius = null)
        {
            _nearbyCache.Clear();
            var targetKey = GetCellKey(position);
            int effectiveRadius = customRadius != null ? customRadius.Value : _searchRadius;

            for (int i = -effectiveRadius; i <= effectiveRadius; i++)
            {
                for (int j = -effectiveRadius; j <= effectiveRadius; j++)
                {
                    var key = new Vector2(targetKey.X + i, targetKey.Y + j);
                    if (_grid.TryGetValue(key, out var itens))
                        _nearbyCache.AddRange(itens);
                }
            }
            return _nearbyCache;
        }
    }

    public class TileMap
    {
        public Vector2 TileSize = new Vector2(34, 34);
        public Vector2 PlayerSize = new Vector2(32, 32);

        public enum TileTypes
        {
            None = 0,
            Floor = 1,
            Bricks = 2,
            QuestionBlocks = 3,
            GreenPipes = 4,
            Player = 9
        }

        internal class objectsFactory
        {
            public static Dictionary<TileTypes, Color> TileColors = new Dictionary<TileTypes, Color>()
            {
                {TileTypes.Floor, Color.Chocolate},
                {TileTypes.Bricks, Color.Sienna},
                {TileTypes.QuestionBlocks, Color.Gold},
                {TileTypes.GreenPipes, Color.Green},
                {TileTypes.Player, Color.LimeGreen}
            };

            public static GameObject BuildObject(TileTypes type, Scene scene, Vector2 size, Vector2 hitboxSize, Vector2 position)
            {
                if (type == TileTypes.None)
                    return null;

                if (type != TileTypes.Player)
                    return new Wall(scene, TileColors[type], size, hitboxSize, position, true);

                else
                    return new Player(scene, TileColors[type], size, hitboxSize, position, false);
            }
        }

        public int[,] Tiles =
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,2,2,2,0,0,0,0,0,2,2,2,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,0,0,0,0,2,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,0,0,0,2,3,2,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,0,0,0,0,2,2,0,0,0,0,0,0,3,0,0,3,0,0,3,0,0,0,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,2,2,2,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,1,0,0,0,0,0,0,0,0,0,1,1,1,0,0,1,1,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,0,1,1,1,0,0,0,0,0,0,0,1,1,1,1,0,0,1,1,1,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,2,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,9,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,0,0,1,1,1,1,0,0,0,0,0,1,1,1,1,1,0,0,1,1,1,1,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,2,2,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,2,0,0,2,2,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
        };
    }

    public static class TileMapLoader
    { 
        public static void Load(Scene scene, TileMap tileMap)
        {
            int rowCount = tileMap.Tiles.GetLength(0);
            int colunmCount = tileMap.Tiles.GetLength(1);

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colunmCount; j++)
                {
                    int currentTile = tileMap.Tiles[i, j];

                    float x = (j + 1) * tileMap.TileSize.X;
                    float y = scene.Screen.GetSize().Y - ((rowCount - i - 1) * tileMap.TileSize.Y);

                    var obj = TileMap.objectsFactory.BuildObject(
                                (TileMap.TileTypes)currentTile,
                                scene, 
                                new Vector2(tileMap.TileSize.X, tileMap.TileSize.Y),
                                new Vector2(tileMap.TileSize.X, tileMap.TileSize.Y),
                                new Vector2(x, y));

                    if (obj == null)
                        continue;

                    if (obj is Player player) 
                    {
                        player.Size = tileMap.PlayerSize;
                        player.BoxCollider.Size = tileMap.PlayerSize;
                        player.AddTag("player");
                        scene.MainCamera.AllowFollow = true;
                        scene.MainCamera.Target = player;
                    }

                    scene.AddObject(obj);
                }
            }
        }
    }
}