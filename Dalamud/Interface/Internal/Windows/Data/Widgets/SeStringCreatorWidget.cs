using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;

using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using LSeStringBuilder = Lumina.Text.SeStringBuilder;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to create SeStrings.
/// </summary>
internal class SeStringCreatorWidget : IDataWindowWidget
{
    private const LinkMacroPayloadType DalamudLinkType = (LinkMacroPayloadType)Payload.EmbeddedInfoType.DalamudLink - 1;

    private readonly Dictionary<MacroCode, string[]> expressionNames = new()
    {
        { MacroCode.SetResetTime, ["Hour", "WeekDay"] },
        { MacroCode.SetTime, ["Time"] },
        { MacroCode.If, ["Condition", "StatementTrue", "StatementFalse"] },
        { MacroCode.Switch, ["Condition"] },
        { MacroCode.PcName, ["EntityId"] },
        { MacroCode.IfPcGender, ["EntityId", "CaseMale", "CaseFemale"] },
        { MacroCode.IfPcName, ["EntityId", "CaseTrue", "CaseFalse"] },
        // { MacroCode.Josa, [] },
        // { MacroCode.Josaro, [] },
        { MacroCode.IfSelf, ["EntityId", "CaseTrue", "CaseFalse"] },
        // { MacroCode.NewLine, [] },
        { MacroCode.Wait, ["Seconds"] },
        { MacroCode.Icon, ["IconId"] },
        { MacroCode.Color, ["Color"] },
        { MacroCode.EdgeColor, ["Color"] },
        { MacroCode.ShadowColor, ["Color"] },
        // { MacroCode.SoftHyphen, [] },
        // { MacroCode.Key, [] },
        // { MacroCode.Scale, [] },
        { MacroCode.Bold, ["Enabled"] },
        { MacroCode.Italic, ["Enabled"] },
        // { MacroCode.Edge, [] },
        // { MacroCode.Shadow, [] },
        // { MacroCode.NonBreakingSpace, [] },
        { MacroCode.Icon2, ["IconId"] },
        // { MacroCode.Hyphen, [] },
        { MacroCode.Num, ["Value"] },
        { MacroCode.Hex, ["Value"] },
        { MacroCode.Kilo, ["Value", "Separator"] },
        { MacroCode.Byte, ["Value"] },
        { MacroCode.Sec, ["Time"] },
        { MacroCode.Time, ["Value"] },
        { MacroCode.Float, ["Value", "Radix", "Separator"] },
        { MacroCode.Link, ["Type"] },
        { MacroCode.Sheet, ["SheetName", "RowId", "ColumnIndex", "ColumnParam"] },
        { MacroCode.String, ["String"] },
        { MacroCode.Caps, ["String"] },
        { MacroCode.Head, ["String"] },
        { MacroCode.Split, ["String", "Separator"] },
        { MacroCode.HeadAll, ["String"] },
        // { MacroCode.Fixed, [] },
        { MacroCode.Lower, ["String"] },
        { MacroCode.JaNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.EnNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.DeNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.FrNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.ChNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.LowerHead, ["String"] },
        { MacroCode.SheetSub, ["SheetName", "RowId", "SubrowId", "ColumnIndex", "SecondarySheetName", "SecondarySheetColumnIndex"] },
        { MacroCode.ColorType, ["ColorType"] },
        { MacroCode.EdgeColorType, ["ColorType"] },
        { MacroCode.Ruby, ["StandardText", "RubyText"] },
        { MacroCode.Digit, ["Value", "TargetLength"] },
        { MacroCode.Ordinal, ["Value"] },
        { MacroCode.Sound, ["IsJingle", "SoundId"] },
        { MacroCode.LevelPos, ["LevelId"] },
    };

    private readonly Dictionary<LinkMacroPayloadType, string[]> linkExpressionNames = new()
    {
        { LinkMacroPayloadType.Character, ["Flags", "WorldId"] },
        { LinkMacroPayloadType.Item, ["ItemId", "Rarity"] },
        { LinkMacroPayloadType.MapPosition, ["TerritoryType/MapId", "RawX", "RawY"] },
        { LinkMacroPayloadType.Quest, ["RowId"] },
        { LinkMacroPayloadType.Achievement, ["RowId"] },
        { LinkMacroPayloadType.HowTo, ["RowId"] },
        // PartyFinderNotification
        { LinkMacroPayloadType.Status, ["StatusId"] },
        { LinkMacroPayloadType.PartyFinder, ["ListingId", string.Empty, "WorldId"] },
        { LinkMacroPayloadType.AkatsukiNote, ["RowId"] },
        { LinkMacroPayloadType.Description, ["RowId"] },
        { LinkMacroPayloadType.WKSPioneeringTrail, ["RowId", "SubrowId"] },
        { LinkMacroPayloadType.MKDLore, ["RowId"] },
        { DalamudLinkType, ["CommandId", "Extra1", "Extra2", "ExtraString"] },
    };

