namespace AIAssistantEndpoint.Streaming
{
    using System;

    public delegate void StreamingChunkReceived(string chunk);
    public delegate void StreamingCompleted(string fullResponse);
    public delegate void StreamingError(Exception error);

    public interface IStreamingResponse
    {
        event StreamingChunkReceived OnChunkReceived;
        event StreamingCompleted OnCompleted;
        event StreamingError OnError;

        string FullResponse { get; }
        bool IsCompleted { get; }
        bool IsError { get; }
        Exception LastError { get; }
    }
}
