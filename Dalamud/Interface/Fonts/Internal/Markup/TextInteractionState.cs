using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>Tracks link hover and click state for the most recently rendered styled text item.</summary>
/// <remarks><ul>
/// <li>Main thread only.</li>
/// <li>Interactions are reported until another item is submitted, as <see cref="ImGui.IsItemClicked()"/> does. Text
/// that is only measured does not replace it.</li>
/// <li>Text drawn for the same item keeps it unless a link of that text is hovered or held. ImGui draws the value and
/// then the label of <see cref="ImGui.LabelText(ImU8String, ImU8String)"/> for one item.</li>
/// </ul></remarks>
internal static class TextInteractionState
{
    private static State last;

    /// <summary>Records link interactions for the styled text item currently being rendered.</summary>
    /// <param name="markup">The markup format of the text.</param>
    /// <param name="offset">Byte offset of the link in the caller's input buffer.</param>
    /// <param name="length">Byte length of the link in the caller's input buffer.</param>
    /// <param name="source">The parsed markup buffer, which is never mutated after layout construction.</param>
    /// <param name="link">The link, with offsets into <paramref name="source"/>.</param>
    /// <param name="flags">The interaction flags.</param>
    /// <param name="linkState">Current hover or held state of the link.</param>
    /// <remarks>The text belongs to the last item of the current window.</remarks>
    public static unsafe void Set(
        TextMarkup markup, int offset, int length, byte[] source, in TextLink link, TextInteractionFlags flags,
        TextLinkState linkState)
    {
        var g = ImGui.GetCurrentContext();
        last = new(
            (nint)g.Handle,
            FontClock.Frame,
            (nint)g.CurrentWindow.Handle,
            GetLastItem(g),
            markup,
            offset,
            length,
            source,
            link,
            flags,
            linkState);
    }

    /// <summary>Clears recorded link state when another styled text item begins rendering without link interaction.</summary>
    /// <remarks>Keeps it if the text belongs to the same item. Cheap while nothing is recorded.</remarks>
    public static unsafe void Reset()
    {
        if (last.Flags == TextInteractionFlags.None)
            return;

        if (!IsLastItem(ImGui.GetCurrentContext()))
            last = default;
    }

    /// <summary>Gets the recorded link interaction if it belongs to the current window's last submitted item.</summary>
    /// <param name="interaction">The recorded interaction when returning <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if an interaction was recorded and no later item was submitted in the current
    /// window.</returns>
    public static unsafe bool TryGet(out TextInteraction interaction)
    {
        interaction = default;
        var r = last;
        if (r.Flags == TextInteractionFlags.None)
            return false;

        // A window that skips its items reports nothing. Any later item, even a clipped one, replaces the text's item.
        var g = ImGui.GetCurrentContext();
        var window = g.CurrentWindow;
        if (window.IsNull || window.SkipItems || !IsLastItem(g))
            return false;

        interaction = new(r.Link.ToContext(r.Markup, r.Source, r.LinkState), r.Offset, r.Length, r.Flags);
        return true;
    }

    private static unsafe bool IsLastItem(ImGuiContextPtr g) =>
        last.Context == (nint)g.Handle && last.Frame == FontClock.Frame &&
        last.Window == (nint)g.CurrentWindow.Handle && last.Item == GetLastItem(g);

    // The last item, by its ID and rectangle. Text items all have ID 0, and the rectangle tells them apart.
    private static (uint Id, Vector2 Min, Vector2 Max) GetLastItem(ImGuiContextPtr g)
    {
        ref readonly var item = ref g.LastItemData;
        return (item.ID, item.Rect.Min, item.Rect.Max);
    }

    private readonly record struct State(
        nint Context,
        long Frame,
        nint Window,
        (uint Id, Vector2 Min, Vector2 Max) Item,
        TextMarkup Markup,
        int Offset,
        int Length,
        byte[] Source,
        TextLink Link,
        TextInteractionFlags Flags,
        TextLinkState LinkState);
}
