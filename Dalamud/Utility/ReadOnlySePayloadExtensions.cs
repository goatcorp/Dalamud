using Dalamud.Game.Text.SeStringHandling.Payloads;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// Extension functions for <see cref="ReadOnlySePayload"/>.
/// </summary>
public static class ReadOnlySePayloadExtensions
{
    /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.IsMacro(ReadOnlySePayloadSpan, MacroCode)"/>
    public static bool IsMacro(this ReadOnlySePayload payload, MacroCode macroCode)
    {
        return payload.AsSpan().IsMacro(macroCode);
    }

    /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.IsLink(ReadOnlySePayloadSpan, LinkMacroPayloadType)"/>
    public static bool IsLink(this ReadOnlySePayload payload, LinkMacroPayloadType linkType)
    {
        return payload.AsSpan().IsLink(linkType);
    }

    /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.IsLinkTerminator(ReadOnlySePayloadSpan)"/>
    public static bool IsLinkTerminator(this ReadOnlySePayload payload)
    {
        return payload.AsSpan().IsLinkTerminator();
    }

    /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.TryReadLink(ReadOnlySePayloadSpan, out LinkMacroPayloadType, out ReadOnlySeExpressionSpan, out ReadOnlySeExpressionSpan, out ReadOnlySeExpressionSpan, out ReadOnlySeExpressionSpan)"/>
    public static bool TryReadLink(
        this ReadOnlySePayload payload,
        out LinkMacroPayloadType linkType,
        out ReadOnlySeExpressionSpan expr1,
        out ReadOnlySeExpressionSpan expr2,
        out ReadOnlySeExpressionSpan expr3,
        out ReadOnlySeExpressionSpan expr4)
    {
        return payload.AsSpan().TryReadLink(out linkType, out expr1, out expr2, out expr3, out expr4);
    }

    /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.TryReadDalamudLink(ReadOnlySePayloadSpan, out DalamudLinkPayload)"/>
    public static bool TryReadDalamudLink(this ReadOnlySePayload rosp, out DalamudLinkPayload payload)
    {
        return rosp.AsSpan().TryReadDalamudLink(out payload);
    }
}
