using System.Collections.Generic;
using System.Text;

using NLog;
using NLog.LayoutRenderers;

namespace Dalamud.Logging.Internal
{
    /// <summary>
    /// NLog renderer to transform the log level into a 3 letter abbreviation.
    /// </summary>
    [LayoutRenderer("dalamud-level")]
    public class DalamudLevelLayoutRenderer : LayoutRenderer
    {
        private static readonly Dictionary<int, string> Mapper = new()
        {
            { LogLevel.Trace.Ordinal, "VRB" },
            { LogLevel.Debug.Ordinal, "DBG" },
            { LogLevel.Info.Ordinal, "INF" },
            { LogLevel.Warn.Ordinal, "WRN" },
            { LogLevel.Error.Ordinal, "ERR" },
            { LogLevel.Fatal.Ordinal, "FTL" },
            { LogLevel.Off.Ordinal, "OFF" },
        };

        /// <inheritdoc/>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (!Mapper.TryGetValue(logEvent.Level.Ordinal, out var abbrev))
                abbrev = "UNK";

            builder.Append(abbrev);
        }
    }
}
