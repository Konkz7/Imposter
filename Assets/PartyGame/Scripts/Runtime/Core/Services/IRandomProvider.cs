using System;
using System.Collections.Generic;

namespace PartyGame.Core.Services
{
    /// <summary>
    /// Abstraction over randomness so game rules stay deterministic under test
    /// and never reach for UnityEngine.Random directly.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Inclusive minimum, exclusive maximum.</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>0..1 inclusive-exclusive.</summary>
        double NextDouble();
    }

    /// <summary>Default implementation. Seeded from the clock plus a call counter to avoid repeats.</summary>
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider() : this(unchecked(Environment.TickCount * 397) ^ Guid.NewGuid().GetHashCode())
        {
        }

        public SystemRandomProvider(int seed)
        {
            _random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _random.Next(minInclusive, maxExclusive);
        }

        public double NextDouble() => _random.NextDouble();
    }

    /// <summary>Fully deterministic provider used by unit tests.</summary>
    public sealed class SequenceRandomProvider : IRandomProvider
    {
        private readonly int[] _values;
        private int _cursor;

        public SequenceRandomProvider(params int[] values)
        {
            _values = values != null && values.Length > 0 ? values : new[] { 0 };
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            var span = maxExclusive - minInclusive;
            var value = _values[_cursor % _values.Length];
            _cursor++;
            var offset = ((value % span) + span) % span;
            return minInclusive + offset;
        }

        public double NextDouble()
        {
            var value = _values[_cursor % _values.Length];
            _cursor++;
            return Math.Abs(value % 1000) / 1000.0;
        }
    }

    /// <summary>Shared collection helpers built on <see cref="IRandomProvider"/>.</summary>
    public static class Shuffler
    {
        /// <summary>Fisher-Yates. Unbiased, in place.</summary>
        public static void ShuffleInPlace<T>(IList<T> list, IRandomProvider random)
        {
            if (list == null || random == null) return;
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static List<T> Shuffled<T>(IEnumerable<T> source, IRandomProvider random)
        {
            var copy = new List<T>(source ?? new List<T>());
            ShuffleInPlace(copy, random);
            return copy;
        }

        public static T Pick<T>(IReadOnlyList<T> source, IRandomProvider random)
        {
            if (source == null || source.Count == 0) return default;
            return source[random.Range(0, source.Count)];
        }

        /// <summary>Picks up to <paramref name="count"/> distinct entries.</summary>
        public static List<T> PickDistinct<T>(IReadOnlyList<T> source, int count, IRandomProvider random)
        {
            var result = new List<T>();
            if (source == null || source.Count == 0 || count <= 0) return result;
            var pool = new List<T>(source);
            ShuffleInPlace(pool, random);
            for (var i = 0; i < Math.Min(count, pool.Count); i++) result.Add(pool[i]);
            return result;
        }
    }
}
