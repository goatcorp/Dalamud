using System;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Dalamud.Interface
{
    internal class SerilogEventSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public static SerilogEventSink Instance;

        public event EventHandler<(string line, LogEventLevel level)> OnLogLine;

        public SerilogEventSink(IFormatProvider formatProvider)
        {
            this._formatProvider = formatProvider;

            Instance = this;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = $"[{DateTimeOffset.Now:HH:mm:ss.fff}][{logEvent.Level}] {logEvent.RenderMessage(this._formatProvider)}";

            if (logEvent.Exception != null)
                message += "\n" + logEvent.Exception;

            this.OnLogLine?.Invoke(this, (message, logEvent.Level));
        }
    }

    public static class MySinkExtensions
    {
        public static LoggerConfiguration EventSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new SerilogEventSink(formatProvider));
        }
    }
}
