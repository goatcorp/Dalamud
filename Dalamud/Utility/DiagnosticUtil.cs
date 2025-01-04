using System.Diagnostics;
using System.Linq;

namespace Dalamud.Utility;

/// <summary>
/// A set of utilities for diagnostics.
/// </summary>
public static class DiagnosticUtil
{
    private static readonly string[] IgnoredNamespaces = [
        nameof(System),
        nameof(ImGuiNET.ImGuiNative)
    ];

    /// <summary>
    /// Gets a stack trace that filters out irrelevant frames.
    /// </summary>
    /// <param name="source">The source stacktrace to filter.</param>
    /// <returns>Returns a stack trace with "extra" frames removed.</returns>
    public static StackTrace GetUsefulTrace(StackTrace source)
    {
        var frames = source.GetFrames().SkipWhile(
            f =>
            {
                var frameNs = f.GetMethod()?.DeclaringType?.Namespace;
                return frameNs == null || IgnoredNamespaces.Any(i => frameNs.StartsWith(i, true, null));
            });

        return new StackTrace(frames);
    }
}
