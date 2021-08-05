using System;

using NLog;

namespace Dalamud.Logging.Internal
{
    /// <summary>
    /// This class offers a familiar Serilog styled interface for NLog logging.
    /// </summary>
    internal static class Log
    {
        private static Logger logger = LogManager.GetLogger("Dalamud");

        /// <summary>
        /// Log a templated verbose (trace) message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Verbose(string messageTemplate, params object[] values)
            => logger.Trace(messageTemplate, values);

        /// <summary>
        /// Log a templated verbose (trace) message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Verbose(Exception exception, string messageTemplate, params object[] values)
            => logger.Trace(exception, messageTemplate, values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Debug(string messageTemplate, params object[] values)
            => logger.Debug(messageTemplate, values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Debug(Exception exception, string messageTemplate, params object[] values)
            => logger.Debug(exception, messageTemplate, values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Information(string messageTemplate, params object[] values)
            => logger.Info(messageTemplate, values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Information(Exception exception, string messageTemplate, params object[] values)
            => logger.Info(exception, messageTemplate, values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Warning(string messageTemplate, params object[] values)
            => logger.Warn(messageTemplate, values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Warning(Exception exception, string messageTemplate, params object[] values)
            => logger.Warn(exception, messageTemplate, values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Error(string messageTemplate, params object[] values)
            => logger.Error(messageTemplate, values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Error(Exception exception, string messageTemplate, params object[] values)
            => logger.Error(exception, messageTemplate, values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Fatal(string messageTemplate, params object[] values)
            => logger.Fatal(messageTemplate, values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Fatal(Exception exception, string messageTemplate, params object[] values)
            => logger.Fatal(exception, messageTemplate, values);
    }
}
