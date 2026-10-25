using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>What happened to a link of the styled text drawn last.</summary>
/// <remarks>Main thread only.</remarks>
internal static class TextInteractionState
{
    private static (nint Context, long Frame, TextInteractionRecord Record) last;

    /// <summary>Records what happened to a link of styled text being drawn, replacing what was before.</summary>
    /// <param name="record">What happened, or <see langword="default"/> for nothing.</param>
    public static unsafe void Set(in TextInteractionRecord record) =>
        last = ((nint)ImGui.GetCurrentContext().Handle, FontClock.Frame, record);

    /// <summary>Gets what happened to a link of the styled text drawn last in this frame.</summary>
    /// <param name="interaction">What happened.</param>
    /// <returns>Whether anything did.</returns>
    public static unsafe bool TryGet(out TextInteraction interaction)
    {
        ref readonly var r = ref last.Record;
        if (r.Flags == TextInteractionFlags.None ||
            last.Context != (nint)ImGui.GetCurrentContext().Handle ||
            last.Frame != FontClock.Frame)
        {
            interaction = default;
            return false;
        }

        var source = r.Source.AsSpan();
        interaction = new(
            r.Markup,
            source,
            r.Link.Offset,
            r.Link.Length,
            r.Offset,
            r.Length,
            source.Slice(r.Link.TargetOffset, r.Link.TargetLength),
            r.Flags,
            r.Held);
        return true;
    }
}
