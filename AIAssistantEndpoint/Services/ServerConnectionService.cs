namespace AIAssistantEndpoint.Services
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AIAssistantEndpoint.Settings;
    using AIAssistantEndpoint.Caching;
    using AIAssistantEndpoint.Streaming;

    public class ServerConnectionService : IServerConnectionService
    {
        private readonly IServerConnectionSettings _settings;
        private readonly ICacheProvider _cacheProvider;
        private HttpClient _httpClient;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public ServerConnectionService(IServerConnectionSettings settings)
            : this(settings, new MemoryCacheProvider())
        {
        }

        public ServerConnectionService(IServerConnectionSettings settings, ICacheProvider cacheProvider)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _isConnected = false;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMilliseconds(_settings.TimeoutMs),
                    BaseAddress = new Uri(_settings.ServerUrl)
                };

                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");

                var response = await _httpClient.GetAsync("/health");
                _isConnected = response.IsSuccessStatusCode;

                LogInfo($"Подключение к серверу: {(_isConnected ? "успешно" : "ошибка")}");
                return _isConnected;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка подключения к серверу: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public async Task<string> SendRequestAsync(string prompt)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Соединение с сервером не установлено.");

            var cacheKey = $"prompt_{HashString(prompt)}";
            
            var cachedResult = await _cacheProvider.GetAsync<string>(cacheKey);
            if (cachedResult != null)
            {
                LogInfo($"Результат получен из кэша для: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");
                return cachedResult;
            }

            try
            {
                var requestContent = new StringContent(
                    $"{{\"prompt\":\"{EscapeJson(prompt)}\"}}",
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/api/assistant", requestContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    await _cacheProvider.SetAsync(cacheKey, result, TimeSpan.FromHours(1));
                    return result;
                }

                throw new HttpRequestException($"Ошибка сервера: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                LogError($"Ошибка отправки запроса: {ex.Message}");
                throw;
            }
        }

        public async Task<IStreamingResponse> SendStreamingRequestAsync(string prompt)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Соединение с сервером не установлено.");

            var streamingResponse = new StreamingResponse();

            try
            {
                var requestContent = new StringContent(
                    $"{{\"prompt\":\"{EscapeJson(prompt)}\",\"stream\":true}}",
                    System.Text.Encoding.UTF8,
                    "application/json");

                var httpResponse = await _httpClient.PostAsync("/api/assistant/stream", requestContent);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Ошибка сервера: {httpResponse.StatusCode}");
                }

                using (var stream = await httpResponse.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        var chunk = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        streamingResponse.AppendChunk(chunk);
                    }
                }

                streamingResponse.Complete();
                LogInfo("Потоковый запрос завершён успешно");
            }
            catch (Exception ex)
            {
                streamingResponse.SetError(ex);
                LogError($"Ошибка потокового запроса: {ex.Message}");
            }

            return streamingResponse;
        }

        public async void ClearCache()
        {
            await _cacheProvider.ClearAsync();
            LogInfo("Кэш очищен");
        }

        public void Disconnect()
        {
            _httpClient?.Dispose();
            _isConnected = false;
            LogInfo("Соединение закрыто");
        }

        private static string EscapeJson(string input)
        {
            return input.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string HashString(string input)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private static void LogError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[AIAssistant Error] {message}");
        }

        private static void LogInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[AIAssistant Info] {message}");
        }
    }
}