using System;

namespace ColorFlow.Core
{
    /// <summary>
    /// Orchestrateur de la boucle de jeu : spawn automatique, tap, résolution,
    /// transitions d'état. Pur C# : le MonoBehaviour ne fait que lui passer le temps
    /// et les entrées.
    /// </summary>
    public sealed class GameController
    {
        private readonly IRandomSource _random;
        private GameState _state;

        public GameController(
            LevelDefinition level,
            IRandomSource random,
            int conveyorCapacity = ConveyorQueue<Creature>.DefaultCapacity,
            CreatureSpawner spawner = null,
            PaintResolver resolver = null)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            Conveyor = new ConveyorQueue<Creature>(Math.Max(1, conveyorCapacity));
            Spawner = spawner ?? new CreatureSpawner(level.SpawnInterval);
            Resolver = resolver ?? new PaintResolver();

            Conveyor.ItemAdded += OnCreatureAdded;
            Conveyor.ItemRemoved += OnCreatureRemoved;
            Conveyor.Overflow += OnConveyorOverflow;
            Spawner.Spawned += OnCreatureSpawned;

            Restart();
        }

        public event Action<GameState> StateChanged;

        public event Action<int, int, RgbaColor> PixelPainted;

        public event Action<Creature, int> CreatureAdded;

        public event Action<Creature, int> CreatureRemoved;

        public event Action<Creature, PaintResult> PaintResolved;

        public event Action LevelRestarted;

        public LevelDefinition Level { get; private set; }

        public GridModel Grid { get; private set; }

        public ConveyorQueue<Creature> Conveyor { get; }

        public CreatureSpawner Spawner { get; }

        public PaintResolver Resolver { get; }

        public GameState State
        {
            get => _state;
            private set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                StateChanged?.Invoke(_state);
            }
        }

        public int PaintedPixels => Grid.PaintedCount;

        public int RemainingPixels => Grid.RemainingCount;

        public int TargetPixels => Grid.TargetCount;

        /// <summary>Avance la simulation (spawn automatique).</summary>
        public void Tick(float deltaTime)
        {
            if (_state != GameState.Playing || Grid == null)
            {
                return;
            }

            Spawner.Tick(deltaTime, Grid, _random);
        }

        /// <summary>Tap sur une créature du tapis roulant.</summary>
        public PaintResult Tap(Creature creature)
        {
            if (creature == null)
            {
                return Reject(null, PaintFailureReason.NoCreature);
            }

            if (_state != GameState.Playing)
            {
                return Reject(creature, PaintFailureReason.GameNotPlaying);
            }

            if (Grid.IsComplete)
            {
                return Reject(creature, PaintFailureReason.LevelComplete);
            }

            if (!Conveyor.Contains(creature))
            {
                return Reject(creature, PaintFailureReason.UnknownCreature);
            }

            var result = Resolver.Resolve(creature, Grid);
            if (!result.IsSuccess)
            {
                return Reject(creature, result.Failure);
            }

            Conveyor.TryRemove(creature);
            PaintResolved?.Invoke(creature, result);

            if (result.LevelCompleted)
            {
                Spawner.Enabled = false;
                State = GameState.Won;
            }

            return result;
        }

        /// <summary>Génère une créature immédiatement (tests, debug).</summary>
        public Creature SpawnNow()
        {
            if (_state != GameState.Playing)
            {
                return null;
            }

            return Spawner.SpawnNow(Grid, _random);
        }

        /// <summary>Relance le niveau courant.</summary>
        public void Restart()
        {
            Grid = new GridModel(Level);
            Grid.PixelPainted += OnPixelPainted;

            Conveyor.Clear();

            Spawner.Interval = Level.SpawnInterval;
            Spawner.Reset();
            Spawner.Enabled = true;

            _state = GameState.Playing;
            LevelRestarted?.Invoke();
            StateChanged?.Invoke(_state);
        }

        /// <summary>Change de niveau et relance une partie.</summary>
        public void LoadLevel(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Restart();
        }

        private PaintResult Reject(Creature creature, PaintFailureReason reason)
        {
            var result = PaintResult.Fail(reason);
            PaintResolved?.Invoke(creature, result);
            return result;
        }

        private void OnCreatureSpawned(Creature creature)
        {
            if (_state != GameState.Playing)
            {
                return;
            }

            Conveyor.TryEnqueue(creature);
        }

        private void OnConveyorOverflow(Creature creature)
        {
            // Plus de place sur le tapis : la partie est perdue.
            Spawner.Enabled = false;
            State = GameState.Lost;
        }

        private void OnCreatureAdded(Creature creature, int index)
        {
            CreatureAdded?.Invoke(creature, index);
        }

        private void OnCreatureRemoved(Creature creature, int index)
        {
            CreatureRemoved?.Invoke(creature, index);
        }

        private void OnPixelPainted(int x, int y, RgbaColor color)
        {
            PixelPainted?.Invoke(x, y, color);
        }
    }
}
