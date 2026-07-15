using physics.Engine.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormVisualizer : Form, IScreen
    {
        private Scene _scene = new Scene();
        private Graphics gfxBuffer;
        private Bitmap bmpBuffer;
        private readonly FastLoop _fastLoop;

        public FormVisualizer()
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
            _scene.UpdateDebugStats();
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
}