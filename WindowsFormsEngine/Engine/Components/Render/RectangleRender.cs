using System.Drawing;

namespace WindowsFormsApp1
{
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
}