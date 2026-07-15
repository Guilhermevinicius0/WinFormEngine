using System.Numerics;

namespace WindowsFormsApp1
{
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