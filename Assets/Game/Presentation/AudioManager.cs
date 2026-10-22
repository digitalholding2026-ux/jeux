using System;
using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Sons générés à l'exécution (aucun fichier audio importé).
    /// Chaque clip est synthétisé en sinus / bruit filtré puis mis en cache.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private static AudioManager _instance;

        private AudioSource _source;
        private AudioClip _pop;
        private AudioClip _splat;
        private AudioClip _spawn;
        private AudioClip _win;
        private AudioClip _lose;
        private AudioClip _click;
        private AudioClip _error;

        public static AudioManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                var host = new GameObject("~AudioManager");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<AudioManager>();
                return _instance;
            }
        }

        /// <summary>Force la création (et donc la synthèse) au démarrage du jeu.</summary>
        public static void Init()
        {
            var unused = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 1f;

            BuildClips();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void PlayPop() => Instance.PlayOneShot(Instance._pop, 0.85f);

        public static void PlaySplat() => Instance.PlayOneShot(Instance._splat, 0.9f);

        public static void PlaySpawn() => Instance.PlayOneShot(Instance._spawn, 0.35f);

        public static void PlayWin() => Instance.PlayOneShot(Instance._win, 0.95f);

        public static void PlayLose() => Instance.PlayOneShot(Instance._lose, 0.9f);

        public static void PlayClick() => Instance.PlayOneShot(Instance._click, 0.5f);

        public static void PlayError() => Instance.PlayOneShot(Instance._error, 0.7f);

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || _source == null)
            {
                return;
            }

            _source.PlayOneShot(clip, volume);
        }

        private void BuildClips()
        {
            _pop = Make("pop", 0.10f, PopWave);
            _splat = Make("splat", 0.18f, SplotchWave());
            _spawn = Make("spawn", 0.14f, SpawnWave);
            _click = Make("click", 0.05f, ClickWave);
            _error = Make("error", 0.22f, ErrorWave);
            _win = Make("win", 1.05f, WinWave);
            _lose = Make("lose", 0.75f, LoseWave);
        }

        private static AudioClip Make(string name, float duration, Func<float, float> generator)
        {
            var sampleCount = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var time = (float)i / SampleRate;
                data[i] = Mathf.Clamp(generator(time), -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>« Pop » aigu et joyeux : chirp montant + harmonique.</summary>
        private static float PopWave(float t)
        {
            const float duration = 0.10f;
            const float from = 760f;
            const float to = 1320f;
            var phase = 2f * Mathf.PI * (from * t + 0.5f * (to - from) / duration * t * t);
            var envelope = Mathf.Exp(-t * 26f);
            return (Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2.01f)) * envelope * 0.55f;
        }

        /// <summary>« Splotch » : bruit blanc filtré + corps grave qui descend.</summary>
        private static Func<float, float> SplotchWave()
        {
            var random = new System.Random(17);
            var smoothed = 0f;

            return time =>
            {
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                smoothed = Mathf.Lerp(smoothed, noise, 0.18f);
                var envelope = Mathf.Exp(-time * 17f);
                var body = Mathf.Sin(2f * Mathf.PI * (210f - 90f * Mathf.Clamp01(time / 0.18f)) * time);
                return (smoothed * 0.85f + body * 0.45f) * envelope * 0.8f;
            };
        }

        private static float SpawnWave(float t)
        {
            const float duration = 0.14f;
            const float from = 320f;
            const float to = 640f;
            var phase = 2f * Mathf.PI * (from * t + 0.5f * (to - from) / duration * t * t);
            return Mathf.Sin(phase) * Mathf.Exp(-t * 20f) * 0.45f;
        }

        private static float ClickWave(float t)
        {
            var envelope = Mathf.Exp(-t * 90f);
            return Mathf.Sin(2f * Mathf.PI * 1180f * t) * envelope * 0.35f;
        }

        /// <summary>Deux petits blips graves : action refusée.</summary>
        private static float ErrorWave(float t)
        {
            var segment = t < 0.09f ? 0f : 0.09f;
            var local = t - segment;
            var frequency = t < 0.09f ? 280f : 200f;
            var envelope = Mathf.Exp(-local * 28f);
            return Mathf.Sin(2f * Mathf.PI * frequency * local) * envelope * 0.5f;
        }

        /// <summary>Arpège montant (do majeur) + scintillement.</summary>
        private static float WinWave(float t)
        {
            var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float noteDuration = 0.13f;
            var value = 0f;

            for (var i = 0; i < notes.Length; i++)
            {
                var start = i * noteDuration;
                if (t < start)
                {
                    continue;
                }

                var local = t - start;
                var envelope = Mathf.Exp(-local * 9f);
                var phase = 2f * Mathf.PI * notes[i] * local;
                value += (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * envelope * 0.28f;
            }

            var shimmer = Mathf.Sin(2f * Mathf.PI * 2093f * t) * Mathf.Exp(-t * 6f) * 0.05f;
            return value + shimmer;
        }

        /// <summary>Descente de fréquence avec vibrato : défaite.</summary>
        private static float LoseWave(float t)
        {
            var from = 440f;
            var to = 98f;
            var sweep = Mathf.Lerp(from, to, Mathf.Clamp01(t / 0.75f));
            var vibrato = 1f + Mathf.Sin(2f * Mathf.PI * 7f * t) * 0.02f;
            var phase = 2f * Mathf.PI * sweep * vibrato * t;
            var envelope = Mathf.Exp(-t * 3.4f);
            var triangle = Mathf.Sin(phase) + 0.22f * Mathf.Sin(phase * 3f);
            return triangle * envelope * 0.55f;
        }
    }
}
