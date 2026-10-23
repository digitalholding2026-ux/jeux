using System.Collections;
using ColorFlow.Core;
using ColorFlow.Game.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Point d'entrée de la scène Game : construit le décor, branche les événements
    /// du <see cref="GameController"/> pur et déclenche tout le « juice »
    /// (particules, sons, haptique, secousse d'écran).
    ///
    /// Sprint 4 : les blobs se glissent depuis le tapis vers la grille
    /// (IBeginDragHandler / IDragHandler / IEndDragHandler), les alignements
    /// explosent, le chrono reste visible et la victoire révèle le motif case
    /// par case.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private LevelData levelOverride = null;

        private RectTransform _root;
        private RectTransform _safeRoot;
        private RectTransform _shakeRoot;
        private RectTransform _particleLayer;

        private GameController _game;
        private GridView _gridView;
        private ConveyorView _conveyorView;
        private HudView _hudView;

        private bool _coinsAwarded;
        private Coroutine _revealRoutine;

        public GameController Game => _game;

        public int PaintedPixels => _game != null ? _game.PaintedPixels : 0;

        private void Awake()
        {
            AudioManager.Init();
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Canvas_Game");
            _root = UiFactory.CreateRect("Root", canvas.transform);
            BuildBackground();

            // Toute la zone jouable respecte les encoches et barres système.
            _safeRoot = UiFactory.CreateSafeArea("SafeArea", _root);
            _shakeRoot = UiFactory.CreateRect("ShakeRoot", _safeRoot);

            var gridArea = UiFactory.CreateRect("GridArea", _shakeRoot);
            UiFactory.SetRect(
                gridArea,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f),
                new Vector2(980f, 980f));

            var conveyorArea = UiFactory.CreateRect("ConveyorArea", _shakeRoot);
            UiFactory.SetRect(
                conveyorArea,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 40f),
                new Vector2(0f, 430f));

            _hudView = new HudView(_safeRoot);
            _gridView = new GridView(gridArea);
            _conveyorView = new ConveyorView(conveyorArea);

            _particleLayer = UiFactory.CreateRect("ParticleLayer", _root);
            _particleLayer.SetAsLastSibling();

            _hudView.ReplayRequested += RestartGame;
            _hudView.MenuRequested += BackToMenu;

            _conveyorView.DragStarted += OnDragStarted;
            _conveyorView.DragMoved += OnDragMoved;
            _conveyorView.DragEnded += OnDragEnded;
            _conveyorView.SlotTapped += OnSlotTapped;

            var level = LevelSelection.Current != null ? LevelSelection.Current : levelOverride;
            if (level == null)
            {
                level = LevelCatalog.First();
            }

            StartLevel(level);
            SceneTransition.FadeIn();
        }

        private void BuildBackground()
        {
            var background = UiFactory.CreateImage("Background", _root, Color.white);
            background.sprite = ArtFactory.VerticalGradient(JuicePalette.SkyBottom, JuicePalette.SkyTop, 256);
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.transform.SetAsFirstSibling();

            var sun = UiFactory.CreateGlow("Sun", _root, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.SetRect(
                sun.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(120f, 120f),
                new Vector2(620f, 620f));

            var sunDisc = UiFactory.CreateDot("SunDisc", _root, JuicePalette.WithAlpha(JuicePalette.Sun, 0.85f));
            UiFactory.SetRect(
                sunDisc.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(150f, 190f),
                new Vector2(220f, 220f));

            var cloudLeft = UiFactory.CreateGlow("CloudLeft", _root, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.SetRect(
                cloudLeft.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(-120f, -120f),
                new Vector2(520f, 380f));

            var cloudRight = UiFactory.CreateGlow("CloudRight", _root, new Color(1f, 1f, 1f, 0.25f));
            UiFactory.SetRect(
                cloudRight.rectTransform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(150f, -220f),
                new Vector2(560f, 420f));
        }

        private void Update()
        {
            if (_game == null)
            {
                return;
            }

            _game.Tick(Time.deltaTime);
            _hudView.Tick(Time.deltaTime);
            _conveyorView.Tick(Time.deltaTime);

            if (_game.HasTimeLimit)
            {
                _hudView.SetTimeRemaining(_game.TimeRemaining);
            }
            else
            {
                _hudView.SetElapsed(_game.ElapsedTime);
            }
        }

        /// <summary>Relance le niveau courant (bouton « Rejouer »).</summary>
        public void RestartGame()
        {
            if (_game == null)
            {
                return;
            }

            StopReveal();
            _game.Restart();
        }

        private void BackToMenu()
        {
            SceneTransition.Load("MainMenu");
        }

        // --- Accès utilisés par les tests PlayMode ---

        public void SpawnCreatureForTest()
        {
            if (_game != null)
            {
                _game.SpawnNow();
            }
        }

        public Button GetCreatureButton(int index)
        {
            return _conveyorView.GetButton(index);
        }

        public Image GetCreatureAccessory(int index)
        {
            return _conveyorView.GetAccessory(index);
        }

        /// <summary>Simule un vrai drag &amp; drop du premier blob vers une case libre.</summary>
        public bool DragFirstCreatureToValidCellForTest()
        {
            if (_game == null || _game.Conveyor.Count == 0)
            {
                return false;
            }

            var creature = _game.Conveyor[0];
            for (var y = 0; y < _game.Grid.Height; y++)
            {
                for (var x = 0; x < _game.Grid.Width; x++)
                {
                    if (!_game.IsValidDropTarget(creature, x, y) ||
                        !_gridView.TryGetCellWorldPosition(x, y, out var world))
                    {
                        continue;
                    }

                    var screen = RectTransformUtility.WorldToScreenPoint(null, world);
                    var pointer = new PointerEventData(EventSystem.current) { position = screen };
                    var before = _game.Conveyor.Count;

                    _conveyorView.BeginDrag(0, pointer);
                    _conveyorView.DragTo(pointer);
                    _conveyorView.EndDrag(pointer);

                    return _game.Conveyor.Count < before;
                }
            }

            return false;
        }

        private void StartLevel(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("Aucun niveau jouable : catalogue Resources/Levels vide.");
                return;
            }

            if (!data.TryBuild(out var definition, out var error))
            {
                Debug.LogError("Niveau invalide : " + error);
                return;
            }

            if (_game == null)
            {
                _game = new GameController(definition, new RandomSource());
                _game.PixelPainted += OnPixelPainted;
                _game.CreatureAdded += OnCreatureAdded;
                _game.CreatureRemoved += OnConveyorChanged;
                _game.StateChanged += OnStateChanged;
                _game.LevelRestarted += OnLevelRestarted;
                _game.PaintResolved += OnPaintResolved;
                _game.DropResolved += OnDropResolved;
                _game.DropRejected += OnDropRejected;
                _game.ScoreChanged += OnScoreChanged;
                _game.ComboChanged += OnComboChanged;

                ConfigureHud(definition);
                RebuildViews();
                return;
            }

            _game.LoadLevel(definition);
            ConfigureHud(definition);
        }

        /// <summary>Prépare le HUD pour le niveau : nom, objectif et chrono.</summary>
        private void ConfigureHud(LevelDefinition definition)
        {
            _hudView.SetLevelName(definition.DisplayName);
            _hudView.SetGoal(definition.GoalDescription, definition.TimeLimitSeconds > 0f);
            _hudView.SetScore(0);
            _hudView.HideCombo();
            if (definition.TimeLimitSeconds > 0f)
            {
                _hudView.SetTimeRemaining(definition.TimeLimitSeconds);
            }
            else
            {
                _hudView.SetElapsed(0f);
            }
        }

        private void OnScoreChanged(int score)
        {
            _hudView.SetScore(score);
        }

        private void OnComboChanged(int multiplier)
        {
            if (multiplier >= 2)
            {
                AudioManager.PlayCombo(multiplier);
                _hudView.ShowCombo(multiplier);
            }
            else
            {
                _hudView.HideCombo();
            }
        }

        private void RebuildViews()
        {
            _gridView.Build(_game.Grid);
            _conveyorView.Render(_game.Conveyor.Items);
            _hudView.SetProgress(_game.PaintedPixels, _game.TargetPixels);
            _hudView.HideResult();
            _coinsAwarded = false;
        }

        private void OnLevelRestarted()
        {
            StopReveal();
            RebuildViews();
        }

        private void OnPixelPainted(int x, int y, RgbaColor color)
        {
            _gridView.RefreshCell(x, y);
            _hudView.SetProgress(_game.PaintedPixels, _game.TargetPixels);

            if (_gridView.TryGetCellWorldPosition(x, y, out var world))
            {
                UiParticles.Sparkle(_particleLayer, _root.InverseTransformPoint(world), UiFactory.ToColor(color), 4);
            }
        }

        private void OnCreatureAdded(Creature creature, int index)
        {
            _conveyorView.Render(_game.Conveyor.Items);
            AudioManager.PlaySpawn();
        }

        private void OnConveyorChanged(Creature creature, int index)
        {
            _conveyorView.Render(_game.Conveyor.Items);
        }

        // --- Sprint 4 : glisser-déposer ---------------------------------------

        private void OnDragStarted(Creature creature)
        {
            ClearHighlight();
        }

        private void OnDragMoved(Creature creature, Vector2 screenPosition)
        {
            ClearHighlight();
            if (_game == null || creature == null)
            {
                return;
            }

            if (!_gridView.TryScreenPointToCell(screenPosition, null, out var x, out var y))
            {
                return;
            }

            var valid = _game.IsValidDropTarget(creature, x, y);
            _gridView.SetHighlight(x, y, valid ? JuicePalette.Mint : JuicePalette.Pink);
        }

        private void OnDragEnded(Creature creature, Vector2 screenPosition)
        {
            ClearHighlight();
            if (_game == null || creature == null)
            {
                return;
            }

            if (!_gridView.TryScreenPointToCell(screenPosition, null, out var x, out var y))
            {
                // Lâché hors de la grille : retour au tapis avec rebond.
                _conveyorView.PlayDropReturn(creature);
                _hudView.ShowHint("Lâche le blob sur une case du motif !", 1.3f);
                return;
            }

            // Le GameController valide et déclenche DropResolved / DropRejected.
            _game.Drop(creature, x, y);
        }

        private void OnSlotTapped(Creature creature)
        {
            _hudView.ShowHint("Glisse le blob du tapis vers une case du motif !", 1.5f);
            _conveyorView.PlayTapBounce(creature);
        }

        private void ClearHighlight()
        {
            _gridView.ClearHighlights();
        }

        private void OnDropResolved(Creature creature, DropResult result)
        {
            if (result == null || creature == null)
            {
                return;
            }

            var color = UiFactory.ToColor(creature.Color);

            if (result.Matched && result.MatchedCells.Count > 0)
            {
                // Alignement : explosion, son, vibration et secousse d'écran.
                AudioManager.PlaySplat();
                Haptics.Impact();
                TweenRunner.Run(Tween.Shake(
                    _shakeRoot,
                    Vector2.zero,
                    Mathf.Min(10f + result.MatchLength * 2f, 22f),
                    0.32f));

                for (var i = 0; i < result.MatchedCells.Count; i++)
                {
                    var cell = result.MatchedCells[i];
                    _gridView.RefreshCell(cell.X, cell.Y);

                    if (_gridView.TryGetCellWorldPosition(cell.X, cell.Y, out var world))
                    {
                        UiParticles.Splash(
                            _particleLayer,
                            _root.InverseTransformPoint(world),
                            color,
                            8 + result.MatchLength * 3,
                            1.25f);
                    }
                }

                if (result.HasSpecial)
                {
                    AudioManager.PlayPop();
                    _hudView.ShowHint(
                        result.SpecialKind == CreatureKind.Bomb
                            ? "Blob Bombe créé ! Glisse-le pour exploser une zone 3×3."
                            : "Blob Ligne créé ! Glisse-le pour peindre une ligne entière.",
                        2.6f);
                }
                else
                {
                    _hudView.ShowHint(result.PaintedCount + " cases peintes d'un coup !", 1.2f);
                }

                return;
            }

            if (result.PaintedCount > 0)
            {
                // Blob spécial lancé (bombe, ligne, arc-en-ciel).
                AudioManager.PlaySplat();
                Haptics.Impact();
                TweenRunner.Run(Tween.Shake(_shakeRoot, Vector2.zero, 15f, 0.36f));

                if (_gridView.TryGetCellWorldPosition(result.X, result.Y, out var impact))
                {
                    UiParticles.Splash(
                        _particleLayer,
                        _root.InverseTransformPoint(impact),
                        color,
                        18,
                        1.4f);
                }

                return;
            }

            // Pose simple sans alignement : petit feedback, le blob reste posé.
            AudioManager.PlayPop();
            Haptics.Tap();
            _gridView.RefreshCell(result.X, result.Y);

            if (_gridView.TryGetCellWorldPosition(result.X, result.Y, out var placed))
            {
                UiParticles.Sparkle(_particleLayer, _root.InverseTransformPoint(placed), color, 5);
            }

            _hudView.ShowHint("Pas d'alignement… prépare le prochain coup !", 1.1f);
        }

        private void OnDropRejected(Creature creature, DropFailureReason reason)
        {
            _conveyorView.PlayDropReturn(creature);
            AudioManager.PlayError();
            Haptics.Tap();

            switch (reason)
            {
                case DropFailureReason.CellAlreadyPainted:
                    _hudView.ShowHint("Cette case est déjà peinte !");
                    break;
                case DropFailureReason.CellOccupied:
                    _hudView.ShowHint("Cette case est déjà occupée !");
                    break;
                case DropFailureReason.NotInPattern:
                    _hudView.ShowHint("Cette case ne fait pas partie du motif !");
                    break;
                case DropFailureReason.NoEffect:
                    _hudView.ShowHint("Ce pouvoir n'a aucun effet ici.");
                    break;
                case DropFailureReason.GameNotPlaying:
                    break;
                default:
                    _hudView.ShowHint("Placement impossible.");
                    break;
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Won)
            {
                AudioManager.PlayWin();
                Haptics.Impact();
                MetaProgress.RecordWin(_game.Level.Id);

                StopReveal();
                _revealRoutine = StartCoroutine(RevealThenResult());
            }
            else if (state == GameState.Lost)
            {
                Haptics.Impact();
                TweenRunner.Run(Tween.Shake(_shakeRoot, Vector2.zero, 26f, 0.5f));
                _hudView.ShowResult(state, BuildObjectiveLine(state), BuildStats(state));
            }
            else
            {
                StopReveal();
                _hudView.HideResult();
            }
        }

        private void StopReveal()
        {
            if (_revealRoutine == null)
            {
                return;
            }

            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        /// <summary>Révélation spectaculaire du motif, case par case, avant l'écran de fin.</summary>
        private IEnumerator RevealThenResult()
        {
            _hudView.HideResult();
            _hudView.ShowHint("Motif révélé !", 1.4f);
            yield return new WaitForSeconds(0.2f);

            var grid = _game.Grid;
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (!grid.HasTargetAt(x, y) || !grid.IsPainted(x, y))
                    {
                        continue;
                    }

                    _gridView.PulseCell(x, y);
                    if (_gridView.TryGetCellWorldPosition(x, y, out var world))
                    {
                        UiParticles.Sparkle(
                            _particleLayer,
                            _root.InverseTransformPoint(world),
                            UiFactory.ToColor(grid.TargetColorAt(x, y)),
                            6);
                    }

                    yield return new WaitForSeconds(0.022f);
                }
            }

            AudioManager.PlayPop();
            yield return new WaitForSeconds(0.35f);

            _hudView.ShowResult(GameState.Won, BuildObjectiveLine(GameState.Won), BuildStats(GameState.Won));
            _revealRoutine = null;
        }

        /// <summary>Ligne « objectif atteint / manqué » de l'écran de fin.</summary>
        private string BuildObjectiveLine(GameState state)
        {
            var level = _game.Level;
            if (state == GameState.Won)
            {
                switch (level.GoalType)
                {
                    case GoalType.ScoreTarget:
                        return "Objectif atteint : " + level.TargetScore + " pts";
                    case GoalType.TimeAttack:
                        return "Objectif atteint : en " + Mathf.CeilToInt(_game.ElapsedTime) + " s";
                    default:
                        return "Objectif atteint : motif rempli";
                }
            }

            return GoalRules.DescribeFailure(level.GoalType, _game.Score, level.TargetScore, level.TimeLimitSeconds);
        }

        /// <summary>Ligne « score + pièces gagnées » ; crédite le portefeuille.</summary>
        private string BuildStats(GameState state)
        {
            var won = state == GameState.Won;
            var coins = EconomyRules.CoinsForRun(_game.Score, won);

            if (!_coinsAwarded)
            {
                MetaProgress.AddCoins(coins);
                _coinsAwarded = true;
            }

            return "Score " + _game.Score + "  ·  +" + coins + " pièces";
        }

        private void OnPaintResolved(Creature creature, PaintResult result)
        {
            // Parcours historique (Sprint 0-2) conservé pour les tests : l'UI
            // n'appelle plus Tap, mais les événements restent branchés.
            if (creature == null)
            {
                return;
            }

            if (result.IsSuccess)
            {
                AudioManager.PlaySplat();
                Haptics.Tap();
                _conveyorView.PlayTapBounce(creature);
                return;
            }

            _conveyorView.PlayRefuse(creature);
            AudioManager.PlayError();
            Haptics.Tap();

            if (result.Failure == PaintFailureReason.NoCompatiblePixel)
            {
                _hudView.ShowHint("Cette couleur n'est plus utile !");
            }
        }
    }
}
