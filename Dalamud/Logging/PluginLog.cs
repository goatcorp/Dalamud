using System;
using System.Reflection;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Serilog;
using Serilog.Events;

namespace Dalamud.Logging;

/// <summary>
/// Class offering various static methods to allow for logging in plugins.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
public class PluginLog : IServiceType, IDisposable
{
    private readonly LocalPlugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLog"/> class.
    /// Do not use this ctor, inject PluginLog instead.
    /// </summary>
    /// <param name="plugin">The plugin this service is scoped for.</param>
    internal PluginLog(LocalPlugin plugin)
    {
        this.plugin = plugin;
    }

    /// <summary>
    /// Gets or sets a prefix appended to log messages.
    /// </summary>
    public string? LogPrefix { get; set; } = null;

    #region Legacy static "Log" prefixed Serilog style methods

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

    #region Legacy static Serilog style methods

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

    #region New instanced methods

    /// <summary>
    /// Log some information.
    /// </summary>
    /// <param name="message">The message.</param>
    internal void Information(string message)
    {
        Serilog.Log.Information($"[{this.plugin.InternalName}] {this.LogPrefix} {message}");
    }

    #endregion

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        // ignored
    }

    private static ILogger GetPluginLogger(string? pluginName)
    {
        return Serilog.Log.ForContext("SourceContext", pluginName ?? string.Empty);
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

/// <summary>
/// Class offering logging services, for a specific type.
/// </summary>
/// <typeparam name="T">The type to log for.</typeparam>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
public class PluginLog<T> : PluginLog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLog{T}"/> class.
    /// Do not use this ctor, inject PluginLog instead.
    /// </summary>
    /// <param name="plugin">The plugin this service is scoped for.</param>
    internal PluginLog(LocalPlugin plugin)
        : base(plugin)
    {
        this.LogPrefix = typeof(T).Name;
    }
}
