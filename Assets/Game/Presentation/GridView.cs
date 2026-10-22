using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Plateau de jeu : chaque case est un « trou » arrondi plus foncé que le fond,
    /// avec un fantôme de la couleur cible. Une case peinte s'illumine et « pop ».
    /// </summary>
    public sealed class GridView
    {
        private const float Gap = 12f;
        private const float GhostTint = 0.42f;

        private struct CellVisual
        {
            public RectTransform Root;
            public Image Hole;
            public Image Ghost;
            public Image Fill;
            public Image Gloss;
        }

        private readonly RectTransform _root;
        private CellVisual[] _cells;
        private bool[] _painted;
        private GridModel _grid;
        private int _builtWidth;
        private int _builtHeight;
        private float _cellSize;

        public GridView(RectTransform root)
        {
            _root = root;
        }

        public void Build(GridModel grid)
        {
            _grid = grid;

            if (_cells != null && _builtWidth == grid.Width && _builtHeight == grid.Height)
            {
                RefreshAll(true);
                return;
            }

            Clear();

            var area = _root.rect.size;
            if (area.x <= 1f || area.y <= 1f)
            {
                area = new Vector2(980f, 980f);
            }

            _cellSize = Mathf.Min(
                (area.x - Gap * (grid.Width - 1)) / grid.Width,
                (area.y - Gap * (grid.Height - 1)) / grid.Height);

            var totalWidth = _cellSize * grid.Width + Gap * (grid.Width - 1);
            var totalHeight = _cellSize * grid.Height + Gap * (grid.Height - 1);

            _cells = new CellVisual[grid.Width * grid.Height];
            _painted = new bool[grid.Width * grid.Height];
            _builtWidth = grid.Width;
            _builtHeight = grid.Height;

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    _cells[y * grid.Width + x] = CreateCell(
                        x,
                        y,
                        -totalWidth * 0.5f + _cellSize * 0.5f + x * (_cellSize + Gap),
                        totalHeight * 0.5f - _cellSize * 0.5f - y * (_cellSize + Gap));
                }
            }

            RefreshAll(true);
        }

        private CellVisual CreateCell(int x, int y, float posX, float posY)
        {
            var cellRoot = UiFactory.CreateRect("Cell_" + x + "_" + y, _root);
            cellRoot.anchorMin = new Vector2(0.5f, 0.5f);
            cellRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cellRoot.pivot = new Vector2(0.5f, 0.5f);
            cellRoot.sizeDelta = new Vector2(_cellSize, _cellSize);
            cellRoot.anchoredPosition = new Vector2(posX, posY);

            var rim = UiFactory.CreateRounded("Rim", cellRoot, JuicePalette.SlotRim, _cellSize * 0.34f);
            UiFactory.Stretch(rim.rectTransform, 0f, 0f, 0f, 0f);

            var hole = UiFactory.CreateRounded("Hole", cellRoot, JuicePalette.Slot, _cellSize * 0.34f);
            UiFactory.Stretch(hole.rectTransform, 5f, 5f, 5f, 5f);

            var ghost = UiFactory.CreateRounded("Ghost", cellRoot, Color.clear, _cellSize * 0.34f);
            var ghostRect = ghost.rectTransform;
            ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
            ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
            ghostRect.pivot = new Vector2(0.5f, 0.5f);
            ghostRect.sizeDelta = new Vector2(_cellSize * 0.88f, _cellSize * 0.88f);
            ghostRect.anchoredPosition = Vector2.zero;

            var fill = UiFactory.CreateRounded("Fill", cellRoot, Color.clear, _cellSize * 0.34f);
            UiFactory.Stretch(fill.rectTransform, 1f, 1f, 1f, 1f);

            var gloss = UiFactory.CreateRounded("Gloss", fill.transform, JuicePalette.Gloss, _cellSize * 0.16f);
            var glossRect = gloss.rectTransform;
            glossRect.anchorMin = new Vector2(0.5f, 0.5f);
            glossRect.anchorMax = new Vector2(0.5f, 0.5f);
            glossRect.pivot = new Vector2(0.5f, 0.5f);
            glossRect.sizeDelta = new Vector2(_cellSize * 0.34f, _cellSize * 0.15f);
            glossRect.anchoredPosition = new Vector2(-_cellSize * 0.10f, _cellSize * 0.24f);
            gloss.color = JuicePalette.WithAlpha(JuicePalette.Gloss, 0.30f);

            return new CellVisual
            {
                Root = cellRoot,
                Hole = hole,
                Ghost = ghost,
                Fill = fill,
                Gloss = gloss
            };
        }

        public void RefreshCell(int x, int y)
        {
            RefreshCell(x, y, true);
        }

        private void RefreshCell(int x, int y, bool animate)
        {
            if (_grid == null || x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height)
            {
                return;
            }

            var index = y * _grid.Width + x;
            if (index >= _cells.Length)
            {
                return;
            }

            var cell = _cells[index];
            if (cell.Fill == null)
            {
                return;
            }

            var painted = _grid.HasTargetAt(x, y) && _grid.IsPainted(x, y);
            var color = _grid.HasTargetAt(x, y) ? UiFactory.ToColor(_grid.TargetColorAt(x, y)) : Color.clear;

            // Teinte pastel opaque : la cible reste lisible et joyeuse (pas de gris).
            var ghostColor = Color.Lerp(Color.white, color, GhostTint);
            cell.Ghost.color = ghostColor;
            cell.Ghost.enabled = _grid.HasTargetAt(x, y) && !painted;

            if (painted && !_painted[index])
            {
                _painted[index] = true;
                cell.Fill.color = color;
                cell.Fill.enabled = true;
                cell.Gloss.enabled = true;

                if (animate)
                {
                    TweenRunner.Run(Tween.PopIn(cell.Fill.transform, 0.24f));
                }
                else
                {
                    cell.Fill.transform.localScale = Vector3.one;
                }
            }
            else if (!painted)
            {
                _painted[index] = false;
                cell.Fill.enabled = false;
                cell.Gloss.enabled = false;
            }
        }

        public void RefreshAll(bool instant = false)
        {
            if (_grid == null || _cells == null)
            {
                return;
            }

            for (var y = 0; y < _grid.Height; y++)
            {
                for (var x = 0; x < _grid.Width; x++)
                {
                    RefreshCell(x, y, !instant);
                }
            }
        }

        /// <summary>Centre d'une case en coordonnées locales de la zone de grille.</summary>
        public Vector2 GetCellLocalCenter(int x, int y)
        {
            if (_cells == null || x < 0 || y < 0 || x >= _builtWidth || y >= _builtHeight)
            {
                return Vector2.zero;
            }

            var cell = _cells[y * _builtWidth + x];
            return cell.Root != null ? (Vector2)_root.InverseTransformPoint(cell.Root.position) : Vector2.zero;
        }

        /// <summary>Position monde (espace Canvas) du centre d'une case, pour les particules.</summary>
        public bool TryGetCellWorldPosition(int x, int y, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (_cells == null || x < 0 || y < 0 || x >= _builtWidth || y >= _builtHeight)
            {
                return false;
            }

            var cell = _cells[y * _builtWidth + x];
            if (cell.Root == null)
            {
                return false;
            }

            worldPosition = cell.Root.position;
            return true;
        }

        private void Clear()
        {
            if (_cells == null)
            {
                return;
            }

            for (var i = 0; i < _cells.Length; i++)
            {
                if (_cells[i].Root != null)
                {
                    Object.Destroy(_cells[i].Root.gameObject);
                }
            }

            _cells = null;
        }
    }
}
