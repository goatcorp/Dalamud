using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Config;
using Dalamud.Game.Text.Evaluator.Internal;
using Dalamud.Game.Text.Noun;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Lumina.Text;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using AddonSheet = Lumina.Excel.Sheets.Addon;

namespace Dalamud.Game.Text.Evaluator;

#pragma warning disable SeStringEvaluator

/// <summary>
/// Evaluator for SeStrings.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class SeStringEvaluator : IServiceType, ISeStringEvaluator
{
    private static readonly ModuleLog Log = new("SeStringEvaluator");

    [ServiceManager.ServiceDependency]
    private readonly ClientState.ClientState clientState = Service<ClientState.ClientState>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameConfig gameConfig = Service<GameConfig>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration dalamudConfiguration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly NounProcessor nounProcessor = Service<NounProcessor>.Get();

    [ServiceManager.ServiceDependency]
    private readonly SheetRedirectResolver sheetRedirectResolver = Service<SheetRedirectResolver>.Get();

    private Dictionary<(ActionKind ActionKind, uint Id, ClientLanguage Language), string> actStrCache = [];
    private Dictionary<(ObjectKind ObjectKind, uint Id, ClientLanguage Language), string> objStrCache = [];

    [ServiceManager.ServiceConstructor]
    private SeStringEvaluator()
    {
    }

    /// <inheritdoc/>
    public ReadOnlySeString Evaluate(ReadOnlySeString str, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null)
    {
        return this.Evaluate(str.AsSpan(), localParameters, language);
    }

    /// <inheritdoc/>
    public ReadOnlySeString Evaluate(ReadOnlySeStringSpan str, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null)
    {
        if (str.IsTextOnly())
            return new(str);

        var builder = SeStringBuilder.SharedPool.Get();
        var lang = language ?? this.dalamudConfiguration.EffectiveLanguage.ToClientLanguage();

        try
        {
            var context = new SeStringContext(ref builder, localParameters, lang);

            foreach (var payload in str)
            {
                if (!this.ResolvePayload(ref context, payload))
                {
                    context.Builder.Append(payload);
                }
            }

            return builder.ToReadOnlySeString();
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateFromAddon(uint addonId, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null)
    {
        var lang = language ?? this.dalamudConfiguration.EffectiveLanguage.ToClientLanguage();

        if (!this.dataManager.GetExcelSheet<AddonSheet>(lang).TryGetRow(addonId, out var addonRow))
            return default;

        return this.Evaluate(addonRow.Text.AsSpan(), localParameters, lang);
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateFromLobby(uint lobbyId, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null)
    {
        var lang = language ?? this.dalamudConfiguration.EffectiveLanguage.ToClientLanguage();

        if (!this.dataManager.GetExcelSheet<Lobby>(lang).TryGetRow(lobbyId, out var lobbyRow))
            return default;

        return this.Evaluate(lobbyRow.Text.AsSpan(), localParameters, lang);
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateFromLogMessage(uint logMessageId, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null)
    {
        var lang = language ?? this.dalamudConfiguration.EffectiveLanguage.ToClientLanguage();

        if (!this.dataManager.GetExcelSheet<LogMessage>(lang).TryGetRow(logMessageId, out var logMessageRow))
            return default;

        return this.Evaluate(logMessageRow.Text.AsSpan(), localParameters, lang);
    }

    /// <inheritdoc/>
    public string EvaluateActStr(ActionKind actionKind, uint id, ClientLanguage? language = null)
    {
        var lang = language ?? this.dalamudConfiguration.EffectiveLanguage.ToClientLanguage();
        var key = (actionKind, id, lang);

        if (this.actStrCache.TryGetValue(key, out var text))
            return text;

        text = string.Intern(this.EvaluateFromAddon(2026, [actionKind.GetActStrId(id)], lang).ExtractText().StripSoftHypen());
        this.actStrCache.Add(key, text);
        return text;
    }

    /// <inheritdoc/>
    public string EvaluateObjStr(ObjectKind objectKind, uint id, ClientLanguage? language = null)
    {
        var lang = language ?? this.dalamudConfiguration.EffectiveLanguage.ToClientLanguage();
        var key = (objectKind, id, lang);

        if (this.objStrCache.TryGetValue(key, out var text))
            return text;

        text = string.Intern(this.EvaluateFromAddon(2025, [objectKind.GetObjStrId(id)], lang).ExtractText().StripSoftHypen());
        this.objStrCache.Add(key, text);
        return text;
    }

    // TODO: move this to MapUtil?
    private static uint ConvertRawToMapPos(Lumina.Excel.Sheets.Map map, short offset, float value)
    {
        var scale = map.SizeFactor / 100.0f;
        return (uint)(10 - (int)(((value + offset) * scale + 1024f) * -0.2f / scale));
    }

    private static uint ConvertRawToMapPosX(Lumina.Excel.Sheets.Map map, float x)
        => ConvertRawToMapPos(map, map.OffsetX, x);

    private static uint ConvertRawToMapPosY(Lumina.Excel.Sheets.Map map, float y)
        => ConvertRawToMapPos(map, map.OffsetY, y);

    private bool ResolvePayload(ref SeStringContext context, ReadOnlySePayloadSpan payload)
    {
        if (payload.Type != ReadOnlySePayloadType.Macro)
            return false;

        // if (context.HandlePayload(payload, ref context))
        //    return true;

        switch (payload.MacroCode)
        {
            case MacroCode.SetResetTime:
                return this.TryResolveSetResetTime(ref context, payload);

            case MacroCode.SetTime:
                return this.TryResolveSetTime(ref context, payload);

            case MacroCode.If:
                return this.TryResolveIf(ref context, payload);

            case MacroCode.Switch:
                return this.TryResolveSwitch(ref context, payload);

            case MacroCode.PcName:
                return this.TryResolvePcName(ref context, payload);

            case MacroCode.IfPcGender:
                return this.TryResolveIfPcGender(ref context, payload);

            case MacroCode.IfPcName:
                return this.TryResolveIfPcName(ref context, payload);

            // case MacroCode.Josa:
            // case MacroCode.Josaro:

            case MacroCode.IfSelf:
                return this.TryResolveIfSelf(ref context, payload);

            // case MacroCode.NewLine: // pass through
            // case MacroCode.Wait: // pass through
            // case MacroCode.Icon: // pass through

            case MacroCode.Color:
                return this.TryResolveColor(ref context, payload);

            case MacroCode.EdgeColor:
                return this.TryResolveEdgeColor(ref context, payload);

            case MacroCode.ShadowColor:
                return this.TryResolveShadowColor(ref context, payload);

            // case MacroCode.SoftHyphen: // pass through
            // case MacroCode.Key:
            // case MacroCode.Scale:

            case MacroCode.Bold:
                return this.TryResolveBold(ref context, payload);

            case MacroCode.Italic:
                return this.TryResolveItalic(ref context, payload);

            // case MacroCode.Edge:
            // case MacroCode.Shadow:
            // case MacroCode.NonBreakingSpace: // pass through
            // case MacroCode.Icon2: // pass through
            // case MacroCode.Hyphen: // pass through

            case MacroCode.Num:
                return this.TryResolveNum(ref context, payload);

            case MacroCode.Hex:
                return this.TryResolveHex(ref context, payload);

            case MacroCode.Kilo:
                return this.TryResolveKilo(ref context, payload);

            // case MacroCode.Byte:

            case MacroCode.Sec:
                return this.TryResolveSec(ref context, payload);

            // case MacroCode.Time:

            case MacroCode.Float:
                return this.TryResolveFloat(ref context, payload);

            // case MacroCode.Link: // pass through

            case MacroCode.Sheet:
                return this.TryResolveSheet(ref context, payload);

            case MacroCode.String:
                return this.TryResolveString(ref context, payload);

            case MacroCode.Caps:
                return this.TryResolveCaps(ref context, payload);

            case MacroCode.Head:
                return this.TryResolveHead(ref context, payload);

            case MacroCode.Split:
                return this.TryResolveSplit(ref context, payload);

            case MacroCode.HeadAll:
                return this.TryResolveHeadAll(ref context, payload);

            case MacroCode.Fixed:
                return this.TryResolveFixed(ref context, payload);

            case MacroCode.Lower:
                return this.TryResolveLower(ref context, payload);

            case MacroCode.JaNoun:
                return this.TryResolveNoun(ClientLanguage.Japanese, ref context, payload);

            case MacroCode.EnNoun:
                return this.TryResolveNoun(ClientLanguage.English, ref context, payload);

            case MacroCode.DeNoun:
                return this.TryResolveNoun(ClientLanguage.German, ref context, payload);

            case MacroCode.FrNoun:
                return this.TryResolveNoun(ClientLanguage.French, ref context, payload);

            // case MacroCode.ChNoun:

            case MacroCode.LowerHead:
                return this.TryResolveLowerHead(ref context, payload);

            case MacroCode.ColorType:
                return this.TryResolveColorType(ref context, payload);

            case MacroCode.EdgeColorType:
                return this.TryResolveEdgeColorType(ref context, payload);

            // case MacroCode.Ruby:

            case MacroCode.Digit:
                return this.TryResolveDigit(ref context, payload);

            case MacroCode.Ordinal:
                return this.TryResolveOrdinal(ref context, payload);

            // case MacroCode.Sound: // pass through

            case MacroCode.LevelPos:
                return this.TryResolveLevelPos(ref context, payload);

            default:
                return false;
        }
    }

    private unsafe bool TryResolveSetResetTime(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        DateTime date;

        if (payload.TryGetExpression(out var eHour, out var eWeekday)
            && this.TryResolveInt(ref context, eHour, out var eHourVal)
            && this.TryResolveInt(ref context, eWeekday, out var eWeekdayVal))
        {
            var t = DateTime.UtcNow.AddDays((eWeekdayVal - (int)DateTime.UtcNow.DayOfWeek + 7) % 7);
            date = new DateTime(t.Year, t.Month, t.Day, eHourVal, 0, 0, DateTimeKind.Utc).ToLocalTime();
        }
        else if (payload.TryGetExpression(out eHour)
                 && this.TryResolveInt(ref context, eHour, out eHourVal))
        {
            var t = DateTime.UtcNow;
            date = new DateTime(t.Year, t.Month, t.Day, eHourVal, 0, 0, DateTimeKind.Utc).ToLocalTime();
        }
        else
        {
            return false;
        }

        MacroDecoder.GetMacroTime()->SetTime(date);

        return true;
    }

    private unsafe bool TryResolveSetTime(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eTime) || !this.TryResolveUInt(ref context, eTime, out var eTimeVal))
            return false;

        var date = DateTimeOffset.FromUnixTimeSeconds(eTimeVal).LocalDateTime;
        MacroDecoder.GetMacroTime()->SetTime(date);

        return true;
    }

    private bool TryResolveIf(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        return
            payload.TryGetExpression(out var eCond, out var eTrue, out var eFalse)
            && this.ResolveStringExpression(
                ref context,
                this.TryResolveBool(ref context, eCond, out var eCondVal) && eCondVal
                    ? eTrue
                    : eFalse);
    }

    private bool TryResolveSwitch(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var cond = -1;
        foreach (var e in payload)
        {
            switch (cond)
            {
                case -1:
                    cond = this.TryResolveUInt(ref context, e, out var eVal) ? (int)eVal : 0;
                    break;
                case > 1:
                    cond--;
                    break;
                default:
                    return this.ResolveStringExpression(ref context, e);
            }
        }

        return false;
    }

    private unsafe bool TryResolvePcName(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId))
            return false;

        if (!this.TryResolveUInt(ref context, eEntityId, out var entityId))
            return false;

        // TODO: handle LogNameType

        NameCache.CharacterInfo characterInfo = default;
        if (NameCache.Instance()->TryGetCharacterInfoByEntityId(entityId, &characterInfo))
        {
            context.Builder.Append((ReadOnlySeStringSpan)characterInfo.Name.AsSpan());

            if (characterInfo.HomeWorldId != AgentLobby.Instance()->LobbyData.HomeWorldId &&
                WorldHelper.Instance()->AllWorlds.TryGetValue((ushort)characterInfo.HomeWorldId, out var world, false))
            {
                context.Builder.AppendIcon(88);

                if (this.gameConfig.UiConfig.TryGetUInt("LogCrossWorldName", out var logCrossWorldName) && logCrossWorldName == 1)
                    context.Builder.Append((ReadOnlySeStringSpan)world.Name);
            }

            return true;
        }

        // TODO: lookup via InstanceContentCrystallineConflictDirector
        // TODO: lookup via MJIManager

        return false;
    }

    private unsafe bool TryResolveIfPcGender(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId, out var eMale, out var eFemale))
            return false;

        if (!this.TryResolveUInt(ref context, eEntityId, out var entityId))
            return false;

        NameCache.CharacterInfo characterInfo = default;
        if (NameCache.Instance()->TryGetCharacterInfoByEntityId(entityId, &characterInfo))
            return this.ResolveStringExpression(ref context, characterInfo.Sex == 0 ? eMale : eFemale);

        // TODO: lookup via InstanceContentCrystallineConflictDirector

        return false;
    }

    private unsafe bool TryResolveIfPcName(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId, out var eName, out var eTrue, out var eFalse))
            return false;

        if (!this.TryResolveUInt(ref context, eEntityId, out var entityId) || !eName.TryGetString(out var name))
            return false;

        name = this.Evaluate(name, context.LocalParameters, context.Language).AsSpan();

        NameCache.CharacterInfo characterInfo = default;
        return NameCache.Instance()->TryGetCharacterInfoByEntityId(entityId, &characterInfo) &&
            this.ResolveStringExpression(ref context, name.Equals((ReadOnlySeStringSpan)characterInfo.Name.AsSpan())
                ? eTrue
                : eFalse);
    }

    private unsafe bool TryResolveIfSelf(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId, out var eTrue, out var eFalse))
            return false;

        if (!this.TryResolveUInt(ref context, eEntityId, out var entityId))
            return false;

        // the game uses LocalPlayer here, but using PlayerState seems more safe..
        return this.ResolveStringExpression(ref context, PlayerState.Instance()->EntityId == entityId ? eTrue : eFalse);
    }

    private bool TryResolveColor(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColor))
            return false;

        if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
            context.Builder.PopColor();
        else if (this.TryResolveUInt(ref context, eColor, out var eColorVal))
            context.Builder.PushColorBgra(eColorVal);

        return true;
    }

    private bool TryResolveEdgeColor(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColor))
            return false;

        if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
            context.Builder.PopEdgeColor();
        else if (this.TryResolveUInt(ref context, eColor, out var eColorVal))
            context.Builder.PushEdgeColorBgra(eColorVal);

        return true;
    }

    private bool TryResolveShadowColor(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColor))
            return false;

        if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
            context.Builder.PopShadowColor();
        else if (this.TryResolveUInt(ref context, eColor, out var eColorVal))
            context.Builder.PushShadowColorBgra(eColorVal);

        return true;
    }

    private bool TryResolveBold(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEnable) || !this.TryResolveBool(ref context, eEnable, out var eEnableVal))
            return false;

        context.Builder.AppendSetBold(eEnableVal);

        return true;
    }

    private bool TryResolveItalic(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEnable) || !this.TryResolveBool(ref context, eEnable, out var eEnableVal))
            return false;

        context.Builder.AppendSetItalic(eEnableVal);

        return true;
    }

    private bool TryResolveNum(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eInt) || !this.TryResolveInt(ref context, eInt, out var eIntVal))
        {
            context.Builder.Append('0');
            return true;
        }

        context.Builder.Append(eIntVal.ToString());

        return true;
    }

    private bool TryResolveHex(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eUInt) || !this.TryResolveUInt(ref context, eUInt, out var eUIntVal))
        {
            // TODO: throw?
            // ERROR: mismatch parameter type ('' is not numeric)
            return false;
        }

        context.Builder.Append("0x{0:X08}".Format(eUIntVal));

        return true;
    }

    private bool TryResolveKilo(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eInt, out var eSep) || !this.TryResolveInt(ref context, eInt, out var eIntVal))
        {
            context.Builder.Append('0');
            return true;
        }

        if (eIntVal == int.MinValue)
        {
            // -2147483648
            context.Builder.Append("-2"u8);
            this.ResolveStringExpression(ref context, eSep);
            context.Builder.Append("147"u8);
            this.ResolveStringExpression(ref context, eSep);
            context.Builder.Append("483"u8);
            this.ResolveStringExpression(ref context, eSep);
            context.Builder.Append("648"u8);
            return true;
        }

        if (eIntVal < 0)
        {
            context.Builder.Append('-');
            eIntVal = -eIntVal;
        }

        if (eIntVal == 0)
        {
            context.Builder.Append('0');
            return true;
        }

        var anyDigitPrinted = false;
        for (var i = 1_000_000_000; i > 0; i /= 10)
        {
            var digit = eIntVal / i % 10;
            switch (anyDigitPrinted)
            {
                case false when digit == 0:
                    continue;
                case true when i % 3 == 0:
                    this.ResolveStringExpression(ref context, eSep);
                    break;
            }

            anyDigitPrinted = true;
            context.Builder.Append((char)('0' + digit));
        }

        return true;
    }

    private bool TryResolveSec(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eInt) || !this.TryResolveUInt(ref context, eInt, out var eIntVal))
        {
            // TODO: throw?
            // ERROR: mismatch parameter type ('' is not numeric)
            return false;
        }

        context.Builder.Append("{0:00}".Format(eIntVal));
        return true;
    }

    private bool TryResolveFloat(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eValue, out var eRadix, out var eSeparator)
            || !this.TryResolveInt(ref context, eValue, out var eValueVal)
            || !this.TryResolveInt(ref context, eRadix, out var eRadixVal))
        {
            return false;
        }

        var (integerPart, fractionalPart) = int.DivRem(eValueVal, eRadixVal);
        if (fractionalPart < 0)
        {
            integerPart--;
            fractionalPart += eRadixVal;
        }

        context.Builder.Append(integerPart.ToString());
        this.ResolveStringExpression(ref context, eSeparator);

        // brain fried code
        Span<byte> fractionalDigits = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var pos = fractionalDigits.Length - 1;
        for (var r = eRadixVal; r > 1; r /= 10)
        {
            fractionalDigits[pos--] = (byte)('0' + fractionalPart % 10);
            fractionalPart /= 10;
        }

        context.Builder.Append(fractionalDigits[(pos + 1)..]);

        return true;
    }

    private bool TryResolveSheet(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var eSheetNameStr))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var eRowIdValue))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var eColIndexValue))
            return false;

        var eColParamValue = 0u;
        if (enu.MoveNext())
            this.TryResolveUInt(ref context, enu.Current, out eColParamValue);

        var resolvedSheetName = this.Evaluate(eSheetNameStr, context.LocalParameters, context.Language).ExtractText();

        this.sheetRedirectResolver.Resolve(ref resolvedSheetName, ref eRowIdValue);

        if (string.IsNullOrEmpty(resolvedSheetName))
            return false;

        if (!this.dataManager.Excel.SheetNames.Contains(resolvedSheetName))
            return false;

        if (!this.dataManager.GetExcelSheet<RawRow>(context.Language, resolvedSheetName).TryGetRow(eRowIdValue, out var row))
            return false;

        var column = row.ReadColumn((int)eColIndexValue);
        if (column == null)
            return false;

        switch (column)
        {
            case ReadOnlySeString val:
                context.Builder.Append(this.Evaluate(val, [eColParamValue], context.Language));
                return true;

            case bool val:
                context.Builder.Append((val ? 1u : 0).ToString("D", CultureInfo.InvariantCulture));
                return true;

            case sbyte val:
                context.Builder.Append(val.ToString("D", CultureInfo.InvariantCulture));
                return true;

            case byte val:
                context.Builder.Append(val.ToString("D", CultureInfo.InvariantCulture));
                return true;

            case short val:
                context.Builder.Append(val.ToString("D", CultureInfo.InvariantCulture));
                return true;

            case ushort val:
                context.Builder.Append(val.ToString("D", CultureInfo.InvariantCulture));
                return true;

            case int val:
                context.Builder.Append(val.ToString("D", CultureInfo.InvariantCulture));
                return true;

            case uint val:
                context.Builder.Append(val.ToString("D", CultureInfo.InvariantCulture));
                return true;

            case { } val:
                context.Builder.Append(val.ToString());
                return true;
        }

        return false;
    }

    private bool TryResolveString(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        return payload.TryGetExpression(out var eStr) && this.ResolveStringExpression(ref context, eStr);
    }

    private bool TryResolveCaps(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(ref builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(ref headContext, eStr))
                return false;

            var str = builder.ToReadOnlySeString();
            var pIdx = 0;

            foreach (var p in str)
            {
                pIdx++;

                if (p.Type == ReadOnlySePayloadType.Invalid)
                    continue;

                if (pIdx == 1 && p.Type == ReadOnlySePayloadType.Text)
                {
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.ToArray()).ToUpper(context.CultureInfo));
                    continue;
                }

                context.Builder.Append(p);
            }

            return true;
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    private bool TryResolveHead(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(ref builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(ref headContext, eStr))
                return false;

            var str = builder.ToReadOnlySeString();
            var pIdx = 0;

            foreach (var p in str)
            {
                pIdx++;

                if (p.Type == ReadOnlySePayloadType.Invalid)
                    continue;

                if (pIdx == 1 && p.Type == ReadOnlySePayloadType.Text)
                {
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.ToArray()).FirstCharToUpper());
                    continue;
                }

                context.Builder.Append(p);
            }

            return true;
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    private bool TryResolveSplit(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eText, out var eSeparator, out var eIndex))
            return false;

        if (!eSeparator.TryGetString(out var eSeparatorVal) || !eIndex.TryGetUInt(out var eIndexVal) || eIndexVal <= 0)
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(ref builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(ref headContext, eText))
                return false;

            var separator = eSeparatorVal.ExtractText();
            if (separator.Length < 1)
                return false;

            var splitted = builder.ToReadOnlySeString().ExtractText().Split(separator[0]);
            if (eIndexVal <= splitted.Length)
            {
                context.Builder.Append(splitted[eIndexVal - 1]);
                return true;
            }

            return false;
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    private bool TryResolveHeadAll(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(ref builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(ref headContext, eStr))
                return false;

            var str = builder.ToReadOnlySeString();

            foreach (var p in str)
            {
                if (p.Type == ReadOnlySePayloadType.Invalid)
                    continue;

                if (p.Type == ReadOnlySePayloadType.Text)
                {
                    context.Builder.Append(context.CultureInfo.TextInfo.ToTitleCase(Encoding.UTF8.GetString(p.Body.ToArray())));

                    continue;
                }

                context.Builder.Append(p);
            }

            return true;
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    private bool TryResolveFixed(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        // This is handled by the second function in Client::UI::Misc::PronounModule_ProcessString

        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var e0Val))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var e1Val))
            return false;

        return e0Val switch
        {
            100 or 200 => e1Val switch
            {
                1 => this.TryResolveFixedPlayerLink(ref context, ref enu),
                2 => this.TryResolveFixedClassJobLevel(ref context, ref enu),
                3 => this.TryResolveFixedMapLink(ref context, ref enu),
                4 => this.TryResolveFixedItemLink(ref context, ref enu),
                5 => this.TryResolveFixedChatSoundEffect(ref context, ref enu),
                6 => this.TryResolveFixedObjStr(ref context, ref enu),
                7 => this.TryResolveFixedString(ref context, ref enu),
                8 => this.TryResolveFixedTimeRemaining(ref context, ref enu),
                // Reads a uint and saves it to PronounModule+0x3AC
                // TODO: handle this? looks like it's for the mentor/beginner icon of the player link in novice network
                // see "FF 50 50 8B B0"
                9 => true,
                10 => this.TryResolveFixedStatusLink(ref context, ref enu),
                11 => this.TryResolveFixedPartyFinderLink(ref context, ref enu),
                12 => this.TryResolveFixedQuestLink(ref context, ref enu),
                _ => false,
            },
            _ => this.TryResolveFixedAutoTranslation(ref context, payload, e0Val, e1Val),
        };
    }

    private unsafe bool TryResolveFixedPlayerLink(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var worldId))
            return false;

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var playerName))
            return false;

        if (UIGlobals.IsValidPlayerCharacterName(playerName.ExtractText()))
        {
            var flags = 0u;
            if (InfoModule.Instance()->IsInCrossWorldDuty())
                flags |= 0x10;

            context.Builder.PushLink(LinkMacroPayloadType.Character, flags, worldId, 0u, playerName);
            context.Builder.Append(playerName);
            context.Builder.PopLink();
        }
        else
        {
            context.Builder.Append(playerName);
        }

        if (worldId == AgentLobby.Instance()->LobbyData.HomeWorldId)
            return true;

        if (!this.dataManager.GetExcelSheet<World>(context.Language).TryGetRow(worldId, out var worldRow))
            return false;

        context.Builder.AppendIcon(88);
        context.Builder.Append(worldRow.Name);

        return true;
    }

    private bool TryResolveFixedClassJobLevel(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var classJobId) || classJobId <= 0)
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var level))
            return false;

        if (!this.dataManager.GetExcelSheet<ClassJob>(context.Language).TryGetRow((uint)classJobId, out var classJobRow))
            return false;

        context.Builder.Append(classJobRow.Name);

        if (level != 0)
        {
            context.Builder.Append('(');
            context.Builder.Append(level.ToString("D", CultureInfo.InvariantCulture));
            context.Builder.Append(')');
        }

        return true;
    }

    private bool TryResolveFixedMapLink(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var territoryTypeId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var packedIds))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var rawX))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var rawY))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var rawZ))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var placeNameIdInt))
            return false;

        var instance = packedIds >> 0x10;
        var mapId = packedIds & 0xFF;

        if (this.dataManager.GetExcelSheet<TerritoryType>(context.Language).TryGetRow(territoryTypeId, out var territoryTypeRow))
        {
            if (!this.dataManager.GetExcelSheet<PlaceName>(context.Language).TryGetRow(placeNameIdInt == 0 ? territoryTypeRow.PlaceName.RowId : placeNameIdInt, out var placeNameRow))
                return false;

            if (!this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Map>().TryGetRow(mapId, out var mapRow))
                return false;

            var sb = SeStringBuilder.SharedPool.Get();

            sb.Append(placeNameRow.Name);
            if (instance > 0 && instance <= 9)
                sb.Append((char)((char)0xE0B0 + (char)instance));

            var placeNameWithInstance = sb.ToReadOnlySeString();
            SeStringBuilder.SharedPool.Return(sb);

            var mapPosX = ConvertRawToMapPosX(mapRow, rawX / 1000f);
            var mapPosY = ConvertRawToMapPosY(mapRow, rawY / 1000f);

            var linkText = rawZ == -30000
                ? this.EvaluateFromAddon(1635, [placeNameWithInstance, mapPosX, mapPosY], context.Language)
                : this.EvaluateFromAddon(1636, [placeNameWithInstance, mapPosX, mapPosY, rawZ / (rawZ >= 0 ? 10 : -10), rawZ], context.Language);

            context.Builder.PushLinkMapPosition(territoryTypeId, mapId, rawX, rawY);
            context.Builder.Append(this.EvaluateFromAddon(371, [linkText], context.Language));
            context.Builder.PopLink();

            return true;
        }
        else if (mapId == 0)
        {
            if (this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(875, out var addonRow)) // "(No location set for map link)"
                context.Builder.Append(addonRow.Text);

            return true;
        }
        else if (mapId == 1)
        {
            if (this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(874, out var addonRow)) // "(Map link unavailable in this area)"
                context.Builder.Append(addonRow.Text);

            return true;
        }
        else if (mapId == 2)
        {
            if (this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(13743, out var addonRow)) // "(Unable to set map link)"
                context.Builder.Append(addonRow.Text);

            return true;
        }

        return false;
    }

    private bool TryResolveFixedItemLink(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var itemId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var rarity))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var unk2))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var unk3))
            return false;

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var itemName)) // TODO: unescape??
            return false;

        // rarity color start
        context.Builder.Append(this.EvaluateFromAddon(6, [rarity], context.Language));

        var v2 = (ushort)((unk2 & 0xFF) + (unk3 << 0x10)); // TODO: find out what this does

        context.Builder.PushLink(LinkMacroPayloadType.Item, itemId, rarity, v2);

        // arrow and item name
        context.Builder.Append(this.EvaluateFromAddon(371, [itemName], context.Language));

        context.Builder.PopLink();
        context.Builder.PopColor();

        return true;
    }

    private bool TryResolveFixedChatSoundEffect(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var soundEffectId))
            return false;

        context.Builder.Append($"<se.{soundEffectId + 1}>");

        // the game would play it here

        return true;
    }

    private bool TryResolveFixedObjStr(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var objStrId))
            return false;

        context.Builder.Append(this.EvaluateFromAddon(2025, [objStrId], context.Language));

        return true;
    }

    private bool TryResolveFixedString(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !enu.Current.TryGetString(out var text))
            return false;

        // formats it through vsprintf using "%s"??
        context.Builder.Append(text.ExtractText());

        return true;
    }

    private bool TryResolveFixedTimeRemaining(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var seconds))
            return false;

        if (seconds != 0)
        {
            context.Builder.Append(this.EvaluateFromAddon(33, [seconds / 60, seconds % 60], context.Language));
        }
        else
        {
            if (this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(48, out var addonRow))
                context.Builder.Append(addonRow.Text);
        }

        return true;
    }

    private bool TryResolveFixedStatusLink(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var statusId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveBool(ref context, enu.Current, out var hasOverride))
            return false;

        if (!this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>(context.Language).TryGetRow(statusId, out var statusRow))
            return false;

        ReadOnlySeStringSpan statusName;
        ReadOnlySeStringSpan statusDescription;

        if (hasOverride)
        {
            if (!enu.MoveNext() || !enu.Current.TryGetString(out statusName))
                return false;

            if (!enu.MoveNext() || !enu.Current.TryGetString(out statusDescription))
                return false;
        }
        else
        {
            statusName = statusRow.Name.AsSpan();
            statusDescription = statusRow.Description.AsSpan();
        }

        var sb = SeStringBuilder.SharedPool.Get();

        switch (statusRow.StatusCategory)
        {
            case 1:
                sb.Append(this.EvaluateFromAddon(376, default, context.Language));
                break;

            case 2:
                sb.Append(this.EvaluateFromAddon(377, default, context.Language));
                break;
        }

        sb.Append(statusName);

        var linkText = sb.ToReadOnlySeString();
        SeStringBuilder.SharedPool.Return(sb);

        context.Builder
           .BeginMacro(MacroCode.Link)
            .AppendUIntExpression((uint)LinkMacroPayloadType.Status)
            .AppendUIntExpression(statusId)
            .AppendUIntExpression(0)
            .AppendUIntExpression(0)
            .AppendStringExpression(statusName)
            .AppendStringExpression(statusDescription)
            .EndMacro();

        context.Builder.Append(this.EvaluateFromAddon(371, [linkText], context.Language));

        context.Builder.PopLink();

        return true;
    }

    private bool TryResolveFixedPartyFinderLink(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var listingId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var unk1))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var worldId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var crossWorldFlag)) // 0 = cross world, 1 = not cross world
            return false;

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var playerName))
            return false;

        context.Builder
           .BeginMacro(MacroCode.Link)
            .AppendUIntExpression((uint)LinkMacroPayloadType.PartyFinder)
            .AppendUIntExpression(listingId)
            .AppendUIntExpression(unk1)
            .AppendUIntExpression((uint)(crossWorldFlag << 0x10) + worldId)
            .EndMacro();

        context.Builder.Append(this.EvaluateFromAddon(371, [this.EvaluateFromAddon(2265, [playerName, crossWorldFlag], context.Language)], context.Language));

        context.Builder.PopLink();

        return true;
    }

    private bool TryResolveFixedQuestLink(ref SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var questId))
            return false;

        if (!enu.MoveNext() || !enu.MoveNext() || !enu.MoveNext()) // unused
            return false;

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var questName))
            return false;

        /* TODO: hide incomplete, repeatable special event quest names
        if (!QuestManager.IsQuestComplete(questId) && !QuestManager.Instance()->IsQuestAccepted(questId))
        {
            var questRecompleteManager = QuestRecompleteManager.Instance();
            if (questRecompleteManager == null || !questRecompleteManager->"E8 ?? ?? ?? ?? 0F B6 57 FF"(questId)) {
                if (_excelService.TryGetRow<AddonSheet>(5497, context.Language, out var addonRow))
                    questName = addonRow.Text.AsSpan();
            }
        }
        */

        context.Builder
           .BeginMacro(MacroCode.Link)
            .AppendUIntExpression((uint)LinkMacroPayloadType.Quest)
            .AppendUIntExpression(questId)
            .AppendUIntExpression(0)
            .AppendUIntExpression(0)
            .EndMacro();

        context.Builder.Append(this.EvaluateFromAddon(371, [questName], context.Language));

        context.Builder.PopLink();

        return true;
    }

    private bool TryResolveFixedAutoTranslation(ref SeStringContext context, in ReadOnlySePayloadSpan payload, int e0Val, int e1Val)
    {
        // Auto-Translation / Completion
        var group = (uint)(e0Val + 1);
        var rowId = (uint)e1Val;

        using var icons = new IconWrap(ref context.Builder, 54, 55);

        if (!this.dataManager.GetExcelSheet<Completion>(context.Language).TryGetFirst(row => row.Group == group && !row.LookupTable.IsEmpty, out var groupRow))
            return false;

        var lookupTable = (
            groupRow.LookupTable.IsTextOnly()
                ? groupRow.LookupTable
                : this.Evaluate(groupRow.LookupTable.AsSpan(), context.LocalParameters, context.Language)).ExtractText();

        // Completion sheet
        if (lookupTable.Equals("@"))
        {
            if (this.dataManager.GetExcelSheet<Completion>(context.Language).TryGetRow(rowId, out var completionRow))
            {
                context.Builder.Append(completionRow.Text);
            }

            return true;
        }

        // CategoryDataCache
        else if (lookupTable.Equals("#"))
        {
            // couldn't find any, so we don't handle them :p
            context.Builder.Append(payload);
            return false;
        }

        // All other sheets
        var rangesStart = lookupTable.IndexOf('[');
        RawRow row = default;
        if (rangesStart == -1) // Sheet without ranges
        {
            if (this.dataManager.GetExcelSheet<RawRow>(context.Language, lookupTable).TryGetRow(rowId, out row))
            {
                context.Builder.Append(row.ReadStringColumn(0));
                return true;
            }
        }

        var sheetName = lookupTable[..rangesStart];
        var ranges = lookupTable[(rangesStart + 1)..(lookupTable.Length - 1)];
        if (ranges.Length == 0)
            return true;

        var isNoun = false;
        var col = 0;

        if (ranges.StartsWith("noun"))
        {
            isNoun = true;
        }
        else if (ranges.StartsWith("col"))
        {
            var colRangeEnd = ranges.IndexOf(',');
            if (colRangeEnd == -1)
                colRangeEnd = ranges.Length;

            col = int.Parse(ranges[4..colRangeEnd]);
        }
        else if (ranges.StartsWith("tail"))
        {
            // couldn't find any, so we don't handle them :p
            context.Builder.Append(payload);
            return false;
        }

        if (isNoun && context.Language == ClientLanguage.German && sheetName == "Companion")
        {
            context.Builder.Append(this.nounProcessor.ProcessNoun(sheetName, rowId, ClientLanguage.German, 1, 5));
        }
        else if (this.dataManager.GetExcelSheet<RawRow>(context.Language, sheetName).TryGetRow(rowId, out row))
        {
            context.Builder.Append(row.ReadStringColumn(col));
        }

        return true;
    }

    private bool TryResolveLower(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(ref builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(ref headContext, eStr))
                return false;

            var str = builder.ToReadOnlySeString();

            foreach (var p in str)
            {
                if (p.Type == ReadOnlySePayloadType.Invalid)
                    continue;

                if (p.Type == ReadOnlySePayloadType.Text)
                {
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.ToArray()).ToLower(context.CultureInfo));

                    continue;
                }

                context.Builder.Append(p);
            }

            return true;
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    private bool TryResolveNoun(ClientLanguage language, ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var eAmountVal = 1;
        var eCaseVal = 1;

        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var eSheetNameStr))
            return false;

        var sheetName = this.Evaluate(eSheetNameStr, context.LocalParameters, context.Language).ExtractText();

        if (!enu.MoveNext() || !this.TryResolveInt(ref context, enu.Current, out var eArticleTypeVal))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(ref context, enu.Current, out var eRowIdVal))
            return false;

        this.sheetRedirectResolver.Resolve(ref sheetName, ref eRowIdVal);

        if (string.IsNullOrEmpty(sheetName))
            return false;

        // optional arguments
        if (enu.MoveNext())
        {
            if (!this.TryResolveInt(ref context, enu.Current, out eAmountVal))
                return false;

            if (enu.MoveNext())
            {
                if (!this.TryResolveInt(ref context, enu.Current, out eCaseVal))
                    return false;

                // For Chinese texts?
                /*
                if (enu.MoveNext())
                {
                    var eUnkInt5 = enu.Current;
                    if (!TryResolveInt(ref context, eUnkInt5, out eUnkInt5Val))
                        return false;
                }
                */
            }
        }

        context.Builder.Append(this.nounProcessor.ProcessNoun(sheetName, eRowIdVal, language, eAmountVal, eArticleTypeVal, eCaseVal - 1));

        return true;
    }

    private bool TryResolveLowerHead(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(ref builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(ref headContext, eStr))
                return false;

            var str = builder.ToReadOnlySeString();
            var pIdx = 0;

            foreach (var p in str)
            {
                pIdx++;

                if (p.Type == ReadOnlySePayloadType.Invalid)
                    continue;

                if (pIdx == 1 && p.Type == ReadOnlySePayloadType.Text)
                {
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.ToArray()).FirstCharToLower());
                    continue;
                }

                context.Builder.Append(p);
            }

            return true;
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    private bool TryResolveColorType(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColorType) || !this.TryResolveUInt(ref context, eColorType, out var eColorTypeVal))
            return false;

        if (eColorTypeVal == 0)
            context.Builder.PopColor();
        else if (this.dataManager.GetExcelSheet<UIColor>().TryGetRow(eColorTypeVal, out var row))
            context.Builder.PushColorBgra(row.UIForeground >> 8 | row.UIForeground << 24);

        return true;
    }

    private bool TryResolveEdgeColorType(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColorType) || !this.TryResolveUInt(ref context, eColorType, out var eColorTypeVal))
            return false;

        if (eColorTypeVal == 0)
            context.Builder.PopEdgeColor();
        else if (this.dataManager.GetExcelSheet<UIColor>().TryGetRow(eColorTypeVal, out var row))
            context.Builder.PushEdgeColorBgra(row.UIForeground >> 8 | row.UIForeground << 24);

        return true;
    }

    private bool TryResolveDigit(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eValue, out var eTargetLength))
            return false;

        if (!this.TryResolveInt(ref context, eValue, out var eValueVal))
            return false;

        if (!this.TryResolveInt(ref context, eTargetLength, out var eTargetLengthVal))
            return false;

        context.Builder.Append(eValueVal.ToString(new string('0', eTargetLengthVal)));

        return true;
    }

    private bool TryResolveOrdinal(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eValue) || !this.TryResolveUInt(ref context, eValue, out var eValueVal))
            return false;

        if (MathF.Floor(eValueVal / 10f) % 10 == 1)
        {
            context.Builder.Append($"{eValueVal}th");
            return true;
        }

        context.Builder.Append($"{eValueVal}{(eValueVal % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th",
        }}");
        return true;
    }

    private bool TryResolveLevelPos(ref SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eLevel) || !this.TryResolveUInt(ref context, eLevel, out var eLevelVal))
            return false;

        if (!this.dataManager.GetExcelSheet<Level>(context.Language).TryGetRow(eLevelVal, out var level) || !level.Map.IsValid)
            return false;

        if (!this.dataManager.GetExcelSheet<PlaceName>(context.Language).TryGetRow(level.Map.Value.PlaceName.RowId, out var placeName))
            return false;

        var mapPosX = ConvertRawToMapPosX(level.Map.Value, level.X);
        var mapPosY = ConvertRawToMapPosY(level.Map.Value, level.Z); // Z is [sic]

        context.Builder.Append(
            this.EvaluateFromAddon(
                1637,
                [placeName.Name, mapPosX, mapPosY],
                context.Language));

        return true;
    }

    private unsafe bool TryGetGNumDefault(uint parameterIndex, out uint value)
    {
        value = 0u;

        var rtm = RaptureTextModule.Instance();
        if (rtm is null)
            return false;

        if (!ThreadSafety.IsMainThread)
        {
            Log.Error("Global parameters may only be used from the main thread.");
            return false;
        }

        ref var gp = ref rtm->TextModule.MacroDecoder.GlobalParameters;
        if (parameterIndex >= gp.MySize)
            return false;

        var p = rtm->TextModule.MacroDecoder.GlobalParameters[parameterIndex];
        switch (p.Type)
        {
            case TextParameterType.Integer:
                value = (uint)p.IntValue;
                return true;

            case TextParameterType.ReferencedUtf8String:
                Log.Error("Requested a number; Utf8String global parameter at {parameterIndex}.", parameterIndex);
                return false;

            case TextParameterType.String:
                Log.Error("Requested a number; string global parameter at {parameterIndex}.", parameterIndex);
                return false;

            case TextParameterType.Uninitialized:
                Log.Error("Requested a number; uninitialized global parameter at {parameterIndex}.", parameterIndex);
                return false;

            default:
                return false;
        }
    }

    private unsafe bool TryProduceGStrDefault(ref SeStringBuilder builder, ClientLanguage language, uint parameterIndex)
    {
        var rtm = RaptureTextModule.Instance();
        if (rtm is null)
            return false;

        ref var gp = ref rtm->TextModule.MacroDecoder.GlobalParameters;
        if (parameterIndex >= gp.MySize)
            return false;

        if (!ThreadSafety.IsMainThread)
        {
            Log.Error("Global parameters may only be used from the main thread.");
            return false;
        }

        var p = rtm->TextModule.MacroDecoder.GlobalParameters[parameterIndex];
        switch (p.Type)
        {
            case TextParameterType.Integer:
                builder.Append(p.IntValue.ToString());
                return true;

            case TextParameterType.ReferencedUtf8String:
                builder.Append(this.Evaluate(new ReadOnlySeStringSpan(p.ReferencedUtf8StringValue->Utf8String.AsSpan()), null, language));
                return false;

            case TextParameterType.String:
                builder.Append(this.Evaluate(new ReadOnlySeStringSpan(p.StringValue), null, language));
                return false;

            case TextParameterType.Uninitialized:
            default:
                return false;
        }
    }

    private unsafe bool TryResolveUInt(ref SeStringContext context, in ReadOnlySeExpressionSpan expression, out uint value)
    {
        if (expression.TryGetUInt(out value))
            return true;

        if (expression.TryGetPlaceholderExpression(out var exprType))
        {
            // if (context.TryGetPlaceholderNum(exprType, out value))
            //     return true;

            switch ((ExpressionType)exprType)
            {
                case ExpressionType.Millisecond:
                    value = (uint)DateTime.Now.Millisecond;
                    return true;
                case ExpressionType.Second:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_sec;
                    return true;
                case ExpressionType.Minute:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_min;
                    return true;
                case ExpressionType.Hour:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_hour;
                    return true;
                case ExpressionType.Day:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_mday;
                    return true;
                case ExpressionType.Weekday:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_wday;
                    return true;
                case ExpressionType.Month:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_mon + 1;
                    return true;
                case ExpressionType.Year:
                    value = (uint)MacroDecoder.GetMacroTime()->tm_year + 1900;
                    return true;
                default:
                    return false;
            }
        }

        if (expression.TryGetParameterExpression(out exprType, out var operand1))
        {
            if (!this.TryResolveUInt(ref context, operand1, out var paramIndex))
                return false;
            if (paramIndex == 0)
                return false;
            paramIndex--;
            return (ExpressionType)exprType switch
            {
                ExpressionType.LocalNumber => context.TryGetLNum((int)paramIndex, out value), // lnum
                ExpressionType.GlobalNumber => this.TryGetGNumDefault(paramIndex, out value), // gnum
                _ => false, // gstr, lstr
            };
        }

        if (expression.TryGetBinaryExpression(out exprType, out operand1, out var operand2))
        {
            switch ((ExpressionType)exprType)
            {
                case ExpressionType.GreaterThanOrEqualTo:
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThanOrEqualTo:
                case ExpressionType.LessThan:
                    if (!this.TryResolveInt(ref context, operand1, out var value1)
                        || !this.TryResolveInt(ref context, operand2, out var value2))
                    {
                        return false;
                    }

                    value = (ExpressionType)exprType switch
                    {
                        ExpressionType.GreaterThanOrEqualTo => value1 >= value2 ? 1u : 0u,
                        ExpressionType.GreaterThan => value1 > value2 ? 1u : 0u,
                        ExpressionType.LessThanOrEqualTo => value1 <= value2 ? 1u : 0u,
                        ExpressionType.LessThan => value1 < value2 ? 1u : 0u,
                        _ => 0u,
                    };
                    return true;

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    if (this.TryResolveInt(ref context, operand1, out value1) && this.TryResolveInt(ref context, operand2, out value2))
                    {
                        if ((ExpressionType)exprType == ExpressionType.Equal)
                            value = value1 == value2 ? 1u : 0u;
                        else
                            value = value1 == value2 ? 0u : 1u;
                        return true;
                    }

                    if (operand1.TryGetString(out var strval1) && operand2.TryGetString(out var strval2))
                    {
                        var resolvedStr1 = this.Evaluate(strval1, context.LocalParameters, context.Language);
                        var resolvedStr2 = this.Evaluate(strval2, context.LocalParameters, context.Language);
                        var equals = resolvedStr1.Equals(resolvedStr2);

                        if ((ExpressionType)exprType == ExpressionType.Equal)
                            value = equals ? 1u : 0u;
                        else
                            value = equals ? 0u : 1u;
                        return true;
                    }

                    // compare int with string, string with int??

                    return true;

                default:
                    return false;
            }
        }

        if (expression.TryGetString(out var str))
        {
            var evaluatedStr = this.Evaluate(str, context.LocalParameters, context.Language);

            foreach (var payload in evaluatedStr)
            {
                if (!payload.TryGetExpression(out var expr))
                    return false;

                return this.TryResolveUInt(ref context, expr, out value);
            }

            return false;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryResolveInt(ref SeStringContext context, in ReadOnlySeExpressionSpan expression, out int value)
    {
        if (this.TryResolveUInt(ref context, expression, out var u32))
        {
            value = (int)u32;
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryResolveBool(ref SeStringContext context, in ReadOnlySeExpressionSpan expression, out bool value)
    {
        if (this.TryResolveUInt(ref context, expression, out var u32))
        {
            value = u32 != 0;
            return true;
        }

        value = false;
        return false;
    }

    private bool ResolveStringExpression(ref SeStringContext context, in ReadOnlySeExpressionSpan expression)
    {
        uint u32;

        if (expression.TryGetString(out var innerString))
        {
            context.Builder.Append(this.Evaluate(innerString, context.LocalParameters, context.Language));
            return true;
        }

        /*
        if (expression.TryGetPlaceholderExpression(out var exprType))
        {
            if (context.TryProducePlaceholder(ref context, exprType))
                return true;
        }
        */

        if (expression.TryGetParameterExpression(out var exprType, out var operand1))
        {
            if (!this.TryResolveUInt(ref context, operand1, out var paramIndex))
                return false;
            if (paramIndex == 0)
                return false;
            paramIndex--;
            switch ((ExpressionType)exprType)
            {
                case ExpressionType.LocalNumber: // lnum
                    if (!context.TryGetLNum((int)paramIndex, out u32))
                        return false;

                    context.Builder.Append(unchecked((int)u32).ToString());
                    return true;

                case ExpressionType.LocalString: // lstr
                    if (!context.TryGetLStr((int)paramIndex, out var str))
                        return false;

                    context.Builder.Append(str);
                    return true;

                case ExpressionType.GlobalNumber: // gnum
                    if (!this.TryGetGNumDefault(paramIndex, out u32))
                        return false;

                    context.Builder.Append(unchecked((int)u32).ToString());
                    return true;

                case ExpressionType.GlobalString: // gstr
                    return this.TryProduceGStrDefault(ref context.Builder, context.Language, paramIndex);

                default:
                    return false;
            }
        }

        // Handles UInt and Binary expressions
        if (!this.TryResolveUInt(ref context, expression, out u32))
            return false;

        context.Builder.Append(((int)u32).ToString());
        return true;
    }
}
