using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Affiche la grille cible avec des Image placeholders :
    /// pixel non peint = couleur cible très atténuée, peint = couleur pleine.
    /// </summary>
    public sealed class GridView
    {
        private const float Gap = 10f;
        private const float GhostAlpha = 0.20f;
        private const float EmptyAlpha = 0.035f;

        private readonly RectTransform _root;
        private Image[] _cells;
        private GridModel _grid;
        private int _builtWidth;
        private int _builtHeight;

        public GridView(RectTransform root)
        {
            _root = root;
        }

        public void Build(GridModel grid)
        {
            _grid = grid;

            if (_cells != null && _builtWidth == grid.Width && _builtHeight == grid.Height)
            {
                RefreshAll();
                return;
            }

            Clear();

            var area = _root.rect.size;
            if (area.x <= 1f || area.y <= 1f)
            {
                area = new Vector2(1000f, 1000f);
            }

            var cell = Mathf.Min(
                (area.x - Gap * (grid.Width - 1)) / grid.Width,
                (area.y - Gap * (grid.Height - 1)) / grid.Height);

            var totalWidth = cell * grid.Width + Gap * (grid.Width - 1);
            var totalHeight = cell * grid.Height + Gap * (grid.Height - 1);

            _cells = new Image[grid.Width * grid.Height];
            _builtWidth = grid.Width;
            _builtHeight = grid.Height;

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var image = UiFactory.CreateImage("Cell_" + x + "_" + y, _root, Color.clear);
                    var rect = image.rectTransform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(cell, cell);
                    rect.anchoredPosition = new Vector2(
                        -totalWidth * 0.5f + cell * 0.5f + x * (cell + Gap),
                        totalHeight * 0.5f - cell * 0.5f - y * (cell + Gap));

                    image.color = VisualColor(x, y);
                    _cells[y * grid.Width + x] = image;
                }
            }
        }

        public void RefreshCell(int x, int y)
        {
            if (_grid == null || x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height)
            {
                return;
            }

            var index = y * _grid.Width + x;
            if (index < _cells.Length && _cells[index] != null)
            {
                _cells[index].color = VisualColor(x, y);
            }
        }

        public void RefreshAll()
        {
            if (_grid == null || _cells == null)
            {
                return;
            }

            for (var y = 0; y < _grid.Height; y++)
            {
                for (var x = 0; x < _grid.Width; x++)
                {
                    RefreshCell(x, y);
                }
            }
        }

        private Color VisualColor(int x, int y)
        {
            if (!_grid.HasTargetAt(x, y))
            {
                return new Color(1f, 1f, 1f, EmptyAlpha);
            }

            var color = UiFactory.ToColor(_grid.TargetColorAt(x, y));
            color.a = _grid.IsPainted(x, y) ? 1f : GhostAlpha;
            return color;
        }

        private void Clear()
        {
            if (_cells == null)
            {
                return;
            }

            for (var i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != null)
                {
                    Object.Destroy(_cells[i].gameObject);
                }
            }

            _cells = null;
        }
    }
}
