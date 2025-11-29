namespace AIAssistantEndpoint.Caching
{
    using System;
    using System.Threading.Tasks;

    public interface ICacheProvider
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task ClearAsync();
        bool Contains(string key);
    }
}
