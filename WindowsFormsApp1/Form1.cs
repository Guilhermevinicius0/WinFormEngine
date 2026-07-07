using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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

            _scene.Screen = this;

            this.KeyDown += (s, e) => _scene.Input.PressedKeys.Add(e.KeyCode);
            this.KeyUp += (s, e) => _scene.Input.PressedKeys.Remove(e.KeyCode);

            Timer timer = new Timer();
            timer.Interval = 1000 / 30;
            timer.Tick += UpdateGeral;
            timer.Start();

            _scene.AddObject(new Player(_scene, Color.Green, new Vector2(32,32), new Vector2(32,32), new Vector2(this.ClientRectangle.Width / 2 - 16, this.ClientRectangle.Height / 2 + 16)));
            _scene.AddObject(new Wall(_scene, Color.Gray, new Vector2(128,32), new Vector2(128,32), new Vector2(this.ClientRectangle.Width / 2 - 64, this.ClientRectangle.Height / 2 + 48)));
        }

        private void UpdateGeral(object sender, EventArgs e)
        {
            float delta = 1;

            DateTime now = DateTime.Now;

            if (lastTime.HasValue)
                delta = (float)(now - lastTime.Value).TotalMilliseconds;

            lastTime = now;

            foreach (var obj in _scene.GetObjects())
                obj.Update(delta);

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            foreach (var obj in _scene.GetObjects())
                obj.Draw(e.Graphics);
        }
    }

    public class Player : Entity 
    {
        public Keys Up = Keys.W;
        public Keys Left = Keys.A;
        public Keys Down = Keys.S;
        public Keys Right = Keys.D;

        private float _speed = 6f;

        public Player(Scene scene, Color cor, Vector2 size, Vector2 collisionSize, Vector2 position) 
            : base(scene, cor, size, collisionSize, position) 
        {
        }

        public override void Update(float delta)
        {
            Vector2 moviment = Input.GetVector(Left, Right, Up, Down);
            moviment *= _speed;

            MoveAndCollide(moviment);

            if (IsOnFloor())
                Console.WriteLine("On Ground");
        }
    }

    public class Wall : Entity 
    {
        public Wall(Scene scene, Color cor, Vector2 size, Vector2 collisionSize, Vector2 position) : base(scene, cor, size, collisionSize, position)
        {
        }

        public override void Update(float delta)
        {
        }
    }

    public abstract class Entity : GameObject
    {
        private Brush _brush;

        public Entity(Scene scene, Color cor, Vector2 size, Vector2 collisionSize, Vector2 position) : base(scene, position, size, collisionSize)
        {
            _brush = new SolidBrush(cor);
        }

       
        public override void Draw(Graphics g) 
        {
            g.FillRectangle(_brush, Position.X, Position.Y, Size.X, Size.Y);
        }
    }

    public class Collision 
    {
        private GameObject _owner;
        public Vector2 Size;
        public bool Active = true;

        public Vector2 Position => _owner.Position;

        public Collision(GameObject owner, Vector2 size) 
        {
            _owner = owner;
            Size = size;
        }

        public bool IsColliding(Collision target) 
        {
            if (this.Size == Vector2.Zero || target.Size == Vector2.Zero || !Active)
                return false;

            return this.Position.X < target.Position.X + target.Size.X &&
                this.Position.X + this.Size.X > target.Position.X &&
                this.Position.Y < target.Position.Y + target.Size.Y &&
                this.Position.Y + this.Size.Y > target.Position.Y;
        }
    } 

    public abstract class GameObject 
    {
        public Vector2 Size;
        public Vector2 Position;
        public Collision BoxCollider;
        public Scene _scene;
        public Input Input { get => _scene.Input; }


        public GameObject(Scene scene, Vector2 position, Vector2 size, Vector2 collisionSize)
        {
            this.BoxCollider = new Collision(this, collisionSize);
            _scene = scene;
            Position = position;
            Size = size;
        }

        public abstract void Update(float delta);
        public abstract void Draw(Graphics g);

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

        public Vector2 GetCenteredPosition() 
        {
            return new Vector2(Position.X + Size.X / 2, Position.Y + Size.Y / 2);
        }

        public RayCastResult RayCast(Vector2 origin, Vector2 direction, int size,
            List<GameObject> excludeList = null, List<GameObject> FilterList = null,
            Type type = null, bool selfCollide = false) 
        {
            return _scene.RayCast(this, origin, direction, size, excludeList, FilterList, type, selfCollide);
        }
    }

    public class Scene
    {
        public Form Screen;
        public Input Input = new Input(); 
        private readonly List<GameObject> objects = new List<GameObject>();

        public void AddObject(GameObject obj) 
        {
            objects.Add(obj);
        }

        public void RemoveObject(GameObject obj) 
        {
            if (objects.Contains(obj))
                objects.Remove(obj);
        }

        public void DestroyObject(GameObject obj)
        {
            if (objects.Contains(obj))
                RemoveObject(obj);
        }

        public List<GameObject> GetObjects() 
        {
            return objects.ToList();
        }

        public RayCastResult RayCast(GameObject parent, Vector2 origin, Vector2 direction, int size,
            List<GameObject> excludeList = null, List<GameObject> FilterList = null,
            Type type = null, bool selfCollide = false)
        {
            var ray = new RayCast(this, origin, Vector2.One);

            for(int i = 0; i < size; i++) 
            {
                var obj = GetCollider(ray);
                if (obj != null) 
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
            obj.Position.X += velocity.X;
            if (CheckWorldColision(obj))
            {
                do
                    obj.Position.X -= 1 * Math.Sign(velocity.X);
                while (CheckWorldColision(obj));
            }

            obj.Position.Y += velocity.Y;
            if (CheckWorldColision(obj))
            {
                do
                    obj.Position.Y -= 1 * Math.Sign(velocity.Y);
                while (CheckWorldColision(obj));
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
            var ground = objects.FirstOrDefault(o => o is Wall &&
                target.BoxCollider.IsColliding(o.BoxCollider) &&
                target.Position.Y + target.Size.Y <= o.Position.Y + 1);

            if (ground != null)
                return Vector2.Normalize((ground.GetCenteredPosition() - target.GetCenteredPosition()));

            return Vector2.Zero;
        }

        public bool CheckWorldColision(GameObject target)
        {
            foreach (var obj in objects)
            {
                if (obj == target) continue;

                if (target.BoxCollider.IsColliding(obj.BoxCollider))
                    return true;
            }

            return false;
        }

        public GameObject GetCollider(GameObject target) 
        {
            foreach (var obj in objects)
            {
                if (obj == target) continue;

                if (target.BoxCollider.IsColliding(obj.BoxCollider))
                    return obj;
            }

            return null;
        }
    }

    public class Input 
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

    public class RayCast : GameObject 
    {
        private Brush _rayColor = new SolidBrush(Color.Orange);
        private bool _visible;

        public RayCast(Scene scene, Vector2 position, Vector2 size, bool visible = false) : base (scene, position, size, size)
        {
            _visible = visible;
        }

        public override void Update(float delta)
        {
        }

        public override void Draw(Graphics g)
        {
            if (!_visible)
                return;

            g.FillRectangle(_rayColor, Position.X, Position.Y, Size.X, Size.Y);
        }
        
    }

    public struct RayCastResult 
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public GameObject HittedTarget;
        public int Size;
        public bool Found;

        public RayCastResult(Vector2  origin, Vector2 direction, int size, GameObject hittedTarget) 
        {
            Origin = origin;
            Direction = direction;
            Size = size;
            HittedTarget = hittedTarget;
            Found = hittedTarget != null;
        }
    }
}