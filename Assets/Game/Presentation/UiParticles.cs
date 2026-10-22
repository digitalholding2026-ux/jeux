using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Mini système de particules 100 % UI (Images animées par le code).
    /// Il remplace un ParticleSystem 3D : l'UI est dessinée en Screen Space Overlay,
    /// donc un ParticleSystem classique passerait derrière le fond. Ici les
    /// éclaboussures et les confettis restent toujours au-dessus du jeu.
    /// </summary>
    public sealed class UiParticles : MonoBehaviour
    {
        private struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Color Color;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Gravity;
            public float Life;
            public float Age;
            public float Angle;
            public float Spin;
            public float Shrink;
        }

        private readonly List<Particle> _items = new List<Particle>();
        private int _pendingSpawns;

        /// <summary>Éclaboussure de peinture ronde.</summary>
        public static void Splash(RectTransform parent, Vector2 position, Color color, int count = 14, float power = 1f)
        {
            if (parent == null)
            {
                return;
            }

            var host = CreateHost(parent);
            host.EmitSplash(position, color, count, power);
        }

        /// <summary>Petite gerbe d'étoiles (pixel peint, victoire locale).</summary>
        public static void Sparkle(RectTransform parent, Vector2 position, Color color, int count = 6)
        {
            if (parent == null)
            {
                return;
            }

            var host = CreateHost(parent);
            host.EmitSparkle(position, color, count);
        }

        /// <summary>Pluie de confettis sur tout l'écran.</summary>
        public static void Confetti(RectTransform parent, int count = 90)
        {
            if (parent == null || parent.gameObject == null)
            {
                return;
            }

            var host = CreateHost(parent);
            host.StartCoroutine(host.EmitConfettiRoutine(count));
        }

        private static UiParticles CreateHost(RectTransform parent)
        {
            var go = new GameObject("Particles", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            UiFactory.Stretch(rect, 0f, 0f, 0f, 0f);
            rect.SetAsLastSibling();
            var host = go.AddComponent<UiParticles>();
            return host;
        }

        private void EmitSplash(Vector2 origin, Color color, int count, float power)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var speed = Random.Range(280f, 520f) * power;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                velocity.y += 120f;

                var tint = i % 3 == 0 ? JuicePalette.Lighten(color, 0.35f) : color;
                var size = Random.Range(14f, 34f);
                Add(origin, velocity, Random.Range(0.35f, 0.65f), tint, ArtFactory.Circle(64), new Vector2(size, size), 1250f, Random.Range(-220f, 220f), 0.35f);
            }
        }

        private void EmitSparkle(Vector2 origin, Color color, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count + Random.Range(-0.2f, 0.2f);
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(160f, 320f);
                var tint = Color.Lerp(color, Color.white, 0.35f);
                var size = Random.Range(18f, 30f);
                Add(origin, velocity, Random.Range(0.25f, 0.45f), tint, ArtFactory.Star(64, 5, 0.42f), new Vector2(size, size), 320f, Random.Range(-260f, 260f), 0.2f);
            }
        }

        private IEnumerator EmitConfettiRoutine(int count)
        {
            var width = 1100f;
            var top = 1020f;
            _pendingSpawns += count;

            for (var i = 0; i < count; i++)
            {
                var color = JuicePalette.CreatureColor(Random.Range(0, JuicePalette.Creatures.Length));
                if (Random.value < 0.25f)
                {
                    color = Color.white;
                }

                var origin = new Vector2(Random.Range(-width * 0.5f, width * 0.5f), top + Random.Range(0f, 260f));
                var velocity = new Vector2(Random.Range(-110f, 110f), Random.Range(-240f, -140f));
                var size = new Vector2(Random.Range(12f, 26f), Random.Range(8f, 16f));
                var spin = Random.Range(-420f, 420f);
                var sprite = Random.value < 0.3f ? ArtFactory.Circle(32) : ArtFactory.RoundedRect(4f, 32);
                Add(origin, velocity, Random.Range(2.2f, 3.6f), color, sprite, size, 300f, spin, 1f);
                _pendingSpawns--;
                yield return new WaitForSeconds(Random.Range(0.008f, 0.03f));
            }

            _pendingSpawns = 0;
        }

        private void Add(
            Vector2 position,
            Vector2 velocity,
            float life,
            Color color,
            Sprite sprite,
            Vector2 size,
            float gravity,
            float spin,
            float shrink)
        {
            var go = new GameObject("p", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            _items.Add(new Particle
            {
                Rect = rect,
                Image = image,
                Color = color,
                Position = position,
                Velocity = velocity,
                Gravity = gravity,
                Life = life,
                Age = 0f,
                Angle = rect.localEulerAngles.z,
                Spin = spin,
                Shrink = shrink
            });
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var particle = _items[i];
                particle.Age += deltaTime;

                if (particle.Age >= particle.Life || particle.Rect == null)
                {
                    if (particle.Rect != null)
                    {
                        Destroy(particle.Rect.gameObject);
                    }

                    _items.RemoveAt(i);
                    continue;
                }

                particle.Velocity.y -= particle.Gravity * deltaTime;
                particle.Position += particle.Velocity * deltaTime;
                particle.Angle += particle.Spin * deltaTime;

                var progress = particle.Age / particle.Life;
                particle.Rect.anchoredPosition = particle.Position;
                particle.Rect.localRotation = Quaternion.Euler(0f, 0f, particle.Angle);
                particle.Rect.localScale = Vector3.one * Mathf.Lerp(1f, particle.Shrink, progress);

                var color = particle.Color;
                color.a *= 1f - progress * progress;
                particle.Image.color = color;

                _items[i] = particle;
            }

            if (_items.Count == 0 && _pendingSpawns <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
