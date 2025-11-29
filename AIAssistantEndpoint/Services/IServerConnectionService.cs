namespace AIAssistantEndpoint.Services
{
    using System.Threading.Tasks;
    using AIAssistantEndpoint.Streaming;

    public interface IServerConnectionService
    {
        Task<bool> ConnectAsync();
        Task<string> SendRequestAsync(string prompt);
        Task<IStreamingResponse> SendStreamingRequestAsync(string prompt);
        void Disconnect();
        bool IsConnected { get; }
        void ClearCache();
    }
}