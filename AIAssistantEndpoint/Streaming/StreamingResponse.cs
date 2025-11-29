namespace AIAssistantEndpoint.Streaming
{
    using System;
    using System.Text;

    public class StreamingResponse : IStreamingResponse
    {
        private StringBuilder _responseBuilder = new StringBuilder();
        private bool _isCompleted;
        private bool _isError;
        private Exception _lastError;

        public event StreamingChunkReceived OnChunkReceived;
        public event StreamingCompleted OnCompleted;
        public event StreamingError OnError;

        public string FullResponse => _responseBuilder.ToString();
        public bool IsCompleted => _isCompleted;
        public bool IsError => _isError;
        public Exception LastError => _lastError;

        public void AppendChunk(string chunk)
        {
            if (string.IsNullOrEmpty(chunk) || _isCompleted)
                return;

            _responseBuilder.Append(chunk);
            OnChunkReceived?.Invoke(chunk);
        }

        public void Complete()
        {
            _isCompleted = true;
            OnCompleted?.Invoke(FullResponse);
        }

        public void SetError(Exception exception)
        {
            _isError = true;
            _lastError = exception;
            OnError?.Invoke(exception);
        }

        public void Reset()
        {
            _responseBuilder = new StringBuilder();
            _isCompleted = false;
            _isError = false;
            _lastError = null;
        }
    }
}
