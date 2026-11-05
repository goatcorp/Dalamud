using Dalamud.Game.Text;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// Extension functions for <see cref="ReadOnlySePayload"/>.
/// </summary>
public static class ReadOnlySePayloadExtensions
{
    extension(ReadOnlySePayload payload)
    {
        /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.IsMacro(ReadOnlySePayloadSpan, MacroCode)"/>
        public bool IsMacro(MacroCode macroCode)
        {
            return payload.AsSpan().IsMacro(macroCode);
        }

        /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.IsLink(ReadOnlySePayloadSpan, LinkMacroPayloadType)"/>
        public bool IsLink(LinkMacroPayloadType linkType)
        {
            return payload.AsSpan().IsLink(linkType);
        }

        /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.IsLinkTerminator(ReadOnlySePayloadSpan)"/>
        public bool IsLinkTerminator()
        {
            return payload.AsSpan().IsLinkTerminator();
        }

        /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.TryReadLink(ReadOnlySePayloadSpan, out LinkMacroPayloadType, out ReadOnlySeExpressionSpan, out ReadOnlySeExpressionSpan, out ReadOnlySeExpressionSpan, out ReadOnlySeExpressionSpan)"/>
        public bool TryReadLink(
            out LinkMacroPayloadType linkType,
            out ReadOnlySeExpressionSpan expr1,
            out ReadOnlySeExpressionSpan expr2,
            out ReadOnlySeExpressionSpan expr3,
            out ReadOnlySeExpressionSpan expr4)
        {
            return payload.AsSpan().TryReadLink(out linkType, out expr1, out expr2, out expr3, out expr4);
        }

        /// <inheritdoc cref="ReadOnlySePayloadSpanExtensions.TryReadDalamudLink(ReadOnlySePayloadSpan, out DalamudLinkPayload)"/>
        public bool TryReadDalamudLink(out DalamudLinkPayload dalamudLinkPayload)
        {
            return payload.AsSpan().TryReadDalamudLink(out dalamudLinkPayload);
        }
    }
}
