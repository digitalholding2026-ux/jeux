using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorFlow.Game.Presentation
{
    /// <summary>Scène Boot : verrouille le portrait, prépare l'audio et ouvre le menu.</summary>
    public sealed class BootController : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            AudioManager.Init();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
