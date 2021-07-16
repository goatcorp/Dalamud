using System;
using System.Reflection;

namespace Dalamud.Plugin
{
    /// <summary>
    /// Class offering various static methods to allow for logging in plugins.
    /// </summary>
    public static class PluginLog
    {
        #region "Log" prefixed Serilog style methods

        /// <summary>
        /// Log a templated message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Log(string messageTemplate, params object[] values)
            => Serilog.Log.Information($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Log(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Information(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated verbose message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogVerbose(string messageTemplate, params object[] values)
            => Serilog.Log.Verbose($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated verbose message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogVerbose(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Verbose(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogDebug(string messageTemplate, params object[] values)
            => Serilog.Log.Debug($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogDebug(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Debug(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogInformation(string messageTemplate, params object[] values)
            => Serilog.Log.Information($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogInformation(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Information(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogWarning(string messageTemplate, params object[] values)
            => Serilog.Log.Warning($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogWarning(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Warning(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogError(string messageTemplate, params object[] values)
            => Serilog.Log.Error($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogError(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Error(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogFatal(string messageTemplate, params object[] values)
            => Serilog.Log.Fatal($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogFatal(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Fatal(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        #endregion

        #region Serilog style methods

        /// <summary>
        /// Log a templated verbose message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Verbose(string messageTemplate, params object[] values)
            => Serilog.Log.Verbose($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated verbose message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Verbose(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Verbose(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Debug(string messageTemplate, params object[] values)
            => Serilog.Log.Debug($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Debug(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Debug(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Information(string messageTemplate, params object[] values)
            => Serilog.Log.Information($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Information(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Information(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Warning(string messageTemplate, params object[] values)
            => Serilog.Log.Warning($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Warning(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Warning(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Error(string messageTemplate, params object[] values)
            => Serilog.Log.Error($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Error(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Error(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Fatal(string messageTemplate, params object[] values)
            => Serilog.Log.Fatal($"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Fatal(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Fatal(exception, $"[{Assembly.GetCallingAssembly().GetName().Name}] {messageTemplate}", values);

        #endregion
    }
}
