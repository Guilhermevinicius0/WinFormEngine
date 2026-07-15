using System.Collections.Generic;
using System.Drawing;

namespace WindowsFormsApp1
{
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
}