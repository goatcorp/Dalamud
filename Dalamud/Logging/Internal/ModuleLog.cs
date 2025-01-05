using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;

namespace Dalamud.Logging.Internal;

/// <summary>
/// Class offering various methods to allow for logging in Dalamud modules.
/// </summary>
public class ModuleLog
{
    private readonly string moduleName;
    private readonly ILogger moduleLogger;

    // FIXME (v9): Deprecate this class in favor of using contextualized ILoggers with proper formatting.
    //             We can keep this class around as a Serilog helper, but ModuleLog should no longer be a returned
    //             type, instead returning a (prepared) ILogger appropriately.

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLog"/> class.
    /// This class can be used to prefix logging messages with a Dalamud module name prefix. For example, "[PLUGINR] ...".
    /// </summary>
    /// <param name="moduleName">The module name.</param>
    public ModuleLog(string? moduleName)
    {
        this.moduleName = moduleName ?? "DalamudInternal";
        this.moduleLogger = Log.ForContext("Dalamud.ModuleName", this.moduleName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLog"/> class.
    /// This class will properly attach SourceContext and other attributes per Serilog standards.
    /// </summary>
    /// <param name="type">The type of the class this logger is for.</param>
    public ModuleLog(Type type)
    {
        this.moduleName = type.Name;
        this.moduleLogger = Log.ForContext(
        [
            new PropertyEnricher(Constants.SourceContextPropertyName, type.FullName),
            new PropertyEnricher("Dalamud.ModuleName", this.moduleName)
        ]);
    }

    /// <summary>
    /// Helper method to create a new <see cref="ModuleLog"/> instance based on a type.
    /// </summary>
    /// <typeparam name="T">The class to create this ModuleLog for.</typeparam>
    /// <returns>Returns a ModuleLog with name set.</returns>
    internal static ModuleLog Create<T>() => new(typeof(T));

    /// <summary>
    /// Log a templated verbose message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Verbose(string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Verbose, messageTemplate, null, values);

    /// <summary>
    /// Log a templated verbose message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Verbose(Exception? exception, string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Verbose, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated debug message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Debug(string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Debug, messageTemplate, null, values);

    /// <summary>
    /// Log a templated debug message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Debug(Exception? exception, string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Debug, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated information message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Information(string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Information, messageTemplate, null, values);

    /// <summary>
    /// Log a templated information message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Information(Exception? exception, string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Information, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated warning message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Warning(string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Warning, messageTemplate, null, values);

    /// <summary>
    /// Log a templated warning message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Warning(Exception? exception, string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Warning, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated error message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Error(string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Error, messageTemplate, null, values);

    /// <summary>
    /// Log a templated error message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Error(Exception? exception, string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Error, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated fatal message to the in-game debug log.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Fatal(string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Fatal, messageTemplate, null, values);

    /// <summary>
    /// Log a templated fatal message to the in-game debug log.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void Fatal(Exception? exception, string messageTemplate, params object?[] values)
        => this.WriteLog(LogEventLevel.Fatal, messageTemplate, exception, values);

    /// <summary>
    /// Log a templated message to the in-game debug log.
    /// </summary>
    /// <param name="level">The log level to log with.</param>
    /// <param name="messageTemplate">The message template to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="values">Values to log.</param>
    [MessageTemplateFormatMethod("messageTemplate")]
    public void WriteLog(
        LogEventLevel level, string messageTemplate, Exception? exception = null, params object?[] values)
    {
        // FIXME: Eventually, the `pluginName` tag should be removed from here and moved over to the actual log
        //        formatter.
        this.moduleLogger.Write(
            level,
            exception: exception,
            messageTemplate: $"[{this.moduleName}] {messageTemplate}",
            values);
    }
}
