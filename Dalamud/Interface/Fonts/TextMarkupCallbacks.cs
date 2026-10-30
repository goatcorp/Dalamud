using System.Text;

using Dalamud.Bindings.ImGui;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Built-in <see cref="ITextMarkupCallback"/> implementations.</summary>
public static class TextMarkupCallbacks
{
    /// <summary>Gets a callback that keeps the edge color of the font, ignoring <see cref="MacroCode.EdgeColor"/> and
    /// <see cref="MacroCode.EdgeColorType"/>.</summary>
    public static ISeStringCallback IgnoreEdgeColor { get; } = new IgnoreEdgeColorCallback();

    /// <summary>Gets a callback that finds http and https URLs, and ones that start with <c>www.</c>, in text.
    /// </summary>
    /// <remarks><ul>
    /// <li>The link target is the URL. URLs that start with <c>www.</c> get <c>https://</c> in front.</li>
    /// <li>A URL ends at a space, a control character, a non-ASCII character, or one of <c>"&lt;&gt;\^`{|}</c>.
    /// Trailing <c>.,;:!?'</c> are left out, and so are closing brackets without an opening one in the URL.</li>
    /// <li>Detected links use <see cref="ImGuiCol.TextLink"/> with an underline, and add an
    /// <see cref="ImGuiCol.ButtonHovered"/> or <see cref="ImGuiCol.ButtonActive"/> background when hovered or held.</li>
    /// <li>It does not open links. Use <see cref="CreateUrlsWithHandler"/> or
    /// <see cref="ImGuiTextExtensions.extension(ImGui).TryGetTextInteraction"/> for that.</li>
    /// </ul></remarks>
    public static ITextMarkupCallback Urls { get; } = new UrlCallback(null);

    /// <summary>Creates a callback that finds and draws URLs as <see cref="Urls"/> does, and acts on clicks on them.
    /// </summary>
    /// <param name="handler">Called when a URL it found is clicked.</param>
    /// <returns>The callback. Keep it, rather than creating one every frame.</returns>
    /// <remarks>This callback does not prompt before invoking <paramref name="handler"/>, so handlers used on untrusted
    /// text should confirm with the user before opening external URLs.</remarks>
    /// <example><code>
    /// TextMarkupCallbacks.CreateUrlsWithHandler(static (url, button) =>
    /// {
    ///     if (button == ImGuiMouseButton.Left)
    ///         Util.OpenLink(url);
    /// });
    /// </code></example>
    public static ITextMarkupCallback CreateUrlsWithHandler(UrlClickHandler handler) =>
        new UrlCallback(handler ?? throw new ArgumentNullException(nameof(handler)));

    /// <summary>Creates a callback that acts on clicks on links of SeStrings.</summary>
    /// <param name="handler">Called with the link payload when a link is clicked.</param>
    /// <returns>The callback. Keep it, rather than creating one every frame.</returns>
    /// <remarks>It covers links of <see cref="TextMarkup.SeString"/> and <see cref="TextMarkup.MacroString"/> text.
    /// Links that callbacks found are left to other callbacks.</remarks>
    public static ITextMarkupCallback CreateSeStringLinkHandler(SeStringLinkHandler handler) =>
        new SeStringLinkCallback(handler ?? throw new ArgumentNullException(nameof(handler)));

    private sealed class IgnoreEdgeColorCallback : ISeStringCallback
    {
        public bool? ShouldHandle(ReadOnlySePayloadSpan payload) =>
            payload.MacroCode is MacroCode.EdgeColor or MacroCode.EdgeColorType ? false : null;
    }

    private sealed class SeStringLinkCallback(SeStringLinkHandler handler) : ITextMarkupCallback
    {
        public bool TryActivateLink(in TextLinkContext link, ImGuiMouseButton button)
        {
            if (!link.TryGetSePayload(out var payload))
                return false;
            handler(payload, button);
            return true;
        }
    }

