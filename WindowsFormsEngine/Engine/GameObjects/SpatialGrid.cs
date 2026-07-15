using System;
using System.Collections.Generic;
using System.Numerics;

namespace WindowsFormsApp1
{
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
}