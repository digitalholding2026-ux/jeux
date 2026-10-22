using System;
using System.Collections.Generic;
using ColorFlow.Core;
using ColorFlow.Game.Data;
using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Fabrique tous les sprites du jeu par le code (Texture2D -> Sprite).
    /// Aucun asset image importé : formes arrondies, blobs, dégradés, étoiles,
    /// ombres douces et rayures sont calculés au démarrage puis mis en cache.
    /// </summary>
    public static class ArtFactory
    {
        private const int RoundedTextureSize = 128;
        private const int BlobTextureSize = 256;
        private const float PixelsPerUnit = 100f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Carré à coins arrondis, découpable en 9 tranches (Image.Type.Sliced).</summary>
        public static Sprite RoundedRect(float radiusPixels = 32f, int size = RoundedTextureSize)
        {
            var key = "round_" + size + "_" + Mathf.RoundToInt(radiusPixels);
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var half = size * 0.5f;
            var radius = Mathf.Clamp(radiusPixels, 0.5f, half - 1f);
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    var dy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    var outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                    var distance = outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
                    var alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));

            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Disque parfait (utilisé pour les particules, les trous, les ombres).</summary>
        public static Sprite Circle(int size = 64)
        {
            var key = "circle_" + size;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var half = size * 0.5f;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5f - half;
                    var dy = y + 0.5f - half;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) - (half - 1f);
                    var alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Goutte de peinture mignonne : corps arrondi avec une pointe en haut,
        /// deux yeux noirs et un sourire. Le corps est blanc afin d'être teinté
        /// par l'Image.color de chaque créature.
        /// </summary>
        public static Sprite Blob(int size = BlobTextureSize)
        {
            var key = "blob_" + size;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var pixels = new Color32[size * size];
            var scale = size * 0.5f;
            var dark = new Color32(26, 29, 46, 255);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Coordonnées normalisées : [-1 .. 1], y vers le haut.
                    var px = (x + 0.5f - scale) / scale;
                    var py = (y + 0.5f - scale) / scale;

                    // Corps : disque + pointe conique, unis par une union douce.
                    var dCircle = CircleDistance(px, py, 0f, -0.14f, 0.60f);
                    var dWedge = WedgeDistance(px, py, -0.10f, 0.80f, 0.235f);
                    var body = SmoothUnion(dCircle, dWedge, 0.06f);

                    var alpha = Mathf.Clamp01(0.5f - body * scale);
                    if (alpha <= 0.001f)
                    {
                        continue;
                    }

                    // Léger dégradé vertical pour donner du volume.
                    var shade = py < -0.34f ? 0.88f : 0.98f;
                    var color = new Color(shade, shade, shade, 1f);

                    // Reflet brillant en haut à gauche.
                    var highlight = CircleDistance(px, py, -0.24f, 0.14f, 0.115f);
                    if (highlight < 0f)
                    {
                        color = Color.white;
                    }

                    // Yeux.
                    var eyeLeft = EllipseDistance(px + 0.215f, py + 0.075f, 0.083f, 0.105f);
                    var eyeRight = EllipseDistance(px - 0.215f, py + 0.075f, 0.083f, 0.105f);

                    // Petit point de lumière dans chaque œil.
                    var glintLeft = CircleDistance(px, py, -0.245f, 0.025f, 0.026f);
                    var glintRight = CircleDistance(px, py, 0.185f, 0.025f, 0.026f);

                    // Sourire : arc épais dans la moitié basse du visage.
                    var mouth = 1f;
                    if (py < -0.16f && Mathf.Abs(px) < 0.17f)
                    {
                        var arcRadius = Mathf.Sqrt(px * px + (py + 0.30f) * (py + 0.30f));
                        mouth = Mathf.Abs(arcRadius - 0.185f) - 0.030f;
                    }

                    var inkAlpha = 0f;
                    if (eyeLeft < 0f || eyeRight < 0f)
                    {
                        inkAlpha = Mathf.Clamp01(0.5f - Mathf.Min(eyeLeft, eyeRight) * scale);
                    }
                    else if (mouth < 0f)
                    {
                        inkAlpha = Mathf.Clamp01(0.5f - mouth * scale);
                    }

                    var glintAlpha = 0f;
                    if (glintLeft < 0f || glintRight < 0f)
                    {
                        glintAlpha = 1f;
                    }

                    if (inkAlpha > 0f)
                    {
                        color = new Color32(dark.r, dark.g, dark.b, 255);
                    }
                    else if (glintAlpha > 0f)
                    {
                        color = Color.white;
                    }

                    color.a = alpha;
                    pixels[y * size + x] = color;
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Dégradé vertical plein écran (fond ciel).</summary>
        public static Sprite VerticalGradient(Color bottom, Color top, int height = 256)
        {
            var key = "grad_" + ColorUtility.ToHtmlStringRGB(bottom) + "_" + ColorUtility.ToHtmlStringRGB(top) + "_" + height;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var pixels = new Color32[2 * height];
            for (var y = 0; y < height; y++)
            {
                var t = height <= 1 ? 0f : (float)y / (height - 1);
                var color = Color.Lerp(bottom, top, t);
                pixels[y * 2] = color;
                pixels[y * 2 + 1] = color;
            }

            var texture = NewTexture(2, height);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Halo radial doux : sert d'ombre portée ou de lumière.</summary>
        public static Sprite RadialGlow(int size = 128)
        {
            var key = "glow_" + size;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var half = size * 0.5f;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f - half) / half;
                    var dy = (y + 0.5f - half) / half;
                    var distance = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    var alpha = Mathf.Pow(1f - distance, 1.8f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Étoile à 5 branches (confettis, pastilles de score).</summary>
        public static Sprite Star(int size = 128, int branches = 5, float innerRatio = 0.45f)
        {
            var key = "star_" + size + "_" + branches + "_" + Mathf.RoundToInt(innerRatio * 100f);
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var count = branches * 2;
            var vertices = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var radius = i % 2 == 0 ? 0.96f : innerRatio;
                var angle = Mathf.PI * 0.5f + i * Mathf.PI / branches;
                vertices[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }

            var pixels = new Color32[size * size];
            var half = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var inside = 0;
                    // 2x2 supersampling : suffisant et rapide pour une pastille d'UI.
                    for (var sy = 0; sy < 2; sy++)
                    {
                        for (var sx = 0; sx < 2; sx++)
                        {
                            var px = (x + 0.25f + sx * 0.5f - half) / half;
                            var py = (y + 0.25f + sy * 0.5f - half) / half;
                            if (PointInPolygon(px, py, vertices))
                            {
                                inside++;
                            }
                        }
                    }

                    var alpha = inside / 4f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------
        // Sprint 2 : symboles des créatures spéciales, accessoires de skins,
        // pièce d'or et variante arc-en-ciel du blob. Toujours 100 % code.
        // ------------------------------------------------------------------

        /// <summary>Symbole affiché sur une créature spéciale.</summary>
        public static Sprite SpecialSymbol(CreatureKind kind)
        {
            switch (kind)
            {
                case CreatureKind.Bomb:
                    return BombIcon();
                case CreatureKind.Rainbow:
                    return Star(96, 5, 0.46f);
                case CreatureKind.Line:
                    return BarsIcon();
                default:
                    return null;
            }
        }

        /// <summary>Accessoire dessiné sur le blob pour un skin donné.</summary>
        public static Sprite SkinAccessory(SkinId skin)
        {
            switch (skin)
            {
                case SkinId.Glasses:
                    return GlassesIcon();
                case SkinId.Hat:
                    return HatIcon();
                case SkinId.Crown:
                    return CrownIcon();
                default:
                    return null;
            }
        }

        /// <summary>Petite bombe noire à mèche dorée.</summary>
        public static Sprite BombIcon(int size = 128)
        {
            return BuildIcon("icon_bomb_" + size, size, (px, py) =>
            {
                var body = CircleDistance(px, py, 0f, -0.10f, 0.56f);
                var fuse = SegmentDistance(px, py, 0.34f, 0.40f, 0.60f, 0.68f) - 0.065f;
                var spark = CircleDistance(px, py, 0.65f, 0.74f, 0.15f);

                var distance = Mathf.Min(body, Mathf.Min(fuse, spark));
                if (distance > 0f)
                {
                    return Transparent;
                }

                Color32 color;
                if (body <= fuse && body <= spark)
                {
                    var highlight = CircleDistance(px, py, -0.22f, 0.08f, 0.13f);
                    color = highlight <= 0f ? new Color32(130, 138, 160, 255) : new Color32(52, 58, 76, 255);
                }
                else if (fuse <= spark)
                {
                    color = new Color32(255, 201, 60, 255);
                }
                else
                {
                    var core = CircleDistance(px, py, 0.65f, 0.74f, 0.08f);
                    color = core <= 0f ? new Color32(255, 255, 255, 255) : new Color32(255, 201, 60, 255);
                }

                color.a = (byte)(Mathf.Clamp01(0.5f - distance * size * 0.5f) * 255f);
                return color;
            });
        }

        /// <summary>Trois barres horizontales : symbole de la créature « ligne ».</summary>
        public static Sprite BarsIcon(int size = 128)
        {
            return BuildIcon("icon_bars_" + size, size, (px, py) =>
            {
                var top = RoundedBoxDistance(px, py, 0f, 0.30f, 0.50f, 0.075f, 0.07f);
                var middle = RoundedBoxDistance(px, py, 0f, 0.02f, 0.36f, 0.075f, 0.07f);
                var bottom = RoundedBoxDistance(px, py, 0f, -0.26f, 0.50f, 0.075f, 0.07f);
                var distance = Mathf.Min(top, Mathf.Min(middle, bottom));
                if (distance > 0f)
                {
                    return Transparent;
                }

                var color = new Color32(255, 255, 255, 255);
                color.a = (byte)(Mathf.Clamp01(0.5f - distance * size * 0.5f) * 255f);
                return color;
            });
        }

        /// <summary>Lunettes rondes à monture sombre.</summary>
        public static Sprite GlassesIcon(int size = 128)
        {
            return BuildIcon("icon_glasses_" + size, size, (px, py) =>
            {
                var leftLens = Mathf.Abs(CircleDistance(px, py, -0.34f, 0.02f, 0.30f)) - 0.065f;
                var rightLens = Mathf.Abs(CircleDistance(px, py, 0.34f, 0.02f, 0.30f)) - 0.065f;
                var bridge = SegmentDistance(px, py, -0.08f, 0.08f, 0.08f, 0.08f) - 0.045f;
                var leftTemple = SegmentDistance(px, py, -0.62f, 0.06f, -0.80f, 0.18f) - 0.045f;
                var rightTemple = SegmentDistance(px, py, 0.62f, 0.06f, 0.80f, 0.18f) - 0.045f;
                var distance = Mathf.Min(Mathf.Min(leftLens, rightLens), Mathf.Min(bridge, Mathf.Min(leftTemple, rightTemple)));

                if (distance <= 0f)
                {
                    var color = new Color32(46, 58, 89, 255);
                    color.a = (byte)(Mathf.Clamp01(0.5f - distance * size * 0.5f) * 255f);
                    return color;
                }

                // Reflet sur les verres.
                var glintLeft = CircleDistance(px, py, -0.42f, 0.12f, 0.085f);
                var glintRight = CircleDistance(px, py, 0.26f, 0.12f, 0.085f);
                var glint = Mathf.Min(glintLeft, glintRight);
                if (glint <= 0f)
                {
                    var glintColor = new Color32(255, 255, 255, 255);
                    glintColor.a = (byte)(Mathf.Clamp01(0.5f - glint * size * 0.5f) * 190f);
                    return glintColor;
                }

                return Transparent;
            });
        }

        /// <summary>Chapeau à bandeau rose.</summary>
        public static Sprite HatIcon(int size = 128)
        {
            return BuildIcon("icon_hat_" + size, size, (px, py) =>
            {
                var brim = RoundedBoxDistance(px, py, 0f, 0.00f, 0.66f, 0.12f, 0.10f);
                var dome = CircleDistance(px, py, 0f, 0.14f, 0.42f);
                var band = RoundedBoxDistance(px, py, 0f, 0.00f, 0.46f, 0.08f, 0.05f);

                var distance = Mathf.Min(Mathf.Min(brim, dome), band);
                if (distance > 0f)
                {
                    return Transparent;
                }

                Color32 color;
                if (band <= 0f)
                {
                    color = new Color32(255, 111, 165, 255);
                }
                else if (dome <= 0f)
                {
                    var shine = CircleDistance(px, py, -0.16f, 0.26f, 0.12f);
                    color = shine <= 0f ? new Color32(160, 214, 255, 255) : new Color32(79, 184, 255, 255);
                }
                else
                {
                    color = new Color32(46, 58, 89, 255);
                }

                color.a = (byte)(Mathf.Clamp01(0.5f - distance * size * 0.5f) * 255f);
                return color;
            });
        }

        /// <summary>Couronne dorée à cinq pointes.</summary>
        public static Sprite CrownIcon(int size = 128)
        {
            var polygon = new[]
            {
                new Vector2(-0.62f, -0.40f),
                new Vector2(0.62f, -0.40f),
                new Vector2(0.62f, 0.20f),
                new Vector2(0.36f, 0.02f),
                new Vector2(0.20f, 0.46f),
                new Vector2(0.02f, 0.10f),
                new Vector2(-0.16f, 0.46f),
                new Vector2(-0.34f, 0.02f),
                new Vector2(-0.62f, 0.20f)
            };

            return BuildIcon("icon_crown_" + size, size, (px, py) =>
            {
                var coverage = PolygonCoverage(px, py, polygon, size);
                if (coverage <= 0f)
                {
                    return Transparent;
                }

                var color = new Color32(255, 201, 60, 255);
                if (py < -0.24f)
                {
                    color = new Color32(232, 158, 20, 255);
                }

                color.a = (byte)(coverage * 255f);
                return color;
            });
        }

        /// <summary>Pièce d'or (icône de monnaie).</summary>
        public static Sprite CoinIcon(int size = 128)
        {
            var star = StarPolygon(5, 0.45f, 0.44f);

            return BuildIcon("icon_coin_" + size, size, (px, py) =>
            {
                var outer = CircleDistance(px, py, 0f, 0f, 0.86f);
                if (outer > 0f)
                {
                    return Transparent;
                }

                var alpha = Mathf.Clamp01(0.5f - outer * size * 0.5f);
                Color32 color;
                if (py > 0.18f && px < 0.1f)
                {
                    color = new Color32(255, 224, 130, 255);
                }
                else if (CircleDistance(px, py, 0f, 0f, 0.64f) <= 0f)
                {
                    color = new Color32(255, 201, 60, 255);
                }
                else
                {
                    color = new Color32(232, 158, 20, 255);
                }

                if (CircleDistance(px, py, 0f, 0f, 0.58f) <= 0f && PointInPolygon(px, py, star))
                {
                    color = new Color32(255, 246, 214, 255);
                }

                color.a = (byte)(alpha * 255f);
                return color;
            });
        }

        /// <summary>Sac de boutique : icône blanche à teinter par l'Image appelante.</summary>
        public static Sprite ShopIcon(int size = 128)
        {
            return BuildIcon("icon_shop_" + size, size, (px, py) =>
            {
                var body = RoundedBoxDistance(px, py, 0f, -0.10f, 0.54f, 0.46f, 0.14f);
                var ring = Mathf.Abs(CircleDistance(px, py, 0f, 0.34f, 0.27f)) - 0.065f;
                var handle = py > 0.30f ? ring : 1f;
                var distance = Mathf.Min(body, handle);
                if (distance > 0f)
                {
                    return Transparent;
                }

                var alpha = Mathf.Clamp01(0.5f - distance * size * 0.5f);
                Color32 color = new Color32(255, 255, 255, 255);

                // Rabat supérieur légèrement grisé pour lire la forme sur fond clair.
                if (body <= 0f && py > 0.10f)
                {
                    color = new Color32(226, 234, 248, 255);
                }

                // Deux boutons de couture.
                if (CircleDistance(px, py, -0.16f, -0.12f, 0.045f) <= 0f ||
                    CircleDistance(px, py, 0.16f, -0.12f, 0.045f) <= 0f)
                {
                    color = new Color32(190, 205, 228, 255);
                }

                color.a = (byte)(alpha * 255f);
                return color;
            });
        }

        /// <summary>Blob multicolore de la créature arc-en-ciel (yeux conservés).</summary>
        public static Sprite RainbowBlob(int size = BlobTextureSize)
        {
            var key = "blob_rainbow_" + size;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var baseSprite = Blob(size);
            var source = baseSprite.texture.GetPixels32();
            var pixels = new Color32[source.Length];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var pixel = source[index];
                    if (pixel.a == 0)
                    {
                        pixels[index] = pixel;
                        continue;
                    }

                    var brightness = (pixel.r + pixel.g + pixel.b) / 3;
                    if (brightness < 90)
                    {
                        // Les yeux et la bouche restent sombres.
                        pixels[index] = pixel;
                        continue;
                    }

                    var t = (x / (float)size) * 0.72f + (y / (float)size) * 0.22f;
                    var shade = brightness / 255f;
                    var rainbow = Color.HSVToRGB(Mathf.Repeat(t, 1f), 0.68f, Mathf.Clamp01(shade));
                    pixels[index] = new Color32(
                        (byte)(rainbow.r * 255f),
                        (byte)(rainbow.g * 255f),
                        (byte)(rainbow.b * 255f),
                        pixel.a);
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        private static Sprite BuildIcon(string key, int size, Func<float, float, Color32> sample)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    var py = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    pixels[y * size + x] = sample(px, py);
                }
            }

            var texture = NewTexture(size, size);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        private static float RoundedBoxDistance(float px, float py, float cx, float cy, float halfWidth, float halfHeight, float radius)
        {
            var dx = Mathf.Abs(px - cx) - (halfWidth - radius);
            var dy = Mathf.Abs(py - cy) - (halfHeight - radius);
            var outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
        }

        private static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            var vx = bx - ax;
            var vy = by - ay;
            var wx = px - ax;
            var wy = py - ay;
            var lengthSquared = vx * vx + vy * vy;
            var t = lengthSquared <= 0.0001f ? 0f : Mathf.Clamp01((wx * vx + wy * vy) / lengthSquared);
            var dx = wx - vx * t;
            var dy = wy - vy * t;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static Vector2[] StarPolygon(int branches, float innerRatio, float outerRadius)
        {
            var count = branches * 2;
            var points = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var radius = i % 2 == 0 ? outerRadius : outerRadius * innerRatio;
                var angle = Mathf.PI * 0.5f + i * Mathf.PI / branches;
                points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }

            return points;
        }

        private static float PolygonCoverage(float px, float py, Vector2[] polygon, int size)
        {
            var offset = 1f / size;
            var inside = 0;
            for (var sy = 0; sy < 2; sy++)
            {
                for (var sx = 0; sx < 2; sx++)
                {
                    var x = px + (sx == 0 ? -offset * 0.25f : offset * 0.25f);
                    var y = py + (sy == 0 ? -offset * 0.25f : offset * 0.25f);
                    if (PointInPolygon(x, y, polygon))
                    {
                        inside++;
                    }
                }
            }

            return inside / 4f;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };
            return texture;
        }

        private static float CircleDistance(float px, float py, float cx, float cy, float radius)
        {
            var dx = px - cx;
            var dy = py - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }

        /// <summary>Coin effilé : largeur nulle au sommet (apexY), largeur max en bas (baseY).</summary>
        private static float WedgeDistance(float px, float py, float baseY, float apexY, float baseHalfWidth)
        {
            if (py < baseY - 0.05f || py > apexY)
            {
                return 1f;
            }

            var t = Mathf.Clamp01((apexY - py) / Mathf.Max(0.001f, apexY - baseY));
            return Mathf.Abs(px) - baseHalfWidth * t;
        }

        private static float EllipseDistance(float px, float py, float radiusX, float radiusY)
        {
            var nx = px / radiusX;
            var ny = py / radiusY;
            return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(radiusX, radiusY);
        }

        private static float SmoothUnion(float a, float b, float k)
        {
            var h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        private static bool PointInPolygon(float x, float y, Vector2[] polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                if ((pi.y > y) != (pj.y > y) &&
                    x < (pj.x - pi.x) * (y - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
