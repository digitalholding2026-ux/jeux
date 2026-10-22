using System;

namespace ColorFlow.Core
{
    /// <summary>
    /// Orchestrateur de la boucle de jeu : spawn automatique, tap, résolution,
    /// score, combos, objectifs et transitions d'état. Pur C# : le MonoBehaviour
    /// ne fait que lui passer le temps et les entrées.
    /// </summary>
    public sealed class GameController
    {
        private readonly IRandomSource _random;
        private GameState _state;
        private float _comboTimer;
        private RgbaColor _lastComboColor;
        private int _comboChain;
        private int _multiplier = 1;

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

        /// <summary>Score courant (change après chaque tap réussi).</summary>
        public event Action<int> ScoreChanged;

        /// <summary>Multiplicateur de combo courant (1 = pas de combo).</summary>
        public event Action<int> ComboChanged;

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

        public int Score { get; private set; }

        /// <summary>Multiplicateur appliqué au prochain tap (1 à 4).</summary>
        public int CurrentMultiplier => _multiplier;

        /// <summary>Temps écoulé depuis le début de la partie.</summary>
        public float ElapsedTime { get; private set; }

        /// <summary>Temps restant (0 si le niveau n'a pas de limite).</summary>
        public float TimeRemaining { get; private set; }

        public bool HasTimeLimit => GoalRules.HasTimeLimit(Level.GoalType);

        /// <summary>Vrai si l'objectif du niveau est rempli à cet instant.</summary>
        public bool IsGoalReached => GoalRules.IsReached(Level.GoalType, Grid.IsComplete, Score, Level.TargetScore);

        /// <summary>Avance la simulation (spawn automatique, chrono, combo).</summary>
        public void Tick(float deltaTime)
        {
            if (_state != GameState.Playing || Grid == null || deltaTime <= 0f)
            {
                return;
            }

            ElapsedTime += deltaTime;

            if (HasTimeLimit)
            {
                TimeRemaining -= deltaTime;
                if (TimeRemaining <= 0f)
                {
                    TimeRemaining = 0f;
                    Spawner.Enabled = false;
                    State = GameState.Lost;
                    return;
                }
            }

            UpdateCombo(deltaTime);
            Spawner.Tick(deltaTime, Grid, Conveyor.Items, _random);
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

            var result = Resolver.Resolve(creature, Grid, Conveyor.Items);
            if (!result.IsSuccess)
            {
                ResetCombo();
                return Reject(creature, result.Failure);
            }

            Conveyor.TryRemove(creature);

            var multiplier = RegisterCombo(creature);
            var points = ScoreRules.PointsFor(result.PaintedCount, multiplier);
            if (points > 0)
            {
                Score += points;
                ScoreChanged?.Invoke(Score);
            }

            PaintResolved?.Invoke(creature, result);

            EvaluateGoalAfterAction();
            return result;
        }

        /// <summary>Génère une créature immédiatement (tests, debug).</summary>
        public Creature SpawnNow()
        {
            if (_state != GameState.Playing)
            {
                return null;
            }

            return Spawner.SpawnNow(Grid, Conveyor.Items, _random);
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

            Score = 0;
            _comboChain = 0;
            _comboTimer = 0f;
            _lastComboColor = RgbaColor.Transparent;
            _multiplier = 1;
            ElapsedTime = 0f;
            TimeRemaining = HasTimeLimit ? Level.TimeLimitSeconds : 0f;

            _state = GameState.Playing;
            LevelRestarted?.Invoke();
            ScoreChanged?.Invoke(Score);
            ComboChanged?.Invoke(1);
            StateChanged?.Invoke(_state);
        }

        /// <summary>Change de niveau et relance une partie.</summary>
        public void LoadLevel(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Restart();
        }

        private void EvaluateGoalAfterAction()
        {
            if (IsGoalReached)
            {
                Spawner.Enabled = false;
                State = GameState.Won;
                return;
            }

            // Objectif de score : si le motif est rempli avant le score cible,
            // plus rien ne peut rapporter de points → objectif manqué.
            if (Level.GoalType == GoalType.ScoreTarget && Grid.IsComplete)
            {
                Spawner.Enabled = false;
                State = GameState.Lost;
            }
        }

        /// <summary>Met à jour le combo et retourne le multiplicateur de l'action.</summary>
        private int RegisterCombo(Creature creature)
        {
            var sameColor = _comboTimer > 0f && _lastComboColor.Equals(creature.ComboColor);
            _comboChain = sameColor ? _comboChain + 1 : 1;
            _comboTimer = ScoreRules.ComboWindowSeconds;
            _lastComboColor = creature.ComboColor;

            var multiplier = ScoreRules.MultiplierFor(_comboChain);
            if (multiplier != _multiplier)
            {
                _multiplier = multiplier;
                ComboChanged?.Invoke(_multiplier);
            }

            return multiplier;
        }

        private void UpdateCombo(float deltaTime)
        {
            if (_comboTimer <= 0f)
            {
                return;
            }

            _comboTimer -= deltaTime;
            if (_comboTimer > 0f)
            {
                return;
            }

            _comboTimer = 0f;
            ResetCombo();
        }

        private void ResetCombo()
        {
            _comboChain = 0;
            _comboTimer = 0f;
            if (_multiplier == 1)
            {
                return;
            }

            _multiplier = 1;
            ComboChanged?.Invoke(1);
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
