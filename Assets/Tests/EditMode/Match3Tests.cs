using System.Collections.Generic;
using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    /// <summary>
    /// Tests du Sprint 4 : placement drag &amp; drop, alignements, blobs
    /// spéciaux et détection des situations bloquées. Pur C# (dotnet test).
    /// </summary>
    public sealed class Match3Tests
    {
        private static readonly RgbaColor Red = RgbaColor.FromHex("#FF0000");
        private static readonly RgbaColor Green = RgbaColor.FromHex("#00FF00");
        private static readonly RgbaColor Blue = RgbaColor.FromHex("#0000FF");

        private static LevelDefinition Full(int width, int height)
        {
            var rows = new string[height];
            var line = new char[width];
            for (var x = 0; x < width; x++)
            {
                line[x] = '1';
            }

            for (var y = 0; y < height; y++)
            {
                rows[y] = new string(line);
            }

            return LevelBuilder.FromRows("plein-" + width + "x" + height, "Plein", rows, new[] { "#FF0000" }, 100f);
        }

        private static GridModel Grid(int width, int height)
        {
            return new GridModel(Full(width, height));
        }

        private static Creature Blob(int id, RgbaColor color)
        {
            return new Creature(id, color);
        }

        // --- Placement -------------------------------------------------------

        [Test]
        public void NormalDrop_OnEmptyPatternCell_PlacesTheBlob()
        {
            var grid = Grid(4, 4);
            var resolver = new Match3Resolver();

            var result = resolver.ResolveNormalDrop(Blob(1, Red), grid, 1, 1);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Placed);
            Assert.IsFalse(result.Matched);
            Assert.AreEqual(0, grid.PaintedCount);
            Assert.IsTrue(grid.IsOccupied(1, 1));
            Assert.AreEqual(Red, grid.OccupiedColorAt(1, 1));
        }

        [Test]
        public void NormalDrop_OnPaintedCell_IsRejected()
        {
            var grid = Grid(4, 4);
            grid.TryPaint(1, 1, Red);

            var result = new Match3Resolver().ResolveNormalDrop(Blob(1, Red), grid, 1, 1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(DropFailureReason.CellAlreadyPainted, result.Failure);
            Assert.AreEqual(1, grid.PaintedCount);
        }

        [Test]
        public void NormalDrop_OnOccupiedCell_IsRejected()
        {
            var grid = Grid(4, 4);
            grid.TryPlace(1, 1, Blue);

            var result = new Match3Resolver().ResolveNormalDrop(Blob(1, Red), grid, 1, 1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(DropFailureReason.CellOccupied, result.Failure);
            Assert.AreEqual(Blue, grid.OccupiedColorAt(1, 1));
        }

        [Test]
        public void NormalDrop_OutsideThePattern_IsRejected()
        {
            var level = LevelBuilder.FromRows("croix", "Croix", new[] { "1.1", ".1.", "1.1" }, new[] { "#FF0000" }, 100f);
            var grid = new GridModel(level);

            var result = new Match3Resolver().ResolveNormalDrop(Blob(1, Red), grid, 1, 0);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(DropFailureReason.NotInPattern, result.Failure);
        }

        // --- Alignements -----------------------------------------------------

        [Test]
        public void ThreeAlignedBlobs_PaintTheirCellsAndDisappear()
        {
            var grid = Grid(5, 5);
            var resolver = new Match3Resolver();

            resolver.ResolveNormalDrop(Blob(1, Red), grid, 0, 0);
            resolver.ResolveNormalDrop(Blob(2, Red), grid, 1, 0);
            var result = resolver.ResolveNormalDrop(Blob(3, Red), grid, 2, 0);

            Assert.IsTrue(result.Matched);
            Assert.AreEqual(3, result.MatchLength);
            Assert.AreEqual(3, result.PaintedCount);
            Assert.IsFalse(result.HasSpecial);
            Assert.AreEqual(3, grid.PaintedCount);
            Assert.IsFalse(grid.IsOccupied(0, 0));
            Assert.IsFalse(grid.IsOccupied(1, 0));
            Assert.IsFalse(grid.IsOccupied(2, 0));
        }

        [Test]
        public void FourAlignedBlobs_CreateALineBlob()
        {
            // On pose les blobs avec un trou (0,1,3) : aucun alignement de 3 ne
            // se déclenche avant le blob final qui complète la ligne de 4.
            var grid = Grid(6, 6);
            var resolver = new Match3Resolver();

            grid.TryPlace(0, 0, Red);
            grid.TryPlace(1, 0, Red);
            grid.TryPlace(3, 0, Red);
            var result = resolver.ResolveNormalDrop(Blob(4, Red), grid, 2, 0);

            Assert.IsTrue(result.Matched);
            Assert.AreEqual(4, result.MatchLength);
            Assert.IsTrue(result.HasSpecial);
            Assert.AreEqual(CreatureKind.Line, result.SpecialKind);
            Assert.AreEqual(4, result.PaintedCount);
        }

        [Test]
        public void FiveAlignedBlobs_CreateABomb()
        {
            var grid = Grid(6, 6);
            var resolver = new Match3Resolver();

            grid.TryPlace(0, 0, Red);
            grid.TryPlace(1, 0, Red);
            grid.TryPlace(3, 0, Red);
            grid.TryPlace(4, 0, Red);

            var result = resolver.ResolveNormalDrop(Blob(5, Red), grid, 2, 0);

            Assert.IsTrue(result.Matched);
            Assert.AreEqual(5, result.MatchLength);
            Assert.AreEqual(CreatureKind.Bomb, result.SpecialKind);
            Assert.AreEqual(5, result.PaintedCount);
        }

        [Test]
        public void VerticalAlignedBlobs_AreDetected()
        {
            var grid = Grid(5, 5);
            var resolver = new Match3Resolver();

            resolver.ResolveNormalDrop(Blob(1, Blue), grid, 2, 0);
            resolver.ResolveNormalDrop(Blob(2, Blue), grid, 2, 1);
            var result = resolver.ResolveNormalDrop(Blob(3, Blue), grid, 2, 2);

            Assert.IsTrue(result.Matched);
            Assert.AreEqual(3, result.MatchLength);
            Assert.AreEqual(3, grid.PaintedCount);
        }

        [Test]
        public void CrossShape_PaintsTheUnionOfBothLines()
        {
            // Ligne horizontale de 3 + ligne verticale de 3 partageant (1,1) :
            // 5 cases distinctes sont peintes, sans bonus (aucune ligne de 4).
            var grid = Grid(5, 5);
            var resolver = new Match3Resolver();

            resolver.ResolveNormalDrop(Blob(1, Green), grid, 0, 1);
            resolver.ResolveNormalDrop(Blob(2, Green), grid, 2, 1);
            resolver.ResolveNormalDrop(Blob(3, Green), grid, 1, 0);
            resolver.ResolveNormalDrop(Blob(4, Green), grid, 1, 2);
            var result = resolver.ResolveNormalDrop(Blob(5, Green), grid, 1, 1);

            Assert.IsTrue(result.Matched);
            Assert.AreEqual(3, result.MatchLength);
            Assert.AreEqual(5, result.PaintedCount);
            Assert.IsFalse(result.HasSpecial);
        }

        // --- Blobs spéciaux --------------------------------------------------

        [Test]
        public void BombSpecial_PaintsA3x3Zone()
        {
            var grid = Grid(5, 5);
            grid.TryPlace(2, 2, Blue);

            var result = new Match3Resolver().ResolveSpecialDrop(
                new Creature(1, Red, CreatureKind.Bomb),
                grid,
                2,
                2);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(9, result.PaintedCount);
            Assert.AreEqual(9, grid.PaintedCount);
            Assert.IsFalse(grid.IsOccupied(2, 2), "La bombe doit aussi détruire le blob présent.");
        }

        [Test]
        public void LineSpecial_PaintsTheBestRowOrColumn()
        {
            var level = LevelBuilder.FromRows(
                "ligne",
                "Ligne",
                new[] { "1111", "1111", ".111", "1111" },
                new[] { "#FF0000" },
                100f);
            var grid = new GridModel(level);

            var result = new Match3Resolver().ResolveSpecialDrop(
                new Creature(1, Red, CreatureKind.Line),
                grid,
                2,
                3);

            Assert.IsTrue(result.IsSuccess);
            // Rangée 3 complète (4 cases) plutôt que colonne 2 (3 cases).
            Assert.AreEqual(4, result.PaintedCount);
            for (var x = 0; x < 4; x++)
            {
                Assert.IsTrue(grid.IsPainted(x, 3));
            }
        }

        [Test]
        public void RainbowSpecial_PaintsItsCellAndNeighbours()
        {
            var grid = Grid(5, 5);

            var result = new Match3Resolver().ResolveSpecialDrop(
                new Creature(1, Red, CreatureKind.Rainbow),
                grid,
                2,
                2);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(5, result.PaintedCount);
            Assert.IsTrue(grid.IsPainted(2, 2));
            Assert.IsTrue(grid.IsPainted(1, 2));
            Assert.IsTrue(grid.IsPainted(3, 2));
            Assert.IsTrue(grid.IsPainted(2, 1));
            Assert.IsTrue(grid.IsPainted(2, 3));
        }

        // --- Analyse et blocage ----------------------------------------------

        [Test]
        public void WouldMatch_DetectsAUsefulCell()
        {
            var grid = Grid(5, 5);
            grid.TryPlace(0, 0, Red);
            grid.TryPlace(1, 0, Red);

            Assert.IsTrue(Match3Resolver.WouldMatch(grid, 2, 0, Red));
            Assert.IsFalse(Match3Resolver.WouldMatch(grid, 2, 1, Red));
        }

        [Test]
        public void HasMatch_DetectsExistingAlignments()
        {
            var grid = Grid(5, 5);
            grid.TryPlace(0, 0, Red);
            grid.TryPlace(1, 0, Red);
            Assert.IsFalse(Match3Resolver.HasMatch(grid));

            grid.TryPlace(2, 0, Red);
            Assert.IsTrue(Match3Resolver.HasMatch(grid));
        }

        [Test]
        public void IsBlocked_WhenBoardIsFullWithoutMatch()
        {
            var grid = Grid(3, 3);
            var colors = new[]
            {
                Green, Green, Red,
                Red, Green, Green,
                Green, Red, Red
            };

            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    grid.TryPlace(x, y, colors[y * 3 + x]);
                }
            }

            Assert.IsFalse(grid.HasPlaceableCell);
            Assert.IsFalse(Match3Resolver.HasMatch(grid));
            Assert.IsTrue(Match3Resolver.IsBlocked(grid, false));
            Assert.IsFalse(Match3Resolver.IsBlocked(grid, true), "Un blob spécial sur le tapis débloque la situation.");
        }

        [Test]
        public void IsBlocked_IsFalseWhileTheBoardHasFreeCells()
        {
            var grid = Grid(3, 3);
            Assert.IsTrue(grid.HasPlaceableCell);
            Assert.IsFalse(Match3Resolver.IsBlocked(grid, false));
        }

        // --- Intégration GameController --------------------------------------

        [Test]
        public void GameDrop_RemovesTheCreatureAndScoresTheMatch()
        {
            var game = new GameController(Full(5, 5), new RandomSource(42), 5,
                new CreatureSpawner(100f) { SpecialChancePercent = 0 });

            game.SpawnNow();
            game.SpawnNow();
            game.SpawnNow();

            Assert.IsTrue(game.Drop(game.Conveyor[0], 0, 0).IsSuccess);
            Assert.IsTrue(game.Drop(game.Conveyor[0], 1, 0).IsSuccess);
            Assert.AreEqual(1, game.Conveyor.Count);

            var result = game.Drop(game.Conveyor[0], 2, 0);

            Assert.IsTrue(result.Matched);
            Assert.AreEqual(3, game.PaintedPixels);
            Assert.AreEqual(ScoreRules.PointsPerPixel * 3, game.Score);
            Assert.AreEqual(0, game.Conveyor.Count);
        }

        [Test]
        public void GameDrop_RefusedDropKeepsTheCreatureOnTheBelt()
        {
            var game = new GameController(Full(4, 4), new RandomSource(42), 5,
                new CreatureSpawner(100f) { SpecialChancePercent = 0 });

            var creature = game.SpawnNow();
            game.Drop(creature, 0, 0);
            var second = game.SpawnNow();

            var result = game.Drop(second, 0, 0);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(DropFailureReason.CellOccupied, result.Failure);
            Assert.AreEqual(1, game.Conveyor.Count);
        }

        [Test]
        public void GameDrop_FourInARowAddsALineBlobToTheBelt()
        {
            var game = new GameController(Full(6, 6), new RandomSource(42), 5,
                new CreatureSpawner(100f) { SpecialChancePercent = 0 });

            game.SpawnNow();
            game.SpawnNow();
            game.SpawnNow();
            game.SpawnNow();

            game.Drop(game.Conveyor[0], 0, 0);
            game.Drop(game.Conveyor[0], 1, 0);
            game.Drop(game.Conveyor[0], 3, 0);
            game.Drop(game.Conveyor[0], 2, 0);

            Assert.AreEqual(1, game.Conveyor.Count);
            Assert.AreEqual(CreatureKind.Line, game.Conveyor[0].Kind);
        }

        [Test]
        public void LineBlobOnTheBelt_CanBeDroppedToPaintARow()
        {
            var game = new GameController(Full(5, 5), new RandomSource(42), 5,
                new CreatureSpawner(100f) { SpecialChancePercent = 0 });

            game.SpawnNow();
            game.Drop(game.Conveyor[0], 0, 0);
            game.Drop(game.SpawnNow(), 1, 0);
            game.Drop(game.SpawnNow(), 2, 0);

            // Alignement de 3 : une ligne de 4 n'existe pas, on force le blob
            // spécial avec le spawner (chance 100) pour tester son largage.
            game.Spawner.CreateSpecial(Red, CreatureKind.Line);
            var line = game.Conveyor[game.Conveyor.Count - 1];

            var result = game.Drop(line, 4, 2);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(5, result.PaintedCount);
            Assert.AreEqual(3 + 5, game.PaintedPixels);
        }

        [Test]
        public void Deadlock_WhenTheBoardIsFullWithoutMatch_LosesTheGame()
        {
            var level = LevelBuilder.FromRows(
                "blocage",
                "Blocage",
                new[] { "111", "111", "111" },
                new[] { "#FF0000" },
                100f);
            var game = new GameController(level, new RandomSource(42), 5,
                new CreatureSpawner(100f) { SpecialChancePercent = 0 });

            // Remplit 8 cases sans aucun alignement de 3 (vert/rouge).
            game.Grid.TryPlace(0, 0, Green);
            game.Grid.TryPlace(1, 0, Green);
            game.Grid.TryPlace(2, 0, Red);
            game.Grid.TryPlace(0, 1, Red);
            game.Grid.TryPlace(1, 1, Green);
            game.Grid.TryPlace(2, 1, Green);
            game.Grid.TryPlace(0, 2, Green);
            game.Grid.TryPlace(2, 2, Red);

            var creature = game.SpawnNow();
            var result = game.Drop(creature, 1, 2);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.Matched);
            Assert.AreEqual(GameState.Lost, game.State, "Grille pleine sans alignement : partie perdue.");
        }
    }
}
