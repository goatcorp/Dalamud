using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dalamud.Logging;

/// <summary>
/// Implementation of <see cref="IPluginLog"/>.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IPluginLog>]
#pragma warning restore SA1015
internal class ScopedPluginLogService : IServiceType, IPluginLog
{
    private readonly LocalPlugin localPlugin;

    private readonly LoggingLevelSwitch levelSwitch;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedPluginLogService"/> class.
    /// </summary>
    /// <param name="localPlugin">The plugin that owns this service.</param>
    internal ScopedPluginLogService(LocalPlugin localPlugin)
    {
        this.localPlugin = localPlugin;
        
        this.levelSwitch = new LoggingLevelSwitch(this.GetDefaultLevel());

        var loggerConfiguration = new LoggerConfiguration()
                                  .Enrich.WithProperty("Dalamud.PluginName", localPlugin.InternalName)
                                  .MinimumLevel.ControlledBy(this.levelSwitch)
                                  .WriteTo.Logger(Log.Logger);

        this.Logger = loggerConfiguration.CreateLogger();
    }

    /// <inheritdoc />
    public LogEventLevel MinimumLogLevel
    {
        get => this.levelSwitch.MinimumLevel;
        set => this.levelSwitch.MinimumLevel = value;
    }

    /// <summary>
    /// Gets a logger that may be exposed to plugins some day.
    /// </summary>
    public ILogger Logger { get; }

    /// <inheritdoc />
    public void Fatal(string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Fatal, null, messageTemplate, values);

    /// <inheritdoc />
    public void Fatal(Exception? exception, string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Fatal, exception, messageTemplate, values);

    /// <inheritdoc />
    public void Error(string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Error, null, messageTemplate, values);

    /// <inheritdoc />
    public void Error(Exception? exception, string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Error, exception, messageTemplate, values);

    /// <inheritdoc />
    public void Warning(string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Warning, null, messageTemplate, values);

    /// <inheritdoc />
    public void Warning(Exception? exception, string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Warning, exception, messageTemplate, values);

    /// <inheritdoc />
    public void Information(string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Information, null, messageTemplate, values);

    /// <inheritdoc />
    public void Information(Exception? exception, string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Information, exception, messageTemplate, values);
    
    /// <inheritdoc/>
    public void Info(string messageTemplate, params object[] values) =>
        this.Information(messageTemplate, values);
    
    /// <inheritdoc/>
    public void Info(Exception? exception, string messageTemplate, params object[] values) =>
        this.Information(exception, messageTemplate, values);

    /// <inheritdoc />
    public void Debug(string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Debug, null, messageTemplate, values);

    /// <inheritdoc />
    public void Debug(Exception? exception, string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Debug, exception, messageTemplate, values);

    /// <inheritdoc />
    public void Verbose(string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Verbose, null, messageTemplate, values);

    /// <inheritdoc />
    public void Verbose(Exception? exception, string messageTemplate, params object[] values) =>
        this.Write(LogEventLevel.Verbose, exception, messageTemplate, values);
    
    /// <inheritdoc />
    public void Write(LogEventLevel level, Exception? exception, string messageTemplate, params object[] values)
    {
        this.Logger.Write(
            level,
            exception: exception,
            messageTemplate: $"[{this.localPlugin.InternalName}] {messageTemplate}",
            values);
    }

    /// <summary>
    /// Gets the default log level for this plugin.
    /// </summary>
    /// <returns>A log level.</returns>
    private LogEventLevel GetDefaultLevel()
    {
        // TODO: Add some way to save log levels to a config. Or let plugins handle it?
        
        return this.localPlugin.IsDev ? LogEventLevel.Verbose : LogEventLevel.Debug;
    }
}
