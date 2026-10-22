using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// Logique pure de résolution d'un tap : décide quels pixels peindre,
    /// ou pourquoi l'action est invalide. Aucune référence Unity.
    ///
    /// Sprint 2 : quatre types de créatures.
    /// - Normale   : prochain pixel de sa couleur ;
    /// - Bombe     : carré 3×3 autour du prochain pixel de sa couleur ;
    /// - Arc-en-ciel : prochain pixel cible, toutes couleurs confondues ;
    /// - Ligne     : rangée ou colonne entière (celle qui rapporte le plus).
    ///
    /// Les effets de zone respectent <see cref="PaintAllowance"/> : ils ne
    /// peuvent jamais rendre « morte » une créature déjà posée sur le tapis.
    /// </summary>
    public sealed class PaintResolver
    {
        public PaintResult Resolve(Creature creature, GridModel grid)
        {
            return Resolve(creature, grid, null);
        }

        public PaintResult Resolve(Creature creature, GridModel grid, IReadOnlyList<Creature> onBelt)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (creature == null)
            {
                return PaintResult.Fail(PaintFailureReason.NoCreature);
            }

            if (grid.IsComplete)
            {
                return PaintResult.Fail(PaintFailureReason.LevelComplete);
            }

            var allowance = new PaintAllowance(grid, onBelt, creature);

            switch (creature.Kind)
            {
                case CreatureKind.Bomb:
                    return ResolveBomb(creature, grid, allowance);
                case CreatureKind.Rainbow:
                    return ResolveRainbow(creature, grid, allowance);
                case CreatureKind.Line:
                    return ResolveLine(creature, grid, allowance);
                default:
                    return ResolveNormal(creature, grid);
            }
        }

        private static PaintResult ResolveNormal(Creature creature, GridModel grid)
        {
            if (!grid.TryPaintNext(creature.Color, out var x, out var y))
            {
                return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
            }

            return PaintResult.Ok(x, y, creature.Color, grid.IsComplete, 1, CreatureKind.Normal);
        }

        private static PaintResult ResolveBomb(Creature creature, GridModel grid, PaintAllowance allowance)
        {
            if (!grid.TryFindNextPixel(creature.Color, out var cx, out var cy))
            {
                return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
            }

            var painted = 0;
            var firstX = -1;
            var firstY = -1;
            var firstColor = RgbaColor.Transparent;

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var x = cx + dx;
                    var y = cy + dy;
                    if (!grid.HasTargetAt(x, y) || grid.IsPainted(x, y))
                    {
                        continue;
                    }

                    var color = grid.TargetColorAt(x, y);
                    if (!allowance.CanPaint(color))
                    {
                        continue;
                    }

                    if (!grid.TryPaintTarget(x, y))
                    {
                        continue;
                    }

                    allowance.Consume(color);
                    painted++;

                    if (firstX < 0)
                    {
                        firstX = x;
                        firstY = y;
                        firstColor = color;
                    }
                }
            }

            if (painted == 0)
            {
                return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
            }

            return PaintResult.Ok(firstX, firstY, firstColor, grid.IsComplete, painted, CreatureKind.Bomb);
        }

        private static PaintResult ResolveRainbow(Creature creature, GridModel grid, PaintAllowance allowance)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (grid.IsPainted(x, y) || !grid.HasTargetAt(x, y))
                    {
                        continue;
                    }

                    var color = grid.TargetColorAt(x, y);
                    if (!allowance.CanPaint(color))
                    {
                        continue;
                    }

                    if (!grid.TryPaintTarget(x, y))
                    {
                        continue;
                    }

                    allowance.Consume(color);
                    return PaintResult.Ok(x, y, color, grid.IsComplete, 1, CreatureKind.Rainbow);
                }
            }

            return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
        }

        private static PaintResult ResolveLine(Creature creature, GridModel grid, PaintAllowance allowance)
        {
            if (!grid.TryFindNextPixel(creature.Color, out var cx, out var cy))
            {
                return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
            }

            var rowCount = CountPaintableRow(grid, allowance, cy);
            var columnCount = CountPaintableColumn(grid, allowance, cx);

            var painted = 0;
            var firstColor = RgbaColor.Transparent;

            if (rowCount >= columnCount)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (TryPaintCell(grid, allowance, x, cy, ref firstColor))
                    {
                        painted++;
                    }
                }
            }
            else
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    if (TryPaintCell(grid, allowance, cx, y, ref firstColor))
                    {
                        painted++;
                    }
                }
            }

            if (painted == 0)
            {
                return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
            }

            return PaintResult.Ok(cx, cy, firstColor, grid.IsComplete, painted, CreatureKind.Line);
        }

        private static int CountPaintableRow(GridModel grid, PaintAllowance allowance, int y)
        {
            var count = 0;
            for (var x = 0; x < grid.Width; x++)
            {
                if (IsPaintable(grid, allowance, x, y))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountPaintableColumn(GridModel grid, PaintAllowance allowance, int x)
        {
            var count = 0;
            for (var y = 0; y < grid.Height; y++)
            {
                if (IsPaintable(grid, allowance, x, y))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsPaintable(GridModel grid, PaintAllowance allowance, int x, int y)
        {
            return grid.HasTargetAt(x, y) &&
                   !grid.IsPainted(x, y) &&
                   allowance.CanPaint(grid.TargetColorAt(x, y));
        }

        private static bool TryPaintCell(GridModel grid, PaintAllowance allowance, int x, int y, ref RgbaColor firstColor)
        {
            if (!IsPaintable(grid, allowance, x, y))
            {
                return false;
            }

            var color = grid.TargetColorAt(x, y);
            if (!grid.TryPaintTarget(x, y))
            {
                return false;
            }

            allowance.Consume(color);
            if (firstColor.IsTransparent)
            {
                firstColor = color;
            }

            return true;
        }
    }
}
