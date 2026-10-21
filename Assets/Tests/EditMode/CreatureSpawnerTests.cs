using System.Collections.Generic;
using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class CreatureSpawnerTests
    {
        private static readonly RgbaColor Red = RgbaColor.FromHex("#FF0000");
        private static readonly RgbaColor Green = RgbaColor.FromHex("#00FF00");

        private static LevelDefinition Level()
        {
            return LevelBuilder.FromRows(
                "test",
                "Test",
                new[]
                {
                    "11",
                    "22"
                },
                new[] { "#FF0000", "#00FF00" },
                0.5f);
        }

        private static List<Creature> Belt(params RgbaColor[] colors)
        {
            var belt = new List<Creature>();
            for (var i = 0; i < colors.Length; i++)
            {
                belt.Add(new Creature(i + 1, colors[i]));
            }

            return belt;
        }

        [Test]
        public void SpawnNow_UsesAColorFromTheRemainingPattern()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f);
            var random = new RandomSource(42);

            var creature = spawner.SpawnNow(grid, Belt(), random);

            Assert.IsNotNull(creature);
            Assert.IsTrue(grid.HasTargetAt(0, 0));
            Assert.IsTrue(creature.Color.Equals(grid.TargetColorAt(0, 0)) ||
                          creature.Color.Equals(grid.TargetColorAt(0, 1)));
        }

        [Test]
        public void SpawnNow_IdsAreIncreasing()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f);
            var random = new RandomSource(1);
            var belt = Belt();

            var first = spawner.SpawnNow(grid, belt, random);
            var second = spawner.SpawnNow(grid, belt, random);

            Assert.AreEqual(first.Id + 1, second.Id);
        }

        [Test]
        public void Tick_SpawnsAccordingToInterval()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f);
            var belt = Belt();
            var spawned = new List<Creature>();
            spawner.Spawned += c => spawned.Add(c);

            spawner.Tick(0.6f, grid, belt, new RandomSource(7));
            Assert.AreEqual(1, spawned.Count);

            spawner.Tick(0.5f, grid, belt, new RandomSource(7));
            Assert.AreEqual(2, spawned.Count);

            spawner.Tick(0.1f, grid, belt, new RandomSource(7));
            Assert.AreEqual(2, spawned.Count);
        }

        [Test]
        public void Tick_WhenDisabled_DoesNotSpawn()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f) { Enabled = false };
            var spawned = new List<Creature>();
            spawner.Spawned += c => spawned.Add(c);

            spawner.Tick(5f, grid, Belt(), new RandomSource(3));

            Assert.AreEqual(0, spawned.Count);
        }

        [Test]
        public void SpawnNow_ReturnsNullWhenPatternIsComplete()
        {
            var level = Level();
            var grid = new GridModel(level);
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    grid.TryPaint(x, y, grid.TargetColorAt(x, y));
                }
            }

            var spawner = new CreatureSpawner(0.5f);

            Assert.IsNull(spawner.SpawnNow(grid, Belt(), new RandomSource(9)));
        }

        [Test]
        public void Interval_IsClampedToPositiveValue()
        {
            var spawner = new CreatureSpawner(-1f);

            Assert.Greater(spawner.Interval, 0f);
        }

        [Test]
        public void SpawnNeverUsesAColorWithZeroRemaining()
        {
            var grid = new GridModel(Level());
            // Tous les pixels rouges sont peints : la couleur rouge est épuisée.
            grid.TryPaint(0, 0, Red);
            grid.TryPaint(1, 0, Red);

            var spawner = new CreatureSpawner(0.5f);
            var random = new RandomSource(11);

            for (var i = 0; i < 200; i++)
            {
                var creature = spawner.SpawnNow(grid, Belt(), random);
                Assert.IsNotNull(creature);
                Assert.AreEqual(Green, creature.Color, "Une couleur épuisée a été tirée au spawn.");
            }
        }

        [Test]
        public void Weight_IsRemainingMinusCreaturesOnBelt()
        {
            // 2 pixels rouges, 1 pixel vert.
            var level = LevelBuilder.FromRows(
                "weights",
                "Weights",
                new[] { "112" },
                new[] { "#FF0000", "#00FF00" },
                1f);
            var grid = new GridModel(level);
            var spawner = new CreatureSpawner(1f);
            var belt = Belt(Red, Red);

            Assert.AreEqual(0, spawner.GetWeight(grid, belt, Red));
            Assert.AreEqual(1, spawner.GetWeight(grid, belt, Green));
            Assert.AreEqual(1, spawner.GetTotalWeight(grid, belt));
        }

        [Test]
        public void Weight_IsNeverNegative()
        {
            var level = LevelBuilder.FromRows(
                "weights",
                "Weights",
                new[] { "11" },
                new[] { "#FF0000" },
                1f);
            var grid = new GridModel(level);
            var spawner = new CreatureSpawner(1f);

            Assert.AreEqual(0, spawner.GetWeight(grid, Belt(Red, Red, Red, Red), Red));
        }

        [Test]
        public void SumZero_NoSpawnOn100Ticks()
        {
            // Un seul pixel rouge, déjà occupé par une créature rouge sur le tapis.
            var level = LevelBuilder.FromRows(
                "zero",
                "Zero",
                new[] { "1" },
                new[] { "#FF0000" },
                0.5f);
            var grid = new GridModel(level);
            var spawner = new CreatureSpawner(0.5f);
            var belt = Belt(Red);
            var spawned = 0;
            spawner.Spawned += _ => spawned++;

            for (var i = 0; i < 100; i++)
            {
                spawner.Tick(0.5f, grid, belt, new RandomSource(i + 1));
            }

            Assert.AreEqual(0, spawned, "Aucune créature ne doit spawner quand la somme des poids est nulle.");
        }

        [Test]
        public void ProportionalDraw_RespectsWeights()
        {
            // 3 rouges + 1 vert : le rouge doit sortir ~75 % du temps.
            var level = LevelBuilder.FromRows(
                "proportion",
                "Proportion",
                new[] { "111", "2.." },
                new[] { "#FF0000", "#00FF00" },
                1f);
            var grid = new GridModel(level);
            var spawner = new CreatureSpawner(1f);
            var random = new RandomSource(2024);
            var belt = Belt();

            const int draws = 4000;
            var reds = 0;
            for (var i = 0; i < draws; i++)
            {
                var creature = spawner.SpawnNow(grid, belt, random);
                Assert.IsNotNull(creature);
                if (creature.Color.Equals(Red))
                {
                    reds++;
                }
            }

            var ratio = (double)reds / draws;
            Assert.That(ratio, Is.InRange(0.72, 0.78), "Ratio rouge observé : " + ratio);
        }

        [Test]
        public void CountOnBelt_CountsOnlyMatchingCreatures()
        {
            var belt = Belt(Red, Green, Red, Red);

            Assert.AreEqual(3, CreatureSpawner.CountOnBelt(belt, Red));
            Assert.AreEqual(1, CreatureSpawner.CountOnBelt(belt, Green));
            Assert.AreEqual(0, CreatureSpawner.CountOnBelt(null, Red));
        }

        [Test]
        public void CountRemaining_CountsOnlyUnpaintedTargets()
        {
            var grid = new GridModel(Level());

            Assert.AreEqual(2, CreatureSpawner.CountRemaining(grid, Red));
            Assert.AreEqual(2, CreatureSpawner.CountRemaining(grid, Green));

            grid.TryPaint(0, 0, Red);

            Assert.AreEqual(1, CreatureSpawner.CountRemaining(grid, Red));
        }
    }
}
