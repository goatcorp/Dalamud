using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text.ReadOnly;

// Customised version of https://github.com/aers/FFXIVUIDebug

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class displays a debug window to inspect native addons.
/// </summary>
internal unsafe class UiDebug
{
    private const int UnitListCount = 18;

    private readonly bool[] selectedInList = new bool[UnitListCount];
    private readonly string[] listNames = new string[UnitListCount]
    {
        "Depth Layer 1",
        "Depth Layer 2",
        "Depth Layer 3",
        "Depth Layer 4",
        "Depth Layer 5",
        "Depth Layer 6",
        "Depth Layer 7",
        "Depth Layer 8",
        "Depth Layer 9",
        "Depth Layer 10",
        "Depth Layer 11",
        "Depth Layer 12",
        "Depth Layer 13",
        "Loaded Units",
        "Focused Units",
        "Units 16",
        "Units 17",
        "Units 18",
    };

    private bool doingSearch;
    private string searchInput = string.Empty;
    private AtkUnitBase* selectedUnitBase = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiDebug"/> class.
    /// </summary>
    public UiDebug()
    {
    }

    /// <summary>
    /// Renders this window.
    /// </summary>
    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 2));
        ImGui.BeginChild("st_uiDebug_unitBaseSelect", new Vector2(250, -1), true);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("###atkUnitBaseSearch", "Search", ref this.searchInput, 0x20);

        this.DrawUnitBaseList();
        ImGui.EndChild();
        if (this.selectedUnitBase != null)
        {
            ImGui.SameLine();
            ImGui.BeginChild("st_uiDebug_selectedUnitBase", new Vector2(-1, -1), true);
            this.DrawUnitBase(this.selectedUnitBase);
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
    }

    private void DrawUnitBase(AtkUnitBase* atkUnitBase)
    {
        var isVisible = atkUnitBase->IsVisible;
        var addonName = atkUnitBase->NameString;
        var agent = Service<GameGui>.Get().FindAgentInterface(atkUnitBase);

        ImGui.Text($"{addonName}");
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : 0xFF0000FF);
        ImGui.Text(isVisible ? "Visible" : "Not Visible");
        ImGui.PopStyleColor();

        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - 25);
        if (ImGui.SmallButton("V"))
        {
            atkUnitBase->IsVisible = !atkUnitBase->IsVisible;
        }

        ImGui.Separator();
        ImGuiHelpers.ClickToCopyText($"Address: {(ulong)atkUnitBase:X}", $"{(ulong)atkUnitBase:X}");
        ImGuiHelpers.ClickToCopyText($"Agent: {(ulong)agent:X}", $"{(ulong)agent:X}");
        ImGui.Separator();

        ImGui.Text($"Position: [ {atkUnitBase->X} , {atkUnitBase->Y} ]");
        ImGui.Text($"Scale: {atkUnitBase->Scale * 100}%%");
        ImGui.Text($"Widget Count {atkUnitBase->UldManager.ObjectCount}");

        ImGui.Separator();

        object addonObj = *atkUnitBase;

        Util.ShowStruct(addonObj, (ulong)atkUnitBase);

        ImGui.Dummy(new Vector2(25 * ImGui.GetIO().FontGlobalScale));
        ImGui.Separator();
        if (atkUnitBase->RootNode != null)
            this.PrintNode(atkUnitBase->RootNode);

        if (atkUnitBase->UldManager.NodeListCount > 0)
        {
            ImGui.Dummy(new Vector2(25 * ImGui.GetIO().FontGlobalScale));
            ImGui.Separator();
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
            if (ImGui.TreeNode($"Node List##{(ulong)atkUnitBase:X}"))
            {
                ImGui.PopStyleColor();

                for (var j = 0; j < atkUnitBase->UldManager.NodeListCount; j++)
                {
                    this.PrintNode(atkUnitBase->UldManager.NodeList[j], false, $"[{j}] ");
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }
    }

    private void PrintNode(AtkResNode* node, bool printSiblings = true, string treePrefix = "")
    {
        if (node == null)
            return;

        if ((int)node->Type < 1000)
            this.PrintSimpleNode(node, treePrefix);
        else
            this.PrintComponentNode(node, treePrefix);

        if (printSiblings)
        {
            var prevNode = node;
            while ((prevNode = prevNode->PrevSiblingNode) != null)
                this.PrintNode(prevNode, false, "prev ");

            var nextNode = node;
            while ((nextNode = nextNode->NextSiblingNode) != null)
                this.PrintNode(nextNode, false, "next ");
        }
    }

    private void PrintSimpleNode(AtkResNode* node, string treePrefix)
    {
        var popped = false;
        var isVisible = node->NodeFlags.HasFlag(NodeFlags.Visible);

        if (isVisible)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

        if (ImGui.TreeNode($"{treePrefix}{node->Type} Node (ptr = {(long)node:X})###{(long)node}"))
        {
            if (ImGui.IsItemHovered())
                this.DrawOutline(node);

            if (isVisible)
            {
                ImGui.PopStyleColor();
                popped = true;
            }

            ImGui.Text("Node: ");
            ImGui.SameLine();
            ImGuiHelpers.ClickToCopyText($"{(ulong)node:X}");
            ImGui.SameLine();
            switch (node->Type)
            {
                case NodeType.Text: Util.ShowStruct(*(AtkTextNode*)node, (ulong)node); break;
                case NodeType.Image: Util.ShowStruct(*(AtkImageNode*)node, (ulong)node); break;
                case NodeType.Collision: Util.ShowStruct(*(AtkCollisionNode*)node, (ulong)node); break;
                case NodeType.NineGrid: Util.ShowStruct(*(AtkNineGridNode*)node, (ulong)node); break;
                case NodeType.ClippingMask: Util.ShowStruct(*(AtkClippingMaskNode*)node, (ulong)node); break;
                case NodeType.Counter: Util.ShowStruct(*(AtkCounterNode*)node, (ulong)node); break;
                default: Util.ShowStruct(*node, (ulong)node); break;
            }

            this.PrintResNode(node);

            if (node->ChildNode != null)
                this.PrintNode(node->ChildNode);

            switch (node->Type)
            {
                case NodeType.Text:
                    var textNode = (AtkTextNode*)node;
                    ImGui.Text("text: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textNode->NodeText);

                    ImGui.InputText($"Replace Text##{(ulong)textNode:X}", textNode->NodeText.StringPtr, (uint)textNode->NodeText.BufSize);

                    ImGui.SameLine();
                    if (ImGui.Button($"Encode##{(ulong)textNode:X}"))
                    {
                        using var tmp = new Utf8String();
                        RaptureTextModule.Instance()->MacroEncoder.EncodeString(&tmp, textNode->NodeText.StringPtr);
                        textNode->NodeText.Copy(&tmp);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"Decode##{(ulong)textNode:X}"))
                        textNode->NodeText.SetString(textNode->NodeText.StringPtr.AsReadOnlySeStringSpan().ToString());

                    ImGui.Text($"AlignmentType: {(AlignmentType)textNode->AlignmentFontType}  FontSize: {textNode->FontSize}");
                    int b = textNode->AlignmentFontType;
                    if (ImGui.InputInt($"###setAlignment{(ulong)textNode:X}", ref b, 1))
                    {
                        while (b > byte.MaxValue) b -= byte.MaxValue;
                        while (b < byte.MinValue) b += byte.MaxValue;
                        textNode->AlignmentFontType = (byte)b;
                        textNode->AtkResNode.DrawFlags |= 0x1;
                    }

                    ImGui.Text($"Color: #{textNode->TextColor.R:X2}{textNode->TextColor.G:X2}{textNode->TextColor.B:X2}{textNode->TextColor.A:X2}");
                    ImGui.SameLine();
                    ImGui.Text($"EdgeColor: #{textNode->EdgeColor.R:X2}{textNode->EdgeColor.G:X2}{textNode->EdgeColor.B:X2}{textNode->EdgeColor.A:X2}");
                    ImGui.SameLine();
                    ImGui.Text($"BGColor: #{textNode->BackgroundColor.R:X2}{textNode->BackgroundColor.G:X2}{textNode->BackgroundColor.B:X2}{textNode->BackgroundColor.A:X2}");

                    ImGui.Text($"TextFlags: {textNode->TextFlags}");
                    ImGui.SameLine();
                    ImGui.Text($"TextFlags2: {textNode->TextFlags2}");

                    break;
                case NodeType.Counter:
                    var counterNode = (AtkCounterNode*)node;
                    ImGui.Text("text: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(counterNode->NodeText);
                    break;
                case NodeType.Image:
                    var imageNode = (AtkImageNode*)node;
                    PrintTextureInfo(imageNode->PartsList, imageNode->PartId);
                    break;
                case NodeType.NineGrid:
                    var ngNode = (AtkNineGridNode*)node;
                    PrintTextureInfo(ngNode->PartsList, ngNode->PartId);
                    break;
                case NodeType.ClippingMask:
                    var cmNode = (AtkClippingMaskNode*)node;
                    PrintTextureInfo(cmNode->PartsList, cmNode->PartId);
                    break;
            }

            ImGui.TreePop();
        }
        else if (ImGui.IsItemHovered())
        {
            this.DrawOutline(node);
        }

        if (isVisible && !popped)
            ImGui.PopStyleColor();

        static void PrintTextureInfo(AtkUldPartsList* partsList, uint partId)
        {
            if (partsList != null)
            {
                if (partId > partsList->PartCount)
                {
                    ImGui.Text("part id > part count?");
                }
                else
                {
                    var textureInfo = partsList->Parts[partId].UldAsset;
                    var texType = textureInfo->AtkTexture.TextureType;
                    ImGui.Text(
                        $"texture type: {texType} part_id={partId} part_id_count={partsList->PartCount}");
                    if (texType == TextureType.Resource)
                    {
                        ImGui.Text(
                            $"texture path: {textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName}");
                        var kernelTexture = textureInfo->AtkTexture.Resource->KernelTextureObject;

                        if (ImGui.TreeNode($"Texture##{(ulong)kernelTexture->D3D11ShaderResourceView:X}"))
                        {
                            ImGui.Image(
                                new ImTextureID(kernelTexture->D3D11ShaderResourceView),
                                new Vector2(kernelTexture->ActualWidth, kernelTexture->ActualHeight));
                            ImGui.TreePop();
                        }
                    }
                    else if (texType == TextureType.KernelTexture)
                    {
                        if (ImGui.TreeNode(
                                $"Texture##{(ulong)textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView:X}"))
                        {
                            ImGui.Image(
                                new ImTextureID(textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView),
                                new Vector2(
                                    textureInfo->AtkTexture.KernelTexture->ActualWidth,
                                    textureInfo->AtkTexture.KernelTexture->ActualHeight));
                            ImGui.TreePop();
                        }
                    }

                    if (ImGui.Button($"Replace with a random image##{(ulong)textureInfo:X}"))
                    {
                        var texm = Service<TextureManager>.Get();
                        texm.Shared
                            .GetFromGame(
                                Random.Shared.Next(0, 1) == 0
                                    ? $"ui/loadingimage/-nowloading_base{Random.Shared.Next(1, 33)}.tex"
                                    : $"ui/loadingimage/-nowloading_base{Random.Shared.Next(1, 33)}_hr1.tex")
                            .RentAsync()
                            .ContinueWith(
                                r => Service<Framework>.Get().RunOnFrameworkThread(
                                    () =>
                                    {
                                        if (!r.IsCompletedSuccessfully)
                                            return;

                                        using (r.Result)
                                        {
                                            textureInfo->AtkTexture.ReleaseTexture();
                                            textureInfo->AtkTexture.KernelTexture =
                                                texm.ConvertToKernelTexture(r.Result);
                                            textureInfo->AtkTexture.TextureType = TextureType.KernelTexture;
                                        }
                                    }));
                    }
                }
            }
            else
            {
                ImGui.Text("no texture loaded");
            }
        }
    }

    private void PrintComponentNode(AtkResNode* node, string treePrefix)
    {
        var compNode = (AtkComponentNode*)node;

        var popped = false;
        var isVisible = node->NodeFlags.HasFlag(NodeFlags.Visible);

        var componentInfo = compNode->Component->UldManager;

        var childCount = componentInfo.NodeListCount;

        var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
        if (objectInfo == null)
        {
            return;
        }

        if (isVisible)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

        if (ImGui.TreeNode($"{treePrefix}{objectInfo->ComponentType} Component Node (ptr = {(long)node:X}, component ptr = {(long)compNode->Component:X}) child count = {childCount}  ###{(long)node}"))
        {
            if (ImGui.IsItemHovered())
                this.DrawOutline(node);

            if (isVisible)
            {
                ImGui.PopStyleColor();
                popped = true;
            }

            ImGui.Text("Node: ");
            ImGui.SameLine();
            ImGuiHelpers.ClickToCopyText($"{(ulong)node:X}");
            ImGui.SameLine();
            Util.ShowStruct(*compNode, (ulong)compNode);
            ImGui.Text("Component: ");
            ImGui.SameLine();
            ImGuiHelpers.ClickToCopyText($"{(ulong)compNode->Component:X}");
            ImGui.SameLine();

            switch (objectInfo->ComponentType)
            {
                case ComponentType.Button: Util.ShowStruct(*(AtkComponentButton*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.Slider: Util.ShowStruct(*(AtkComponentSlider*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.Window: Util.ShowStruct(*(AtkComponentWindow*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.CheckBox: Util.ShowStruct(*(AtkComponentCheckBox*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.GaugeBar: Util.ShowStruct(*(AtkComponentGaugeBar*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.RadioButton: Util.ShowStruct(*(AtkComponentRadioButton*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.TextInput: Util.ShowStruct(*(AtkComponentTextInput*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.Icon: Util.ShowStruct(*(AtkComponentIcon*)compNode->Component, (ulong)compNode->Component); break;
                default: Util.ShowStruct(*compNode->Component, (ulong)compNode->Component); break;
            }

            this.PrintResNode(node);
            this.PrintNode(componentInfo.RootNode);

            switch (objectInfo->ComponentType)
            {
                case ComponentType.TextInput:
                    var textInputComponent = (AtkComponentTextInput*)compNode->Component;
                    ImGui.Text("InputBase Text1: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->AtkComponentInputBase.UnkText1);

                    ImGui.Text("InputBase Text2: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->AtkComponentInputBase.UnkText2);

                    ImGui.Text("Text1: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText01);

                    ImGui.Text("Text2: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText02);

                    ImGui.Text("AvailableLines: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->AvailableLines);

                    ImGui.Text("HighlightedAutoTranslateOptionColorPrefix: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->HighlightedAutoTranslateOptionColorPrefix);

                    ImGui.Text("HighlightedAutoTranslateOptionColorSuffix: ");
                    ImGui.SameLine();
                    Service<SeStringRenderer>.Get().Draw(textInputComponent->HighlightedAutoTranslateOptionColorSuffix);
                    break;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
            if (ImGui.TreeNode($"Node List##{(ulong)node:X}"))
            {
                ImGui.PopStyleColor();

                for (var i = 0; i < compNode->Component->UldManager.NodeListCount; i++)
                {
                    this.PrintNode(compNode->Component->UldManager.NodeList[i], false, $"[{i}] ");
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }

            ImGui.TreePop();
        }
        else if (ImGui.IsItemHovered())
        {
            this.DrawOutline(node);
        }

        if (isVisible && !popped)
            ImGui.PopStyleColor();
    }

    private void PrintResNode(AtkResNode* node)
    {
        ImGui.Text($"NodeID: {node->NodeId}");
        ImGui.SameLine();
        if (ImGui.SmallButton($"T:Visible##{(ulong)node:X}"))
        {
            node->NodeFlags ^= NodeFlags.Visible;
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"C:Ptr##{(ulong)node:X}"))
        {
            ImGui.SetClipboardText($"{(ulong)node:X}");
        }

        ImGui.Text(
            $"X: {node->X} Y: {node->Y} " +
            $"ScaleX: {node->ScaleX} ScaleY: {node->ScaleY} " +
            $"Rotation: {node->Rotation} " +
            $"Width: {node->Width} Height: {node->Height} " +
            $"OriginX: {node->OriginX} OriginY: {node->OriginY}");
        ImGui.Text(
            $"RGBA: 0x{node->Color.R:X2}{node->Color.G:X2}{node->Color.B:X2}{node->Color.A:X2} " +
            $"AddRGB: {node->AddRed} {node->AddGreen} {node->AddBlue} " +
            $"MultiplyRGB: {node->MultiplyRed} {node->MultiplyGreen} {node->MultiplyBlue}");
    }

    private bool DrawUnitListHeader(int index, ushort count, ulong ptr, bool highlight)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, highlight ? 0xFFAAAA00 : 0xFFFFFFFF);
        if (!string.IsNullOrEmpty(this.searchInput) && !this.doingSearch)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
        }
        else if (this.doingSearch && string.IsNullOrEmpty(this.searchInput))
        {
            ImGui.SetNextItemOpen(false, ImGuiCond.Always);
        }

        var treeNode = ImGui.TreeNode($"{this.listNames[index]}##unitList_{index}");
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.TextDisabled($"C:{count}  {ptr:X}");
        return treeNode;
    }

    private void DrawUnitBaseList()
    {
        var foundSelected = false;
        var noResults = true;
        var stage = AtkStage.Instance();

        var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;

        var searchStr = this.searchInput;
        var searching = !string.IsNullOrEmpty(searchStr);

        for (var i = 0; i < UnitListCount; i++)
        {
            var headerDrawn = false;

            var highlight = this.selectedUnitBase != null && this.selectedInList[i];
            this.selectedInList[i] = false;
            var unitManager = &unitManagers[i];

            var headerOpen = true;

            if (!searching)
            {
                headerOpen = this.DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
                headerDrawn = true;
                noResults = false;
            }

            for (var j = 0; j < unitManager->Count && headerOpen; j++)
            {
                AtkUnitBase* unitBase = unitManager->Entries[j];
                if (this.selectedUnitBase != null && unitBase == this.selectedUnitBase)
                {
                    this.selectedInList[i] = true;
                    foundSelected = true;
                }

                var name = unitBase->NameString;
                if (searching)
                {
                    if (name == null || !name.ToLowerInvariant().Contains(searchStr.ToLowerInvariant())) continue;
                }

                noResults = false;
                if (!headerDrawn)
                {
                    headerOpen = this.DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
                    headerDrawn = true;
                }

                if (headerOpen)
                {
                    var visible = unitBase->IsVisible;
                    ImGui.PushStyleColor(ImGuiCol.Text, visible ? 0xFF00FF00 : 0xFF999999);

                    if (ImGui.Selectable($"{name}##list{i}-{(ulong)unitBase:X}_{j}", this.selectedUnitBase == unitBase))
                    {
                        this.selectedUnitBase = unitBase;
                        foundSelected = true;
                        this.selectedInList[i] = true;
                    }

                    ImGui.PopStyleColor();
                }
            }

            if (headerDrawn && headerOpen)
            {
                ImGui.TreePop();
            }

            if (this.selectedInList[i] == false && this.selectedUnitBase != null)
            {
                for (var j = 0; j < unitManager->Count; j++)
                {
                    AtkUnitBase* unitBase = unitManager->Entries[j];
                    if (this.selectedUnitBase == null || unitBase != this.selectedUnitBase) continue;
                    this.selectedInList[i] = true;
                    foundSelected = true;
                }
            }
        }

        if (noResults)
        {
            ImGui.TextDisabled("No Results");
        }

        if (!foundSelected)
        {
            this.selectedUnitBase = null;
        }

        if (this.doingSearch && string.IsNullOrEmpty(this.searchInput))
        {
            this.doingSearch = false;
        }
        else if (!this.doingSearch && !string.IsNullOrEmpty(this.searchInput))
        {
            this.doingSearch = true;
        }
    }

    private Vector2 GetNodePosition(AtkResNode* node)
    {
        var pos = new Vector2(node->X, node->Y);
        pos -= new Vector2(node->OriginX * (node->ScaleX - 1), node->OriginY * (node->ScaleY - 1));
        var par = node->ParentNode;
        while (par != null)
        {
            pos *= new Vector2(par->ScaleX, par->ScaleY);
            pos += new Vector2(par->X, par->Y);
            pos -= new Vector2(par->OriginX * (par->ScaleX - 1), par->OriginY * (par->ScaleY - 1));
            par = par->ParentNode;
        }

        return pos;
    }

    private Vector2 GetNodeScale(AtkResNode* node)
    {
        if (node == null) return new Vector2(1, 1);
        var scale = new Vector2(node->ScaleX, node->ScaleY);
        while (node->ParentNode != null)
        {
            node = node->ParentNode;
            scale *= new Vector2(node->ScaleX, node->ScaleY);
        }

        return scale;
    }

    private bool GetNodeVisible(AtkResNode* node)
    {
        if (node == null) return false;
        while (node != null)
        {
            if (!node->NodeFlags.HasFlag(NodeFlags.Visible)) return false;
            node = node->ParentNode;
        }

        return true;
    }

    private void DrawOutline(AtkResNode* node)
    {
        var position = this.GetNodePosition(node);
        var scale = this.GetNodeScale(node);
        var size = new Vector2(node->Width, node->Height) * scale;

        var nodeVisible = this.GetNodeVisible(node);

        position += ImGuiHelpers.MainViewport.Pos;

        ImGui.GetForegroundDrawList(ImGuiHelpers.MainViewportNew).AddRect(position, position + size, nodeVisible ? 0xFF00FF00 : 0xFF0000FF);
    }
}
