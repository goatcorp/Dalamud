using System.Reflection;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Serilog;
using Serilog.Events;

namespace Dalamud.Logging;

/// <summary>
/// Class offering various static methods to allow for logging in plugins.
/// </summary>
/// <remarks>
/// PluginLog has been obsoleted and replaced by the <see cref="IPluginLog"/> service. Developers are encouraged to
/// move over as soon as reasonably possible for performance reasons.
/// </remarks>
[Obsolete("Static PluginLog will be removed in API 10. Developers should use IPluginLog.")]
[Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
public static class PluginLog
{
    #region "Log" prefixed Serilog style methods

    /// <summary>
    /// Log a templated message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Log(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Information, messageTemplate, null, values);

    /// <summary>
    /// Log a templated message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Log(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Information, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated verbose message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogVerbose(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Verbose, messageTemplate, null, values);

    /// <summary>
    /// Log a templated verbose message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogVerbose(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Verbose, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated debug message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogDebug(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Debug, messageTemplate, null, values);

    /// <summary>
    /// Log a templated debug message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogDebug(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Debug, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated information message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogInformation(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Information, messageTemplate, null, values);

    /// <summary>
    /// Log a templated information message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogInformation(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Information, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated warning message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogWarning(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Warning, messageTemplate, null, values);

    /// <summary>
    /// Log a templated warning message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogWarning(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Warning, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated error message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogError(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Error, messageTemplate, null, values);

    /// <summary>
    /// Log a templated error message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogError(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Error, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated fatal message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogFatal(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Fatal, messageTemplate, null, values);

    /// <summary>
    /// Log a templated fatal message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogFatal(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Fatal, messageTemplate, exception, values);

    #endregion

    #region Serilog style methods

    /// <summary>
    /// Log a templated verbose message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Verbose(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Verbose, messageTemplate, null, values);

    /// <summary>
    /// Log a templated verbose message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Verbose(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Verbose, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated debug message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Debug(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Debug, messageTemplate, null, values);

    /// <summary>
    /// Log a templated debug message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Debug(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Debug, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated information message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Information(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Information, messageTemplate, null, values);

    /// <summary>
    /// Log a templated information message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Information(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Information, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated warning message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Warning(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Warning, messageTemplate, null, values);

    /// <summary>
    /// Log a templated warning message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Warning(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Warning, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated error message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Error(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Error, messageTemplate, null, values);

    /// <summary>
    /// Log a templated error message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Error(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Error, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated fatal message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Fatal(string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Fatal, messageTemplate, null, values);

    /// <summary>
    /// Log a templated fatal message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void Fatal(Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, LogEventLevel.Fatal, messageTemplate, exception, values);

    #endregion

    /// <summary>
    /// Log a message to the in-game log, setting level at runtime.
    /// </summary>
    /// <remarks>
    /// This method is primarily meant for interoperability with other logging systems that may want to be forwarded to
    /// the PluginLog.
    /// </remarks>
    /// <param name="level">The log level for this message.</param>
    /// <param name="exception">An exception (if any) to include in this log message.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    public static void LogRaw(LogEventLevel level, Exception? exception, string messageTemplate, params object[] values)
        => WriteLog(Assembly.GetCallingAssembly().GetName().Name, level, messageTemplate, exception, values);

    private static ILogger GetPluginLogger(string? pluginName)
    {
        return Serilog.Log.ForContext("Dalamud.PluginName", pluginName ?? string.Empty);
    }

    private static void WriteLog(string? pluginName, LogEventLevel level, string messageTemplate, Exception? exception = null, params object[] values)
    {
        var pluginLogger = GetPluginLogger(pluginName);

        // FIXME: Eventually, the `pluginName` tag should be removed from here and moved over to the actual log
        //        formatter.
        pluginLogger.Write(
            level,
            exception: exception,
            messageTemplate: $"[{pluginName}] {messageTemplate}",
            values);
    }
}
