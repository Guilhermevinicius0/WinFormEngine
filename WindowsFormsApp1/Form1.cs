using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private Scene _scene = new Scene();
        private DateTime? lastTime;
        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.KeyPreview = true;
            this.BackColor = Color.SkyBlue;

            _scene.Screen = this;
            _scene.MainCamera = new Camera(new Vector2(this.ClientRectangle.X, this.ClientRectangle.Y),
                                            new Vector2(this.ClientRectangle.Width, this.ClientRectangle.Height));

            this.KeyDown += (s, e) => _scene.Input.PressedKeys.Add(e.KeyCode);
            this.KeyUp += (s, e) => _scene.Input.PressedKeys.Remove(e.KeyCode);

            Timer timer = new Timer();
            timer.Interval = 1000 / 60;
            timer.Tick += UpdateGeral;
            timer.Start();

            var tileMap = new TileMap();

            int rowCount = tileMap.Tiles.GetLength(0);
            int colunmCount = tileMap.Tiles.GetLength(1);

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colunmCount; j++)
                {
                    if (tileMap.Tiles[i, j] >= 1 && tileMap.Tiles[i, j] < 9)
                    {
                        float x = (j + 1) * tileMap.TileSize.X;
                        float y = ClientRectangle.Height - ((rowCount - i - 1) * tileMap.TileSize.Y);

                        var color = Color.Gray;

                        switch (tileMap.Tiles[i, j]) 
                        {
                            case 1:
                                color = Color.Chocolate;
                                break;
                            case 2:
                                color = Color.Sienna;
                                break;
                            case 3:
                                color = Color.Gold;
                                break;
                            case 4:
                                color = Color.Green;
                                break;
                        }

                        var wall = new Wall(_scene, color,
                        new Vector2(tileMap.TileSize.X, tileMap.TileSize.Y),
                        new Vector2(tileMap.TileSize.X, tileMap.TileSize.Y),
                        new Vector2(x, y));

                        _scene.AddObject(wall);
                    }
                    else if (tileMap.Tiles[i, j] == 9)
                    {
                        float x = (j + 1) * 32;
                        float y = ClientRectangle.Height - ((rowCount - i - 1) * 32 * 2);

                        var player = new Player(_scene, Color.Green,
                            new Vector2(32, 32),
                            new Vector2(32, 32),
                            new Vector2(x, y));

                        _scene.AddObject(player);
                        _scene.MainCamera.AllowFollow = true;
                        _scene.MainCamera.Target = player;
                    }
                }
            }
        }

        private void UpdateGeral(object sender, EventArgs e)
        {
            DebugStats.FPS++;
            float delta = 1;

            DateTime now = DateTime.Now;

            if (lastTime.HasValue)
                delta = (float)(now - lastTime.Value).TotalSeconds;

            lastTime = now;

            if (_scene.Input.GetKeyState(Keys.F1))
                DebugStats.ShowDebug = !DebugStats.ShowDebug;

            if (_scene.Input.GetKeyState(Keys.F))
            {
                for (int y = 0; y < 500; y++)
                {
                    for (int x = 0; x < 500; x++)
                    {
                        _scene.AddObject(
                            new Wall(
                                _scene,
                                Color.Gray,
                                new Vector2(34, 34),
                                new Vector2(34, 34),
                                new Vector2(x * 34, y * 34)
                            ));
                    }
                }
            }

            _scene.ProcessQueues();

            var objects = _scene.GetObjects();
            GameObject current = null;

            for (int i = 0; i < objects.Count; i++)
            {
                current = objects[i];
                current.Update(delta);
            }

            _scene.MainCamera.Update(delta);

            this.Invalidate();

            if (!DebugStats.InitFpsMetter.HasValue)
                DebugStats.InitFpsMetter = DateTime.Now;

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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _scene.Draw(e.Graphics);
        }
    }

    public class Player : Entity
    {
        public Keys Up = Keys.W;
        public Keys Left = Keys.A;
        public Keys Down = Keys.S;
        public Keys Right = Keys.D;

        private float _speed = 200f;

        public Player(Scene scene, Color color, Vector2 size, Vector2 collisionSize, Vector2 position)
          : base(scene, color, size, collisionSize, position)
        {
        }

        public override void Update(float delta)
        {
            Vector2 moviment = Input.GetVector(Left, Right, Up, Down);
            moviment *= _speed * delta;

            MoveAndCollide(moviment);
        }
    }

    public class Wall : Entity
    {
        public Wall(Scene scene, Color color, Vector2 size, Vector2 collisionSize, Vector2 position) : base(scene, color, size, collisionSize, position)
        {
        }

        public override void Update(float delta)
        {
        }
    }

    public abstract class Entity : GameObject
    {

        public Entity(Scene scene, Color color, Vector2 size, Vector2 collisionSize, Vector2 position) : base(scene, position, size, collisionSize)
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

        public InputComponent Input { get => _scene.Input; }
        public CollisionComponent BoxCollider;
        public GraphicsComponent GraphicsComponent;

        public int ZIndex = 0;

        public GameObject(Scene scene, Vector2 position, Vector2 size, Vector2 collisionSize)
        {
            this.BoxCollider = new CollisionComponent(this, collisionSize);
            _scene = scene;
            Position = position;
            Size = size;
        }

        public abstract void Update(float delta);

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

        public RayCastResult RayCast(Vector2 origin, Vector2 direction, int size,
          List<GameObject> excludeList = null, List<GameObject> FilterList = null,
          Type type = null, bool selfCollide = false)
        {
            return _scene.RayCast(this, origin, direction, size, excludeList, FilterList, type, selfCollide);
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
        public Form Screen;
        public Camera MainCamera;
        public RenderComponent Render = new RenderComponent();
        public InputComponent Input = new InputComponent();

        private readonly List<GameObject> Objects = new List<GameObject>();

        private Queue<GameObject> ToAdd = new Queue<GameObject>();
        private Queue<GameObject> ToRemove = new Queue<GameObject>();

        private int MaxQueueIterations = 1000;

        public void ProcessQueues()
        {
            if (ToAdd.Count == 0 && ToRemove.Count == 0)
                return;

            for (int i = 0; i < MaxQueueIterations; i++)
            {
                if (ToAdd.Count > 0)
                {
                    var obj = ToAdd.Dequeue();
                    int index = Objects.BinarySearch(obj, Comparer<GameObject>.Create((a, b) => a.ZIndex.CompareTo(b.ZIndex)));
                    
                    if (index < 0) 
                        index = ~index;
                    
                    Objects.Insert(index, obj);
                    DebugStats.Objects++;
                }

                else if (ToRemove.Count > 0)
                {
                    var obj = ToRemove.Dequeue();

                    if (Objects.Contains(obj))
                    {
                        Objects.Remove(obj);
                        DebugStats.Objects--;
                    }
                }

                else
                    break;
            }
        }

        public void AddObject(GameObject obj)
        {
            ToAdd.Enqueue(obj);
        }

        public void RemoveObject(GameObject obj)
        {
            ToRemove.Enqueue(obj);
        }

        public void DestroyObject(GameObject obj)
        {
            ToRemove.Enqueue(obj);
        }

        public List<GameObject> GetObjects()
        {
            return Objects;
        }

        public void Draw(Graphics g)
        {
            Render.Draw(g, MainCamera, GetObjects());

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

            if (velocity.X != 0)
            {
                obj.Position.X += velocity.X;
                if (CheckWorldColision(obj))
                {
                    do
                        obj.Position.X -= 1 * Math.Sign(velocity.X);
                    while (CheckWorldColision(obj));
                }
            }
            else
            {
                if (CheckWorldColision(obj))
                {
                    do
                        obj.Position.X -= 1;
                    while (CheckWorldColision(obj));
                }
            }

            if (velocity.Y != 0)
            {
                obj.Position.Y += velocity.Y;
                if (CheckWorldColision(obj))
                {
                    do
                        obj.Position.Y -= 1 * Math.Sign(velocity.Y);
                    while (CheckWorldColision(obj));
                }
            }
            else
            {
                if (CheckWorldColision(obj))
                {
                    do
                        obj.Position.Y -= 1;
                    while (CheckWorldColision(obj));
                }
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
            var ground = GetObjects().FirstOrDefault(o => o is Wall &&
              target.BoxCollider.IsColliding(o.BoxCollider) &&
              target.Position.Y + target.Size.Y <= o.Position.Y + 1);

            if (ground != null)
                return Vector2.Normalize((ground.GetCenteredPosition() - target.GetCenteredPosition()));

            return Vector2.Zero;
        }

        public bool CheckWorldColision(GameObject target)
        {
            var objects = GetObjects();
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
    }

    public class InputComponent
    {
        public HashSet<Keys> PressedKeys = new HashSet<Keys>();
        public bool GetKeyState(Keys key)
         => PressedKeys.Contains(key);

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
        public float Zoom = 1f;
        public bool AllowFollow = false;
        public GameObject Target;

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
                Vector2 objPosition = Target.GetCenteredPosition();
                Position = objPosition - (Size / 2f) / Zoom;
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

            bool visible = (owner.Position.X + owner.Size.X >= camera.Position.X &&
                          owner.Position.X < camera.Position.X + camera.Size.X) &&
                         (owner.Position.Y + owner.Size.Y >= camera.Position.Y &&
                          owner.Position.Y < camera.Position.Y + camera.Size.Y);

            if (visible)
            {
                var pos = camera.WorldToScreen(owner.Position);
                var size = owner.Size * camera.Zoom;
                DebugStats.VisibileCells++;
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

    public class TileMap
    {
        public Vector2 TileSize = new Vector2(34, 34);
        public int[,] Tiles =
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,2,2,2,0,0,0,0,0,2,2,2,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,0,0,0,0,2,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,3,0,0,0,2,3,2,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,0,0,0,0,2,2,0,0,0,0,0,0,3,0,0,3,0,0,3,0,0,0,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,2,2,2,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1,1,0,0,0,0,0,0,0,0,0,1,1,1,0,0,1,1,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,0,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,0,1,1,1,0,0,0,0,0,0,0,1,1,1,1,0,0,1,1,1,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,2,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,9,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,0,0,1,1,1,1,0,0,0,0,0,1,1,1,1,1,0,0,1,1,1,1,0,0,0,0,0,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,4,4,2,2,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,2,0,0,2,2,0,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
        };
    }
}