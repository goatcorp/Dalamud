using Dalamud.Game.Text;

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

    /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.TryParseDalamudLink(ReadOnlySePayloadSpan, out DalamudLinkPayload)"/>
    public static bool TryParseDalamudLink(this ReadOnlySePayload rosp, out DalamudLinkPayload payload)
    {
        return rosp.AsSpan().TryParseDalamudLink(out payload);
    }
}