    private readonly Dictionary<uint, string[]> fixedExpressionNames = new()
    {
        { 1, ["Type0", "Type1", "WorldId"] },
        { 2, ["Type0", "Type1", "ClassJobId", "Level"] },
        { 3, ["Type0", "Type1", "TerritoryTypeId", "Instance & MapId", "RawX", "RawY", "RawZ", "PlaceNameIdOverride"] },
        { 4, ["Type0", "Type1", "ItemId", "Rarity", string.Empty, string.Empty, "Item Name"] },
        { 5, ["Type0", "Type1", "Sound Effect Id"] },
        { 6, ["Type0", "Type1", "ObjStrId"] },
        { 7, ["Type0", "Type1", "Text"] },
        { 8, ["Type0", "Type1", "Seconds"] },
        { 9, ["Type0", "Type1", string.Empty] },
        { 10, ["Type0", "Type1", "StatusId", "HasOverride", "NameOverride", "DescriptionOverride"] },
        { 11, ["Type0", "Type1", "ListingId", string.Empty, "WorldId", "CrossWorldFlag"] },
        { 12, ["Type0", "Type1", "QuestId", string.Empty, string.Empty, string.Empty, "QuestName"] },
    };

    private readonly List<TextEntry> entries = [
        new TextEntry(TextEntryType.String, "Welcome to "),
        new TextEntry(TextEntryType.Macro, "<colortype(17)>"),
        new TextEntry(TextEntryType.Macro, "<edgecolortype(19)>"),
        new TextEntry(TextEntryType.String, "Dalamud"),
        new TextEntry(TextEntryType.Macro, "<edgecolor(stackcolor)>"),
        new TextEntry(TextEntryType.Macro, "<color(stackcolor)>"),
        new TextEntry(TextEntryType.Macro, " <string(lstr1)>"),
    ];

    private SeStringParameter[]? localParameters = [Util.GetScmVersion()];
    private ReadOnlySeString input;
    private ClientLanguage? language;
    private Task? validImportSheetNamesTask;
    private int importSelectedSheetName;
    private int importRowId;
    private string[]? validImportSheetNames;
    private float inputsWidth;
    private float lastContentWidth;

    private enum TextEntryType
    {
        String,
        Macro,
        Fixed,
    }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = [];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "SeString Creator";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.language = Service<ClientState>.Get().ClientLanguage;
        this.UpdateInputString(false);
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var contentWidth = ImGui.GetContentRegionAvail().X;

        // split panels in the middle by default
        if (this.inputsWidth == 0)
        {
            this.inputsWidth = contentWidth / 2f;
        }

        // resize panels relative to the window size
        if (contentWidth != this.lastContentWidth)
        {
            var originalWidth = this.lastContentWidth != 0 ? this.lastContentWidth : contentWidth;
            this.inputsWidth = this.inputsWidth / originalWidth * contentWidth;
            this.lastContentWidth = contentWidth;
        }

        using var tabBar = ImRaii.TabBar("SeStringCreatorWidgetTabBar"u8);
        if (!tabBar) return;

