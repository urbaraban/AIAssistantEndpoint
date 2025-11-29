namespace AIAssistantEndpoint.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class MemoryCacheProvider : ICacheProvider
    {
        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime? ExpirationTime { get; set; }
        }

        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private readonly object _lockObject = new object();

        public Task<T> GetAsync<T>(string key)
        {
            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(key))
                    return Task.FromResult<T>(default(T));

                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.ExpirationTime.HasValue && DateTime.UtcNow >= entry.ExpirationTime)
                    {
                        _cache.Remove(key);
                        return Task.FromResult<T>(default(T));
                    }

                    return Task.FromResult((T)entry.Value);
                }

                return Task.FromResult<T>(default(T));
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            lock (_lockObject)
            {
                var entry = new CacheEntry
                {
                    Value = value,
                    ExpirationTime = expiration.HasValue ? (DateTime?)DateTime.UtcNow.Add(expiration.Value) : null
                };

                _cache[key] = entry;
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            lock (_lockObject)
            {
                _cache.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }

            return Task.CompletedTask;
        }

        public bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.ExpirationTime.HasValue && DateTime.UtcNow >= entry.ExpirationTime)
                    {
                        _cache.Remove(key);
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
