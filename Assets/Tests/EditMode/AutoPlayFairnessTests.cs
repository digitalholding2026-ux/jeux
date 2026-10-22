using System;
using System.Collections.Generic;
using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    /// <summary>
    /// Tests d'intégration "auto-play" de la règle d'équité :
    /// - un joueur automatique tape la première créature compatible à chaque tick ;
    /// - le niveau doit être gagné sans deadlock, sans overflow et sans tap refusé ;
    /// - aucune créature "morte" (couleur épuisée) ne doit apparaître sur le tapis.
    ///
    /// Les 3 niveaux placeholder sont reproduits ici (miroir de
    /// Assets/Resources/Levels/*.asset, LevelData étant Unity-only). Les 5 niveaux
    /// générés utilisent une graine fixe : le test est strictement déterministe.
    /// </summary>
    public sealed class AutoPlayFairnessTests
    {
        private const int Capacity = ConveyorQueue<Creature>.DefaultCapacity;

        private static readonly string[] GeneratedPalette = { "#FF7A59", "#4ECDC4", "#FFD166" };

        private static LevelDefinition Cross()
        {
            return LevelBuilder.FromRows(
                "croix",
                "La Croix",
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
                1.6f);
        }

        private static LevelDefinition Heart()
        {
            return LevelBuilder.FromRows(
                "coeur",
                "Le Coeur",
                new[]
                {
                    ".11...11.",
                    "1111.1111",
                    "111111111",
                    "111111111",
                    ".1111111.",
                    "..11111..",
                    "...111...",
                    "....1...."
                },
                new[] { "#FF4E88" },
                1.4f);
        }

        private static LevelDefinition Checker()
        {
            return LevelBuilder.FromRows(
                "damier",
                "Le Damier",
                new[]
                {
                    "1.2.1.2.",
                    ".2.1.2.1",
                    "2.1.2.1.",
                    ".1.2.1.2",
                    "1.2.1.2.",
                    ".2.1.2.1",
                    "2.1.2.1.",
                    ".1.2.1.2"
                },
                new[] { "#FF7A59", "#4ECDC4" },
                1.2f);
        }

        private static LevelDefinition Generated(int seed)
        {
            var random = new Random(seed * 7919);
            var width = random.Next(5, 10);
            var height = random.Next(5, 10);
            var colorCount = random.Next(1, GeneratedPalette.Length + 1);
            var rows = new string[height];
            var seen = new bool[colorCount];

            for (var y = 0; y < height; y++)
            {
                var chars = new char[width];
                for (var x = 0; x < width; x++)
                {
                    if (random.NextDouble() < 0.62)
                    {
                        var index = random.Next(0, colorCount);
                        chars[x] = (char)('1' + index);
                        seen[index] = true;
                    }
                    else
                    {
                        chars[x] = '.';
                    }
                }

                rows[y] = new string(chars);
            }

            // Garantit au moins un pixel par couleur de la palette.
            for (var c = 0; c < colorCount; c++)
            {
                if (seen[c])
                {
                    continue;
                }

                var y = c % height;
                var chars = rows[y].ToCharArray();
                chars[(c * 3) % width] = (char)('1' + c);
                rows[y] = new string(chars);
            }

            var palette = new string[colorCount];
            Array.Copy(GeneratedPalette, palette, colorCount);

            return LevelBuilder.FromRows("gen-" + seed, "Généré " + seed, rows, palette, 1f);
        }

        public static IEnumerable<TestCaseData> AllLevels()
        {
            yield return new TestCaseData(Cross()).SetName("AutoPlay_Placeholder_Croix");
            yield return new TestCaseData(Heart()).SetName("AutoPlay_Placeholder_Coeur");
            yield return new TestCaseData(Checker()).SetName("AutoPlay_Placeholder_Damier");

            for (var seed = 1; seed <= 5; seed++)
            {
                yield return new TestCaseData(Generated(seed)).SetName("AutoPlay_Generated_Seed" + seed);
            }
        }

        [TestCaseSource(nameof(AllLevels))]
        public void AutoPlay_WinsWithoutDeadlockOverflowOrDeadCreature(LevelDefinition level)
        {
            var game = new GameController(level, new RandomSource(1234), Capacity);
            var rejectedTaps = 0;
            var deadSpawns = 0;

            game.PaintResolved += (creature, result) =>
            {
                // Une créature spéciale peut être bloquée par le budget d'équité
                // (ses pixels restants sont réservés à une créature du tapis) :
                // ce n'est pas un tap refusé « injuste ».
                if (creature != null && !result.IsSuccess && !creature.IsSpecial)
                {
                    rejectedTaps++;
                }
            };

            game.Spawner.Spawned += creature =>
            {
                if (CreatureSpawner.CountRemaining(game.Grid, creature.Color) <= 0)
                {
                    deadSpawns++;
                }
            };

            var guard = 0;
            var maxSteps = level.Width * level.Height + 500;

            while (game.State == GameState.Playing && guard++ < maxSteps)
            {
                game.Tick(level.SpawnInterval);

                // Invariant d'équité : toute créature sur le tapis doit avoir
                // au moins un pixel restant de sa couleur.
                for (var i = 0; i < game.Conveyor.Count; i++)
                {
                    var onBelt = game.Conveyor[i];
                    Assert.Greater(
                        CreatureSpawner.CountRemaining(game.Grid, onBelt.Color),
                        0,
                        "Créature morte sur le tapis : " + onBelt);
                }

                // Joueur automatique : tape la première créature compatible.
                for (var i = 0; i < game.Conveyor.Count; i++)
                {
                    var creature = game.Conveyor[i];
                    if (game.Grid.TryFindNextPixel(creature.Color, out _, out _))
                    {
                        var result = game.Tap(creature);
                        Assert.IsTrue(result.IsSuccess, "Tap refusé sur une créature compatible : " + creature);
                        break;
                    }
                }

                if (game.Conveyor.Count > 1)
                {
                    Assert.Fail("Le tapis a accumulé " + game.Conveyor.Count + " créatures malgré un tap par tick.");
                }
            }

            Assert.AreEqual(GameState.Won, game.State, "Le niveau n'a pas été gagné : deadlock ou overflow.");
            Assert.AreEqual(0, deadSpawns, "Une créature d'une couleur épuisée a été générée.");
            Assert.AreEqual(0, rejectedTaps, "Un tap a été refusé : filet défensif atteint en jeu réel.");
            Assert.LessOrEqual(game.Conveyor.Count, 1, "Le tapis aurait dû être vide ou presque à la victoire.");
        }

        [Test]
        public void SumZero_NoSpawnAndNoOverflow_PlayerMustTap()
        {
            // Un seul pixel rouge : dès qu'une créature rouge est sur le tapis,
            // weight(rouge) = 1 - 1 = 0. Aucun spawn ne doit plus se produire.
            var level = LevelBuilder.FromRows(
                "un-pixel",
                "Un pixel",
                new[] { "1" },
                new[] { "#FF7A59" },
                1f);

            var game = new GameController(level, new RandomSource(7), Capacity);

            Assert.IsNotNull(game.SpawnNow());
            Assert.AreEqual(1, game.Conveyor.Count);

            for (var i = 0; i < 100; i++)
            {
                game.Tick(level.SpawnInterval);
            }

            Assert.AreEqual(GameState.Playing, game.State, "La partie ne doit pas être perdue sans spawn.");
            Assert.AreEqual(1, game.Conveyor.Count, "Aucune créature ne doit spawner quand la somme des poids est nulle.");

            var result = game.Tap(game.Conveyor[0]);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(GameState.Won, game.State);
        }
    }
}
