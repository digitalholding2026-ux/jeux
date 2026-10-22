using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    /// <summary>
    /// Auto-play du Sprint 4 : un joueur automatique glisse les blobs du tapis
    /// vers la grille en cherchant toujours le placement qui crée un alignement.
    /// Le niveau doit être gagné sans débordement du tapis ni blocage.
    /// </summary>
    public sealed class AutoPlayMatch3Tests
    {
        private static LevelDefinition Full(int size)
        {
            var rows = new string[size];
            var line = new char[size];
            for (var x = 0; x < size; x++)
            {
                line[x] = '1';
            }

            for (var y = 0; y < size; y++)
            {
                rows[y] = new string(line);
            }

            return LevelBuilder.FromRows("auto-" + size, "Auto " + size, rows, new[] { "#FF7A59" }, 1f);
        }

        private static LevelDefinition Cross()
        {
            return LevelBuilder.FromRows(
                "auto-croix",
                "Auto Croix",
                new[]
                {
                    "..111..",
                    "..111..",
                    "1111111",
                    "1111111",
                    "1111111",
                    "..111..",
                    "..111.."
                },
                new[] { "#FF7A59" },
                1f);
        }

        [Test]
        public void AutoPlay_IntelligentDragAndDrop_WinsTheLevel()
        {
            RunAutoPlay(Full(6), 6 * 6);
        }

        [Test]
        public void AutoPlay_IntelligentDragAndDrop_WinsTheCross()
        {
            RunAutoPlay(Cross(), 33);
        }

        private static void RunAutoPlay(LevelDefinition level, int targets)
        {
            var game = new GameController(level, new RandomSource(2024), 5,
                new CreatureSpawner(level.SpawnInterval) { SpecialChancePercent = 0 });

            var drops = 0;
            var guard = 0;
            var maxSteps = targets * 30;

            while (game.State == GameState.Playing && guard++ < maxSteps)
            {
                if (game.Conveyor.Count == 0)
                {
                    Assert.IsNotNull(game.SpawnNow(), "Le spawner doit encore produire des blobs.");
                }

                if (TryDropBest(game))
                {
                    drops++;
                    Assert.LessOrEqual(game.Conveyor.Count, 5, "Le tapis a débordé.");
                    continue;
                }

                // Aucun placement utile : on avance le temps pour renouveler le tapis.
                game.Tick(level.SpawnInterval);
            }

            Assert.AreEqual(GameState.Won, game.State, "Le niveau n'a pas été gagné (blocage ou débordement).");
            Assert.AreEqual(targets, game.PaintedPixels);
            Assert.Greater(drops, 0);
            Assert.Greater(game.Score, 0);
        }

        /// <summary>
        /// Cherche d'abord un alignement immédiat, puis un placement adjacent à
        /// la même couleur, puis n'importe quelle case libre. Retourne false si
        /// aucun coup n'est possible.
        /// </summary>
        private static bool TryDropBest(GameController game)
        {
            var grid = game.Grid;
            var items = game.Conveyor.Items;

            for (var i = 0; i < items.Count; i++)
            {
                var creature = items[i];
                if (creature == null || creature.IsSpecial)
                {
                    continue;
                }

                if (FindCell(grid, creature.Color, true, out var x, out var y))
                {
                    return game.Drop(creature, x, y).IsSuccess;
                }
            }

            for (var i = 0; i < items.Count; i++)
            {
                var creature = items[i];
                if (creature == null || creature.IsSpecial)
                {
                    continue;
                }

                if (FindCell(grid, creature.Color, false, out var x, out var y))
                {
                    return game.Drop(creature, x, y).IsSuccess;
                }
            }

            // Aucun voisin de la même couleur : on pose sur la première case libre.
            for (var i = 0; i < items.Count; i++)
            {
                var creature = items[i];
                if (creature == null || creature.IsSpecial)
                {
                    continue;
                }

                if (FindAnyFreeCell(grid, out var x, out var y))
                {
                    return game.Drop(creature, x, y).IsSuccess;
                }
            }

            return false;
        }

        private static bool FindAnyFreeCell(GridModel grid, out int x, out int y)
        {
            for (var cy = 0; cy < grid.Height; cy++)
            {
                for (var cx = 0; cx < grid.Width; cx++)
                {
                    if (grid.IsPlaceable(cx, cy))
                    {
                        x = cx;
                        y = cy;
                        return true;
                    }
                }
            }

            x = -1;
            y = -1;
            return false;
        }

        private static bool FindCell(GridModel grid, RgbaColor color, bool needMatch, out int x, out int y)
        {
            for (var cy = 0; cy < grid.Height; cy++)
            {
                for (var cx = 0; cx < grid.Width; cx++)
                {
                    if (!grid.IsPlaceable(cx, cy))
                    {
                        continue;
                    }

                    var matches = Match3Resolver.WouldMatch(grid, cx, cy, color);
                    if (needMatch != matches)
                    {
                        continue;
                    }

                    if (!needMatch && !HasNeighbour(grid, cx, cy, color))
                    {
                        continue;
                    }

                    x = cx;
                    y = cy;
                    return true;
                }
            }

            x = -1;
            y = -1;
            return false;
        }

        private static bool HasNeighbour(GridModel grid, int x, int y, RgbaColor color)
        {
            return IsColor(grid, x - 1, y, color) ||
                   IsColor(grid, x + 1, y, color) ||
                   IsColor(grid, x, y - 1, color) ||
                   IsColor(grid, x, y + 1, color);
        }

        private static bool IsColor(GridModel grid, int x, int y, RgbaColor color)
        {
            return grid.IsInside(x, y) && grid.IsOccupied(x, y) && grid.OccupiedColorAt(x, y).Equals(color);
        }
    }
}
