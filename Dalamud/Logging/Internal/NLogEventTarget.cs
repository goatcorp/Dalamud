using System;

using NLog;
using NLog.Targets;

namespace Dalamud.Logging.Internal
{
    /// <summary>
    /// An NLog target that fires an event on every log line that is received.
    /// </summary>
    internal class NLogEventTarget : Target
    {
        private static NLogEventTarget instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="NLogEventTarget"/> class.
        /// </summary>
        private NLogEventTarget()
        {
            this.Name = "event";
        }

        /// <summary>
        /// Event on a log line being emitted.
        /// </summary>
        public event EventHandler<(string Line, LogLevel Level, DateTimeOffset TimeStamp)> OnLogLine;

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static NLogEventTarget Instance => instance ??= new NLogEventTarget();

        /// <inheritdoc/>
        protected override void Write(LogEventInfo logEvent)
        {
            var message = logEvent.FormattedMessage;

            if (logEvent.Exception != null)
            {
                message += "\n" + logEvent.Exception;
            }

            this.OnLogLine?.Invoke(this, (message, logEvent.Level, DateTimeOffset.Now));
        }
    }
}
