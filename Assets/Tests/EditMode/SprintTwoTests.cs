using System.Collections.Generic;
using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    /// <summary>
    /// Tests du Sprint 2 : créatures spéciales, score, combos, objectifs.
    /// Tout est pur C# : ces tests tournent sans Unity (dotnet test) et dans
    /// le Test Runner EditMode.
    /// </summary>
    public sealed class SprintTwoTests
    {
        private static readonly RgbaColor Red = RgbaColor.FromHex("#FF0000");
        private static readonly RgbaColor Green = RgbaColor.FromHex("#00FF00");

        // --- Fabriques de niveaux -------------------------------------------

        private static LevelDefinition Grid(int width, int height, string[] palette)
        {
            var rows = new string[height];
            for (var y = 0; y < height; y++)
            {
                var chars = new char[width];
                for (var x = 0; x < width; x++)
                {
                    chars[x] = '1';
                }

                rows[y] = new string(chars);
            }

            return LevelBuilder.FromRows("grille-" + width, "Grille", rows, palette, 100f);
        }

        private static LevelDefinition OnePixel(GoalType goal = GoalType.FillAll, int targetScore = 0, float timeLimit = 0f)
        {
            return LevelBuilder.FromRows(
                "pixel",
                "Pixel",
                new[] { "1" },
                new[] { "#FF0000" },
                100f,
                goal,
                targetScore,
                timeLimit);
        }

        private static LevelDefinition ThreeByThree(GoalType goal = GoalType.FillAll, int targetScore = 0, float timeLimit = 0f)
        {
            return LevelBuilder.FromRows(
                "grille",
                "Grille",
                new[] { "111", "111", "111" },
                new[] { "#FF0000" },
                100f,
                goal,
                targetScore,
                timeLimit);
        }

        /// <summary>Aléa scripté : chaque appel consomme la valeur suivante (cyclique).</summary>
        private sealed class ScriptedRandom : IRandomSource
        {
            private readonly int[] _values;
            private int _index;

            public ScriptedRandom(params int[] values)
            {
                _values = values;
            }

            public int Range(int minInclusive, int maxExclusive)
            {
                var value = _values[_index % _values.Length];
                _index++;
                var span = maxExclusive - minInclusive;
                return span <= 0 ? minInclusive : minInclusive + (value % span);
            }
        }

        private static GameController Game(LevelDefinition level, int specialChance = 0, IRandomSource random = null)
        {
            var spawner = new CreatureSpawner(level.SpawnInterval) { SpecialChancePercent = specialChance };
            return new GameController(
                level,
                random ?? new RandomSource(1234),
                ConveyorQueue<Creature>.DefaultCapacity,
                spawner);
        }

        // --- Créatures spéciales --------------------------------------------

        [Test]
        public void Creature_SpecialKinds_HaveTheirOwnDisplayColor()
        {
            var normal = new Creature(1, Red);
            var bomb = new Creature(2, Red, CreatureKind.Bomb);
            var rainbow = new Creature(3, Red, CreatureKind.Rainbow);
            var line = new Creature(4, Red, CreatureKind.Line);

            Assert.AreEqual(Red, normal.DisplayColor);
            Assert.AreEqual(Creature.BombDisplayColor, bomb.DisplayColor);
            Assert.AreEqual(Creature.RainbowDisplayColor, rainbow.DisplayColor);
            Assert.AreEqual(Creature.LineDisplayColor, line.DisplayColor);
            Assert.IsTrue(bomb.IsSpecial);
            Assert.IsFalse(normal.IsSpecial);

            // La couleur métier reste celle de l'équité pour toutes.
            Assert.AreEqual(Red, bomb.Color);
        }

        [Test]
        public void Spawner_WhenSpecialChanceIsZero_OnlySpawnsNormalCreatures()
        {
            var level = ThreeByThree();
            var grid = new GridModel(level);
            var spawner = new CreatureSpawner(1f) { SpecialChancePercent = 0 };
            var random = new RandomSource(99);

            for (var i = 0; i < 300; i++)
            {
                var creature = spawner.SpawnNow(grid, new List<Creature>(), random);
                Assert.AreEqual(CreatureKind.Normal, creature.Kind);
            }
        }

        [Test]
        public void Spawner_WithSpecialChance100_ProducesTheThreeKinds()
        {
            var level = ThreeByThree();
            var grid = new GridModel(level);
            var spawner = new CreatureSpawner(1f) { SpecialChancePercent = 100 };
            var random = new RandomSource(7);
            var seen = new HashSet<CreatureKind>();

            for (var i = 0; i < 120; i++)
            {
                var creature = spawner.SpawnNow(grid, new List<Creature>(), random);
                seen.Add(creature.Kind);
                // La règle d'équité continue de s'appliquer aux spéciales.
                Assert.Greater(CreatureSpawner.CountRemaining(grid, creature.Color), 0);
            }

            Assert.IsTrue(seen.Contains(CreatureKind.Bomb));
            Assert.IsTrue(seen.Contains(CreatureKind.Rainbow));
            Assert.IsTrue(seen.Contains(CreatureKind.Line));
        }

        [Test]
        public void Bomb_Paints3x3AroundTheNextPixel()
        {
            var grid = new GridModel(Grid(5, 5, new[] { "#FF0000" }));
            var bomb = new Creature(1, Red, CreatureKind.Bomb);

            var result = new PaintResolver().Resolve(bomb, grid);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(CreatureKind.Bomb, result.Kind);
            // Prochain pixel = (0,0) : le carré 3×3 est tronqué au coin → 4 cases.
            Assert.AreEqual(4, result.PaintedCount);
            Assert.AreEqual(4, grid.PaintedCount);
            Assert.IsTrue(grid.IsPainted(0, 0));
            Assert.IsTrue(grid.IsPainted(1, 0));
            Assert.IsTrue(grid.IsPainted(0, 1));
            Assert.IsTrue(grid.IsPainted(1, 1));
        }

        [Test]
        public void Bomb_PaintsOnlyPixelsStillNeeded()
        {
            // 4×4 : la bombe peint le carré 3×3 autour du prochain pixel rouge.
            var level = LevelBuilder.FromRows(
                "bombe",
                "Bombe",
                new[] { "1111", "1111", "1111", "1111" },
                new[] { "#FF0000" },
                100f);
            var grid = new GridModel(level);

            // On amène le prochain pixel rouge en (1,1).
            grid.TryPaint(0, 0, Red);
            grid.TryPaint(1, 0, Red);
            grid.TryPaint(2, 0, Red);
            grid.TryPaint(3, 0, Red);
            grid.TryPaint(0, 1, Red);

            var result = new PaintResolver().Resolve(new Creature(1, Red, CreatureKind.Bomb), grid);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(5, result.PaintedCount);
            Assert.IsTrue(grid.IsPainted(1, 1));
            Assert.IsTrue(grid.IsPainted(2, 2));
        }

        [Test]
        public void Bomb_CannotMakeABeltCreatureUseless()
        {
            // 3 pixels rouges, une créature rouge déjà sur le tapis :
            // la bombe ne peut en peindre que 2, sinon la créature du tapis
            // n'aurait plus rien à peindre.
            var level = LevelBuilder.FromRows(
                "budget",
                "Budget",
                new[] { "111" },
                new[] { "#FF0000" },
                100f);
            var grid = new GridModel(level);
            var beltCreature = new Creature(2, Red);

            var result = new PaintResolver().Resolve(
                new Creature(1, Red, CreatureKind.Bomb),
                grid,
                new List<Creature> { beltCreature });

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.PaintedCount);
            Assert.AreEqual(1, CreatureSpawner.CountRemaining(grid, Red));
        }

        [Test]
        public void Rainbow_PaintsTheNextPixelWhateverItsColor()
        {
            // (0,0) est vert, (1,0) est rouge. L'arc-en-ciel porte la couleur
            // métier rouge mais peint le premier pixel cible : le vert.
            var level = LevelBuilder.FromRows(
                "arc",
                "Arc",
                new[] { "21" },
                new[] { "#FF0000", "#00FF00" },
                100f);
            var grid = new GridModel(level);

            var result = new PaintResolver().Resolve(new Creature(1, Red, CreatureKind.Rainbow), grid);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(CreatureKind.Rainbow, result.Kind);
            Assert.AreEqual(1, result.PaintedCount);
            Assert.IsTrue(grid.IsPainted(0, 0));
            Assert.IsFalse(grid.IsPainted(1, 0));
        }

        [Test]
        public void Line_PaintsAWholeRow()
        {
            var grid = new GridModel(Grid(4, 4, new[] { "#FF0000" }));

            var result = new PaintResolver().Resolve(new Creature(1, Red, CreatureKind.Line), grid);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(CreatureKind.Line, result.Kind);
            Assert.AreEqual(4, result.PaintedCount);
            for (var x = 0; x < 4; x++)
            {
                Assert.IsTrue(grid.IsPainted(x, 0));
            }

            Assert.IsFalse(grid.IsPainted(0, 1));
        }

        // --- Score et combos -------------------------------------------------

        [Test]
        public void Score_IncreasesAndComboMultiplies()
        {
            var game = Game(ThreeByThree());

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);            // chaîne 1 → ×1
            Assert.AreEqual(10, game.Score);
            Assert.AreEqual(1, game.CurrentMultiplier);

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);            // chaîne 2 → ×2
            Assert.AreEqual(30, game.Score);
            Assert.AreEqual(2, game.CurrentMultiplier);

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);            // chaîne 3 → ×3
            Assert.AreEqual(60, game.Score);
            Assert.AreEqual(3, game.CurrentMultiplier);
        }

        [Test]
        public void Combo_IsCappedAtFour()
        {
            var game = Game(Grid(4, 4, new[] { "#FF0000" }));

            for (var i = 0; i < 6; i++)
            {
                game.SpawnNow();
                game.Tap(game.Conveyor[0]);
            }

            Assert.AreEqual(ScoreRules.MaxMultiplier, game.CurrentMultiplier);
        }

        [Test]
        public void Combo_TapOnAnotherColorRestartsTheChain()
        {
            var level = LevelBuilder.FromRows(
                "deux",
                "Deux",
                new[] { "12", "12" },
                new[] { "#FF0000", "#00FF00" },
                100f);
            var game = Game(level, 0, new ScriptedRandom(0, 1));

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);            // rouge ×1
            var first = game.Score;

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);            // vert → chaîne 1

            Assert.AreEqual(ScoreRules.PointsPerPixel, game.Score - first);
            Assert.AreEqual(1, game.CurrentMultiplier);
        }

        [Test]
        public void Combo_ExpiresAfterTheWindow()
        {
            var level = LevelBuilder.FromRows(
                "combo",
                "Combo",
                new[] { "1111" },
                new[] { "#FF0000" },
                100f);
            var game = Game(level);

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);
            game.SpawnNow();
            game.Tap(game.Conveyor[0]);
            Assert.AreEqual(2, game.CurrentMultiplier);

            game.Tick(ScoreRules.ComboWindowSeconds + 0.1f);

            Assert.AreEqual(1, game.CurrentMultiplier);
        }

        // --- Objectifs -------------------------------------------------------

        [Test]
        public void ScoreTarget_WinsAsSoonAsTheTargetIsReached()
        {
            var game = Game(ThreeByThree(GoalType.ScoreTarget, 30, 60f));

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);
            Assert.AreEqual(GameState.Playing, game.State);

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);            // 10 + 20 = 30

            Assert.AreEqual(30, game.Score);
            Assert.AreEqual(GameState.Won, game.State);
        }

        [Test]
        public void ScoreTarget_IsLostWhenTimeRunsOut()
        {
            var game = Game(ThreeByThree(GoalType.ScoreTarget, 500, 60f));

            game.Tick(59f);
            Assert.AreEqual(GameState.Playing, game.State);

            game.Tick(2f);

            Assert.AreEqual(GameState.Lost, game.State);
            Assert.AreEqual(0f, game.TimeRemaining);
            Assert.IsFalse(game.Spawner.Enabled);
        }

        [Test]
        public void TimeAttack_IsLostWhenTimeRunsOut()
        {
            var game = Game(ThreeByThree(GoalType.TimeAttack, 0, 30f));

            game.Tick(30.5f);

            Assert.AreEqual(GameState.Lost, game.State);
        }

        [Test]
        public void TimeAttack_IsWonWhenTheGridIsComplete()
        {
            var game = Game(OnePixel(GoalType.TimeAttack, 0, 30f));

            game.SpawnNow();
            game.Tap(game.Conveyor[0]);

            Assert.AreEqual(GameState.Won, game.State);
            Assert.Greater(game.TimeRemaining, 0f);
        }

        [Test]
        public void LevelDefinition_DefaultsTheTimeLimitPerGoal()
        {
            Assert.AreEqual(60f, OnePixel(GoalType.ScoreTarget, 100).TimeLimitSeconds);
            Assert.AreEqual(30f, OnePixel(GoalType.TimeAttack).TimeLimitSeconds);
            Assert.AreEqual(0f, OnePixel(GoalType.FillAll).TimeLimitSeconds);
            Assert.AreEqual("Objectif : remplir tout le motif", OnePixel(GoalType.FillAll).GoalDescription);
        }

        [Test]
        public void GoalRules_DescribeEveryGoal()
        {
            Assert.AreEqual("Objectif : remplir tout le motif", GoalRules.Describe(GoalType.FillAll, 0, 0f));
            Assert.AreEqual("Objectif : 300 pts en 60 s", GoalRules.Describe(GoalType.ScoreTarget, 300, 60f));
            Assert.AreEqual("Objectif : remplir en 30 s", GoalRules.Describe(GoalType.TimeAttack, 0, 30f));
        }

        // --- Économie --------------------------------------------------------

        [Test]
        public void Economy_ConvertsScoreIntoCoins()
        {
            Assert.AreEqual(3, EconomyRules.CoinsForRun(0, false));
            Assert.AreEqual(20, EconomyRules.CoinsForRun(0, true));
            Assert.AreEqual(30, EconomyRules.CoinsForRun(400, true));
            Assert.AreEqual(12, EconomyRules.CoinsForRun(399, false));
        }

        [Test]
        public void Restart_ResetsScoreComboAndTimer()
        {
            var game = Game(ThreeByThree(GoalType.TimeAttack, 0, 30f));
            game.SpawnNow();
            game.Tap(game.Conveyor[0]);
            game.Tick(2f);

            game.Restart();

            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(1, game.CurrentMultiplier);
            Assert.AreEqual(0f, game.ElapsedTime);
            Assert.AreEqual(30f, game.TimeRemaining);
            Assert.AreEqual(GameState.Playing, game.State);
        }
    }
}
