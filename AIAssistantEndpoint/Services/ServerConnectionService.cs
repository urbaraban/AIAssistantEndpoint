namespace AIAssistantEndpoint.Services
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AIAssistantEndpoint.Settings;
    using AIAssistantEndpoint.Caching;
    using AIAssistantEndpoint.Streaming;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

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

        private Uri BuildUri(string pathTemplate)
        {
            if (string.IsNullOrEmpty(_settings.AgentAccessId))
                throw new InvalidOperationException("AgentAccessId is not configured.");

            var encodedAgent = Uri.EscapeDataString(_settings.AgentAccessId);
            var path = pathTemplate.Replace("{agent_access_id}", encodedAgent);

            var baseUrl = _settings.ServerUrl?.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("ServerUrl is not configured.");

            var combined = baseUrl + (path.StartsWith("/") ? path : "/" + path);
            return new Uri(combined);
        }

        private void ConfigureHttpClient()
        {
            _httpClient?.Dispose();
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(_settings.TimeoutMs)
            };

            // Set BaseAddress if ServerUrl configured so relative URIs may be used
            if (!string.IsNullOrEmpty(_settings.ServerUrl))
            {
                try
                {
                    var baseAddr = _settings.ServerUrl.EndsWith("/") ? _settings.ServerUrl : _settings.ServerUrl + "/";
                    _httpClient.BaseAddress = new Uri(baseAddr);
                }
                catch (Exception ex)
                {
                    LogError($"Invalid ServerUrl for BaseAddress: {ex.Message}");
                }
            }

            // Ensure we accept JSON by default
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Add Authorization header if using header auth
            if (!_settings.UseApiKeyQuery && !string.IsNullOrEmpty(_settings.ApiKey))
            {
                var headerValue = _settings.AuthScheme + " " + _settings.ApiKey;
                _httpClient.DefaultRequestHeaders.Remove(_settings.AuthHeaderName);
                _httpClient.DefaultRequestHeaders.Add(_settings.AuthHeaderName, headerValue);
            }
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                ConfigureHttpClient();

                var uri = BuildUri(_settings.CallEndpoint);

                // Send a minimal valid payload per Timeweb API: message is required
                var payload = new Dictionary<string, object>
                {
                    { "message", "ping from VS extension" },
                    { "parent_message_id", string.Empty },
                    { "file_ids", new string[0] }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(uri, content);
                _isConnected = response.IsSuccessStatusCode;

                LogInfo($"Connection to server: {(_isConnected ? "OK" : "FAILED")}");
                return _isConnected;
            }
            catch (Exception ex)
            {
                LogError($"Error connecting to server: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public async Task<string> SendRequestAsync(string prompt, System.Collections.Generic.IEnumerable<string> fileIds = null, string parentMessageId = null)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server.");

            var cacheKey = $"prompt_{HashString(prompt)}";

            var cachedResult = await _cacheProvider.GetAsync<string>(cacheKey);
            if (cachedResult != null)
            {
                LogInfo($"Returning cached result for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");
                return cachedResult;
            }

            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "message", prompt },
                    { "parent_message_id", parentMessageId ?? string.Empty },
                    { "file_ids", fileIds != null ? System.Linq.Enumerable.ToArray(fileIds) : new string[0] }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

                Uri uri;
                if (_settings.UseApiKeyQuery)
                {
                    var builder = new UriBuilder(BuildUri(_settings.CallEndpoint));
                    var existingQuery = builder.Query;
                    if (!string.IsNullOrEmpty(existingQuery) && existingQuery.StartsWith("?"))
                        existingQuery = existingQuery.Substring(1);

                    var q = string.IsNullOrEmpty(existingQuery) ? "" : existingQuery + "&";
                    q += Uri.EscapeDataString(_settings.ApiKeyQueryName) + "=" + Uri.EscapeDataString(_settings.ApiKey ?? "");
                    builder.Query = q;
                    uri = builder.Uri;
                }
                else
                {
                    uri = BuildUri(_settings.CallEndpoint);
                }

                var response = await _httpClient.PostAsync(uri, requestContent);

                var resultText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Try to parse JSON and extract 'message' or 'choices' etc. per API
                    try
                    {
                        var j = JObject.Parse(resultText);
                        // /call returns { message, id, finish_reason }
                        if (j["message"] != null)
                        {
                            var resp = j["message"].ToString();
                            await _cacheProvider.SetAsync(cacheKey, resp, TimeSpan.FromHours(1));
                            return resp;
                        }

                        // chat completions returns choices[].message.content
                        if (j["choices"] != null && j["choices"].HasValues)
                        {
                            var first = j["choices"][0];
                            if (first["message"] != null)
                            {
                                var content = first["message"]["content"] ?? first["message"]["content"];
                                var text = content?.ToString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    await _cacheProvider.SetAsync(cacheKey, text, TimeSpan.FromHours(1));
                                    return text;
                                }
                            }

                            if (first["text"] != null)
                            {
                                var text = first["text"].ToString();
                                await _cacheProvider.SetAsync(cacheKey, text, TimeSpan.FromHours(1));
                                return text;
                            }
                        }
                    }
                    catch { /* not JSON or unexpected schema */ }

                    await _cacheProvider.SetAsync(cacheKey, resultText, TimeSpan.FromHours(1));
                    return resultText;
                }

                // If the provider reports an error about messages content (common when calling chat completions),
                // try sending as OpenAI-style chat completion with content array per Timeweb docs.
                if (!string.IsNullOrEmpty(resultText) && (resultText.Contains("all messages must have non-empty content") || resultText.Contains("messages.0")))
                {
                    try
                    {
                        // Build chat-style payload
                        var chatPayload = new Dictionary<string, object>
                        {
                            { "messages", new[] { new { role = "user", content = new[] { new { type = "text", text = prompt } } } } },
                            { "stream", false }
                        };

                        var chatJson = Newtonsoft.Json.JsonConvert.SerializeObject(chatPayload);
                        var chatContent = new StringContent(chatJson, Encoding.UTF8, "application/json");

                        Uri chatUri;
                        if (_settings.UseApiKeyQuery)
                        {
                            var builder = new UriBuilder(BuildUri(_settings.ChatEndpoint));
                            var existingQuery = builder.Query;
                            if (!string.IsNullOrEmpty(existingQuery) && existingQuery.StartsWith("?"))
                                existingQuery = existingQuery.Substring(1);

                            var q = string.IsNullOrEmpty(existingQuery) ? "" : existingQuery + "&";
                            q += Uri.EscapeDataString(_settings.ApiKeyQueryName) + "=" + Uri.EscapeDataString(_settings.ApiKey ?? "");
                            builder.Query = q;
                            chatUri = builder.Uri;
                        }
                        else
                        {
                            chatUri = BuildUri(_settings.ChatEndpoint);
                        }

                        var chatResponse = await _httpClient.PostAsync(chatUri, chatContent);
                        var chatResult = await chatResponse.Content.ReadAsStringAsync();
                        if (chatResponse.IsSuccessStatusCode)
                        {
                            try
                            {
                                var j2 = JObject.Parse(chatResult);
                                if (j2["choices"] != null && j2["choices"].HasValues)
                                {
                                    var first = j2["choices"][0];
                                    if (first["message"] != null)
                                    {
                                        var content = first["message"]["content"] ?? first["message"]["content"];
                                        var text = content?.ToString();
                                        if (!string.IsNullOrEmpty(text))
                                        {
                                            await _cacheProvider.SetAsync(cacheKey, text, TimeSpan.FromHours(1));
                                            return text;
                                        }
                                    }

                                    if (first["text"] != null)
                                    {
                                        var text = first["text"].ToString();
                                        await _cacheProvider.SetAsync(cacheKey, text, TimeSpan.FromHours(1));
                                        return text;
                                    }
                                }
                            }
                            catch { }

                            await _cacheProvider.SetAsync(cacheKey, chatResult, TimeSpan.FromHours(1));
                            return chatResult;
                        }
                    }
                    catch (Exception ex2)
                    {
                        LogError($"Retry with chat completion failed: {ex2.Message}");
                    }
                }

                throw new HttpRequestException($"Request failed: {response.StatusCode} - {resultText}");
            }
            catch (Exception ex)
            {
                LogError($"Error sending request: {ex.Message}");
                throw;
            }
        }

        public async Task<IStreamingResponse> SendStreamingRequestAsync(string prompt, System.Collections.Generic.IEnumerable<string> fileIds = null, string parentMessageId = null)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server.");

            var streamingResponse = new StreamingResponse();

            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "messages", new[] { new { role = "user", content = new[] { new { type = "text", text = prompt } } } } },
                    { "stream", true },
                    { "parent_message_id", parentMessageId ?? string.Empty },
                    { "file_ids", fileIds != null ? System.Linq.Enumerable.ToArray(fileIds) : new string[0] }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

                Uri uri;
                if (_settings.UseApiKeyQuery)
                {
                    var builder = new UriBuilder(BuildUri(_settings.ChatEndpoint));
                    var existingQuery = builder.Query;
                    if (!string.IsNullOrEmpty(existingQuery) && existingQuery.StartsWith("?"))
                        existingQuery = existingQuery.Substring(1);

                    var q = string.IsNullOrEmpty(existingQuery) ? "" : existingQuery + "&";
                    q += Uri.EscapeDataString(_settings.ApiKeyQueryName) + "=" + Uri.EscapeDataString(_settings.ApiKey ?? "");
                    builder.Query = q;
                    uri = builder.Uri;
                }
                else
                {
                    uri = BuildUri(_settings.ChatEndpoint);
                }

                var httpResponse = await _httpClient.PostAsync(uri, requestContent);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request failed: {httpResponse.StatusCode}");
                }

                using (var stream = await httpResponse.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        streamingResponse.AppendChunk(chunk);
                    }
                }

                streamingResponse.Complete();
                LogInfo("Streaming response completed");
            }
            catch (Exception ex)
            {
                streamingResponse.SetError(ex);
                LogError($"Error during streaming request: {ex.Message}");
            }

            return streamingResponse;
        }

        public async void ClearCache()
        {
            await _cacheProvider.ClearAsync();
            LogInfo("Cache cleared");
        }

        public void Disconnect()
        {
            _httpClient?.Dispose();
            _isConnected = false;
            LogInfo("Disconnected from server");
        }

        public async Task<string> UploadFileAsync(string fileName, byte[] content)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server.");

            try
            {
                var builder = new UriBuilder(BuildUri($"/api/v1/cloud-ai/agents/{Uri.EscapeDataString(_settings.AgentAccessId)}/files"));
                var uri = builder.Uri;

                using (var form = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(content);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    form.Add(fileContent, "file", fileName);

                    var response = await _httpClient.PostAsync(uri, form);
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"File upload failed: {response.StatusCode} - {body}");

                    try
                    {
                        var j = JObject.Parse(body);
                        // assume response contains { file_id: "..." }
                        if (j["file_id"] != null)
                            return j.Value<string>("file_id");
                    }
                    catch { }

                    // fallback: return whole body if no file_id
                    return body;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error uploading file: {ex.Message}");
                throw;
            }
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