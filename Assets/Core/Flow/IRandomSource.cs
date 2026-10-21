using System;

namespace ColorFlow.Core
{
    /// <summary>Source d'aléa injectable : permet des tests déterministes.</summary>
    public interface IRandomSource
    {
        /// <summary>Entier dans [minInclusive, maxExclusive[.</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>Implémentation basée sur System.Random (graine optionnelle).</summary>
    public sealed class RandomSource : IRandomSource
    {
        private readonly Random _random;

        public RandomSource()
            : this(Environment.TickCount)
        {
        }

        public RandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
