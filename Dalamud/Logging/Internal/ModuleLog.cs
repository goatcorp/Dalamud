using System;

namespace Dalamud.Logging.Internal
{
    /// <summary>
    /// Class offering various methods to allow for logging in Dalamud modules.
    /// </summary>
    public class ModuleLog
    {
        private readonly string moduleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleLog"/> class.
        /// This class can be used to prefix logging messages with a Dalamud module name prefix. For example, "[PLUGINR] ...".
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        public ModuleLog(string moduleName)
        {
            this.moduleName = moduleName;
        }

        /// <summary>
        /// Log a templated verbose message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Verbose(string messageTemplate, params object[] values)
            => Serilog.Log.Verbose($"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated verbose message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Verbose(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Verbose(exception, $"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Debug(string messageTemplate, params object[] values)
            => Serilog.Log.Debug($"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated debug message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Debug(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Debug(exception, $"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Information(string messageTemplate, params object[] values)
            => Serilog.Log.Information($"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated information message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Information(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Information(exception, $"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Warning(string messageTemplate, params object[] values)
            => Serilog.Log.Warning($"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated warning message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Warning(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Warning(exception, $"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Error(string messageTemplate, params object[] values)
            => Serilog.Log.Error($"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Error(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Error(exception, $"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Fatal(string messageTemplate, params object[] values)
            => Serilog.Log.Fatal($"[{this.moduleName}] {messageTemplate}", values);

        /// <summary>
        /// Log a templated fatal message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public void Fatal(Exception exception, string messageTemplate, params object[] values)
            => Serilog.Log.Fatal(exception, $"[{this.moduleName}] {messageTemplate}", values);
    }
}
