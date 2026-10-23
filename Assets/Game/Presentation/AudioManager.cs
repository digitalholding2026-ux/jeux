using System;
using System.Collections;
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
        private const int MusicSampleRate = 22050;
        private const float MusicLoopSeconds = 12.8f;
        private const float MusicVolume = 0.55f;

        private static AudioManager _instance;

        private AudioSource _source;
        private AudioSource _comboSource;
        private AudioSource _musicSource;
        private AudioClip _pop;
        private AudioClip _splat;
        private AudioClip _spawn;
        private AudioClip _win;
        private AudioClip _lose;
        private AudioClip _click;
        private AudioClip _error;
        private AudioClip _combo;
        private AudioClip _music;
        private Coroutine _musicFade;

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
            Instance.StartMusic();
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

            // Source dédiée au combo : le pitch monte avec le multiplicateur
            // sans transposer les autres effets en cours de lecture.
            _comboSource = gameObject.AddComponent<AudioSource>();
            _comboSource.playOnAwake = false;
            _comboSource.loop = false;
            _comboSource.spatialBlend = 0f;
            _comboSource.volume = 1f;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0f;

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

        /// <summary>
        /// Carillon cristallin dont la hauteur monte avec le combo : x2 = 1,0,
        /// +2 demi-tons par palier (x3, x4...), plafonné à une octave.
        /// </summary>
        public static void PlayCombo(int multiplier)
        {
            var instance = Instance;
            if (instance._combo == null || instance._comboSource == null)
            {
                return;
            }

            var step = Mathf.Clamp(multiplier - 2, 0, 6);
            instance._comboSource.pitch = Mathf.Pow(2f, step * 2f / 12f);
            instance._comboSource.PlayOneShot(instance._combo, 0.62f);
        }

        /// <summary>Démarre (ou reprend) la musique de fond, sans la redémarrer.</summary>
        public static void PlayMusic()
        {
            Instance.StartMusic();
        }

        /// <summary>Vrai quand la boucle de fond est réellement en lecture.</summary>
        public static bool IsMusicPlaying =>
            _instance != null && _instance._musicSource != null && _instance._musicSource.isPlaying;

        public static void StopMusic()
        {
            if (_instance == null || _instance._musicSource == null)
            {
                return;
            }

            if (_instance._musicFade != null)
            {
                _instance.StopCoroutine(_instance._musicFade);
                _instance._musicFade = null;
            }

            _instance._musicSource.Stop();
            _instance._musicSource.volume = 0f;
        }

        private void StartMusic()
        {
            if (_musicSource == null || _music == null || _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = _music;
            _musicSource.loop = true;
            _musicSource.volume = 0f;
            _musicSource.Play();
            if (_musicFade != null)
            {
                StopCoroutine(_musicFade);
            }

            _musicFade = StartCoroutine(FadeMusic(MusicVolume, 2.5f));
        }

        private IEnumerator FadeMusic(float target, float duration)
        {
            var start = _musicSource.volume;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _musicSource.volume = target;
            _musicFade = null;
        }

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
            _combo = Make("combo", 0.55f, ComboWave);
            _music = Make("music", MusicLoopSeconds, MusicSampleRate, MusicWave);
        }

        private static AudioClip Make(string name, float duration, Func<float, float> generator)
        {
            return Make(name, duration, SampleRate, generator);
        }

        private static AudioClip Make(
            string name,
            float duration,
            int sampleRate,
            Func<float, float> generator)
        {
            var sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var time = (float)i / sampleRate;
                data[i] = Mathf.Clamp(generator(time), -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
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

        /// <summary>Carillon « cristal » : fondamentale + harmoniques, attaque nette.</summary>
        private static float ComboWave(float t)
        {
            const float fundamental = 880f;
            var attack = Mathf.Clamp01(t / 0.004f);
            var decay = Mathf.Exp(-t * 8.5f);
            var body = Mathf.Sin(2f * Mathf.PI * fundamental * t)
                       + 0.45f * Mathf.Sin(2f * Mathf.PI * fundamental * 2.01f * t)
                       + 0.22f * Mathf.Sin(2f * Mathf.PI * fundamental * 3.02f * t);
            var sparkle = Mathf.Sin(2f * Mathf.PI * fundamental * 4.03f * t) * Mathf.Exp(-t * 18f) * 0.15f;
            return (body * decay + sparkle) * attack * 0.32f;
        }

        /// <summary>
        /// Boucle de 12,8 s : nappe douce (Do, La m, Fa, Sol) + arpège léger.
        /// Les deux extrémités sont fondues à zéro pour une boucle sans clic.
        /// </summary>
        private static float MusicWave(float t)
        {
            const float beat = 0.4f;
            var chords = new[]
            {
                new[] { 261.63f, 329.63f, 392.00f },
                new[] { 220.00f, 261.63f, 329.63f },
                new[] { 174.61f, 220.00f, 261.63f },
                new[] { 196.00f, 246.94f, 293.66f }
            };

            var bar = Mathf.Clamp((int)(t / (beat * 4f)), 0, 7) % 4;
            var chord = chords[bar];

            var pad = 0f;
            for (var i = 0; i < chord.Length; i++)
            {
                pad += Mathf.Sin(2f * Mathf.PI * chord[i] * 0.5f * t + i * 0.9f) * 0.05f;
            }

            var beatIndex = (int)(t / beat);
            var within = t - beatIndex * beat;
            var noteIndex = beatIndex % 4;
            var frequency = noteIndex < 3 ? chord[noteIndex] : chord[0] * 2f;
            var envelope = Mathf.Clamp01(within / 0.012f) * Mathf.Exp(-within * 6f);
            var arpeggio = Mathf.Sin(2f * Mathf.PI * frequency * within) * envelope * 0.09f;

            var edge = Mathf.Clamp01(Mathf.Min(t / 0.03f, (MusicLoopSeconds - t) / 0.05f));
            return (pad + arpeggio) * edge;
        }
    }
}
