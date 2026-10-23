using System.Collections;
using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>Fonction d'interpolation : 0 -> 1.</summary>
    public delegate float EaseFunction(float t);

    /// <summary>Courbes d'animation maison (aucune dépendance externe).</summary>
    public static class Ease
    {
        public static float Linear(float t)
        {
            return t;
        }

        public static float InQuad(float t)
        {
            return t * t;
        }

        public static float OutQuad(float t)
        {
            var inv = 1f - t;
            return 1f - inv * inv;
        }

        public static float InOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        }

        public static float OutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>Dépasse légèrement sa cible puis revient : idéal pour un « pop ».</summary>
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        /// <summary>Rebond élastique amorti : arrivée de créature.</summary>
        public static float OutElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }

            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }

            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }

    /// <summary>
    /// Hôte unique des coroutines d'animation. Persiste entre les scènes pour que
    /// les transitions et les effets ne soient jamais coupés par un chargement.
    /// </summary>
    public sealed class TweenRunner : MonoBehaviour
    {
        private static TweenRunner _instance;

        public static TweenRunner Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                var host = new GameObject("~TweenRunner");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<TweenRunner>();
                return _instance;
            }
        }

        public static Coroutine Run(IEnumerator routine)
        {
            if (routine == null)
            {
                return null;
            }

            var runner = Instance;
            return runner != null ? runner.StartCoroutine(routine) : null;
        }

        public static void Stop(Coroutine routine)
        {
            if (routine == null || _instance == null)
            {
                return;
            }

            _instance.StopCoroutine(routine);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    /// <summary>Petites animations réutilisables, écrites à la main.</summary>
    public static class Tween
    {
        public static IEnumerator Delay(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration, EaseFunction ease = null)
        {
            if (target == null)
            {
                yield break;
            }

            ease = ease ?? Ease.OutQuad;
            target.localScale = from;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, ease(t));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = to;
            }
        }

        public static IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration, EaseFunction ease = null)
        {
            if (target == null)
            {
                yield break;
            }

            ease = ease ?? Ease.OutCubic;
            target.anchoredPosition = from;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(t));
                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = to;
            }
        }

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration, EaseFunction ease = null)
        {
            if (group == null)
            {
                yield break;
            }

            ease = ease ?? Ease.OutQuad;
            group.alpha = from;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (group == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.LerpUnclamped(from, to, ease(t));
                yield return null;
            }

            if (group != null)
            {
                group.alpha = to;
            }
        }

        /// <summary>Grossit puis revient à l'échelle d'origine (feedback de tap).</summary>
        public static IEnumerator Pop(Transform target, float overshoot = 0.35f, float duration = 0.18f)
        {
            if (target == null)
            {
                yield break;
            }

            // Échelle de repos = 1 : deux « pop » simultanés ne doivent jamais
            // s'empiler (sinon la cible grossit sans fin).
            var baseScale = Vector3.one;
            target.localScale = baseScale;
            var peak = baseScale * (1f + overshoot);
            var half = duration * 0.45f;

            var elapsed = 0f;
            while (elapsed < half)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(baseScale, peak, Ease.OutQuad(Mathf.Clamp01(elapsed / half)));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration - half)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                target.localScale = Vector3.LerpUnclamped(peak, baseScale, Ease.OutQuad(Mathf.Clamp01(elapsed / (duration - half))));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = baseScale;
            }
        }

        /// <summary>Apparition « pop » : 0 -> 1.15 -> 1.</summary>
        public static IEnumerator PopIn(Transform target, float duration = 0.26f)
        {
            if (target == null)
            {
                yield break;
            }

            var elapsed = 0f;
            target.localScale = Vector3.one * 0.05f;

            while (elapsed < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(0.05f, 1f, Ease.OutBack(t));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        /// <summary>Apparition « pop » de case : 0.5 -> 1.2 -> 1.0 en ~0.12 s.</summary>
        public static IEnumerator PopCell(Transform target, float duration = 0.12f)
        {
            if (target == null)
            {
                yield break;
            }

            const float from = 0.5f;
            const float peak = 1.2f;
            var up = duration * 0.45f;
            var down = duration - up;

            var elapsed = 0f;
            while (elapsed < up)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / up);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, peak, Ease.OutQuad(t));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < down)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / down);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(peak, 1f, Ease.OutQuad(t));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        /// <summary>Secousse horizontale amortie autour d'une position de repos.</summary>
        public static IEnumerator Shake(RectTransform target, Vector2 home, float amplitude = 22f, float duration = 0.45f)
        {
            if (target == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var falloff = 1f - Mathf.Clamp01(elapsed / duration);
                var offset = Mathf.Sin(elapsed * 58f) * amplitude * falloff;
                var vertical = Mathf.Sin(elapsed * 41f) * amplitude * 0.35f * falloff;
                target.anchoredPosition = home + new Vector2(offset, vertical);
                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = home;
            }
        }
    }
}
