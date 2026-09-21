using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyGame.Core.Services
{
    /// <summary>
    /// Picks content without immediate repeats. Remembers what has already been used
    /// for the current session and only starts recycling once the pool is exhausted.
    /// </summary>
    public class ContentRotation<T>
    {
        private readonly Func<T, string> _idSelector;
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _recent = new Queue<string>();
        private readonly int _recentMemory;

        public ContentRotation(Func<T, string> idSelector, int recentMemory = 8)
        {
            _idSelector = idSelector ?? (item => item?.ToString() ?? string.Empty);
            _recentMemory = Math.Max(1, recentMemory);
        }

        public int UsedCount => _used.Count;

        /// <summary>Picks an unused item if possible, otherwise the least recently used one.</summary>
        public T Next(IReadOnlyList<T> pool, IRandomProvider random)
        {
            if (pool == null || pool.Count == 0) return default;

            var fresh = pool.Where(item => !_used.Contains(_idSelector(item))).ToList();
            if (fresh.Count == 0)
            {
                // Pool exhausted: reset but keep the most recent entries out of the running.
                _used.Clear();
                fresh = pool.Where(item => !_recent.Contains(_idSelector(item))).ToList();
                if (fresh.Count == 0) fresh = new List<T>(pool);
            }

            var chosen = fresh[random != null ? random.Range(0, fresh.Count) : 0];
            MarkUsed(chosen);
            return chosen;
        }

        /// <summary>Picks several distinct items in one go (used when a round needs a batch).</summary>
        public List<T> NextBatch(IReadOnlyList<T> pool, int count, IRandomProvider random)
        {
            var result = new List<T>();
            if (pool == null || pool.Count == 0 || count <= 0) return result;
            for (var i = 0; i < count; i++)
            {
                var item = Next(pool, random);
                if (item == null) break;
                result.Add(item);
            }
            return result;
        }

        public void MarkUsed(T item)
        {
            var id = _idSelector(item);
            if (string.IsNullOrEmpty(id)) return;
            _used.Add(id);
            _recent.Enqueue(id);
            while (_recent.Count > _recentMemory) _recent.Dequeue();
        }

        public bool WasUsed(T item) => _used.Contains(_idSelector(item));

        public void Reset()
        {
            _used.Clear();
            _recent.Clear();
        }
    }
}
