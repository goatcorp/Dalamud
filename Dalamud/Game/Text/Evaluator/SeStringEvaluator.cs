using System.Collections.Concurrent;
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
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.Interop;

using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Lumina.Text;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using AddonSheet = Lumina.Excel.Sheets.Addon;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;
using StatusSheet = Lumina.Excel.Sheets.Status;

namespace Dalamud.Game.Text.Evaluator;

/// <summary>
/// Evaluator for SeStrings.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
[ResolveVia<ISeStringEvaluator>]
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

    private readonly ConcurrentDictionary<StringCacheKey<ActionKind>, string> actStrCache = [];
    private readonly ConcurrentDictionary<StringCacheKey<ObjectKind>, string> objStrCache = [];

    [ServiceManager.ServiceConstructor]
    private SeStringEvaluator()
    {
    }

    /// <inheritdoc/>
    public ReadOnlySeString Evaluate(
        ReadOnlySeString str,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        return this.Evaluate(str.AsSpan(), localParameters, language);
    }

    /// <inheritdoc/>
    public ReadOnlySeString Evaluate(
        ReadOnlySeStringSpan str,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        if (str.IsTextOnly())
            return new(str);

        var lang = language ?? this.GetEffectiveClientLanguage();

        // TODO: remove culture info toggling after supporting CultureInfo for SeStringBuilder.Append,
        //       and then remove try...finally block (discard builder from the pool on exception)
        var previousCulture = CultureInfo.CurrentCulture;
        var builder = SeStringBuilder.SharedPool.Get();
        try
        {
            CultureInfo.CurrentCulture = Localization.GetCultureInfoFromLangCode(lang.ToCode());
            return this.EvaluateAndAppendTo(builder, str, localParameters, lang).ToReadOnlySeString();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            SeStringBuilder.SharedPool.Return(builder);
        }
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateMacroString(
        string macroString,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        return this.Evaluate(ReadOnlySeString.FromMacroString(macroString).AsSpan(), localParameters, language);
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateMacroString(
        ReadOnlySpan<byte> macroString,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        return this.Evaluate(ReadOnlySeString.FromMacroString(macroString).AsSpan(), localParameters, language);
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateFromAddon(
        uint addonId,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        var lang = language ?? this.GetEffectiveClientLanguage();

        if (!this.dataManager.GetExcelSheet<AddonSheet>(lang).TryGetRow(addonId, out var addonRow))
            return default;

        return this.Evaluate(addonRow.Text.AsSpan(), localParameters, lang);
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateFromLobby(
        uint lobbyId,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        var lang = language ?? this.GetEffectiveClientLanguage();

        if (!this.dataManager.GetExcelSheet<Lobby>(lang).TryGetRow(lobbyId, out var lobbyRow))
            return default;

        return this.Evaluate(lobbyRow.Text.AsSpan(), localParameters, lang);
    }

    /// <inheritdoc/>
    public ReadOnlySeString EvaluateFromLogMessage(
        uint logMessageId,
        Span<SeStringParameter> localParameters = default,
        ClientLanguage? language = null)
    {
        var lang = language ?? this.GetEffectiveClientLanguage();

        if (!this.dataManager.GetExcelSheet<LogMessage>(lang).TryGetRow(logMessageId, out var logMessageRow))
            return default;

        return this.Evaluate(logMessageRow.Text.AsSpan(), localParameters, lang);
    }

    /// <inheritdoc/>
    public string EvaluateActStr(ActionKind actionKind, uint id, ClientLanguage? language = null) =>
        this.actStrCache.GetOrAdd(
            new(actionKind, id, language ?? this.GetEffectiveClientLanguage()),
            static (key, t) => t.EvaluateFromAddon(2026, [key.Kind.GetActStrId(key.Id)], key.Language)
                                .ExtractText()
                                .StripSoftHyphen(),
            this);

    /// <inheritdoc/>
    public string EvaluateObjStr(ObjectKind objectKind, uint id, ClientLanguage? language = null) =>
        this.objStrCache.GetOrAdd(
            new(objectKind, id, language ?? this.GetEffectiveClientLanguage()),
            static (key, t) => t.EvaluateFromAddon(2025, [key.Kind.GetObjStrId(key.Id)], key.Language)
                                .ExtractText()
                                .StripSoftHyphen(),
            this);

    // TODO: move this to MapUtil?
    private static uint ConvertRawToMapPos(Lumina.Excel.Sheets.Map map, short offset, float value)
    {
        var scale = map.SizeFactor / 100.0f;
        return (uint)(10 - (int)(((((value + offset) * scale) + 1024f) * -0.2f) / scale));
    }

    private static uint ConvertRawToMapPosX(Lumina.Excel.Sheets.Map map, float x)
        => ConvertRawToMapPos(map, map.OffsetX, x);

    private static uint ConvertRawToMapPosY(Lumina.Excel.Sheets.Map map, float y)
        => ConvertRawToMapPos(map, map.OffsetY, y);

    private ClientLanguage GetEffectiveClientLanguage()
    {
        return this.dalamudConfiguration.EffectiveLanguage switch
        {
            "ja" => ClientLanguage.Japanese,
            "en" => ClientLanguage.English,
            "de" => ClientLanguage.German,
            "fr" => ClientLanguage.French,
            _ => this.clientState.ClientLanguage,
        };
    }

    private SeStringBuilder EvaluateAndAppendTo(
        SeStringBuilder builder,
        ReadOnlySeStringSpan str,
        Span<SeStringParameter> localParameters,
        ClientLanguage language)
    {
        var context = new SeStringContext(builder, localParameters, language);

        foreach (var payload in str)
        {
            if (!this.ResolvePayload(in context, payload))
            {
                context.Builder.Append(payload);
            }
        }

        return builder;
    }

    private bool ResolvePayload(in SeStringContext context, ReadOnlySePayloadSpan payload)
    {
        if (payload.Type != ReadOnlySePayloadType.Macro)
            return false;

        // if (context.HandlePayload(payload, in context))
        //    return true;

        switch (payload.MacroCode)
        {
            case MacroCode.SetResetTime:
                return this.TryResolveSetResetTime(in context, payload);

            case MacroCode.SetTime:
                return this.TryResolveSetTime(in context, payload);

            case MacroCode.If:
                return this.TryResolveIf(in context, payload);

            case MacroCode.Switch:
                return this.TryResolveSwitch(in context, payload);

            case MacroCode.SwitchPlatform:
                return this.TryResolveSwitchPlatform(in context, payload);

            case MacroCode.PcName:
                return this.TryResolvePcName(in context, payload);

            case MacroCode.IfPcGender:
                return this.TryResolveIfPcGender(in context, payload);

            case MacroCode.IfPcName:
                return this.TryResolveIfPcName(in context, payload);

            // case MacroCode.Josa:
            // case MacroCode.Josaro:

            case MacroCode.IfSelf:
                return this.TryResolveIfSelf(in context, payload);

            // case MacroCode.NewLine: // pass through
            // case MacroCode.Wait: // pass through
            // case MacroCode.Icon: // pass through

            case MacroCode.Color:
                return this.TryResolveColor(in context, payload);

            case MacroCode.EdgeColor:
                return this.TryResolveEdgeColor(in context, payload);

            case MacroCode.ShadowColor:
                return this.TryResolveShadowColor(in context, payload);

            // case MacroCode.SoftHyphen: // pass through
            // case MacroCode.Key:
            // case MacroCode.Scale:

            case MacroCode.Bold:
                return this.TryResolveBold(in context, payload);

            case MacroCode.Italic:
                return this.TryResolveItalic(in context, payload);

            // case MacroCode.Edge:
            // case MacroCode.Shadow:
            // case MacroCode.NonBreakingSpace: // pass through
            // case MacroCode.Icon2: // pass through
            // case MacroCode.Hyphen: // pass through

            case MacroCode.Num:
                return this.TryResolveNum(in context, payload);

            case MacroCode.Hex:
                return this.TryResolveHex(in context, payload);

            case MacroCode.Kilo:
                return this.TryResolveKilo(in context, payload);

            // case MacroCode.Byte:

            case MacroCode.Sec:
                return this.TryResolveSec(in context, payload);

            // case MacroCode.Time:

            case MacroCode.Float:
                return this.TryResolveFloat(in context, payload);

            // case MacroCode.Link: // pass through

            case MacroCode.Sheet:
                return this.TryResolveSheet(in context, payload);

            case MacroCode.SheetSub:
                return this.TryResolveSheetSub(in context, payload);

            case MacroCode.String:
                return this.TryResolveString(in context, payload);

            case MacroCode.Caps:
                return this.TryResolveCaps(in context, payload);

            case MacroCode.Head:
                return this.TryResolveHead(in context, payload);

            case MacroCode.Split:
                return this.TryResolveSplit(in context, payload);

            case MacroCode.HeadAll:
                return this.TryResolveHeadAll(in context, payload);

            case MacroCode.Fixed:
                return this.TryResolveFixed(in context, payload);

            case MacroCode.Lower:
                return this.TryResolveLower(in context, payload);

            case MacroCode.JaNoun:
                return this.TryResolveNoun(ClientLanguage.Japanese, in context, payload);

            case MacroCode.EnNoun:
                return this.TryResolveNoun(ClientLanguage.English, in context, payload);

            case MacroCode.DeNoun:
                return this.TryResolveNoun(ClientLanguage.German, in context, payload);

            case MacroCode.FrNoun:
                return this.TryResolveNoun(ClientLanguage.French, in context, payload);

            // case MacroCode.ChNoun:

            case MacroCode.LowerHead:
                return this.TryResolveLowerHead(in context, payload);

            case MacroCode.ColorType:
                return this.TryResolveColorType(in context, payload);

            case MacroCode.EdgeColorType:
                return this.TryResolveEdgeColorType(in context, payload);

            // case MacroCode.Ruby:

            case MacroCode.Digit:
                return this.TryResolveDigit(in context, payload);

            case MacroCode.Ordinal:
                return this.TryResolveOrdinal(in context, payload);

            // case MacroCode.Sound: // pass through

            case MacroCode.LevelPos:
                return this.TryResolveLevelPos(in context, payload);

            default:
                return false;
        }
    }

    private unsafe bool TryResolveSetResetTime(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        DateTime date;

        if (payload.TryGetExpression(out var eHour, out var eWeekday)
            && this.TryResolveInt(in context, eHour, out var eHourVal)
            && this.TryResolveInt(in context, eWeekday, out var eWeekdayVal))
        {
            var t = DateTime.UtcNow.AddDays(((eWeekdayVal - (int)DateTime.UtcNow.DayOfWeek) + 7) % 7);
            date = new DateTime(t.Year, t.Month, t.Day, eHourVal, 0, 0, DateTimeKind.Utc).ToLocalTime();
        }
        else if (payload.TryGetExpression(out eHour)
                 && this.TryResolveInt(in context, eHour, out eHourVal))
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

    private unsafe bool TryResolveSetTime(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eTime) || !this.TryResolveUInt(in context, eTime, out var eTimeVal))
            return false;

        var date = DateTimeOffset.FromUnixTimeSeconds(eTimeVal).LocalDateTime;
        MacroDecoder.GetMacroTime()->SetTime(date);

        return true;
    }

    private bool TryResolveIf(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        return
            payload.TryGetExpression(out var eCond, out var eTrue, out var eFalse)
            && this.ResolveStringExpression(
                context,
                this.TryResolveBool(in context, eCond, out var eCondVal) && eCondVal
                    ? eTrue
                    : eFalse);
    }

    private bool TryResolveSwitch(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var cond = -1;
        foreach (var e in payload)
        {
            switch (cond)
            {
                case -1:
                    cond = this.TryResolveUInt(in context, e, out var eVal) ? (int)eVal : 0;
                    break;
                case > 1:
                    cond--;
                    break;
                default:
                    return this.ResolveStringExpression(in context, e);
            }
        }

        return false;
    }

    private bool TryResolveSwitchPlatform(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var expr1))
            return false;

        if (!expr1.TryGetInt(out var intVal))
            return false;

        // Our version of the game uses IsMacClient() here and the
        // Xbox version seems to always return 7 for the platform.
        var platform = Util.IsWine() ? 5 : 3;

        // The sheet is seeminly split into first 20 rows for wired controllers
        // and the last 20 rows for wireless controllers.
        var rowId = (uint)((20 * ((intVal - 1) / 20)) + (platform - 4 < 2 ? 2 : 1));

        if (!this.dataManager.GetExcelSheet<Platform>().TryGetRow(rowId, out var platformRow))
            return false;

        context.Builder.Append(platformRow.Name);
        return true;
    }

    private unsafe bool TryResolvePcName(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId))
            return false;

        if (!this.TryResolveUInt(in context, eEntityId, out var entityId))
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

                if (this.gameConfig.UiConfig.TryGetUInt("LogCrossWorldName", out var logCrossWorldName) &&
                    logCrossWorldName == 1)
                    context.Builder.Append(new ReadOnlySeStringSpan(world.Name.GetPointer(0)));
            }

            return true;
        }

        // TODO: lookup via InstanceContentCrystallineConflictDirector
        // TODO: lookup via MJIManager

        return false;
    }

    private unsafe bool TryResolveIfPcGender(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId, out var eMale, out var eFemale))
            return false;

        if (!this.TryResolveUInt(in context, eEntityId, out var entityId))
            return false;

        NameCache.CharacterInfo characterInfo = default;
        if (NameCache.Instance()->TryGetCharacterInfoByEntityId(entityId, &characterInfo))
            return this.ResolveStringExpression(in context, characterInfo.Sex == 0 ? eMale : eFemale);

        // TODO: lookup via InstanceContentCrystallineConflictDirector

        return false;
    }

    private unsafe bool TryResolveIfPcName(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId, out var eName, out var eTrue, out var eFalse))
            return false;

        if (!this.TryResolveUInt(in context, eEntityId, out var entityId) || !eName.TryGetString(out var name))
            return false;

        name = this.Evaluate(name, context.LocalParameters, context.Language).AsSpan();

        NameCache.CharacterInfo characterInfo = default;
        return NameCache.Instance()->TryGetCharacterInfoByEntityId(entityId, &characterInfo) &&
               this.ResolveStringExpression(
                   context,
                   name.Equals(characterInfo.Name.AsSpan())
                       ? eTrue
                       : eFalse);
    }

    private unsafe bool TryResolveIfSelf(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEntityId, out var eTrue, out var eFalse))
            return false;

        if (!this.TryResolveUInt(in context, eEntityId, out var entityId))
            return false;

        // the game uses LocalPlayer here, but using PlayerState seems more safe.
        return this.ResolveStringExpression(in context, PlayerState.Instance()->EntityId == entityId ? eTrue : eFalse);
    }

    private bool TryResolveColor(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColor))
            return false;

        if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
            context.Builder.PopColor();
        else if (this.TryResolveUInt(in context, eColor, out var eColorVal))
            context.Builder.PushColorBgra(eColorVal);

        return true;
    }

    private bool TryResolveEdgeColor(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColor))
            return false;

        if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
            context.Builder.PopEdgeColor();
        else if (this.TryResolveUInt(in context, eColor, out var eColorVal))
            context.Builder.PushEdgeColorBgra(eColorVal);

        return true;
    }

    private bool TryResolveShadowColor(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColor))
            return false;

        if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
            context.Builder.PopShadowColor();
        else if (this.TryResolveUInt(in context, eColor, out var eColorVal))
            context.Builder.PushShadowColorBgra(eColorVal);

        return true;
    }

    private bool TryResolveBold(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEnable) ||
            !this.TryResolveBool(in context, eEnable, out var eEnableVal))
            return false;

        context.Builder.AppendSetBold(eEnableVal);

        return true;
    }

    private bool TryResolveItalic(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eEnable) ||
            !this.TryResolveBool(in context, eEnable, out var eEnableVal))
            return false;

        context.Builder.AppendSetItalic(eEnableVal);

        return true;
    }

    private bool TryResolveNum(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eInt) || !this.TryResolveInt(in context, eInt, out var eIntVal))
        {
            context.Builder.Append('0');
            return true;
        }

        context.Builder.Append(eIntVal.ToString());

        return true;
    }

    private bool TryResolveHex(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eUInt) || !this.TryResolveUInt(in context, eUInt, out var eUIntVal))
        {
            // TODO: throw?
            // ERROR: mismatch parameter type ('' is not numeric)
            return false;
        }

        context.Builder.Append("0x{0:X08}".Format(eUIntVal));

        return true;
    }

    private bool TryResolveKilo(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eInt, out var eSep) ||
            !this.TryResolveInt(in context, eInt, out var eIntVal))
        {
            context.Builder.Append('0');
            return true;
        }

        if (eIntVal == int.MinValue)
        {
            // -2147483648
            context.Builder.Append("-2"u8);
            this.ResolveStringExpression(in context, eSep);
            context.Builder.Append("147"u8);
            this.ResolveStringExpression(in context, eSep);
            context.Builder.Append("483"u8);
            this.ResolveStringExpression(in context, eSep);
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
            var digit = (eIntVal / i) % 10;
            switch (anyDigitPrinted)
            {
                case false when digit == 0:
                    continue;
                case true when MathF.Log10(i) % 3 == 2:
                    this.ResolveStringExpression(in context, eSep);
                    break;
            }

            anyDigitPrinted = true;
            context.Builder.Append((char)('0' + digit));
        }

        return true;
    }

    private bool TryResolveSec(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eInt) || !this.TryResolveUInt(in context, eInt, out var eIntVal))
        {
            // TODO: throw?
            // ERROR: mismatch parameter type ('' is not numeric)
            return false;
        }

        context.Builder.Append("{0:00}".Format(eIntVal));
        return true;
    }

    private bool TryResolveFloat(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eValue, out var eRadix, out var eSeparator)
            || !this.TryResolveInt(in context, eValue, out var eValueVal)
            || !this.TryResolveInt(in context, eRadix, out var eRadixVal))
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
        this.ResolveStringExpression(in context, eSeparator);

        // brain fried code
        Span<byte> fractionalDigits = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var pos = fractionalDigits.Length - 1;
        for (var r = eRadixVal; r > 1; r /= 10)
        {
            fractionalDigits[pos--] = (byte)('0' + (fractionalPart % 10));
            fractionalPart /= 10;
        }

        context.Builder.Append(fractionalDigits[(pos + 1)..]);

        return true;
    }

    private bool TryResolveSheet(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var eSheetNameStr))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var eRowIdValue))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var eColIndexValue))
            return false;

        var eColParamValue = 0u;
        if (enu.MoveNext())
            this.TryResolveUInt(in context, enu.Current, out eColParamValue);

        var resolvedSheetName = this.Evaluate(eSheetNameStr, context.LocalParameters, context.Language).ExtractText();
        var originalRowIdValue = eRowIdValue;
        var flags = this.sheetRedirectResolver.Resolve(ref resolvedSheetName, ref eRowIdValue, ref eColIndexValue);

        if (string.IsNullOrEmpty(resolvedSheetName))
            return false;

        var text = this.FormatSheetValue(context.Language, resolvedSheetName, eRowIdValue, eColIndexValue, eColParamValue);
        if (text.IsEmpty)
            return false;

        this.AddSheetRedirectItemDecoration(context, ref text, flags, eRowIdValue);

        if (resolvedSheetName != "DescriptionString")
            eColParamValue = originalRowIdValue;

        // Note: The link marker symbol is added by RaptureLogMessage, probably somewhere in it's Update function.
        // It is not part of this generated link.
        this.CreateSheetLink(context, resolvedSheetName, text, eRowIdValue, eColParamValue);

        return true;
    }

    private bool TryResolveSheetSub(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var eSheetNameStr))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var eRowIdValue))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var eSubrowIdValue))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var eColIndexValue))
            return false;

        var secondaryRowId = this.GetSubrowSheetIntValue(context.Language, eSheetNameStr.ExtractText(), eRowIdValue, (ushort)eSubrowIdValue, eColIndexValue);
        if (secondaryRowId == -1)
            return false;

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var eSecondarySheetNameStr))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var secondaryColIndex))
            return false;

        var text = this.FormatSheetValue(context.Language, eSecondarySheetNameStr.ExtractText(), (uint)secondaryRowId, secondaryColIndex, 0);
        if (text.IsEmpty)
            return false;

        this.CreateSheetLink(context, eSecondarySheetNameStr.ExtractText(), text, eRowIdValue, eSubrowIdValue);

        return true;
    }

    private int GetSubrowSheetIntValue(ClientLanguage language, string sheetName, uint rowId, ushort subrowId, uint colIndex)
    {
        if (!this.dataManager.Excel.SheetNames.Contains(sheetName))
            return -1;

        if (!this.dataManager.GetSubrowExcelSheet<RawSubrow>(language, sheetName)
            .TryGetSubrow(rowId, subrowId, out var row))
            return -1;

        if (colIndex >= row.Columns.Count)
            return -1;

        var column = row.Columns[(int)colIndex];
        return column.Type switch
        {
            ExcelColumnDataType.Int8 => row.ReadInt8(column.Offset),
            ExcelColumnDataType.UInt8 => row.ReadUInt8(column.Offset),
            ExcelColumnDataType.Int16 => row.ReadInt16(column.Offset),
            ExcelColumnDataType.UInt16 => row.ReadUInt16(column.Offset),
            ExcelColumnDataType.Int32 => row.ReadInt32(column.Offset),
            _ => -1,
        };
    }

    private ReadOnlySeString FormatSheetValue(ClientLanguage language, string sheetName, uint rowId, uint colIndex, uint colParam)
    {
        if (!this.dataManager.Excel.SheetNames.Contains(sheetName))
            return default;

        if (!this.dataManager.GetExcelSheet<RawRow>(language, sheetName)
                 .TryGetRow(rowId, out var row))
            return default;

        if (colIndex >= row.Columns.Count)
            return default;

        var column = row.Columns[(int)colIndex];
        return column.Type switch
        {
            ExcelColumnDataType.String => this.Evaluate(row.ReadString(column.Offset), [colParam], language),
            ExcelColumnDataType.Bool => (row.ReadBool(column.Offset) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.Int8 => row.ReadInt8(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.UInt8 => row.ReadUInt8(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.Int16 => row.ReadInt16(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.UInt16 => row.ReadUInt16(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.Int32 => row.ReadInt32(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.UInt32 => row.ReadUInt32(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.Float32 => row.ReadFloat32(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.Int64 => row.ReadInt64(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.UInt64 => row.ReadUInt64(column.Offset).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool0 => (row.ReadPackedBool(column.Offset, 0) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool1 => (row.ReadPackedBool(column.Offset, 1) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool2 => (row.ReadPackedBool(column.Offset, 2) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool3 => (row.ReadPackedBool(column.Offset, 3) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool4 => (row.ReadPackedBool(column.Offset, 4) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool5 => (row.ReadPackedBool(column.Offset, 5) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool6 => (row.ReadPackedBool(column.Offset, 6) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            ExcelColumnDataType.PackedBool7 => (row.ReadPackedBool(column.Offset, 7) ? 1u : 0).ToString("D", CultureInfo.InvariantCulture),
            _ => default,
        };
    }

    private void AddSheetRedirectItemDecoration(in SeStringContext context, ref ReadOnlySeString text, SheetRedirectFlags flags, uint eRowIdValue)
    {
        if (!flags.HasFlag(SheetRedirectFlags.Item))
            return;

        var rarity = 1u;
        var skipLink = false;

        if (flags.HasFlag(SheetRedirectFlags.EventItem))
        {
            rarity = 8;
            skipLink = true;
        }

        var itemId = eRowIdValue;

        if (this.dataManager.GetExcelSheet<Item>(context.Language).TryGetRow(itemId, out var itemRow))
        {
            rarity = itemRow.Rarity;
            if (rarity == 0)
                rarity = 1;

            if (itemRow.FilterGroup is 38 or 50)
                skipLink = true;
        }

        if (flags.HasFlag(SheetRedirectFlags.Collectible))
        {
            itemId += 500000;
        }
        else if (flags.HasFlag(SheetRedirectFlags.HighQuality))
        {
            itemId += 1000000;
        }

        var sb = SeStringBuilder.SharedPool.Get();

        sb.Append(this.EvaluateFromAddon(6, [rarity], context.Language));

        if (!skipLink)
            sb.PushLink(LinkMacroPayloadType.Item, itemId, rarity, 0u); // arg3 = some LogMessage flag based on LogKind RowId? => "89 5C 24 20 E8 ?? ?? ?? ?? 48 8B 1F"

        // there is code here for handling noun link markers (//), but i don't know why

        sb.Append(text);

        if (flags.HasFlag(SheetRedirectFlags.HighQuality)
            && this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(9, out var hqSymbol))
        {
            sb.Append(hqSymbol.Text);
        }
        else if (flags.HasFlag(SheetRedirectFlags.Collectible)
            && this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(150, out var collectibleSymbol))
        {
            sb.Append(collectibleSymbol.Text);
        }

        if (!skipLink)
            sb.PopLink();

        text = sb.ToReadOnlySeString();
        SeStringBuilder.SharedPool.Return(sb);
    }

    private void CreateSheetLink(in SeStringContext context, string resolvedSheetName, ReadOnlySeString text, uint eRowIdValue, uint eColParamValue)
    {
        switch (resolvedSheetName)
        {
            case "Achievement":
                context.Builder.PushLink(LinkMacroPayloadType.Achievement, eRowIdValue, 0u, 0u, text.AsSpan());
                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            case "HowTo":
                context.Builder.PushLink(LinkMacroPayloadType.HowTo, eRowIdValue, 0u, 0u, text.AsSpan());
                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            case "Status" when this.dataManager.GetExcelSheet<StatusSheet>(context.Language).TryGetRow(eRowIdValue, out var statusRow):
                context.Builder.PushLink(LinkMacroPayloadType.Status, eRowIdValue, 0u, 0u, []);

                switch (statusRow.StatusCategory)
                {
                    case 1: context.Builder.Append(this.EvaluateFromAddon(376)); break; // buff symbol
                    case 2: context.Builder.Append(this.EvaluateFromAddon(377)); break; // debuff symbol
                }

                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            case "AkatsukiNoteString":
                context.Builder.PushLink(LinkMacroPayloadType.AkatsukiNote, eColParamValue, 0u, 0u, text.AsSpan());
                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            case "DescriptionString" when eColParamValue > 0:
                context.Builder.PushLink((LinkMacroPayloadType)11, eRowIdValue, eColParamValue, 0u, text.AsSpan());
                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            case "WKSPioneeringTrailString":
                context.Builder.PushLink((LinkMacroPayloadType)12, eRowIdValue, eColParamValue, 0u, text.AsSpan());
                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            case "MKDLore":
                context.Builder.PushLink((LinkMacroPayloadType)13, eRowIdValue, 0u, 0u, text.AsSpan());
                context.Builder.Append(text);
                context.Builder.PopLink();
                return;

            default:
                context.Builder.Append(text);
                return;
        }
    }

    private bool TryResolveString(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        return payload.TryGetExpression(out var eStr) && this.ResolveStringExpression(in context, eStr);
    }

    private bool TryResolveCaps(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(headContext, eStr))
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

    private bool TryResolveHead(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(headContext, eStr))
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
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.Span).FirstCharToUpper(context.CultureInfo));
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

    private bool TryResolveSplit(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eText, out var eSeparator, out var eIndex))
            return false;

        if (!eSeparator.TryGetString(out var eSeparatorVal) || !eIndex.TryGetUInt(out var eIndexVal) || eIndexVal <= 0)
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(headContext, eText))
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

    private bool TryResolveHeadAll(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(headContext, eStr))
                return false;

            var str = builder.ToReadOnlySeString();

            foreach (var p in str)
            {
                if (p.Type == ReadOnlySePayloadType.Invalid)
                    continue;

                if (p.Type == ReadOnlySePayloadType.Text)
                {
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.Span).ToUpper(true, true, false, context.Language));
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

    private bool TryResolveFixed(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        // This is handled by the second function in Client::UI::Misc::PronounModule_ProcessString

        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var e0Val))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var e1Val))
            return false;

        return e0Val switch
        {
            100 or 200 => e1Val switch
            {
                1 => this.TryResolveFixedPlayerLink(in context, ref enu),
                2 => this.TryResolveFixedClassJobLevel(in context, ref enu),
                3 => this.TryResolveFixedMapLink(in context, ref enu),
                4 => this.TryResolveFixedItemLink(in context, ref enu),
                5 => this.TryResolveFixedChatSoundEffect(in context, ref enu),
                6 => this.TryResolveFixedObjStr(in context, ref enu),
                7 => this.TryResolveFixedString(in context, ref enu),
                8 => this.TryResolveFixedTimeRemaining(in context, ref enu),
                // Reads a uint and saves it to PronounModule+0x3AC
                // TODO: handle this? looks like it's for the mentor/beginner icon of the player link in novice network
                // see "FF 50 50 8B B0"
                9 => true,
                10 => this.TryResolveFixedStatusLink(in context, ref enu),
                11 => this.TryResolveFixedPartyFinderLink(in context, ref enu),
                12 => this.TryResolveFixedQuestLink(in context, ref enu),
                _ => false,
            },
            _ => this.TryResolveFixedAutoTranslation(in context, payload, e0Val, e1Val),
        };
    }

    private unsafe bool TryResolveFixedPlayerLink(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var worldId))
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

    private bool TryResolveFixedClassJobLevel(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var classJobId) || classJobId <= 0)
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var level))
            return false;

        if (!this.dataManager.GetExcelSheet<ClassJob>(context.Language)
                 .TryGetRow((uint)classJobId, out var classJobRow))
            return false;

        context.Builder.Append(classJobRow.Name);

        if (level != 0)
            context.Builder.Append(context.CultureInfo, $"({level:D})");

        return true;
    }

    private bool TryResolveFixedMapLink(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var territoryTypeId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var packedIds))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var rawX))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var rawY))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var rawZ))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var placeNameIdInt))
            return false;

        var instance = packedIds >> 16;
        var mapId = packedIds & 0xFFFF;

        if (this.dataManager.GetExcelSheet<TerritoryType>(context.Language)
                .TryGetRow(territoryTypeId, out var territoryTypeRow))
        {
            if (!this.dataManager.GetExcelSheet<PlaceName>(context.Language)
                     .TryGetRow(
                         placeNameIdInt == 0 ? territoryTypeRow.PlaceName.RowId : placeNameIdInt,
                         out var placeNameRow))
                return false;

            if (!this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Map>().TryGetRow(mapId, out var mapRow))
                return false;

            var sb = SeStringBuilder.SharedPool.Get();

            sb.Append(placeNameRow.Name);
            if (instance is > 0 and <= 9)
                sb.Append((char)((char)0xE0B0 + (char)instance));

            var placeNameWithInstance = sb.ToReadOnlySeString();
            SeStringBuilder.SharedPool.Return(sb);

            var mapPosX = ConvertRawToMapPosX(mapRow, rawX / 1000f);
            var mapPosY = ConvertRawToMapPosY(mapRow, rawY / 1000f);

            var linkText = rawZ == -30000
                               ? this.EvaluateFromAddon(
                                   1635,
                                   [placeNameWithInstance, mapPosX, mapPosY],
                                   context.Language)
                               : this.EvaluateFromAddon(
                                   1636,
                                   [placeNameWithInstance, mapPosX, mapPosY, rawZ / (rawZ >= 0 ? 10 : -10), rawZ],
                                   context.Language);

            context.Builder.PushLinkMapPosition(territoryTypeId, mapId, rawX, rawY);
            context.Builder.Append(this.EvaluateFromAddon(371, [linkText], context.Language));
            context.Builder.PopLink();

            return true;
        }

        var rowId = mapId switch
        {
            0 => 875u, // "(No location set for map link)"
            1 => 874u, // "(Map link unavailable in this area)"
            2 => 13743u, // "(Unable to set map link)"
            _ => 0u,
        };
        if (rowId == 0u)
            return false;
        if (this.dataManager.GetExcelSheet<AddonSheet>(context.Language).TryGetRow(rowId, out var addonRow))
            context.Builder.Append(addonRow.Text);
        return true;
    }

    private bool TryResolveFixedItemLink(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var itemId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var rarity))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var unk2))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var unk3))
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

    private bool TryResolveFixedChatSoundEffect(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var soundEffectId))
            return false;

        context.Builder.Append($"<se.{soundEffectId + 1}>");

        // the game would play it here

        return true;
    }

    private bool TryResolveFixedObjStr(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var objStrId))
            return false;

        context.Builder.Append(this.EvaluateFromAddon(2025, [objStrId], context.Language));

        return true;
    }

    private bool TryResolveFixedString(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !enu.Current.TryGetString(out var text))
            return false;

        // formats it through vsprintf using "%s"??
        context.Builder.Append(text.ExtractText());

        return true;
    }

    private bool TryResolveFixedTimeRemaining(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var seconds))
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

    private bool TryResolveFixedStatusLink(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var statusId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveBool(in context, enu.Current, out var hasOverride))
            return false;

        if (!this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>(context.Language)
                 .TryGetRow(statusId, out var statusRow))
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

    private bool TryResolveFixedPartyFinderLink(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var listingId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var unk1))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var worldId))
            return false;

        if (!enu.MoveNext() || !this.TryResolveInt(
                context,
                enu.Current,
                out var crossWorldFlag)) // 0 = cross world, 1 = not cross world
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

        context.Builder.Append(
            this.EvaluateFromAddon(
                371,
                [this.EvaluateFromAddon(2265, [playerName, crossWorldFlag], context.Language)],
                context.Language));

        context.Builder.PopLink();

        return true;
    }

    private bool TryResolveFixedQuestLink(in SeStringContext context, ref ReadOnlySePayloadSpan.Enumerator enu)
    {
        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var questId))
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

    private bool TryResolveFixedAutoTranslation(
        in SeStringContext context, in ReadOnlySePayloadSpan payload, int e0Val, int e1Val)
    {
        // Auto-Translation / Completion
        var group = (uint)(e0Val + 1);
        var rowId = (uint)e1Val;

        if (!this.dataManager.GetExcelSheet<Completion>(context.Language).TryGetFirst(
                row => row.Group == group && !row.LookupTable.IsEmpty,
                out var groupRow))
            return false;

        var lookupTable = (
                              groupRow.LookupTable.IsTextOnly()
                                  ? groupRow.LookupTable
                                  : this.Evaluate(
                                      groupRow.LookupTable.AsSpan(),
                                      context.LocalParameters,
                                      context.Language)).ExtractText();

        // Completion sheet
        if (lookupTable.Equals("@"))
        {
            if (this.dataManager.GetExcelSheet<Completion>(context.Language).TryGetRow(rowId, out var completionRow))
            {
                context.Builder.Append(completionRow.Text);
            }

            return true;
        }

        using var icons = new SeStringBuilderIconWrap(context.Builder, 54, 55);

        // CategoryDataCache
        if (lookupTable.Equals("#"))
        {
            // couldn't find any, so we don't handle them :p
            context.Builder.Append(payload);
            return false;
        }

        // All other sheets
        var rangesStart = lookupTable.IndexOf('[');
        // Sheet without ranges
        if (rangesStart == -1)
        {
            if (this.dataManager.GetExcelSheet<RawRow>(context.Language, lookupTable).TryGetRow(rowId, out var row))
            {
                context.Builder.Append(row.ReadStringColumn(0));
                return true;
            }
        }

        var sheetName = lookupTable[..rangesStart];
        var ranges = lookupTable[(rangesStart + 1)..^1];
        if (ranges.Length == 0)
            return true;

        var isNoun = false;

        var colIndex = 0;
        Span<int> cols = stackalloc int[8];
        cols.Clear();
        var hasRanges = false;
        var isInRange = false;

        while (!string.IsNullOrWhiteSpace(ranges))
        {
            // find the end of the current entry
            var entryEnd = ranges.IndexOf(',');
            if (entryEnd == -1)
                entryEnd = ranges.Length;

            if (ranges.StartsWith("noun", StringComparison.Ordinal))
            {
                isNoun = true;
            }
            else if (ranges.StartsWith("col", StringComparison.Ordinal) && colIndex < cols.Length)
            {
                cols[colIndex++] = int.Parse(ranges.AsSpan(4, entryEnd - 4));
            }
            else if (ranges.StartsWith("tail", StringComparison.Ordinal))
            {
                // currently not supported, since there are no known uses
                context.Builder.Append(payload);
                return false;
            }
            else
            {
                var dash = ranges.IndexOf('-');

                hasRanges |= true;

                if (dash == -1)
                {
                    isInRange |= int.Parse(ranges.AsSpan(0, entryEnd)) == rowId;
                }
                else
                {
                    isInRange |= rowId >= int.Parse(ranges.AsSpan(0, dash))
                        && rowId <= int.Parse(ranges.AsSpan(dash + 1, entryEnd - dash - 1));
                }
            }

            // if it's the end of the string, we're done
            if (entryEnd == ranges.Length)
                break;

            // else, move to the next entry
            ranges = ranges[(entryEnd + 1)..].TrimStart();
        }

        if (hasRanges && !isInRange)
        {
            context.Builder.Append(payload);
            return false;
        }

        if (isNoun && context.Language == ClientLanguage.German && sheetName == "Companion")
        {
            context.Builder.Append(this.nounProcessor.ProcessNoun(new NounParams()
            {
                Language = ClientLanguage.German,
                SheetName = sheetName,
                RowId = rowId,
                Quantity = 1,
                ArticleType = (int)GermanArticleType.ZeroArticle,
            }));
        }
        else if (this.dataManager.GetExcelSheet<RawRow>(context.Language, sheetName).TryGetRow(rowId, out var row))
        {
            if (colIndex == 0)
            {
                context.Builder.Append(row.ReadStringColumn(0));
                return true;
            }
            else
            {
                for (var i = 0; i < colIndex; i++)
                {
                    var text = row.ReadStringColumn(cols[i]);
                    if (!text.IsEmpty)
                    {
                        context.Builder.Append(text);
                        break;
                    }
                }
            }
        }

        return true;
    }

    private bool TryResolveLower(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(headContext, eStr))
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

    private bool TryResolveNoun(ClientLanguage language, in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        var eAmountVal = 1;
        var eCaseVal = 1;

        var enu = payload.GetEnumerator();

        if (!enu.MoveNext() || !enu.Current.TryGetString(out var eSheetNameStr))
            return false;

        var sheetName = this.Evaluate(eSheetNameStr, context.LocalParameters, context.Language).ExtractText();

        if (!enu.MoveNext() || !this.TryResolveInt(in context, enu.Current, out var eArticleTypeVal))
            return false;

        if (!enu.MoveNext() || !this.TryResolveUInt(in context, enu.Current, out var eRowIdVal))
            return false;

        uint colIndex = ushort.MaxValue;
        var flags = this.sheetRedirectResolver.Resolve(ref sheetName, ref eRowIdVal, ref colIndex);

        if (string.IsNullOrEmpty(sheetName))
            return false;

        // optional arguments
        if (enu.MoveNext())
        {
            if (!this.TryResolveInt(in context, enu.Current, out eAmountVal))
                return false;

            if (enu.MoveNext())
            {
                if (!this.TryResolveInt(in context, enu.Current, out eCaseVal))
                    return false;

                // For Chinese texts?
                /*
                if (enu.MoveNext())
                {
                    var eUnkInt5 = enu.Current;
                    if (!TryResolveInt(context,eUnkInt5, out eUnkInt5Val))
                        return false;
                }
                */
            }
        }

        context.Builder.Append(
            this.nounProcessor.ProcessNoun(new NounParams()
            {
                Language = language,
                SheetName = sheetName,
                RowId = eRowIdVal,
                Quantity = eAmountVal,
                ArticleType = eArticleTypeVal,
                GrammaticalCase = eCaseVal - 1,
                IsActionSheet = flags.HasFlag(SheetRedirectFlags.Action),
            }));

        return true;
    }

    private bool TryResolveLowerHead(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eStr))
            return false;

        var builder = SeStringBuilder.SharedPool.Get();

        try
        {
            var headContext = new SeStringContext(builder, context.LocalParameters, context.Language);

            if (!this.ResolveStringExpression(headContext, eStr))
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
                    context.Builder.Append(Encoding.UTF8.GetString(p.Body.Span).FirstCharToLower(context.CultureInfo));
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

    private bool TryResolveColorType(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColorType) ||
            !this.TryResolveUInt(in context, eColorType, out var eColorTypeVal))
            return false;

        if (eColorTypeVal == 0)
            context.Builder.PopColor();
        else if (this.dataManager.GetExcelSheet<UIColor>().TryGetRow(eColorTypeVal, out var row))
            context.Builder.PushColorBgra((row.Dark >> 8) | (row.Dark << 24));

        return true;
    }

    private bool TryResolveEdgeColorType(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eColorType) ||
            !this.TryResolveUInt(in context, eColorType, out var eColorTypeVal))
            return false;

        if (eColorTypeVal == 0)
            context.Builder.PopEdgeColor();
        else if (this.dataManager.GetExcelSheet<UIColor>().TryGetRow(eColorTypeVal, out var row))
            context.Builder.PushEdgeColorBgra((row.Dark >> 8) | (row.Dark << 24));

        return true;
    }

    private bool TryResolveDigit(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eValue, out var eTargetLength))
            return false;

        if (!this.TryResolveInt(in context, eValue, out var eValueVal))
            return false;

        if (!this.TryResolveInt(in context, eTargetLength, out var eTargetLengthVal))
            return false;

        context.Builder.Append(eValueVal.ToString(new string('0', eTargetLengthVal)));

        return true;
    }

    private bool TryResolveOrdinal(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eValue) || !this.TryResolveUInt(in context, eValue, out var eValueVal))
            return false;

        // TODO: Culture support?
        context.Builder.Append(
            $"{eValueVal}{(eValueVal % 10) switch
            {
                _ when eValueVal is >= 10 and <= 19 => "th",
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            }}");
        return true;
    }

    private bool TryResolveLevelPos(in SeStringContext context, in ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var eLevel) || !this.TryResolveUInt(in context, eLevel, out var eLevelVal))
            return false;

        if (!this.dataManager.GetExcelSheet<Level>(context.Language).TryGetRow(eLevelVal, out var level) ||
            !level.Map.IsValid)
            return false;

        if (!this.dataManager.GetExcelSheet<PlaceName>(context.Language).TryGetRow(
                level.Map.Value.PlaceName.RowId,
                out var placeName))
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

        ThreadSafety.AssertMainThread("Global parameters may only be used from the main thread.");

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

    private unsafe bool TryProduceGStrDefault(SeStringBuilder builder, ClientLanguage language, uint parameterIndex)
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
                builder.Append($"{p.IntValue:D}");
                return true;

            case TextParameterType.ReferencedUtf8String:
                this.EvaluateAndAppendTo(
                    builder,
                    p.ReferencedUtf8StringValue->Utf8String.AsSpan(),
                    null,
                    language);
                return false;

            case TextParameterType.String:
                this.EvaluateAndAppendTo(builder, p.StringValue.AsSpan(), null, language);
                return false;

            case TextParameterType.Uninitialized:
            default:
                return false;
        }
    }

    private unsafe bool TryResolveUInt(
        in SeStringContext context, in ReadOnlySeExpressionSpan expression, out uint value)
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
                    value = (uint)MacroDecoder.GetMacroTime()->tm_wday + 1;
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
            if (!this.TryResolveUInt(in context, operand1, out var paramIndex))
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
                    if (!this.TryResolveInt(in context, operand1, out var value1)
                        || !this.TryResolveInt(in context, operand2, out var value2))
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
                    if (this.TryResolveInt(in context, operand1, out value1) &&
                        this.TryResolveInt(in context, operand2, out value2))
                    {
                        if ((ExpressionType)exprType == ExpressionType.Equal)
                            value = value1 == value2 ? 1u : 0u;
                        else
                            value = value1 == value2 ? 0u : 1u;
                        return true;
                    }

                    if (operand1.TryGetString(out var strval1) && operand2.TryGetString(out var strval2))
                    {
                        var resolvedStr1 = this.EvaluateAndAppendTo(
                            SeStringBuilder.SharedPool.Get(),
                            strval1,
                            context.LocalParameters,
                            context.Language);
                        var resolvedStr2 = this.EvaluateAndAppendTo(
                            SeStringBuilder.SharedPool.Get(),
                            strval2,
                            context.LocalParameters,
                            context.Language);
                        var equals = resolvedStr1.GetViewAsSpan().SequenceEqual(resolvedStr2.GetViewAsSpan());
                        SeStringBuilder.SharedPool.Return(resolvedStr1);
                        SeStringBuilder.SharedPool.Return(resolvedStr2);

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

                return this.TryResolveUInt(in context, expr, out value);
            }

            return false;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryResolveInt(in SeStringContext context, in ReadOnlySeExpressionSpan expression, out int value)
    {
        if (this.TryResolveUInt(in context, expression, out var u32))
        {
            value = (int)u32;
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryResolveBool(in SeStringContext context, in ReadOnlySeExpressionSpan expression, out bool value)
    {
        if (this.TryResolveUInt(in context, expression, out var u32))
        {
            value = u32 != 0;
            return true;
        }

        value = false;
        return false;
    }

    private bool ResolveStringExpression(in SeStringContext context, in ReadOnlySeExpressionSpan expression)
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
            if (context.TryProducePlaceholder(context,exprType))
                return true;
        }
        */

        if (expression.TryGetParameterExpression(out var exprType, out var operand1))
        {
            if (!this.TryResolveUInt(in context, operand1, out var paramIndex))
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
                    return this.TryProduceGStrDefault(context.Builder, context.Language, paramIndex);

                default:
                    return false;
            }
        }

        // Handles UInt and Binary expressions
        if (!this.TryResolveUInt(in context, expression, out u32))
            return false;

        context.Builder.Append(((int)u32).ToString());
        return true;
    }

    private readonly record struct StringCacheKey<TK>(TK Kind, uint Id, ClientLanguage Language)
        where TK : struct, Enum;
}
