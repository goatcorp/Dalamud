using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

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
    /// Determines whether the specified payload represents a link terminator.
    /// </summary>
    /// <param name="payload">The payload to check.</param>
    /// <returns><see langword="true"/> if the payload is a link terminator; otherwise, <see langword="false"/>.</returns>
    public static bool IsLinkTerminator(this ReadOnlySePayloadSpan payload)
    {
        return payload.IsLink(LinkMacroPayloadType.Terminator);
    }

    /// <summary>
    /// Attempts to read link parameters from the specified payload.
    /// </summary>
    /// <param name="payload">The payload to read from.</param>
    /// <param name="linkType">The link type.</param>
    /// <param name="expr1">The first integer expression.</param>
    /// <param name="expr2">The second integer expression.</param>
    /// <param name="expr3">The third integer expression.</param>
    /// <param name="expr4">An optional string expression.</param>
    /// <returns><see langword="true"/> if the payload is a link and its expressions were successfully read; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadLink(
        this ReadOnlySePayloadSpan payload,
        out LinkMacroPayloadType linkType,
        out ReadOnlySeExpressionSpan expr1,
        out ReadOnlySeExpressionSpan expr2,
        out ReadOnlySeExpressionSpan expr3,
        out ReadOnlySeExpressionSpan expr4)
    {
        linkType = default;
        expr1 = default;
        expr2 = default;
        expr3 = default;
        expr4 = default;

        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !enu.Current.TryGetInt(out var typeInt))
            return false;

        linkType = (LinkMacroPayloadType)typeInt;

        if (!enu.MoveNext())
            return false;

        expr1 = enu.Current;

        if (!enu.MoveNext())
            return false;

        expr2 = enu.Current;

        if (!enu.MoveNext())
            return false;

        expr3 = enu.Current;

        // probably an optional display string
        if (!enu.MoveNext())
            return true;

        expr4 = enu.Current;

        return true;
    }

    /// <summary>
    /// Attempts to read a Dalamud link payload from the specified payload.
    /// </summary>
    /// <param name="rosps">The payload to read from.</param>
    /// <param name="payload">The parsed <see cref="DalamudLinkPayload"/> if parsing succeeded, or <see langword="default"/> if parsing failed.</param>
    /// <returns><see langword="true"/> if the payload was successfully parsed into a <see cref="DalamudLinkPayload"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadDalamudLink(this ReadOnlySePayloadSpan rosps, out DalamudLinkPayload payload)
    {
        payload = default;

        if (!rosps.IsLink((LinkMacroPayloadType)Payload.EmbeddedInfoType.DalamudLink))
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

            payload = new DalamudLinkPayload()
            {
                CommandId = commandId,
                Plugin = pluginString.ToString(),
            };
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

        payload = new DalamudLinkPayload()
        {
            CommandId = commandId,
            Plugin = extraData[0],
            Extra1 = extra1,
            Extra2 = extra2,
            ExtraString = extraData[1],
        };

        return true;
    }
}
