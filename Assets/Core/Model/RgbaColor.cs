using System;
using System.Globalization;

namespace ColorFlow.Core
{
    /// <summary>
    /// Couleur RGBA 8 bits du domaine (aucune dépendance à UnityEngine.Color,
    /// qui ne peut pas vivre dans Core/).
    /// </summary>
    public readonly struct RgbaColor : IEquatable<RgbaColor>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        /// <summary>Couleur "pas de pixel" : alpha à 0.</summary>
        public static readonly RgbaColor Transparent = new RgbaColor(0, 0, 0, 0);

        public RgbaColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public bool IsTransparent => A == 0;

        public bool IsOpaque => A == 255;

        /// <summary>Analyse "#RRGGBB" ou "#RRGGBBAA" (le # est optionnel).</summary>
        public static RgbaColor FromHex(string hex)
        {
            if (!TryFromHex(hex, out var color))
            {
                throw new FormatException("Couleur hexadécimale invalide : '" + hex + "'.");
            }

            return color;
        }

        public static bool TryFromHex(string hex, out RgbaColor color)
        {
            color = Transparent;
            if (string.IsNullOrEmpty(hex))
            {
                return false;
            }

            var value = hex.Trim();
            if (value.Length > 0 && value[0] == '#')
            {
                value = value.Substring(1);
            }

            if (value.Length != 6 && value.Length != 8)
            {
                return false;
            }

            if (!byte.TryParse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !byte.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !byte.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return false;
            }

            byte a = 255;
            if (value.Length == 8 &&
                !byte.TryParse(value.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
            {
                return false;
            }

            color = new RgbaColor(r, g, b, a);
            return true;
        }

        public string ToHex()
        {
            return IsOpaque
                ? "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2")
                : "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2") + A.ToString("X2");
        }

        public bool Equals(RgbaColor other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object obj)
        {
            return obj is RgbaColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)R;
                hash = (hash * 397) ^ G;
                hash = (hash * 397) ^ B;
                hash = (hash * 397) ^ A;
                return hash;
            }
        }

        public static bool operator ==(RgbaColor left, RgbaColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RgbaColor left, RgbaColor right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ToHex();
        }
    }
}
