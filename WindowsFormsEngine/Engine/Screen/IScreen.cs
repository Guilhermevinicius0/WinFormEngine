using System.Drawing;
using System.Numerics;

namespace WindowsFormsApp1
{
    public interface IScreen
    {
        Vector2 GetSize();
        void WorldUpdate(double deltaTime);
        void Draw();
        Color BackColor { get; set; }
    }
}