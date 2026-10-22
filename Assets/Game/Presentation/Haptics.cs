using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Retour haptique centralisé. <see cref="Handheld.Vibrate"/> ajoute
    /// automatiquement la permission Android VIBRATE au manifeste fusionné.
    /// </summary>
    public static class Haptics
    {
        /// <summary>Permet de couper les vibrations (accessibilité, tests).</summary>
        public static bool Enabled = true;

        private static float _lastVibration;

        public static void Tap()
        {
            Vibrate(0.06f);
        }

        public static void Impact()
        {
            Vibrate(0.12f);
        }

        private static void Vibrate(float minimumInterval)
        {
            if (!Enabled || !Application.isMobilePlatform)
            {
                return;
            }

            if (Time.unscaledTime - _lastVibration < minimumInterval)
            {
                return;
            }

            _lastVibration = Time.unscaledTime;
            Handheld.Vibrate();
        }
    }
}
