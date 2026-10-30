using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Logging.Internal;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>Render-time hook for styled <see cref="TextLayout"/> instances that draws inline entities and handles link
/// hover, styling, and clicks.</summary>
/// <seealso cref="TextLayout.RenderHook"/>
internal sealed unsafe class StyledTextHook
{
    // Links are not items, so they must not become the navigation ID.
    private const ImGuiButtonFlags LinkButtons =
        ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle |
        (ImGuiButtonFlags)ImGuiButtonFlagsPrivate.NoNavFocus;

    private static readonly ModuleLog Log = new(typeof(StyledTextHook));

    private readonly TextMarkup markup;
    private readonly TextObjectContent[] contents;
    private readonly TextLink[] links;
    private readonly byte[] source;
    private readonly int[]? sourceToDrawn;
    private readonly ITextMarkupCallback[] callbacks;
    private readonly float size;
    private readonly int sourceHash;

    // The link BeginRender found hovered or held last, for GetLinkStyle.
    private ulong highlightLink;
    private bool highlightHeld;

    // Active ID and link from the last press, kept to track a held link when the pointer drags away.
    private uint heldId;
    private ulong heldLink;

    /// <summary>Initializes a new instance of the <see cref="StyledTextHook"/> class.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="styled">The styled text, whose source, objects and links are kept.</param>
    /// <param name="callbacks">The callbacks it was laid out with.</param>
    /// <param name="size">The glyph size.</param>
    public StyledTextHook(TextMarkup markup, StyledText styled, ITextMarkupCallback[] callbacks, float size)
    {
        this.markup = markup;
        this.contents = [.. styled.Contents];
        this.links = [.. styled.Links];
        this.source = styled.Source;
        this.sourceToDrawn = styled.SourceToDrawn;
        this.callbacks = callbacks;
        this.size = size;
        var hash = default(HashCode);
        hash.AddBytes(this.source);
        this.sourceHash = hash.ToHashCode();
    }

    /// <summary>Gets a value indicating whether the text has links, whose runs are drawn with <see cref="GetLinkStyle"/>.
    /// </summary>
    public bool HasLinks => this.links.Length != 0;

    /// <summary>Draws an inline object.</summary>
    /// <param name="drawList">The draw list.</param>
    /// <param name="objectIndex">The object.</param>
    /// <param name="topLeft">The top left of its box.</param>
    /// <param name="col">The text color the layout is drawn with.</param>
    /// <remarks>An entity that throws is logged and left out, so that the rest of the text is drawn.</remarks>
    public void DrawObject(ImDrawListPtr drawList, int objectIndex, Vector2 topLeft, uint col)
    {
        if ((uint)objectIndex >= (uint)this.contents.Length)
            return;

        ref readonly var c = ref this.contents[objectIndex];
        try
        {
            c.Entity.Draw(
                drawList,
                topLeft,
                new(
                    this.markup,
                    this.source,
                    c.SourceOffset,
                    c.Font,
                    this.size,
                    c.Entity.Data,
                    c.Size,
                    col));
        }
        catch (Exception e)
        {
            ReportCallbackFailure(c.Entity.GetType(), e);
        }
    }