        this.DrawCreatorTab(contentWidth);
        this.DrawGlobalParametersTab();
    }

    private void DrawCreatorTab(float contentWidth)
    {
        using var tab = ImRaii.TabItem("Creator"u8);
        if (!tab) return;

        this.DrawControls();
        ImGui.Spacing();
        this.DrawInputs();

        this.localParameters ??= this.GetLocalParameters(this.input.AsSpan(), []);

        var evaluated = Service<SeStringEvaluator>.Get().Evaluate(
            this.input.AsSpan(),
            this.localParameters,
            this.language);

        ImGui.SameLine(0, 0);

        ImGui.Button("###InputPanelResizer"u8, new Vector2(4, -1));
        if (ImGui.IsItemActive())
        {
            this.inputsWidth += ImGui.GetIO().MouseDelta.X;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                this.inputsWidth = contentWidth / 2f;
            }
        }

        ImGui.SameLine();

        using var child = ImRaii.Child("Preview"u8, new Vector2(ImGui.GetContentRegionAvail().X, -1));
        if (!child) return;

        if (this.localParameters!.Length != 0)
        {
            ImGui.Spacing();
            this.DrawParameters();
        }

        this.DrawPreview(evaluated);

        ImGui.Spacing();
        this.DrawPayloads(evaluated);
    }

    private unsafe void DrawGlobalParametersTab()
    {
        using var tab = ImRaii.TabItem("Global Parameters"u8);
        if (!tab) return;

        using var table = ImRaii.Table("GlobalParametersTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ValuePtr"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Description"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var deque = RaptureTextModule.Instance()->GlobalParameters;
        for (var i = 0u; i < deque.MySize; i++)
        {
            var item = deque[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Type
            ImGui.Text(item.Type.ToString());

            ImGui.TableNextColumn(); // ValuePtr
            WidgetUtil.DrawCopyableText($"0x{(nint)item.ValuePtr:X}");

            ImGui.TableNextColumn(); // Value
            switch (item.Type)
            {
                case TextParameterType.Integer:
                    WidgetUtil.DrawCopyableText($"0x{item.IntValue:X}");
                    ImGui.SameLine();
                    WidgetUtil.DrawCopyableText(item.IntValue.ToString());
                    break;

                case TextParameterType.ReferencedUtf8String:
                    if (item.ReferencedUtf8StringValue != null)
                        WidgetUtil.DrawCopyableText(new ReadOnlySeStringSpan(item.ReferencedUtf8StringValue->Utf8String).ToString());
                    else
                        ImGui.Text("null"u8);

                    break;

                case TextParameterType.String:
                    if (item.StringValue.Value != null)
                        WidgetUtil.DrawCopyableText(item.StringValue.ToString());
                    else
                        ImGui.Text("null"u8);
                    break;
            }

            ImGui.TableNextColumn();
            ImGui.Text(i switch
            {
                0 => "Player Name",
                1 => "Temp Entity 1: Name",
                2 => "Temp Entity 2: Name",
                3 => "Player Sex",
                4 => "Temp Entity 1: Sex",
                5 => "Temp Entity 2: Sex",
                6 => "Temp Entity 1: ObjStrId",
                7 => "Temp Entity 2: ObjStrId",
                10 => "Eorzea Time Hours",
                11 => "Eorzea Time Minutes",
                12 => "ColorSay",
                13 => "ColorShout",
                14 => "ColorTell",
                15 => "ColorParty",
                16 => "ColorAlliance",
                17 => "ColorLS1",
                18 => "ColorLS2",
                19 => "ColorLS3",
                20 => "ColorLS4",
                21 => "ColorLS5",
                22 => "ColorLS6",
                23 => "ColorLS7",
                24 => "ColorLS8",
                25 => "ColorFCompany",
                26 => "ColorPvPGroup",
                27 => "ColorPvPGroupAnnounce",
                28 => "ColorBeginner",
                29 => "ColorEmoteUser",
                30 => "ColorEmote",
                31 => "ColorYell",
                32 => "ColorFCAnnounce",
                33 => "ColorBeginnerAnnounce",
                34 => "ColorCWLS",
                35 => "ColorAttackSuccess",
                36 => "ColorAttackFailure",
                37 => "ColorAction",
                38 => "ColorItem",
                39 => "ColorCureGive",
                40 => "ColorBuffGive",
                41 => "ColorDebuffGive",
                42 => "ColorEcho",
                43 => "ColorSysMsg",
                51 => "Player Grand Company Rank (Maelstrom)",
                52 => "Player Grand Company Rank (Twin Adders)",
                53 => "Player Grand Company Rank (Immortal Flames)",
                54 => "Companion Name",
                55 => "Content Name",
                56 => "ColorSysBattle",
                57 => "ColorSysGathering",
                58 => "ColorSysErr",
                59 => "ColorNpcSay",
                60 => "ColorItemNotice",
                61 => "ColorGrowup",
                62 => "ColorLoot",
                63 => "ColorCraft",
                64 => "ColorGathering",
                65 => "Temp Entity 1: Name starts with Vowel",
                66 => "Temp Entity 2: Name starts with Vowel",
                67 => "Player ClassJobId",
                68 => "Player Level",
                69 => "Player StartTown",
                70 => "Player Race",
                71 => "Player Synced Level",
                73 => "Quest#66047: Has met Alphinaud and Alisaie",
                74 => "PlayStation Generation",
                75 => "Is Legacy Player",
                77 => "Client/Platform?",
                78 => "Player BirthMonth",
                79 => "PadMode",
                82 => "Datacenter Region",
                83 => "ColorCWLS2",
                84 => "ColorCWLS3",
                85 => "ColorCWLS4",
                86 => "ColorCWLS5",
                87 => "ColorCWLS6",
                88 => "ColorCWLS7",
                89 => "ColorCWLS8",
                91 => "Player Grand Company",
                92 => "TerritoryType Id",
                93 => "Is Soft Keyboard Enabled",
                94 => "LogSetRoleColor 1: LogColorRoleTank",
                95 => "LogSetRoleColor 2: LogColorRoleTank",
                96 => "LogSetRoleColor 1: LogColorRoleHealer",
                97 => "LogSetRoleColor 2: LogColorRoleHealer",
                98 => "LogSetRoleColor 1: LogColorRoleDPS",
                99 => "LogSetRoleColor 2: LogColorRoleDPS",
                100 => "LogSetRoleColor 1: LogColorOtherClass",
                101 => "LogSetRoleColor 2: LogColorOtherClass",
                102 => "Has Login Security Token",
                103 => "Is subscribed to PlayStation Plus",
                104 => "PadMouseMode",
                106 => "Preferred World Bonus Max Level",
                107 => "Occult Crescent Support Job Level",
                108 => "Deep Dungeon Id",
                _ => string.Empty,
            });
        }
    }

    private unsafe void DrawControls()
    {
        if (ImGui.Button("Add entry"u8))
        {
            this.entries.Add(new(TextEntryType.String, string.Empty));
        }

        ImGui.SameLine();

        if (ImGui.Button("Add from Sheet"u8))
        {
            ImGui.OpenPopup("AddFromSheetPopup"u8);
        }

        this.DrawAddFromSheetPopup();

        ImGui.SameLine();

        if (ImGui.Button("Print"u8))
        {
            var output = Utf8String.CreateEmpty();
            var temp = Utf8String.CreateEmpty();
            var temp2 = Utf8String.CreateEmpty();

            foreach (var entry in this.entries)
            {
                switch (entry.Type)
                {
                    case TextEntryType.String:
                        output->ConcatCStr(entry.Message);
                        break;

                    case TextEntryType.Macro:
                        temp->Clear();
                        RaptureTextModule.Instance()->MacroEncoder.EncodeString(temp, entry.Message);
                        output->Append(temp);
                        break;

                    case TextEntryType.Fixed:
                        temp->SetString(entry.Message);
                        temp2->Clear();

                        RaptureTextModule.Instance()->TextModule.ProcessMacroCode(temp2, temp->StringPtr);
                        var out1 = PronounModule.Instance()->ProcessString(temp2, true);
                        var out2 = PronounModule.Instance()->ProcessString(out1, false);

                        output->Append(out2);
                        break;
                }
            }

            RaptureLogModule.Instance()->PrintString(output->StringPtr);
            temp2->Dtor(true);
            temp->Dtor(true);
            output->Dtor(true);
        }

        ImGui.SameLine();

        if (ImGui.Button("Print Evaluated"u8))
        {
            var sb = new LSeStringBuilder();

            foreach (var entry in this.entries)
            {
                switch (entry.Type)
                {
                    case TextEntryType.String:
                        sb.Append(entry.Message);
                        break;

                    case TextEntryType.Macro:
                    case TextEntryType.Fixed:
                        sb.AppendMacroString(entry.Message);
                        break;
                }
            }

            var evaluated = Service<SeStringEvaluator>.Get().Evaluate(
                sb.ToReadOnlySeString(),
                this.localParameters,
                this.language);

            RaptureLogModule.Instance()->PrintString(evaluated);
        }

        if (this.entries.Count != 0)
        {
            ImGui.SameLine();

            if (ImGui.Button("Copy MacroString"u8))
            {
                var sb = new LSeStringBuilder();

                foreach (var entry in this.entries)
                {
                    switch (entry.Type)
                    {
                        case TextEntryType.String:
                            sb.Append(entry.Message);
                            break;

                        case TextEntryType.Macro:
                        case TextEntryType.Fixed:
                            sb.AppendMacroString(entry.Message);
                            break;
                    }
                }

                ImGui.SetClipboardText(sb.ToReadOnlySeString().ToMacroString());
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear entries"u8))
            {
                this.entries.Clear();
                this.UpdateInputString();
            }
        }

        var raptureTextModule = RaptureTextModule.Instance();
        if (!raptureTextModule->MacroEncoder.EncoderError.IsEmpty)
        {
            ImGui.SameLine();
            ImGui.Text(raptureTextModule->MacroEncoder.EncoderError.ToString()); // TODO: EncoderError doesn't clear
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language"u8, this.language.ToString() ?? "Language..."))
        {
            if (dropdown)
            {
                var values = Enum.GetValues<ClientLanguage>().OrderBy((ClientLanguage lang) => lang.ToString());
                foreach (var value in values)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == this.language))
                    {
                        this.language = value;
                        this.UpdateInputString();
                    }
                }
            }
        }
    }

    private void DrawAddFromSheetPopup()
    {
        using var popup = ImRaii.Popup("AddFromSheetPopup"u8);
        if (!popup) return;

        var dataManager = Service<DataManager>.Get();

        this.validImportSheetNamesTask ??= Task.Run(() =>
        {
            this.validImportSheetNames = dataManager.Excel.SheetNames.Where(sheetName =>
            {
                try
                {
                    var headerFile = dataManager.GameData.GetFile<ExcelHeaderFile>($"exd/{sheetName}.exh");
                    if (headerFile.Header.Variant != ExcelVariant.Default)
                        return false;

                    var sheet = dataManager.Excel.GetSheet<RawRow>(Language.English, sheetName);
                    return sheet.Columns.Any(col => col.Type == ExcelColumnDataType.String);
                }
                catch
                {
                    return false;
                }
            }).OrderBy(sheetName => sheetName, StringComparer.InvariantCulture).ToArray();
        });

        if (this.validImportSheetNames == null)
        {
            ImGui.Text("Loading sheets..."u8);
            return;
        }

        var sheetChanged = ImGui.Combo("Sheet Name", ref this.importSelectedSheetName, this.validImportSheetNames);

        try
        {
            var sheet = dataManager.Excel.GetSheet<RawRow>(this.language?.ToLumina() ?? Language.English, this.validImportSheetNames[this.importSelectedSheetName]);
            var minRowId = (int)sheet.FirstOrDefault().RowId;
            var maxRowId = (int)sheet.LastOrDefault().RowId;

            var rowIdChanged = ImGui.InputInt("RowId"u8, ref this.importRowId, 1, 10);

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.Text($"(Range: {minRowId} - {maxRowId})");

            if (sheetChanged || rowIdChanged)
            {
                if (sheetChanged || this.importRowId < minRowId)
                    this.importRowId = minRowId;

                if (this.importRowId > maxRowId)
                    this.importRowId = maxRowId;
            }

            if (!sheet.TryGetRow((uint)this.importRowId, out var row))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Row not found"u8);
                return;
            }

            ImGui.Text("Select string to add:"u8);

            using var table = ImRaii.Table("StringSelectionTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
            if (!table) return;

            ImGui.TableSetupColumn("Column"u8, ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            for (var i = 0; i < sheet.Columns.Count; i++)
            {
                var column = sheet.Columns[i];
                if (column.Type != ExcelColumnDataType.String)
                    continue;

                var value = row.ReadStringColumn(i);
                if (value.IsEmpty)
                    continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(i.ToString());

                ImGui.TableNextColumn();
                if (ImGui.Selectable($"{value.ToMacroString().Truncate(100)}###Column{i}"))
                {
                    foreach (var payload in value)
                    {
                        switch (payload.Type)
                        {
                            case ReadOnlySePayloadType.Text:
                                this.entries.Add(new(TextEntryType.String, Encoding.UTF8.GetString(payload.Body.Span)));
                                break;

                            case ReadOnlySePayloadType.Macro:
                                this.entries.Add(new(TextEntryType.Macro, payload.ToString()));
                                break;
                        }
                    }

                    this.UpdateInputString();
                    ImGui.CloseCurrentPopup();
                }
            }
        }
        catch (Exception e)
        {
            ImGui.Text(e.Message);
            return;
        }
    }

    private unsafe void DrawInputs()
    {
        using var child = ImRaii.Child("Inputs"u8, new Vector2(this.inputsWidth, -1));
        if (!child) return;

        using var table = ImRaii.Table("StringMakerTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var arrowUpButtonSize = this.GetIconButtonSize(FontAwesomeIcon.ArrowUp);
        var arrowDownButtonSize = this.GetIconButtonSize(FontAwesomeIcon.ArrowDown);
        var trashButtonSize = this.GetIconButtonSize(FontAwesomeIcon.Trash);
        var terminalButtonSize = this.GetIconButtonSize(FontAwesomeIcon.Terminal);

        var entryToRemove = -1;
        var entryToMoveUp = -1;
        var entryToMoveDown = -1;
        var updateString = false;

        for (var i = 0; i < this.entries.Count; i++)
        {
            var key = $"##Entry{i}";
            var entry = this.entries[i];

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Type
            var type = (int)entry.Type;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.Combo($"##Type{i}", ref type, ["String", "Macro", "Fixed"]))
            {
                entry.Type = (TextEntryType)type;
                updateString |= true;
            }

            ImGui.TableNextColumn(); // Text
            var message = entry.Message;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText($"##{i}_Message", ref message, 2048))
            {
                entry.Message = message;
                updateString |= true;
            }

            ImGui.TableNextColumn(); // Actions

            if (i > 0)
            {
                if (this.IconButton(key + "_Up", FontAwesomeIcon.ArrowUp, "Move up"))
                {
                    entryToMoveUp = i;
                }
            }
            else
            {
                ImGui.Dummy(arrowUpButtonSize);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

            if (i < this.entries.Count - 1)
            {
                if (this.IconButton(key + "_Down", FontAwesomeIcon.ArrowDown, "Move down"))
                {
                    entryToMoveDown = i;
                }
            }
            else
            {
                ImGui.Dummy(arrowDownButtonSize);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                if (this.IconButton(key + "_Delete", FontAwesomeIcon.Trash, "Delete"))
                {
                    entryToRemove = i;
                }
            }
            else
            {
                this.IconButton(
                    key + "_Delete",
                    FontAwesomeIcon.Trash,
                    "Delete with shift",
                    disabled: true);
            }
        }

        table.Dispose();

        if (entryToMoveUp != -1)
        {
            var removedItem = this.entries[entryToMoveUp];
            this.entries.RemoveAt(entryToMoveUp);
            this.entries.Insert(entryToMoveUp - 1, removedItem);
            updateString |= true;
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = this.entries[entryToMoveDown];
            this.entries.RemoveAt(entryToMoveDown);
            this.entries.Insert(entryToMoveDown + 1, removedItem);
            updateString |= true;
        }

        if (entryToRemove != -1)
        {
            this.entries.RemoveAt(entryToRemove);
            updateString |= true;
        }

        if (updateString)
        {
            this.UpdateInputString();
        }
    }

    private unsafe void UpdateInputString(bool resetLocalParameters = true)
    {
        var sb = new LSeStringBuilder();

        foreach (var entry in this.entries)
        {
            switch (entry.Type)
            {
                case TextEntryType.String:
                    sb.Append(entry.Message);
                    break;

                case TextEntryType.Macro:
                case TextEntryType.Fixed:
                    sb.AppendMacroString(entry.Message);
                    break;
            }
        }

        this.input = sb.ToReadOnlySeString();

        if (resetLocalParameters)
            this.localParameters = null;
    }

    private void DrawPreview(ReadOnlySeString str)
    {
        using var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00);
        using var node = ImRaii.TreeNode("Preview"u8, ImGuiTreeNodeFlags.DefaultOpen);
        nodeColor.Pop();
        if (!node) return;

        ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeight()));
        ImGui.SameLine(0, 0);
        ImGuiHelpers.SeStringWrapped(str);
    }

    private void DrawParameters()
    {
        using var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00);
        using var node = ImRaii.TreeNode("Parameters"u8, ImGuiTreeNodeFlags.DefaultOpen);
        nodeColor.Pop();
        if (!node) return;

        for (var i = 0; i < this.localParameters!.Length; i++)
        {
            if (this.localParameters[i].IsString)
            {
                var str = this.localParameters[i].StringValue.ExtractText();
                if (ImGui.InputText($"lstr({i + 1})", ref str, 255))
                {
                    this.localParameters[i] = new(str);
                }
            }
            else
            {
                var num = (int)this.localParameters[i].UIntValue;
                if (ImGui.InputInt($"lnum({i + 1})", ref num))
                {
                    this.localParameters[i] = new((uint)num);
                }
            }
        }
    }

    private void DrawPayloads(ReadOnlySeString evaluated)
    {
        using (var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00))
        using (var node = ImRaii.TreeNode("Payloads"u8, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            nodeColor.Pop();
            if (node) this.DrawSeString("payloads", this.input.AsSpan(), treeNodeFlags: ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
        }

        if (this.input.Equals(evaluated))
            return;

        using (var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00))
        using (var node = ImRaii.TreeNode("Payloads (Evaluated)"u8, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            nodeColor.Pop();
            if (node) this.DrawSeString("payloads-evaluated", evaluated.AsSpan(), treeNodeFlags: ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
        }
    }

    private void DrawSeString(string id, ReadOnlySeStringSpan rosss, bool asTreeNode = false, bool renderSeString = false, int depth = 0, ImGuiTreeNodeFlags treeNodeFlags = ImGuiTreeNodeFlags.None)
    {
        using var seStringId = ImRaii.PushId(id);

        if (rosss.PayloadCount == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        using var node = asTreeNode ? this.SeStringTreeNode(id, rosss) : null;
        if (asTreeNode && !node!) return;

        if (!asTreeNode && renderSeString)
        {
            ImGuiHelpers.SeStringWrapped(rosss, new()
            {
                ForceEdgeColor = true,
            });
        }

        var payloadIdx = -1;
        foreach (var payload in rosss)
        {
            payloadIdx++;
            using var payloadId = ImRaii.PushId(payloadIdx);

            var preview = payload.Type.ToString();
            if (payload.Type == ReadOnlySePayloadType.Macro)
                preview += $": {payload.MacroCode}";

            using var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
            using var payloadNode = ImRaii.TreeNode($"[{payloadIdx}] {preview}", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
            nodeColor.Pop();
            if (!payloadNode) continue;

            using var table = ImRaii.Table($"##Payload{payloadIdx}Table", 2);
            if (!table) return;

            ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Tree"u8, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(payload.Type == ReadOnlySePayloadType.Text ? "Text" : "ToString()");
            ImGui.TableNextColumn();
            var text = payload.ToString();
            WidgetUtil.DrawCopyableText($"\"{text}\"", text);

            if (payload.Type != ReadOnlySePayloadType.Macro)
                continue;

            if (payload.ExpressionCount > 0)
            {
                var exprIdx = 0;
                uint? subType = null;
                uint? fixedType = null;

                if (payload.MacroCode == MacroCode.Link && payload.TryGetExpression(out var linkExpr1) && linkExpr1.TryGetUInt(out var linkExpr1Val))
                {
                    subType = linkExpr1Val;
                }
                else if (payload.MacroCode == MacroCode.Fixed && payload.TryGetExpression(out var fixedTypeExpr, out var linkExpr2) && fixedTypeExpr.TryGetUInt(out var fixedTypeVal) && linkExpr2.TryGetUInt(out var linkExpr2Val))
                {
                    subType = linkExpr2Val;
                    fixedType = fixedTypeVal;
                }

                foreach (var expr in payload)
                {
                    using var exprId = ImRaii.PushId(exprIdx);

                    this.DrawExpression(payload.MacroCode, subType, fixedType, exprIdx++, expr);
                }
            }
        }
    }

    private unsafe void DrawExpression(MacroCode macroCode, uint? subType, uint? fixedType, int exprIdx, ReadOnlySeExpressionSpan expr)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var expressionName = this.GetExpressionName(macroCode, subType, exprIdx, expr);
        ImGui.Text($"[{exprIdx}] " + (string.IsNullOrEmpty(expressionName) ? $"Expr {exprIdx}" : expressionName));

        ImGui.TableNextColumn();

        if (expr.Body.IsEmpty)
        {
            ImGui.Text("(?)"u8);
            return;
        }

        if (expr.TryGetUInt(out var u32))
        {
            if (macroCode is MacroCode.Icon or MacroCode.Icon2 && exprIdx == 0)
            {
                var iconId = u32;

                if (macroCode == MacroCode.Icon2)
                {
                    var iconMapping = RaptureAtkModule.Instance()->AtkFontManager.Icon2RemapTable;
                    for (var i = 0; i < 30; i++)
                    {
                        if (iconMapping[i].IconId == iconId)
                        {
                            iconId = iconMapping[i].RemappedIconId;
                            break;
                        }
                    }
                }

                var builder = LSeStringBuilder.SharedPool.Get();
                builder.AppendIcon(iconId);
                ImGuiHelpers.SeStringWrapped(builder.ToArray());
                LSeStringBuilder.SharedPool.Return(builder);

                ImGui.SameLine();
            }

            WidgetUtil.DrawCopyableText(u32.ToString());
            ImGui.SameLine();
            WidgetUtil.DrawCopyableText($"0x{u32:X}");

            if (macroCode == MacroCode.Link && exprIdx == 0)
            {
                var name = subType != null && (LinkMacroPayloadType)subType == DalamudLinkType
                    ? "Dalamud"
                    : Enum.GetName((LinkMacroPayloadType)u32);

                if (!string.IsNullOrEmpty(name))
                {
                    ImGui.SameLine();
                    ImGui.Text(name);
                }
            }

            if (macroCode is MacroCode.JaNoun or MacroCode.EnNoun or MacroCode.DeNoun or MacroCode.FrNoun && exprIdx == 1)
            {
                var language = macroCode switch
                {
                    MacroCode.JaNoun => ClientLanguage.Japanese,
                    MacroCode.DeNoun => ClientLanguage.German,
                    MacroCode.FrNoun => ClientLanguage.French,
                    _ => ClientLanguage.English,
                };
                var articleTypeEnumType = language switch
                {
                    ClientLanguage.Japanese => typeof(JapaneseArticleType),
                    ClientLanguage.German => typeof(GermanArticleType),
                    ClientLanguage.French => typeof(FrenchArticleType),
                    _ => typeof(EnglishArticleType),
                };
                ImGui.SameLine();
                ImGui.Text(Enum.GetName(articleTypeEnumType, u32));
            }

            if (macroCode is MacroCode.DeNoun && exprIdx == 4 && u32 is >= 0 and <= 4)
            {
                ImGui.SameLine();
                ImGui.Text(NounProcessorWidget.GermanCases[u32]);
            }

            if (macroCode is MacroCode.Fixed && subType != null && fixedType != null && fixedType is 100 or 200 && subType == 5 && exprIdx == 2)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Play"u8))
                {
                    UIGlobals.PlayChatSoundEffect(u32 + 1);
                }
            }

            if (macroCode is MacroCode.Link && subType != null && exprIdx == 1)
            {
                var dataManager = Service<DataManager>.Get();

                switch ((LinkMacroPayloadType)subType)
                {
                    case LinkMacroPayloadType.Item when dataManager.GetExcelSheet<Item>(this.language).TryGetRow(u32, out var itemRow):
                        ImGui.SameLine();
                        ImGui.Text(itemRow.Name.ExtractText());
                        break;

                    case LinkMacroPayloadType.Quest when dataManager.GetExcelSheet<Quest>(this.language).TryGetRow(u32, out var questRow):
                        ImGui.SameLine();
                        ImGui.Text(questRow.Name.ExtractText());
                        break;

                    case LinkMacroPayloadType.Achievement when dataManager.GetExcelSheet<Achievement>(this.language).TryGetRow(u32, out var achievementRow):
                        ImGui.SameLine();
                        ImGui.Text(achievementRow.Name.ExtractText());
                        break;

                    case LinkMacroPayloadType.HowTo when dataManager.GetExcelSheet<HowTo>(this.language).TryGetRow(u32, out var howToRow):
                        ImGui.SameLine();
                        ImGui.Text(howToRow.Name.ExtractText());
                        break;

                    case LinkMacroPayloadType.Status when dataManager.GetExcelSheet<Status>(this.language).TryGetRow(u32, out var statusRow):
                        ImGui.SameLine();
                        ImGui.Text(statusRow.Name.ExtractText());
                        break;

                    case LinkMacroPayloadType.AkatsukiNote when
                        dataManager.GetSubrowExcelSheet<AkatsukiNote>(this.language).TryGetSubrow(u32, 0, out var akatsukiNoteRow) &&
                        akatsukiNoteRow.ListName.ValueNullable is { } akatsukiNoteStringRow:
                        ImGui.SameLine();
                        ImGui.Text(akatsukiNoteStringRow.Text.ExtractText());
                        break;
                }
            }

            return;
        }

        if (expr.TryGetString(out var s))
        {
            this.DrawSeString("Preview", s, treeNodeFlags: ImGuiTreeNodeFlags.DefaultOpen);
            return;
        }

        if (expr.TryGetPlaceholderExpression(out var exprType))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.Text(nativeName);
                return;
            }

            ImGui.Text($"?x{exprType:X02}");
            return;
        }

        if (expr.TryGetParameterExpression(out exprType, out var e1))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.Text($"{nativeName}({e1.ToString()})");
                return;
            }

            throw new InvalidOperationException("All native names must be defined for unary expressions.");
        }

        if (expr.TryGetBinaryExpression(out exprType, out e1, out var e2))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.Text($"{e1.ToString()} {nativeName} {e2.ToString()}");
                return;
            }

            throw new InvalidOperationException("All native names must be defined for binary expressions.");
        }

        var sb = new StringBuilder();
        sb.EnsureCapacity(1 + 3 * expr.Body.Length);
        sb.Append($"({expr.Body[0]:X02}");
        for (var i = 1; i < expr.Body.Length; i++)
            sb.Append($" {expr.Body[i]:X02}");
        sb.Append(')');
        ImGui.Text(sb.ToString());
    }

    private string GetExpressionName(MacroCode macroCode, uint? subType, int idx, ReadOnlySeExpressionSpan expr)
    {
        if (this.expressionNames.TryGetValue(macroCode, out var names) && idx < names.Length)
            return names[idx];

        if (macroCode == MacroCode.Switch)
            return $"Case {idx - 1}";

        if (macroCode == MacroCode.Link && subType != null && this.linkExpressionNames.TryGetValue((LinkMacroPayloadType)subType, out var linkNames) && idx - 1 < linkNames.Length)
            return linkNames[idx - 1];

        if (macroCode == MacroCode.Fixed && subType != null && this.fixedExpressionNames.TryGetValue((uint)subType, out var fixedNames) && idx < fixedNames.Length)
            return fixedNames[idx];

        if (macroCode == MacroCode.Link && idx == 4)
            return "Copy String";

        return string.Empty;
    }

    private SeStringParameter[] GetLocalParameters(ReadOnlySeStringSpan rosss, Dictionary<uint, SeStringParameter>? parameters)
    {
        parameters ??= [];

        void ProcessString(ReadOnlySeStringSpan rosss)
        {
            foreach (var payload in rosss)
            {
                foreach (var expression in payload)
                {
                    ProcessExpression(expression);
                }
            }
        }

        void ProcessExpression(ReadOnlySeExpressionSpan expression)
        {
            if (expression.TryGetString(out var exprString))
            {
                ProcessString(exprString);
                return;
            }

            if (expression.TryGetBinaryExpression(out var expressionType, out var operand1, out var operand2))
            {
                ProcessExpression(operand1);
                ProcessExpression(operand2);
                return;
            }

            if (expression.TryGetParameterExpression(out expressionType, out var operand))
            {
                if (!operand.TryGetUInt(out var index))
                    return;

                if (parameters.ContainsKey(index))
                    return;

                if (expressionType == (int)ExpressionType.LocalNumber)
                {
                    parameters[index] = new SeStringParameter(0);
                    return;
                }
                else if (expressionType == (int)ExpressionType.LocalString)
                {
                    parameters[index] = new SeStringParameter(string.Empty);
                    return;
                }
            }
        }

        ProcessString(rosss);

        if (parameters.Count > 0)
        {
            var last = parameters.OrderBy(x => x.Key).Last();

            if (parameters.Count != last.Key)
            {
                // fill missing local parameter slots, so we can go off the array index in SeStringContext

                for (var i = 1u; i <= last.Key; i++)
                {
                    if (!parameters.ContainsKey(i))
                        parameters[i] = new SeStringParameter(0);
                }
            }
        }

        return parameters.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
    }

    private ImRaii.IEndObject SeStringTreeNode(string id, ReadOnlySeStringSpan previewText, uint color = 0xFF00FFFF, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, color);
        var node = ImRaii.TreeNode("##" + id, flags);
        ImGui.SameLine();
        ImGuiHelpers.SeStringWrapped(previewText, new()
        {
            ForceEdgeColor = true,
            WrapWidth = 9999,
        });
        return node;
    }

    private bool IconButton(string key, FontAwesomeIcon icon, string tooltip, Vector2 size = default, bool disabled = false, bool active = false)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        if (!key.StartsWith("##")) key = "##" + key;

        var disposables = new List<IDisposable>();

        if (disabled)
        {
            disposables.Add(ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]));
            disposables.Add(ImRaii.PushColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]));
            disposables.Add(ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]));
        }
        else if (active)
        {
            disposables.Add(ImRaii.PushColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]));
        }

        var pressed = ImGui.Button(icon.ToIconString() + key, size);

        foreach (var disposable in disposables)
            disposable.Dispose();

        iconFont?.Dispose();

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(tooltip);
            ImGui.EndTooltip();
        }

        return pressed;
    }

    private Vector2 GetIconButtonSize(FontAwesomeIcon icon)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        return ImGui.CalcTextSize(icon.ToIconString()) + ImGui.GetStyle().FramePadding * 2;
    }

    private class TextEntry(TextEntryType type, string text)
    {
        public string Message { get; set; } = text;

        public TextEntryType Type { get; set; } = type;
    }
}
