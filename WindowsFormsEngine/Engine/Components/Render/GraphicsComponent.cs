using System.Drawing;

namespace WindowsFormsApp1
{
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
}