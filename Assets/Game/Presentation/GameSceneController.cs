using ColorFlow.Core;
using ColorFlow.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Point d'entrée de la scène Game : construit la vue, branche les événements
    /// du <see cref="GameController"/> pur et lui transmet le temps et les taps.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private LevelData levelOverride = null;

        private GameController _game;
        private GridView _gridView;
        private ConveyorView _conveyorView;
        private HudView _hudView;

        public GameController Game => _game;

        public int PaintedPixels => _game != null ? _game.PaintedPixels : 0;

        private void Awake()
        {
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Canvas_Game");
            var root = UiFactory.CreateRect("Root", canvas.transform);

            var gridArea = UiFactory.CreateRect("GridArea", root);
            UiFactory.SetRect(
                gridArea,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 50f),
                new Vector2(1000f, 1000f));

            var conveyorArea = UiFactory.CreateRect("ConveyorArea", root);
            UiFactory.SetRect(
                conveyorArea,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(0f, 400f));

            _hudView = new HudView(root);
            _gridView = new GridView(gridArea);
            _conveyorView = new ConveyorView(conveyorArea);

            _hudView.ReplayRequested += RestartGame;
            _conveyorView.CreatureTapped += OnCreatureTapped;

            var level = LevelSelection.Current != null ? LevelSelection.Current : levelOverride;
            if (level == null)
            {
                level = LevelCatalog.First();
            }

            StartLevel(level);
        }

        private void Update()
        {
            if (_game == null)
            {
                return;
            }

            _game.Tick(Time.deltaTime);
            _hudView.Tick(Time.deltaTime);
        }

        /// <summary>Relance le niveau courant (bouton "Rejouer").</summary>
        public void RestartGame()
        {
            if (_game == null)
            {
                return;
            }

            _game.Restart();
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
                _game.CreatureAdded += OnConveyorChanged;
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
        }

        private void OnConveyorChanged(Creature creature, int index)
        {
            _conveyorView.Render(_game.Conveyor.Items);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Won || state == GameState.Lost)
            {
                _hudView.ShowResult(state);
            }
        }

        private void OnPaintResolved(Creature creature, PaintResult result)
        {
            if (!result.IsSuccess && result.Failure == PaintFailureReason.NoCompatiblePixel)
            {
                _hudView.ShowHint("Cette couleur n'est plus utile");
            }
        }

        private void OnCreatureTapped(Creature creature)
        {
            _game?.Tap(creature);
        }
    }
}
