using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class GameControllerTests
    {
        private static LevelDefinition Grid3x3()
        {
            return LevelBuilder.FromRows(
                "grille",
                "Grille",
                new[]
                {
                    "111",
                    "111",
                    "111"
                },
                new[] { "#FF0000" },
                0.5f);
        }

        private static LevelDefinition Pixel()
        {
            return LevelBuilder.FromRows(
                "pixel",
                "Pixel",
                new[] { "1" },
                new[] { "#00FF00" },
                1f);
        }

        private static GameController NewGame(LevelDefinition level)
        {
            // Les tests du Sprint 0 vérifient la boucle « un tap = un pixel » :
            // les créatures spéciales sont désactivées ici, elles ont leurs
            // propres tests dans SprintTwoTests.
            var spawner = new CreatureSpawner(level.SpawnInterval) { SpecialChancePercent = 0 };
            return new GameController(level, new RandomSource(123), ConveyorQueue<Creature>.DefaultCapacity, spawner);
        }

        private static GameController NoSpecialGame(LevelDefinition level, IRandomSource random)
        {
            var spawner = new CreatureSpawner(level.SpawnInterval) { SpecialChancePercent = 0 };
            return new GameController(level, random, ConveyorQueue<Creature>.DefaultCapacity, spawner);
        }

        /// <summary>Aléa figé : permet de garantir la couleur générée.</summary>
        private sealed class FixedRandom : IRandomSource
        {
            private readonly int _value;

            public FixedRandom(int value)
            {
                _value = value;
            }

            public int Range(int minInclusive, int maxExclusive)
            {
                return _value;
            }
        }

        [Test]
        public void NewGame_StartsInPlayingState()
        {
            var game = NewGame(Grid3x3());

            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(9, game.TargetPixels);
            Assert.AreEqual(0, game.PaintedPixels);
            Assert.AreEqual(0, game.Conveyor.Count);
        }

        [Test]
        public void SpawnNow_EnqueuesCreature()
        {
            var game = NewGame(Grid3x3());

            var creature = game.SpawnNow();

            Assert.IsNotNull(creature);
            Assert.AreEqual(1, game.Conveyor.Count);
        }

        [Test]
        public void Tap_PaintsPixelAndRemovesCreature()
        {
            var game = NewGame(Grid3x3());
            var creature = game.SpawnNow();

            var result = game.Tap(creature);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, game.PaintedPixels);
            Assert.AreEqual(0, game.Conveyor.Count);
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [Test]
        public void Tap_NullCreature_ReturnsNoCreature()
        {
            var game = NewGame(Grid3x3());

            var result = game.Tap(null);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.NoCreature, result.Failure);
        }

        [Test]
        public void Tap_CreatureNotOnConveyor_IsRejected()
        {
            var game = NewGame(Grid3x3());

            var result = game.Tap(new Creature(99, RgbaColor.FromHex("#FF0000")));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.UnknownCreature, result.Failure);
            Assert.AreEqual(0, game.PaintedPixels);
        }

        [Test]
        public void SixthCreature_Overflows_AndLosesTheGame()
        {
            var game = NewGame(Grid3x3());

            for (var i = 0; i < 5; i++)
            {
                Assert.IsNotNull(game.SpawnNow());
            }

            game.SpawnNow();

            Assert.AreEqual(GameState.Lost, game.State);
            Assert.AreEqual(5, game.Conveyor.Count);
        }

        [Test]
        public void WhenLost_TapIsRejected()
        {
            var game = NewGame(Grid3x3());
            for (var i = 0; i < 6; i++)
            {
                game.SpawnNow();
            }

            var result = game.Tap(game.Conveyor[0]);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.GameNotPlaying, result.Failure);
        }

        [Test]
        public void PaintingTheLastPixel_WinsTheGame()
        {
            var game = NewGame(Pixel());
            var creature = game.SpawnNow();

            var result = game.Tap(creature);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.LevelCompleted);
            Assert.AreEqual(GameState.Won, game.State);
            Assert.IsTrue(game.Grid.IsComplete);
            Assert.IsFalse(game.Spawner.Enabled);
        }

        [Test]
        public void WhenWon_SpawnerNoLongerSpawns()
        {
            var game = NewGame(Pixel());
            game.Tap(game.SpawnNow());

            game.Tick(10f);

            Assert.IsNull(game.SpawnNow());
            Assert.AreEqual(0, game.Conveyor.Count);
        }

        [Test]
        public void Restart_ResetsGridAndState()
        {
            var game = NewGame(Grid3x3());
            game.Tap(game.SpawnNow());
            Assert.AreEqual(1, game.PaintedPixels);

            game.Restart();

            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(0, game.PaintedPixels);
            Assert.AreEqual(0, game.Conveyor.Count);
        }

        [Test]
        public void Events_AreRaisedForSpawnTapAndStateChange()
        {
            var game = NewGame(Grid3x3());
            var spawned = 0;
            var painted = 0;
            var stateChanges = 0;
            game.CreatureAdded += (c, i) => spawned++;
            game.PixelPainted += (x, y, c) => painted++;
            game.StateChanged += s => stateChanges++;

            var creature = game.SpawnNow();
            game.Tap(creature);

            Assert.AreEqual(1, spawned);
            Assert.AreEqual(1, painted);
            Assert.AreEqual(0, stateChanges);
        }

        [Test]
        public void Tap_RejectedWhenNoPixelOfThatColorRemains()
        {
            var level = LevelBuilder.FromRows(
                "test",
                "Test",
                new[] { "12" },
                new[] { "#FF0000", "#00FF00" },
                1f);
            var game = NoSpecialGame(level, new FixedRandom(0));

            var red = game.SpawnNow();
            Assert.IsTrue(red.Color.Equals(RgbaColor.FromHex("#FF0000")));

            // Le seul pixel rouge est peint : la couleur de la créature ne sert plus.
            game.Grid.TryPaint(0, 0, red.Color);

            var result = game.Tap(red);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.NoCompatiblePixel, result.Failure);
            Assert.AreEqual(1, game.PaintedPixels);
            Assert.AreEqual(1, game.Conveyor.Count);
        }
    }
}
