using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Plugin
{
    public static class PluginLog
    {
        /// <summary>
        /// Log a templated message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void Log(string messageTemplate, params object[] values)
        {
            var name = Assembly.GetCallingAssembly().GetName().Name;

            Serilog.Log.Information($"[{name}] {messageTemplate}", values);
        }

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogError(string messageTemplate, params object[] values)
        {
            var name = Assembly.GetCallingAssembly().GetName().Name;

            Serilog.Log.Error($"[{name}] {messageTemplate}", values);
        }

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        public static void LogError(Exception exception, string messageTemplate, params object[] values)
        {
            var name = Assembly.GetCallingAssembly().GetName().Name;

            Serilog.Log.Error(exception, $"[{name}] {messageTemplate}", values);
        }
    }
}
