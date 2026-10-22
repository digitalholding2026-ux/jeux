using ColorFlow.Core;
using ColorFlow.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Point d'entrée de la scène Game : construit le décor, branche les événements
    /// du <see cref="GameController"/> pur et déclenche tout le « juice »
    /// (particules, sons, haptique, secousse d'écran).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private LevelData levelOverride = null;

        private RectTransform _root;
        private RectTransform _shakeRoot;
        private RectTransform _particleLayer;

        private GameController _game;
        private GridView _gridView;
        private ConveyorView _conveyorView;
        private HudView _hudView;

        private Vector2 _lastTapPosition;
        private bool _hasLastTap;

        public GameController Game => _game;

        public int PaintedPixels => _game != null ? _game.PaintedPixels : 0;

        private void Awake()
        {
            AudioManager.Init();
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Canvas_Game");
            _root = UiFactory.CreateRect("Root", canvas.transform);
            BuildBackground();

            _shakeRoot = UiFactory.CreateRect("ShakeRoot", _root);
            UiFactory.SetRect(
                _shakeRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1080f, 1920f));

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

            _hudView = new HudView(_root);
            _gridView = new GridView(gridArea);
            _conveyorView = new ConveyorView(conveyorArea);

            _particleLayer = UiFactory.CreateRect("ParticleLayer", _root);
            _particleLayer.SetAsLastSibling();

            _hudView.ReplayRequested += RestartGame;
            _hudView.MenuRequested += BackToMenu;
            _conveyorView.CreatureTapped += OnCreatureTapped;

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
        }

        /// <summary>Relance le niveau courant (bouton « Rejouer »).</summary>
        public void RestartGame()
        {
            if (_game == null)
            {
                return;
            }

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

        public void TapFirstCreatureForTest()
        {
            if (_game == null || _game.Conveyor.Count == 0)
            {
                return;
            }

            _game.Tap(_game.Conveyor[0]);
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

                _hudView.SetLevelName(definition.DisplayName);
                RebuildViews();
                return;
            }

            _game.LoadLevel(definition);
            _hudView.SetLevelName(definition.DisplayName);
        }

        private void RebuildViews()
        {
            _gridView.Build(_game.Grid);
            _conveyorView.Render(_game.Conveyor.Items);
            _hudView.SetProgress(_game.PaintedPixels, _game.TargetPixels);
            _hudView.HideResult();
        }

        private void OnLevelRestarted()
        {
            RebuildViews();
        }

        private void OnPixelPainted(int x, int y, RgbaColor color)
        {
            _gridView.RefreshCell(x, y);
            _hudView.SetProgress(_game.PaintedPixels, _game.TargetPixels);

            if (_gridView.TryGetCellWorldPosition(x, y, out var world))
            {
                UiParticles.Sparkle(_particleLayer, _root.InverseTransformPoint(world), UiFactory.ToColor(color), 5);
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

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Won)
            {
                AudioManager.PlayWin();
                Haptics.Impact();
                _hudView.ShowResult(state);
            }
            else if (state == GameState.Lost)
            {
                Haptics.Impact();
                TweenRunner.Run(Tween.Shake(_shakeRoot, Vector2.zero, 26f, 0.5f));
                _hudView.ShowResult(state);
            }
            else
            {
                _hudView.HideResult();
            }
        }

        private void OnPaintResolved(Creature creature, PaintResult result)
        {
            if (creature == null)
            {
                return;
            }

            if (result.IsSuccess)
            {
                AudioManager.PlaySplat();
                Haptics.Tap();

                if (_hasLastTap)
                {
                    UiParticles.Splash(_particleLayer, _lastTapPosition, UiFactory.ToColor(creature.Color), 14, 1f);
                    _hasLastTap = false;
                }

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

        private void OnCreatureTapped(Creature creature)
        {
            if (_game == null)
            {
                return;
            }

            _hasLastTap = _conveyorView.TryGetSlotScreenCenter(creature, out var screen);
            if (_hasLastTap)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out _lastTapPosition);
            }

            _game.Tap(creature);
        }
    }
}
