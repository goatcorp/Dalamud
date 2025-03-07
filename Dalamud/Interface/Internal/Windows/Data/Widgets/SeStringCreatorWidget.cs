using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using ImGuiNET;

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
        { MacroCode.ColorType, ["ColorType"] },
        { MacroCode.EdgeColorType, ["ColorType"] },
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
        { LinkMacroPayloadType.Quest, ["QuestId"] },
        { LinkMacroPayloadType.Achievement, ["AchievementId"] },
        { LinkMacroPayloadType.HowTo, ["HowToId"] },
        // PartyFinderNotification
        { LinkMacroPayloadType.Status, ["StatusId"] },
        { LinkMacroPayloadType.PartyFinder, ["ListingId", string.Empty, "WorldId"] },
        { LinkMacroPayloadType.AkatsukiNote, ["AkatsukiNoteId"] },
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
        new TextEntry(TextEntryType.String, "Test1 "),
        new TextEntry(TextEntryType.Macro, "<color(0xFF9000)>"),
        new TextEntry(TextEntryType.String, "Test2 "),
        new TextEntry(TextEntryType.Macro, "<color(0)>"),
        new TextEntry(TextEntryType.String, "Test3 "),
        new TextEntry(TextEntryType.Macro, "<color(stackcolor)>"),
        new TextEntry(TextEntryType.String, "Test 4 "),
        new TextEntry(TextEntryType.Macro, "<color(stackcolor)>"),
        new TextEntry(TextEntryType.String, "Test 5"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,1,1,Some Player)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,2,28,100,0,0,ClassJob)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,3,156,65561,65035,-696153,63,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,3,156,65561,65035,-696153,-63,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,4,39246,1,0,0,Phoenix Riser Suit)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,5,4)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,6,1031195)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,6,1031197)>"),
        // 7 formats a string??
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,8,190)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,8,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        // 9 writes a uint to PronounModule
        new TextEntry(TextEntryType.Fixed, "<fixed(200,10,3,0)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,10,3,1,Title,Description)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,11,12345,0,65536,0,Player Name)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Fixed, "<fixed(200,12,70058,0,0,0,The Ultimate Weapon)>"),
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(48,209)>"), // Mount
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(49,28)>"), // ClassJob
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(50,2957)>"), // PlaceName
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(51,4)>"), // Race
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(52,7)>"), // Tribe
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(64,13)>"), // Companion
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<fixed(60,21)>"), // MainCommand
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<ordinal(501)>"), // 501st
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<split(Hello World, ,1)>"), // Hello
        new TextEntry(TextEntryType.Macro, "<br>"),
        new TextEntry(TextEntryType.Macro, "<split(Hello World, ,2)>"), // World
    ];

    private SeStringParameter[]? localParameters = null;
    private ReadOnlySeString input;
    private ClientLanguage? language;

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
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        // Init
        if (this.language == null)
        {
            this.language = Service<DalamudConfiguration>.Get().EffectiveLanguage.ToClientLanguage();
            this.UpdateInputString();
        }

        this.DrawControls();
        ImGui.Spacing();
        this.DrawInputs();

        this.localParameters ??= this.GetLocalParameters(this.input.AsSpan(), []);

        var evaluated = Service<SeStringEvaluator>.Get().Evaluate(
            this.input.AsSpan(),
            this.localParameters,
            this.language);

        ImGui.SameLine();
        using var child = ImRaii.Child("Preview", new Vector2(ImGui.GetContentRegionAvail().X, -1));
        if (!child) return;

        this.DrawPreview(evaluated);

        if (this.localParameters!.Length != 0)
        {
            ImGui.Spacing();
            this.DrawParameters();
        }

        ImGui.Spacing();
        this.DrawPayloads(evaluated);
    }

    private unsafe void DrawControls()
    {
        if (ImGui.Button("Add entry"))
        {
            this.entries.Add(new(TextEntryType.String, string.Empty));
        }

        ImGui.SameLine();

        if (ImGui.Button("Print"))
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

        if (ImGui.Button("Print Evaluated"))
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

            RaptureLogModule.Instance()->PrintString(Service<SeStringEvaluator>.Get().Evaluate(sb.ToReadOnlySeString()));
        }

        if (this.entries.Count != 0)
        {
            ImGui.SameLine();

            if (ImGui.Button("Clear entries"))
            {
                this.entries.Clear();
                this.UpdateInputString();
            }
        }

        var raptureTextModule = RaptureTextModule.Instance();
        if (!raptureTextModule->MacroEncoder.EncoderError.IsEmpty)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(raptureTextModule->MacroEncoder.EncoderError.ToString()); // TODO: EncoderError doesn't clear
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language", this.language.ToString() ?? "Language..."))
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

    private unsafe void DrawInputs()
    {
        using var child = ImRaii.Child("Inputs", new Vector2(ImGui.GetContentRegionAvail().X / 2, -1));
        if (!child) return;

        using var table = ImRaii.Table("StringMakerTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
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
            if (ImGui.Combo($"##Type{i}", ref type, ["String", "Macro", "Fixed"], 3))
            {
                entry.Type = (TextEntryType)type;
                updateString |= true;
            }

            ImGui.TableNextColumn(); // Text
            var message = entry.Message;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText($"##{i}_Message", ref message, 255))
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

    private unsafe void UpdateInputString()
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
        this.localParameters = null;
    }

    private void DrawPreview(ReadOnlySeString evaluated)
    {
        using var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00);
        using var node = ImRaii.TreeNode("Preview", ImGuiTreeNodeFlags.DefaultOpen);
        nodeColor.Pop();
        if (!node) return;

        ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeight()));
        ImGui.SameLine(0, 0);
        ImGuiHelpers.SeStringWrapped(evaluated, new SeStringDrawParams()
        {
            ForceEdgeColor = true,
        });
    }

    private void DrawParameters()
    {
        using var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00);
        using var node = ImRaii.TreeNode("Parameters", ImGuiTreeNodeFlags.DefaultOpen);
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
        using (var node = ImRaii.TreeNode("Payloads", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            nodeColor.Pop();
            if (node) this.DrawSeString("payloads", this.input.AsSpan(), treeNodeFlags: ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
        }

        if (this.input.Equals(evaluated))
            return;

        using (var nodeColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FF00))
        using (var node = ImRaii.TreeNode("Payloads (Evaluated)", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
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

            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Tree", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(payload.Type == ReadOnlySePayloadType.Text ? "Text" : "ToString()");
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
        ImGui.TextUnformatted($"[{exprIdx}] " + (string.IsNullOrEmpty(expressionName) ? $"Expr {exprIdx}" : expressionName));

        ImGui.TableNextColumn();

        if (expr.Body.IsEmpty)
        {
            ImGui.TextUnformatted("(?)");
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
                    ImGui.TextUnformatted(name);
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
                ImGui.TextUnformatted(Enum.GetName(articleTypeEnumType, u32));
            }

            if (macroCode is MacroCode.Fixed && subType != null && fixedType != null && fixedType is 100 or 200 && subType == 5 && exprIdx == 2)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Play"))
                {
                    UIGlobals.PlayChatSoundEffect(u32 + 1);
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
                ImGui.TextUnformatted(nativeName);
                return;
            }

            ImGui.TextUnformatted($"?x{exprType:X02}");
            return;
        }

        if (expr.TryGetParameterExpression(out exprType, out var e1))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.TextUnformatted($"{nativeName}({e1.ToString()})");
                return;
            }

            throw new InvalidOperationException("All native names must be defined for unary expressions.");
        }

        if (expr.TryGetBinaryExpression(out exprType, out e1, out var e2))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.TextUnformatted($"{e1.ToString()} {nativeName} {e2.ToString()}");
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
        ImGui.TextUnformatted(sb.ToString());
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
            ImGui.TextUnformatted(tooltip);
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
