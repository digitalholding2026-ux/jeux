using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// Cerveau du Sprint 4 : placement des blobs, détection des alignements,
    /// création des blobs spéciaux et détection des situations bloquées.
    /// Pur C# : aucune référence Unity, testable avec <c>dotnet test</c>.
    ///
    /// Règles :
    /// - un blob normal ne peut être posé que sur une case du motif, non peinte
    ///   et vide ;
    /// - 3 blobs alignés (horizontal ou vertical) peignent leurs cases et
    ///   disparaissent ;
    /// - 4 blobs alignés créent un blob Ligne, 5 (ou plus) un blob Bombe ;
    /// - un blob spécial se lance sur n'importe quelle case du motif non peinte.
    /// </summary>
    public sealed class Match3Resolver
    {
        public const int MinimumMatch = 3;
        public const int LineMatchLength = 4;
        public const int BombMatchLength = 5;

        /// <summary>Place un blob normal puis résout l'alignement éventuel.</summary>
        public DropResult ResolveNormalDrop(Creature creature, GridModel grid, int x, int y)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (creature == null)
            {
                return DropResult.Fail(DropFailureReason.NoCreature);
            }

            if (!grid.HasTargetAt(x, y))
            {
                return DropResult.Fail(DropFailureReason.NotInPattern);
            }

            if (grid.IsPainted(x, y))
            {
                return DropResult.Fail(DropFailureReason.CellAlreadyPainted);
            }

            if (grid.IsOccupied(x, y))
            {
                return DropResult.Fail(DropFailureReason.CellOccupied);
            }

            var color = creature.Color;
            if (!grid.TryPlace(x, y, color))
            {
                return DropResult.Fail(DropFailureReason.CellOccupied);
            }

            var result = DropResult.Ok(x, y, color);
            var horizontal = RunLength(grid, x, y, color, 1, 0) + RunLength(grid, x, y, color, -1, 0) + 1;
            var vertical = RunLength(grid, x, y, color, 0, 1) + RunLength(grid, x, y, color, 0, -1) + 1;
            var longest = Math.Max(horizontal, vertical);

            if (longest < MinimumMatch)
            {
                return result;
            }

            var cells = CollectRunCells(grid, x, y, color);
            CreatureKind special = CreatureKind.Normal;
            if (longest >= BombMatchLength)
            {
                special = CreatureKind.Bomb;
            }
            else if (longest >= LineMatchLength)
            {
                special = CreatureKind.Line;
            }

            result.WithMatch(x, y, color, longest, special);

            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                result.MatchedCells.Add(new PaintedCell(cell.X, cell.Y, color));

                if (!grid.TryPaintTarget(cell.X, cell.Y))
                {
                    continue;
                }

                result.PaintedCells.Add(new PaintedCell(cell.X, cell.Y, grid.TargetColorAt(cell.X, cell.Y)));
            }

            result.SetLevelCompleted(grid.IsComplete);
            return result;
        }

        /// <summary>Applique un blob spécial (bombe, ligne, arc-en-ciel) sur une case visée.</summary>
        public DropResult ResolveSpecialDrop(Creature creature, GridModel grid, int x, int y)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (creature == null || !creature.IsSpecial)
            {
                return DropResult.Fail(DropFailureReason.NoCreature);
            }

            if (!grid.HasTargetAt(x, y))
            {
                return DropResult.Fail(DropFailureReason.NotInPattern);
            }

            if (grid.IsPainted(x, y))
            {
                return DropResult.Fail(DropFailureReason.CellAlreadyPainted);
            }

            var result = DropResult.Ok(x, y, creature.Color);
            result.WithMatch(x, y, creature.Color, 0, CreatureKind.Normal);

            switch (creature.Kind)
            {
                case CreatureKind.Bomb:
                    PaintBomb(grid, x, y, result);
                    break;
                case CreatureKind.Line:
                    PaintLine(grid, x, y, result);
                    break;
                case CreatureKind.Rainbow:
                    PaintRainbow(grid, x, y, result);
                    break;
                default:
                    return DropResult.Fail(DropFailureReason.NoEffect);
            }

            if (result.PaintedCount == 0)
            {
                return DropResult.Fail(DropFailureReason.NoEffect);
            }

            result.SetLevelCompleted(grid.IsComplete);
            return result;
        }

        private static void PaintBomb(GridModel grid, int cx, int cy, DropResult result)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    TryPaint(grid, cx + dx, cy + dy, result);
                }
            }
        }

        private static void PaintLine(GridModel grid, int cx, int cy, DropResult result)
        {
            var row = CountPaintableRow(grid, cy);
            var column = CountPaintableColumn(grid, cx);

            if (row >= column)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    TryPaint(grid, x, cy, result);
                }
            }
            else
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    TryPaint(grid, cx, y, result);
                }
            }
        }

        private static void PaintRainbow(GridModel grid, int cx, int cy, DropResult result)
        {
            TryPaint(grid, cx, cy, result);
            TryPaint(grid, cx - 1, cy, result);
            TryPaint(grid, cx + 1, cy, result);
            TryPaint(grid, cx, cy - 1, result);
            TryPaint(grid, cx, cy + 1, result);
        }

        private static void TryPaint(GridModel grid, int x, int y, DropResult result)
        {
            if (!grid.HasTargetAt(x, y) || grid.IsPainted(x, y))
            {
                return;
            }

            if (!grid.TryPaintTarget(x, y))
            {
                return;
            }

            result.PaintedCells.Add(new PaintedCell(x, y, grid.TargetColorAt(x, y)));
        }

        private static int CountPaintableRow(GridModel grid, int y)
        {
            var count = 0;
            for (var x = 0; x < grid.Width; x++)
            {
                if (grid.HasTargetAt(x, y) && !grid.IsPainted(x, y))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountPaintableColumn(GridModel grid, int x)
        {
            var count = 0;
            for (var y = 0; y < grid.Height; y++)
            {
                if (grid.HasTargetAt(x, y) && !grid.IsPainted(x, y))
                {
                    count++;
                }
            }

            return count;
        }

        private static int RunLength(GridModel grid, int x, int y, RgbaColor color, int dx, int dy)
        {
            var length = 0;
            var cx = x + dx;
            var cy = y + dy;

            while (grid.IsInside(cx, cy) && grid.IsOccupied(cx, cy) && grid.OccupiedColorAt(cx, cy).Equals(color))
            {
                length++;
                cx += dx;
                cy += dy;
            }

            return length;
        }

        private static List<PaintedCell> CollectRunCells(GridModel grid, int x, int y, RgbaColor color)
        {
            var cells = new List<PaintedCell>();
            var seen = new bool[grid.Width * grid.Height];
            AddRun(grid, x, y, color, 1, 0, cells, seen);
            AddRun(grid, x, y, color, -1, 0, cells, seen);
            AddRun(grid, x, y, color, 0, 1, cells, seen);
            AddRun(grid, x, y, color, 0, -1, cells, seen);
            return cells;
        }

        private static void AddRun(
            GridModel grid,
            int x,
            int y,
            RgbaColor color,
            int dx,
            int dy,
            List<PaintedCell> cells,
            bool[] seen)
        {
            var cx = x;
            var cy = y;

            while (grid.IsInside(cx, cy) && grid.IsOccupied(cx, cy) && grid.OccupiedColorAt(cx, cy).Equals(color))
            {
                var index = cy * grid.Width + cx;
                if (!seen[index])
                {
                    seen[index] = true;
                    cells.Add(new PaintedCell(cx, cy, color));
                }

                cx += dx;
                cy += dy;
            }
        }

        // --- Analyse (sans mutation) -----------------------------------------

        /// <summary>Le placement de cette couleur créerait-il un alignement ?</summary>
        public static bool WouldMatch(GridModel grid, int x, int y, RgbaColor color)
        {
            if (grid == null || color.IsTransparent || !grid.IsPlaceable(x, y))
            {
                return false;
            }

            var horizontal = CountNeighbours(grid, x, y, color, 1, 0) + CountNeighbours(grid, x, y, color, -1, 0) + 1;
            var vertical = CountNeighbours(grid, x, y, color, 0, 1) + CountNeighbours(grid, x, y, color, 0, -1) + 1;
            return horizontal >= MinimumMatch || vertical >= MinimumMatch;
        }

        private static int CountNeighbours(GridModel grid, int x, int y, RgbaColor color, int dx, int dy)
        {
            var count = 0;
            var cx = x + dx;
            var cy = y + dy;

            while (grid.IsInside(cx, cy) && grid.IsOccupied(cx, cy) && grid.OccupiedColorAt(cx, cy).Equals(color))
            {
                count++;
                cx += dx;
                cy += dy;
            }

            return count;
        }

        /// <summary>Existe-t-il déjà un alignement de 3 blobs sur la grille ?</summary>
        public static bool HasMatch(GridModel grid)
        {
            if (grid == null)
            {
                return false;
            }

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (!grid.IsOccupied(x, y))
                    {
                        continue;
                    }

                    var color = grid.OccupiedColorAt(x, y);
                    if (CountNeighbours(grid, x, y, color, 1, 0) >= MinimumMatch - 1 ||
                        CountNeighbours(grid, x, y, color, 0, 1) >= MinimumMatch - 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Situation sans issue : plus aucune case libre, aucun alignement
        /// possible et aucun blob spécial sur le tapis pour se débloquer.
        /// </summary>
        public static bool IsBlocked(GridModel grid, bool specialOnBelt)
        {
            if (grid == null || grid.IsComplete)
            {
                return false;
            }

            if (specialOnBelt || grid.HasPlaceableCell)
            {
                return false;
            }

            return !HasMatch(grid);
        }
    }
}
