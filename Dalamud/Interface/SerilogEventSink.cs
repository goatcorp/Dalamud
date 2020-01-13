using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Dalamud.Interface
{
    public class SerilogEventSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public static SerilogEventSink Instance;

        public event EventHandler<string> OnLogLine;

        public SerilogEventSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;

            Instance = this;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(_formatProvider);
            OnLogLine?.Invoke(this, $"[{DateTimeOffset.Now.ToString()}][{logEvent.Level}] {message}");
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