    /// <summary>Called before the layout is drawn.</summary>
    /// <param name="layout">The layout.</param>
    /// <param name="drawList">The draw list.</param>
    /// <param name="pos">Position of the layout origin.</param>
    /// <param name="clip">The clip rectangle of the render.</param>
    /// <param name="textBegin">Start offset of the range drawn, in code units.</param>
    /// <param name="textEnd">End offset of the range drawn, in code units.</param>
    /// <remarks>Links react to the mouse when the text is drawn into the current window, as buttons. They do not
    /// take the mouse from items submitted before them, such as a selectable the text is the label of. Only the parts
    /// of links drawn within the clip rectangle and the range are hovered. Their IDs are from the ID stack and where
    /// the text is, as equal texts share this hook.</remarks>
    public void BeginRender(
        TextLayout layout, ImDrawListPtr drawList, Vector2 pos, in Vector4 clip, int textBegin, int textEnd)
    {
        this.highlightLink = 0;
        var g = ImGui.GetCurrentContext();
        var window = g.CurrentWindow;
        if (window.IsNull || window.DrawList.Handle != drawList.Handle)
            return;

        TextInteractionState.Reset();
        if (this.links.Length == 0)
            return;

        // Relative to the window's contents, so that scrolling or moving the window keeps the IDs.
        var at = pos - window.DC.CursorStartPos;

        var screenMouse = ImGui.GetMousePos();
        var mouse = screenMouse - pos;
        var link = 0ul;
        var rect = default(Vector4);
        if (screenMouse.X >= clip.X && screenMouse.Y >= clip.Y && screenMouse.X < clip.Z && screenMouse.Y < clip.W &&
            layout.TryGetItemAt(mouse, textBegin, textEnd, out var hitRun, out var hitLink) && hitLink != 0)
        {
            foreach (var r in layout.GetRunRects(hitRun))
            {
                if (mouse.X >= r.X && mouse.Y >= r.Y && mouse.X < r.Z && mouse.Y < r.W)
                    (link, rect) = (hitLink, r);
            }
        }

        // Keep tracking the held link via ButtonBehavior even when dragged outside its rect or clip bounds.
        if (g.ActiveId != 0 && g.ActiveId == this.heldId && g.ActiveIdWindow.Handle == window.Handle &&
            this.heldLink != 0 && this.heldLink != link && this.GetLinkId(this.heldLink, at) == g.ActiveId)
        {
            (link, rect) = (this.heldLink, default);
        }

        if (link == 0)
            return;

        // Clipped, so that a held link is not hovered where it is not drawn.
        var bb = new ImRect(
            Vector2.Max(pos + new Vector2(rect.X, rect.Y), new(clip.X, clip.Y)),
            Vector2.Min(pos + new Vector2(rect.Z, rect.W), new(clip.Z, clip.W)));

        // Links are not items (ItemAdd), so they keep their IDs alive themselves. ImGui drops a held ID otherwise.
        var hovered = false;
        var held = false;
        var id = this.GetLinkId(link, at);
        ImGuiP.KeepAliveID(id);

        var button = g.ActiveId == id ? g.ActiveIdMouseButton : -1;
        var pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held, LinkButtons);
        if (held)
            (this.heldId, this.heldLink) = (id, link);

        if (!hovered && !held)
            return;

        var flags = hovered ? TextInteractionFlags.Hover : TextInteractionFlags.None;
        if (pressed)
        {
            if (button is >= (int)ImGuiMouseButton.Left and <= (int)ImGuiMouseButton.Middle)
            {
                flags |= (TextInteractionFlags)((int)TextInteractionFlags.LeftClick << button);
            }
            else
            {
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    flags |= TextInteractionFlags.LeftClick;
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    flags |= TextInteractionFlags.RightClick;
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
                    flags |= TextInteractionFlags.MiddleClick;
            }
        }

        if (hovered)
            flags |= ExtraButtonClicks(layout, pos, link);

        ref readonly var l = ref this.links[(int)(link - 1)];
        var linkState = held ? TextLinkState.Held : TextLinkState.Hovered;
        for (var clicks = (uint)(flags & TextInteractionFlags.AnyClick); clicks != 0; clicks &= clicks - 1)
        {
            this.ActivateLink(l, (ImGuiMouseButton)(BitOperations.TrailingZeroCount(clicks) - 1), linkState);
        }

