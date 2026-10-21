using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorFlow.Game.Presentation
{
    /// <summary>Scène Boot : verrouille le portrait et ouvre le menu.</summary>
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
        }

        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
