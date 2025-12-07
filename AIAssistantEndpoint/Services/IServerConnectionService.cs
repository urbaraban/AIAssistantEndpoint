namespace AIAssistantEndpoint.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AIAssistantEndpoint.Streaming;

    public interface IServerConnectionService
    {
        Task<bool> ConnectAsync();
        Task<string> SendRequestAsync(string prompt, IEnumerable<string> fileIds = null, string parentMessageId = null);
        Task<IStreamingResponse> SendStreamingRequestAsync(string prompt, IEnumerable<string> fileIds = null, string parentMessageId = null);
        Task<string> UploadFileAsync(string fileName, byte[] content);
        void Disconnect();
        bool IsConnected { get; }
        void ClearCache();
    }
}