        var drawnBegin = this.sourceToDrawn?[l.Offset] ?? l.Offset;
        var drawnEnd = this.sourceToDrawn?[l.Offset + l.Length] ?? l.Offset + l.Length;
        TextInteractionState.Set(this.markup, drawnBegin, drawnEnd - drawnBegin, this.source, l, flags, linkState);
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        (this.highlightLink, this.highlightHeld) = (link, held);
    }

    /// <summary>Gets the style to draw a run of a link with.</summary>
    /// <param name="link">The user data of the run.</param>
    /// <param name="runStyle">The style of the run.</param>
    /// <param name="highlight">Whether to draw the link found hovered or held by <see cref="BeginRender"/> as such.
    /// </param>
    /// <returns>The style. Its fields that change the layout do not apply.</returns>
    /// <remarks>The callbacks may draw the link in another style. By default, a hovered or held link gets a
    /// background.</remarks>
    public TextStyle GetLinkStyle(ulong link, in TextStyle runStyle, bool highlight)
    {
        ref readonly var l = ref this.links[(int)(link - 1)];
        var style = TryGetLinkStyle(this.callbacks, this.GetLinkContext(l, TextLinkState.Normal), runStyle, out var s)
                        ? s
                        : runStyle;
        if (!highlight || link != this.highlightLink)
            return style;

        var state = this.highlightHeld ? TextLinkState.Held : TextLinkState.Hovered;
        return TryGetLinkStyle(this.callbacks, this.GetLinkContext(l, state), style, out s)
                   ? s
                   : style with
                   {
                       // Not GetColorU32, which applies the style alpha that the text color already has.
                       Background = ImGui.ColorConvertFloat4ToU32(
                           ImGui.GetStyle().Colors[
                               (int)(this.highlightHeld ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered)]),
                   };
    }

    /// <summary>Logs a failing markup callback.</summary>
    /// <param name="type">The callback type.</param>
    /// <param name="e">The exception it threw.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ReportCallbackFailure(Type type, Exception e) =>
        Log.Error(e, "A text callback of {Type} failed, and was left out", type);

    /// <summary>Queries callbacks in order for a link's style, logging and skipping any that throw.</summary>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="context">The link.</param>
    /// <param name="style">The style of the link so far.</param>
    /// <param name="result">The style the callback answered with.</param>
    /// <returns>Whether a callback answered.</returns>
    internal static bool TryGetLinkStyle(
        ITextMarkupCallback[] callbacks, in TextLinkContext context, in TextStyle style, out TextStyle result)
    {
        foreach (var callback in callbacks)
        {
            try
            {
                if (callback.TryGetLinkStyle(context, style, out result))
                    return true;
            }
            catch (Exception e)
            {
                ReportCallbackFailure(callback.GetType(), e);
            }
        }

        result = default;
        return false;
    }

    // ImGui buttons cannot take the extra mouse buttons. As for the others, a click is a press and a release on the
    // link.
    private static TextInteractionFlags ExtraButtonClicks(TextLayout layout, Vector2 pos, ulong link)
    {
        var flags = TextInteractionFlags.None;
        var clickedPos = ImGui.GetIO().MouseClickedPos;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.X1) &&
            IsInLink(layout, link, clickedPos[(int)ImGuiMouseButton.X1] - pos))
        {
            flags |= TextInteractionFlags.X1Click;
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.X2) &&
            IsInLink(layout, link, clickedPos[(int)ImGuiMouseButton.X2] - pos))
        {
            flags |= TextInteractionFlags.X2Click;
        }

        return flags;

        static bool IsInLink(TextLayout layout, ulong link, Vector2 p)
        {
            for (var i = 0; i < layout.RunCount; i++)
            {
                if (layout.GetRunUserData(i) != link)
                    continue;
                foreach (var r in layout.GetRunRects(i))
                {
                    if (p.X >= r.X && p.Y >= r.Y && p.X < r.Z && p.Y < r.W)
                        return true;
                }
            }

            return false;
        }
    }

    private void ActivateLink(in TextLink l, ImGuiMouseButton button, TextLinkState state)
    {
        var context = this.GetLinkContext(l, state);
        foreach (var callback in this.callbacks)
        {
            try
            {
                if (callback.TryActivateLink(context, button))
                    return;
            }
            catch (Exception e)
            {
                ReportCallbackFailure(callback.GetType(), e);
            }
        }
    }

    private TextLinkContext GetLinkContext(in TextLink l, TextLinkState state) =>
        l.ToContext(this.markup, this.source, state);

    private uint GetLinkId(ulong link, Vector2 at) => ImGui.GetID(HashCode.Combine(this.sourceHash, link, at.X, at.Y));
}
