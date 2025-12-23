using System;
using System.Collections.Generic;

namespace HowLongToBeat.Services
{
    internal class LruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly TimeSpan? _ttl;
        private readonly Dictionary<TKey, CacheEntry> _map;
        private readonly LinkedList<TKey> _lruList;
        private readonly object _sync = new object();

        private class CacheEntry
        {
            public TValue Value;
            public LinkedListNode<TKey> Node;
            public DateTime? ExpiryUtc;
        }

        public LruCache(int capacity, TimeSpan? ttl = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _ttl = ttl;
            _map = new Dictionary<TKey, CacheEntry>(capacity);
            _lruList = new LinkedList<TKey>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_sync)
            {
                if (_map.TryGetValue(key, out CacheEntry entry))
                {
                    if (entry.ExpiryUtc != null && DateTime.UtcNow > entry.ExpiryUtc.Value)
                    {
                        RemoveInternal(entry, key);
                        value = default;
                        return false;
                    }

                    _lruList.Remove(entry.Node);
                    _lruList.AddFirst(entry.Node);
                    value = entry.Value;
                    return true;
                }

                value = default;
                return false;
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_sync)
            {
                if (_map.TryGetValue(key, out CacheEntry existing))
                {
                    existing.Value = value;
                    existing.ExpiryUtc = _ttl.HasValue ? DateTime.UtcNow.Add(_ttl.Value) : (DateTime?)null;
                    _lruList.Remove(existing.Node);
                    _lruList.AddFirst(existing.Node);
                    return false;
                }

                var node = new LinkedListNode<TKey>(key);
                var entry = new CacheEntry
                {
                    Value = value,
                    Node = node,
                    ExpiryUtc = _ttl.HasValue ? DateTime.UtcNow.Add(_ttl.Value) : (DateTime?)null
                };

                _map[key] = entry;
                _lruList.AddFirst(node);

                if (_map.Count > _capacity)
                {
                    var lru = _lruList.Last;
                    if (lru != null)
                    {
                        if (_map.TryGetValue(lru.Value, out CacheEntry toRemove))
                        {
                            RemoveInternal(toRemove, lru.Value);
                        }
                    }
                }

                return true;
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _map.Clear();
                _lruList.Clear();
            }
        }

        private void RemoveInternal(CacheEntry entry, TKey key)
        {
            try
            {
                _map.Remove(key);
            }
            catch { }
            try
            {
                if (entry.Node != null)
                {
                    _lruList.Remove(entry.Node);
                }
            }
            catch { }
        }

        public int Count
        {
            get
            {
                lock (_sync) { return _map.Count; }
            }
        }
    }
}
