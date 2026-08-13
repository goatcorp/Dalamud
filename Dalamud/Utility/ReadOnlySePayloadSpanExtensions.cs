using Dalamud.Game.Text;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

namespace Dalamud.Utility;

/// <summary>
/// Extension functions for <see cref="ReadOnlySePayloadSpan"/>.
/// </summary>
public static class ReadOnlySePayloadSpanExtensions
{
    /// <summary>
    /// Determines whether the payload is a macro payload matching the specified macro code.
    /// </summary>
    /// <param name="payload">The payload span to evaluate.</param>
    /// <param name="macroCode">The macro code to check against.</param>
    /// <returns><see langword="true"/> if the payload is a macro matching <paramref name="macroCode"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsMacro(this ReadOnlySePayloadSpan payload, MacroCode macroCode)
    {
        return payload.Type == ReadOnlySePayloadType.Macro
            && payload.MacroCode == macroCode;
    }

    /// <summary>
    /// Determines whether the payload is a link macro payload of the specified link type.
    /// </summary>
    /// <param name="payload">The payload span to evaluate.</param>
    /// <param name="linkType">The link macro type to check against.</param>
    /// <returns><see langword="true"/> if the payload is a link macro matching <paramref name="linkType"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsLink(this ReadOnlySePayloadSpan payload, LinkMacroPayloadType linkType)
    {
        return payload.IsMacro(MacroCode.Link)
            && payload.TryGetExpression(out var linkTypeExpression)
            && linkTypeExpression.TryGetInt(out var payloadLinkType)
            && payloadLinkType == (int)linkType;
    }

    /// <summary>
    /// Attempts to parse a Dalamud link payload from the specified payload span.
    /// </summary>
    /// <param name="rosps">The payload span to parse.</param>
    /// <param name="payload">When this method returns, contains the parsed <see cref="DalamudLinkPayload"/> if parsing succeeded, or <see langword="default"/> if parsing failed.</param>
    /// <returns><see langword="true"/> if the payload was successfully parsed into a <see cref="DalamudLinkPayload"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseDalamudLink(this ReadOnlySePayloadSpan rosps, out DalamudLinkPayload payload)
    {
        payload = default;

        if (!rosps.IsLink(DalamudLinkPayload.LinkType))
            return false;

        uint commandId;

        // compatibility for older versions of the payload
        if (!rosps.TryGetExpression(
                out _,
                out var commandIdExpression,
                out var extra1Expression,
                out var extra2Expression,
                out var compositeExpression))
        {
            if (!rosps.TryGetExpression(out _, out var pluginExpression, out commandIdExpression))
                return false;

            if (!pluginExpression.TryGetString(out var pluginString))
                return false;

            if (!commandIdExpression.TryGetUInt(out commandId))
                return false;

            payload = new DalamudLinkPayload(commandId, pluginString.ToString(), 0, 0, string.Empty);
            return true;
        }

        if (!commandIdExpression.TryGetUInt(out commandId))
            return false;

        if (!extra1Expression.TryGetInt(out var extra1))
            return false;

        if (!extra2Expression.TryGetInt(out var extra2))
            return false;

        if (!compositeExpression.TryGetString(out var compositeString))
            return false;

        string[] extraData;
        try
        {
            extraData = JsonConvert.DeserializeObject<string[]>(compositeString.ExtractText());
        }
        catch
        {
            return false;
        }

        payload = new DalamudLinkPayload(commandId, extraData[0], extra1, extra2, extraData[1]);

        return true;
    }
}
