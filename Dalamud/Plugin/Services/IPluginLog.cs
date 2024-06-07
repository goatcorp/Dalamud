using Serilog.Events;

#pragma warning disable CS1573 // See https://github.com/dotnet/roslyn/issues/40325

namespace Dalamud.Plugin.Services;

/// <summary>
/// An opinionated service to handle logging for plugins.
/// </summary>
public interface IPluginLog
{
    /// <summary>
    /// Gets or sets the minimum log level that will be recorded from this plugin to Dalamud's logs. This may be set
    /// by either the plugin or by Dalamud itself.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="LogEventLevel.Debug"/> for downloaded plugins, and <see cref="LogEventLevel.Verbose"/>
    /// for dev plugins.
    /// </remarks>
    LogEventLevel MinimumLogLevel { get; set; }

    /// <summary>
    /// Log a <see cref="LogEventLevel.Fatal" /> message to the Dalamud log for this plugin. This log level should be
    /// used primarily for unrecoverable errors or critical faults in a plugin.
    /// </summary>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Fatal(string messageTemplate, params object[] values);
    
    /// <inheritdoc cref="Fatal(string,object[])"/>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    void Fatal(Exception? exception, string messageTemplate, params object[] values);
    
    /// <summary>
    /// Log a <see cref="LogEventLevel.Error" /> message to the Dalamud log for this plugin. This log level should be
    /// used for recoverable errors or faults that impact plugin functionality.
    /// </summary>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Error(string messageTemplate, params object[] values);
    
    /// <inheritdoc cref="Error(string,object[])"/>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    void Error(Exception? exception, string messageTemplate, params object[] values);
    
    /// <summary>
    /// Log a <see cref="LogEventLevel.Warning" /> message to the Dalamud log for this plugin. This log level should be
    /// used for user error, potential problems, or high-importance messages that should be logged. 
    /// </summary>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Warning(string messageTemplate, params object[] values);
    
    /// <inheritdoc cref="Warning(string,object[])"/>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    void Warning(Exception? exception, string messageTemplate, params object[] values);
    
    /// <summary>
    /// Log an <see cref="LogEventLevel.Information" /> message to the Dalamud log for this plugin. This log level
    /// should be used for general plugin operations and other relevant information to track a plugin's behavior. 
    /// </summary>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Information(string messageTemplate, params object[] values);
    
    /// <inheritdoc cref="Information(string,object[])"/>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    void Information(Exception? exception, string messageTemplate, params object[] values);

    /// <inheritdoc cref="Information(string,object[])"/>
    void Info(string messageTemplate, params object[] values);

    /// <inheritdoc cref="Information(Exception?,string,object[])"/>
    void Info(Exception? exception, string messageTemplate, params object[] values);
    
    /// <summary>
    /// Log a <see cref="LogEventLevel.Debug" /> message to the Dalamud log for this plugin. This log level should be
    /// used for messages or information that aid with debugging or tracing a plugin's operations, but should not be
    /// recorded unless requested.
    /// </summary>
    /// <remarks>
    /// By default, this log level is below the default log level of Dalamud. Messages logged at this level will not be
    /// recorded unless the global log level is specifically set to Debug or lower. If information should be generally
    /// or easily accessible for support purposes without the user taking additional action, consider using the
    /// Information level instead. Developers should <em>not</em> use this log level where it can be triggered on a
    /// per-frame basis.
    /// </remarks>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Debug(string messageTemplate, params object[] values);

    /// <inheritdoc cref="Debug(string,object[])"/>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    void Debug(Exception? exception, string messageTemplate, params object[] values);

    /// <summary>
    /// Log a <see cref="LogEventLevel.Verbose" /> message to the Dalamud log for this plugin. This log level is
    /// intended almost primarily for development purposes and detailed tracing of a plugin's operations. Verbose logs
    /// should not be used to expose information useful for support purposes.
    /// </summary>
    /// <remarks>
    /// By default, this log level is below the default log level of Dalamud. Messages logged at this level will not be
    /// recorded unless the global log level is specifically set to Verbose. Release plugins must also set the
    /// <see cref="MinimumLogLevel"/> to <see cref="LogEventLevel.Verbose"/> to use this level, and should only do so
    /// upon specific user request (e.g. a "Enable Troubleshooting Logs" button). 
    /// </remarks>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Verbose(string messageTemplate, params object[] values);

    /// <inheritdoc cref="Verbose(string,object[])"/>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    void Verbose(Exception? exception, string messageTemplate, params object[] values);

    /// <summary>
    /// Write a raw log event to the plugin's log. Used for interoperability with other log systems, as well as
    /// advanced use cases.
    /// </summary>
    /// <param name="level">The log level for this event.</param>
    /// <param name="exception">An (optional) exception that should be recorded alongside this event.</param>
    /// <param name="messageTemplate">Message template describing the event.</param>
    /// <param name="values">Objects positionally formatted into the message template.</param>
    void Write(LogEventLevel level, Exception? exception, string messageTemplate, params object[] values);
}