    private sealed class UrlCallback(UrlClickHandler? handler) : ITextMarkupCallback
    {
        public bool TryGetLinkStyle(in TextLinkContext link, in TextStyle style, out TextStyle result)
        {
            result = style;
            if (!IsUrl(link))
                return false;
            var colors = ImGui.GetStyle().Colors;
            result.Color = colors[(int)ImGuiCol.TextLink];
            result.Lines = style.Lines.IsOn
                               ? style.Lines with { Positions = style.Lines.Positions | TextLinePosition.Under }
                               : new(TextLinePosition.Under);

            // Not GetColorU32, which applies the style alpha that the text color already has.
            if (link.State != TextLinkState.Normal)
            {
                result.Background = ImGui.ColorConvertFloat4ToU32(
                    colors[(int)(link.Held ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered)]);
            }

            return true;
        }

        public bool TryActivateLink(in TextLinkContext link, ImGuiMouseButton button)
        {
            if (handler is null || !IsUrl(link))
                return false;
            handler(Encoding.UTF8.GetString(link.Target), button);
            return true;
        }

        public void FindLinks(in TextLinkSearchContext context, TextLinkSearchResults links)
        {
            var text = context.Text;
            Span<byte> buffer = stackalloc byte[256];
            for (var i = 0; i < text.Length; i++)
            {
                if (i > 0 && char.IsAsciiLetterOrDigit((char)text[i - 1]))
                    continue;
                var rest = text[i..];
                var prefix = StartsWith(rest, "https://"u8) ? 8
                             : StartsWith(rest, "http://"u8) ? 7
                             : StartsWith(rest, "www."u8) ? 4
                             : 0;
                if (prefix == 0)
                    continue;
                // Skip www. after domain, path, email, or identifier delimiters so mid-token segments are not matched.
                if (prefix == 4 && i > 0 &&
                    text[i - 1] is (byte)'.' or (byte)'/' or (byte)'@' or (byte)'-' or (byte)'_')
                    continue;

                var end = prefix;
                while (end < rest.Length && IsUrlByte(rest[end]))
                    end++;
                while (end > prefix && IsTrailing(rest[..end]))
                    end--;
                if (end == prefix)
                    continue;

                var url = rest[..end];
                if (prefix == 4)
                {
                    var host = rest[prefix..end];
                    var authorityEnd = host.IndexOfAny("/?#"u8);
                    var domain = authorityEnd >= 0 ? host[..authorityEnd] : host;
                    var dot = domain.IndexOf((byte)'.');
                    if (dot <= 0 || domain[^1] == (byte)'.')
                        continue;

                    var target = url.Length + 8 <= buffer.Length ? buffer[..(url.Length + 8)] : new byte[url.Length + 8];
                    "https://"u8.CopyTo(target);
                    url.CopyTo(target[8..]);
                    links.Add(i, end, target);
                }
                else
                {
                    links.Add(i, end, url);
                }

                i += end - 1;
            }
        }

        // A link found by a callback whose target is an http or https URL, as this one makes them. Links other
        // callbacks found are left to them.
        private static bool IsUrl(in TextLinkContext link) =>
            link.Detected && (StartsWith(link.Target, "https://"u8) || StartsWith(link.Target, "http://"u8));

        private static bool StartsWith(ReadOnlySpan<byte> text, ReadOnlySpan<byte> prefix) =>
            text.Length >= prefix.Length && Ascii.EqualsIgnoreCase(text[..prefix.Length], prefix);

        private static bool IsUrlByte(byte b) => b is > 0x20 and < 0x7F && !"\"<>\\^`{|}"u8.Contains(b);

        private static bool IsTrailing(ReadOnlySpan<byte> url)
        {
            var last = url[^1];
            if (".,;:!?'"u8.Contains(last))
                return true;
            var open = last switch
            {
                (byte)')' => (byte)'(',
                (byte)']' => (byte)'[',
                _ => (byte)0,
            };
            return open != 0 && url.Count(open) < url.Count(last);
        }
    }
}
