using System;
using System.Text;

using NLog;
using NLog.LayoutRenderers;

namespace Dalamud.Logging.Internal
{
    /// <summary>
    /// The date and time in a long, sortable format yyyy-MM-dd HH:mm:ss.fff zzz.
    /// </summary>
    [LayoutRenderer("dalamud-datetime")]
    public class DalamudDateTimeLayoutRenderer : LayoutRenderer
    {
        /// <summary>
        /// Renders the date in the long format (yyyy-MM-dd HH:mm:ss.fff +/-TZ) and appends it to the specified <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the rendered data to.</param>
        /// <param name="logEvent">Logging event.</param>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var dt = new DateTimeOffset(logEvent.TimeStamp);
            builder.Append(dt.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        }
    }
}
