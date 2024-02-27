using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using CheapLoc;

using Dalamud.Logging.Internal;

namespace Dalamud.Utility;

/// <summary>
/// Utility functions for <see cref="DateTime"/> and <see cref="TimeSpan"/>.
/// </summary>
public static class DateTimeSpanExtensions
{
    private static readonly ModuleLog Log = new(nameof(DateTimeSpanExtensions));

    private static ParsedRelativeFormatStrings? relativeFormatStringLong;

    private static ParsedRelativeFormatStrings? relativeFormatStringShort;

    /// <summary>Formats an instance of <see cref="DateTime"/> as a localized absolute time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    /// <remarks>The string will be formatted according to Square Enix Account region settings, if Dalamud default
    /// language is English.</remarks>
    public static unsafe string LocAbsolute(this DateTime when)
    {
        var culture = Service<Localization>.GetNullable()?.DalamudLanguageCultureInfo ?? CultureInfo.InvariantCulture;
        if (!Equals(culture, CultureInfo.InvariantCulture))
            return when.ToString("G", culture);

        var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        var region = 0;
        if (framework is not null)
            region = framework->Region;
        return region switch
        {
            1 => when.ToString("MM/dd/yyyy HH:mm:ss"), // na
            2 => when.ToString("dd-mm-yyyy HH:mm:ss"), // eu
            _ => when.ToString("yyyy-MM-dd HH:mm:ss"), // jp(0), cn(3), kr(4), and other possible errorneous cases
        };
    }

    /// <summary>Formats an instance of <see cref="DateTime"/> as a localized relative time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    public static string LocRelativePastLong(this DateTime when)
    {
        var loc = Loc.Localize(
            "DateTimeSpanExtensions.RelativeFormatStringsLong",
            "172800,{0:%d} days ago\n86400,yesterday\n7200,{0:%h} hours ago\n3600,an hour ago\n120,{0:%m} minutes ago\n60,a minute ago\n2,{0:%s} seconds ago\n1,a second ago\n-Infinity,just now");
        Debug.Assert(loc != null, "loc != null");

        if (relativeFormatStringLong?.FormatStringLoc != loc)
            relativeFormatStringLong ??= new(loc);

        return relativeFormatStringLong.Format(DateTime.Now - when);
    }

    /// <summary>Formats an instance of <see cref="DateTime"/> as a localized relative time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    public static string LocRelativePastShort(this DateTime when)
    {
        var loc = Loc.Localize(
            "DateTimeSpanExtensions.RelativeFormatStringsShort",
            "86400,{0:%d}d\n3600,{0:%h}h\n60,{0:%m}m\n1,{0:%s}s\n-Infinity,now");
        Debug.Assert(loc != null, "loc != null");

        if (relativeFormatStringShort?.FormatStringLoc != loc)
            relativeFormatStringShort = new(loc);

        return relativeFormatStringShort.Format(DateTime.Now - when);
    }

    private sealed class ParsedRelativeFormatStrings
    {
        private readonly List<(float MinSeconds, string FormatString)> formatStrings = new();

        public ParsedRelativeFormatStrings(string value)
        {
            this.FormatStringLoc = value;
            foreach (var line in value.Split("\n"))
            {
                var sep = line.IndexOf(',');
                if (sep < 0)
                {
                    Log.Error("A line without comma has been found: {line}", line);
                    continue;
                }

                if (!float.TryParse(
                        line.AsSpan(0, sep),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var seconds))
                {
                    Log.Error("Could not parse the duration: {line}", line);
                    continue;
                }

                this.formatStrings.Add((seconds, line[(sep + 1)..]));
            }

            this.formatStrings.Sort((a, b) => b.MinSeconds.CompareTo(a.MinSeconds));
        }

        public string FormatStringLoc { get; }

        /// <summary>Formats an instance of <see cref="TimeSpan"/> as a localized string.</summary>
        /// <param name="ts">The duration.</param>
        /// <returns>The formatted string.</returns>
        public string Format(TimeSpan ts)
        {
            foreach (var (minSeconds, formatString) in this.formatStrings)
            {
                if (ts.TotalSeconds >= minSeconds)
                    return string.Format(formatString, ts);
            }

            return this.formatStrings[^1].FormatString.Format(ts);
        }
    }
}
