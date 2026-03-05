using Dalamud.Game.Text;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

namespace Dalamud.Utility;

/// <summary>
/// Extension functions for <see cref="ReadOnlySePayload"/>/<see cref="ReadOnlySePayloadSpan"/>.
/// </summary>
public static class SeStringPayloadExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="macroCode"></param>
    /// <returns></returns>
    public static bool IsMacro(this ReadOnlySePayloadSpan payload, MacroCode macroCode)
    {
        return payload.Type == ReadOnlySePayloadType.Macro
            && payload.MacroCode == macroCode;
    }

    /// <inheritdoc cref="IsMacro(ReadOnlySePayloadSpan, MacroCode)"/>
    public static bool IsMacro(this ReadOnlySePayload payload, MacroCode macroCode)
    {
        return IsMacro(payload.AsSpan(), macroCode);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="linkType"></param>
    /// <returns></returns>
    public static bool IsLink(this ReadOnlySePayloadSpan payload, LinkMacroPayloadType linkType)
    {
        return payload.IsMacro(MacroCode.Link)
            && payload.TryGetExpression(out var linkTypeExpression)
            && linkTypeExpression.TryGetInt(out var payloadLinkType)
            && payloadLinkType == (int)linkType;
    }

    /// <inheritdoc cref="IsLink(ReadOnlySePayloadSpan, LinkMacroPayloadType)"/>
    public static bool IsLink(this ReadOnlySePayload payload, LinkMacroPayloadType linkType)
    {
        return IsLink(payload.AsSpan(), linkType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rosps"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
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

    /// <inheritdoc cref="TryParseDalamudLink(ReadOnlySePayloadSpan, out DalamudLinkPayload)"/>
    public static bool TryParseDalamudLink(this ReadOnlySePayload rosps, out DalamudLinkPayload payload)
    {
        return TryParseDalamudLink(rosps.AsSpan(), out payload);
    }
}
