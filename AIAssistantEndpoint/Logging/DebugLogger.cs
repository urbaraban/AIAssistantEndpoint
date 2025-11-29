namespace AIAssistantEndpoint.Logging
{
    using System;
    using System.Diagnostics;

    public class DebugLogger : ILogger
    {
        private readonly string _source;

        public DebugLogger(string source = "AIAssistant")
        {
            _source = source;
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{_source}] [{level}] {message}";

            if (exception != null)
            {
                logMessage += Environment.NewLine + exception.ToString();
            }

            System.Diagnostics.Debug.WriteLine(logMessage);
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void Error(string message, Exception exception = null)
        {
            Log(LogLevel.Error, message, exception);
        }

        public void Critical(string message, Exception exception = null)
        {
            Log(LogLevel.Critical, message, exception);
        }
    }
}
