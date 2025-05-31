using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.ComponentType;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A tree for an <see cref="AtkComponentNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe class ComponentNodeTree : ResNodeTree
{
    private readonly ComponentType componentType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal ComponentNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
        this.NodeType = 0;
        this.componentType = ((AtkUldComponentInfo*)this.UldManager->Objects)->ComponentType;
    }

    private AtkComponentBase* Component => this.CompNode->Component;

    private AtkComponentNode* CompNode => (AtkComponentNode*)this.Node;

    private AtkUldManager* UldManager => &this.Component->UldManager;

    private int? ComponentFieldOffset { get; set; }

    /// <inheritdoc/>
    private protected override string GetHeaderText()
    {
        var childCount = (int)this.UldManager->NodeListCount;
        return $"{this.componentType} Component Node{(childCount > 0 ? $" [+{childCount}]" : string.Empty)}";
    }

    /// <inheritdoc/>
    private protected override void PrintNodeObject()
    {
        base.PrintNodeObject();
        this.PrintComponentObject();
        ImGui.SameLine();
        ImGui.NewLine();
        this.PrintComponentDataObject();
        ImGui.SameLine();
        ImGui.NewLine();
    }

    /// <inheritdoc/>
    private protected override void PrintChildNodes()
    {
        base.PrintChildNodes();
        var count = this.UldManager->NodeListCount;
        PrintNodeListAsTree(this.UldManager->NodeList, count, $"Node List [{count}]:", this.AddonTree, new(0f, 0.5f, 0.8f, 1f));
    }

    /// <inheritdoc/>
    private protected override void PrintFieldLabels()
    {
        this.PrintFieldLabel((nint)this.Node, new(0, 0.85F, 1, 1), this.NodeFieldOffset);
        this.PrintFieldLabel((nint)this.Component, new(0f, 0.5f, 0.8f, 1f), this.ComponentFieldOffset);
    }

    /// <inheritdoc/>
    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false)
    {
        if (this.Component == null)
        {
            return;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (this.componentType)
        {
            case TextInput:
                var textInputComponent = (AtkComponentTextInput*)this.Component;
                ImGui.TextUnformatted(
                    $"InputBase Text1: {Marshal.PtrToStringAnsi(new(textInputComponent->AtkComponentInputBase.UnkText1.StringPtr))}");
                ImGui.TextUnformatted(
                    $"InputBase Text2: {Marshal.PtrToStringAnsi(new(textInputComponent->AtkComponentInputBase.UnkText2.StringPtr))}");
                ImGui.TextUnformatted(
                    $"Text1: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText01.StringPtr))}");
                ImGui.TextUnformatted(
                    $"Text2: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText02.StringPtr))}");
                ImGui.TextUnformatted(
                    $"AvailableLines: {Marshal.PtrToStringAnsi(new(textInputComponent->AvailableLines.StringPtr))}");
                ImGui.TextUnformatted(
                    $"HighlightedAutoTranslateOptionColorPrefix: {Marshal.PtrToStringAnsi(new(textInputComponent->HighlightedAutoTranslateOptionColorPrefix.StringPtr))}");
                ImGui.TextUnformatted(
                    $"HighlightedAutoTranslateOptionColorSuffix: {Marshal.PtrToStringAnsi(new(textInputComponent->HighlightedAutoTranslateOptionColorSuffix.StringPtr))}");
                break;
            case List:
            case TreeList:
                var l = (AtkComponentList*)this.Component;
                if (ImGui.SmallButton("Inc.Selected"))
                {
                    l->SelectedItemIndex++;
                }

                break;
        }
    }

    /// <inheritdoc/>
    private protected override void GetFieldOffset()
    {
        var nodeFound = false;
        var componentFound = false;
        for (var i = 0; i < this.AddonTree.AddonSize; i += 0x8)
        {
            var readPtr = Marshal.ReadIntPtr(this.AddonTree.InitialPtr + i);

            if (readPtr == (nint)this.Node)
            {
                this.NodeFieldOffset = i;
                nodeFound = true;
            }

            if (readPtr == (nint)this.Component)
            {
                this.ComponentFieldOffset = i;
                componentFound = true;
            }

            if (nodeFound && componentFound)
            {
                break;
            }
        }
    }

    private void PrintComponentObject()
    {
        PrintFieldValuePair("Component", $"{(nint)this.Component:X}");

        ImGui.SameLine();

        switch (this.componentType)
        {
            case Button:
                ShowStruct((AtkComponentButton*)this.Component);
                break;
            case Slider:
                ShowStruct((AtkComponentSlider*)this.Component);
                break;
            case Window:
                ShowStruct((AtkComponentWindow*)this.Component);
                break;
            case CheckBox:
                ShowStruct((AtkComponentCheckBox*)this.Component);
                break;
            case GaugeBar:
                ShowStruct((AtkComponentGaugeBar*)this.Component);
                break;
            case RadioButton:
                ShowStruct((AtkComponentRadioButton*)this.Component);
                break;
            case TextInput:
                ShowStruct((AtkComponentTextInput*)this.Component);
                break;
            case Icon:
                ShowStruct((AtkComponentIcon*)this.Component);
                break;
            case NumericInput:
                ShowStruct((AtkComponentNumericInput*)this.Component);
                break;
            case List:
                ShowStruct((AtkComponentList*)this.Component);
                break;
            case TreeList:
                ShowStruct((AtkComponentTreeList*)this.Component);
                break;
            case DropDownList:
                ShowStruct((AtkComponentDropDownList*)this.Component);
                break;
            case ScrollBar:
                ShowStruct((AtkComponentScrollBar*)this.Component);
                break;
            case ListItemRenderer:
                ShowStruct((AtkComponentListItemRenderer*)this.Component);
                break;
            case IconText:
                ShowStruct((AtkComponentIconText*)this.Component);
                break;
            case ComponentType.DragDrop:
                ShowStruct((AtkComponentDragDrop*)this.Component);
                break;
            case GuildLeveCard:
                ShowStruct((AtkComponentGuildLeveCard*)this.Component);
                break;
            case TextNineGrid:
                ShowStruct((AtkComponentTextNineGrid*)this.Component);
                break;
            case JournalCanvas:
                ShowStruct((AtkComponentJournalCanvas*)this.Component);
                break;
            case HoldButton:
                ShowStruct((AtkComponentHoldButton*)this.Component);
                break;
            case Portrait:
                ShowStruct((AtkComponentPortrait*)this.Component);
                break;
            default:
                ShowStruct(this.Component);
                break;
        }
    }

    private void PrintComponentDataObject()
    {
        var componentData = this.Component->UldManager.ComponentData;
        PrintFieldValuePair("Data", $"{(nint)componentData:X}");

        if (componentData != null)
        {
            ImGui.SameLine();
            switch (this.componentType)
            {
                case Base:
                    ShowStruct(componentData);
                    break;
                case Button:
                    ShowStruct((AtkUldComponentDataButton*)componentData);
                    break;
                case Window:
                    ShowStruct((AtkUldComponentDataWindow*)componentData);
                    break;
                case CheckBox:
                    ShowStruct((AtkUldComponentDataCheckBox*)componentData);
                    break;
                case RadioButton:
                    ShowStruct((AtkUldComponentDataRadioButton*)componentData);
                    break;
                case GaugeBar:
                    ShowStruct((AtkUldComponentDataGaugeBar*)componentData);
                    break;
                case Slider:
                    ShowStruct((AtkUldComponentDataSlider*)componentData);
                    break;
                case TextInput:
                    ShowStruct((AtkUldComponentDataTextInput*)componentData);
                    break;
                case NumericInput:
                    ShowStruct((AtkUldComponentDataNumericInput*)componentData);
                    break;
                case List:
                    ShowStruct((AtkUldComponentDataList*)componentData);
                    break;
                case DropDownList:
                    ShowStruct((AtkUldComponentDataDropDownList*)componentData);
                    break;
                case Tab:
                    ShowStruct((AtkUldComponentDataTab*)componentData);
                    break;
                case TreeList:
                    ShowStruct((AtkUldComponentDataTreeList*)componentData);
                    break;
                case ScrollBar:
                    ShowStruct((AtkUldComponentDataScrollBar*)componentData);
                    break;
                case ListItemRenderer:
                    ShowStruct((AtkUldComponentDataListItemRenderer*)componentData);
                    break;
                case Icon:
                    ShowStruct((AtkUldComponentDataIcon*)componentData);
                    break;
                case IconText:
                    ShowStruct((AtkUldComponentDataIconText*)componentData);
                    break;
                case ComponentType.DragDrop:
                    ShowStruct((AtkUldComponentDataDragDrop*)componentData);
                    break;
                case GuildLeveCard:
                    ShowStruct((AtkUldComponentDataGuildLeveCard*)componentData);
                    break;
                case TextNineGrid:
                    ShowStruct((AtkUldComponentDataTextNineGrid*)componentData);
                    break;
                case JournalCanvas:
                    ShowStruct((AtkUldComponentDataJournalCanvas*)componentData);
                    break;
                case Multipurpose:
                    ShowStruct((AtkUldComponentDataMultipurpose*)componentData);
                    break;
                case Map:
                    ShowStruct((AtkUldComponentDataMap*)componentData);
                    break;
                case Preview:
                    ShowStruct((AtkUldComponentDataPreview*)componentData);
                    break;
                case HoldButton:
                    ShowStruct((AtkUldComponentDataHoldButton*)componentData);
                    break;
                case Portrait:
                    ShowStruct((AtkUldComponentDataPortrait*)componentData);
                    break;
                default:
                    ShowStruct(componentData);
                    break;
            }
        }
    }
}
