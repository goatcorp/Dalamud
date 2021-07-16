using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Component.GUI.ULD;
using ImGuiNET;

using AlignmentType = FFXIVClientStructs.FFXIV.Component.GUI.AlignmentType;

// Customised version of https://github.com/aers/FFXIVUIDebug

namespace Dalamud.Interface.Internal
{
    /// <summary>
    /// This class displays a debug window to inspect native addons.
    /// </summary>
    internal unsafe class UIDebug
    {
        private const int UnitListCount = 18;

        private readonly Dalamud dalamud;
        private readonly GetAtkStageSingleton getAtkStageSingleton;
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
        private ulong beginModule;
        private ulong endModule;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIDebug"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public UIDebug(Dalamud dalamud)
        {
            this.dalamud = dalamud;
            var getSingletonAddr = dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF");
            this.getAtkStageSingleton = Marshal.GetDelegateForFunctionPointer<GetAtkStageSingleton>(getSingletonAddr);
        }

        private delegate AtkStage* GetAtkStageSingleton();

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

        private static void ClickToCopyText(string text, string textCopy = null)
        {
            textCopy ??= text;
            ImGui.Text($"{text}");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (textCopy != text) ImGui.SetTooltip(textCopy);
            }

            if (ImGui.IsItemClicked()) ImGui.SetClipboardText($"{textCopy}");
        }

        private void DrawUnitBase(AtkUnitBase* atkUnitBase)
        {
            var isVisible = (atkUnitBase->Flags & 0x20) == 0x20;
            var addonName = Marshal.PtrToStringAnsi(new IntPtr(atkUnitBase->Name));
            var agent = this.dalamud.Framework.Gui.FindAgentInterface((IntPtr)atkUnitBase);

            ImGui.Text($"{addonName}");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : 0xFF0000FF);
            ImGui.Text(isVisible ? "Visible" : "Not Visible");
            ImGui.PopStyleColor();

            ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - 25);
            if (ImGui.SmallButton("V"))
            {
                atkUnitBase->Flags ^= 0x20;
            }

            ImGui.Separator();
            ClickToCopyText($"Address: {(ulong)atkUnitBase:X}", $"{(ulong)atkUnitBase:X}");
            ClickToCopyText($"Agent: {(ulong)agent:X}", $"{(ulong)agent:X}");
            ImGui.Separator();

            ImGui.Text($"Position: [ {atkUnitBase->X} , {atkUnitBase->Y} ]");
            ImGui.Text($"Scale: {atkUnitBase->Scale * 100}%%");
            ImGui.Text($"Widget Count {atkUnitBase->UldManager.ObjectCount}");

            ImGui.Separator();

            object addonObj = *atkUnitBase;

            this.PrintOutObject(addonObj, (ulong)atkUnitBase, new List<string>());

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
            var isVisible = (node->Flags & 0x10) == 0x10;

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
                ClickToCopyText($"{(ulong)node:X}");
                ImGui.SameLine();
                switch (node->Type)
                {
                    case NodeType.Text: this.PrintOutObject(*(AtkTextNode*)node, (ulong)node, new List<string>()); break;
                    case NodeType.Image: this.PrintOutObject(*(AtkImageNode*)node, (ulong)node, new List<string>()); break;
                    case NodeType.Collision: this.PrintOutObject(*(AtkCollisionNode*)node, (ulong)node, new List<string>()); break;
                    case NodeType.NineGrid: this.PrintOutObject(*(AtkNineGridNode*)node, (ulong)node, new List<string>()); break;
                    case NodeType.Counter: this.PrintOutObject(*(AtkCounterNode*)node, (ulong)node, new List<string>()); break;
                    default: this.PrintOutObject(*node, (ulong)node, new List<string>()); break;
                }

                this.PrintResNode(node);

                if (node->ChildNode != null)
                    this.PrintNode(node->ChildNode);

                switch (node->Type)
                {
                    case NodeType.Text:
                        var textNode = (AtkTextNode*)node;
                        ImGui.Text($"text: {Marshal.PtrToStringAnsi(new IntPtr(textNode->NodeText.StringPtr))}");

                        ImGui.InputText($"Replace Text##{(ulong)textNode:X}", new IntPtr(textNode->NodeText.StringPtr), (uint)textNode->NodeText.BufSize);

                        ImGui.Text($"AlignmentType: {(AlignmentType)textNode->AlignmentFontType}  FontSize: {textNode->FontSize}");
                        int b = textNode->AlignmentFontType;
                        if (ImGui.InputInt($"###setAlignment{(ulong)textNode:X}", ref b, 1))
                        {
                            while (b > byte.MaxValue) b -= byte.MaxValue;
                            while (b < byte.MinValue) b += byte.MaxValue;
                            textNode->AlignmentFontType = (byte)b;
                            textNode->AtkResNode.Flags_2 |= 0x1;
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
                        ImGui.Text($"text: {Marshal.PtrToStringAnsi(new IntPtr(counterNode->NodeText.StringPtr))}");
                        break;
                    case NodeType.Image:
                        var imageNode = (AtkImageNode*)node;
                        if (imageNode->PartsList != null)
                        {
                            if (imageNode->PartId > imageNode->PartsList->PartCount)
                            {
                                ImGui.Text("part id > part count?");
                            }
                            else
                            {
                                var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
                                var texType = textureInfo->AtkTexture.TextureType;
                                ImGui.Text($"texture type: {texType} part_id={imageNode->PartId} part_id_count={imageNode->PartsList->PartCount}");
                                if (texType == TextureType.Resource)
                                {
                                    var texFileNamePtr = textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
                                    var texString = Marshal.PtrToStringAnsi(new IntPtr(texFileNamePtr));
                                    ImGui.Text($"texture path: {texString}");
                                    var kernelTexture = textureInfo->AtkTexture.Resource->KernelTextureObject;

                                    if (ImGui.TreeNode($"Texture##{(ulong)kernelTexture->D3D11ShaderResourceView:X}"))
                                    {
                                        ImGui.Image(new IntPtr(kernelTexture->D3D11ShaderResourceView), new Vector2(kernelTexture->Width, kernelTexture->Height));
                                        ImGui.TreePop();
                                    }
                                }
                                else if (texType == TextureType.KernelTexture)
                                {
                                    if (ImGui.TreeNode($"Texture##{(ulong)textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView:X}"))
                                    {
                                        ImGui.Image(new IntPtr(textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView), new Vector2(textureInfo->AtkTexture.KernelTexture->Width, textureInfo->AtkTexture.KernelTexture->Height));
                                        ImGui.TreePop();
                                    }
                                }
                            }
                        }
                        else
                        {
                            ImGui.Text("no texture loaded");
                        }

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
        }

        private void PrintComponentNode(AtkResNode* node, string treePrefix)
        {
            var compNode = (AtkComponentNode*)node;

            var popped = false;
            var isVisible = (node->Flags & 0x10) == 0x10;

            if (isVisible)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

            var componentInfo = compNode->Component->UldManager;

            var childCount = componentInfo.NodeListCount;

            var objectInfo = (ULDComponentInfo*)componentInfo.Objects;
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
                ClickToCopyText($"{(ulong)node:X}");
                ImGui.SameLine();
                this.PrintOutObject(*compNode, (ulong)compNode, new List<string>());
                ImGui.Text("Component: ");
                ImGui.SameLine();
                ClickToCopyText($"{(ulong)compNode->Component:X}");
                ImGui.SameLine();

                switch (objectInfo->ComponentType)
                {
                    case ComponentType.Button: this.PrintOutObject(*(AtkComponentButton*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.Slider: this.PrintOutObject(*(AtkComponentSlider*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.Window: this.PrintOutObject(*(AtkComponentWindow*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.CheckBox: this.PrintOutObject(*(AtkComponentCheckBox*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.GaugeBar: this.PrintOutObject(*(AtkComponentGaugeBar*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.RadioButton: this.PrintOutObject(*(AtkComponentRadioButton*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.TextInput: this.PrintOutObject(*(AtkComponentTextInput*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    case ComponentType.Icon: this.PrintOutObject(*(AtkComponentIcon*)compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                    default: this.PrintOutObject(*compNode->Component, (ulong)compNode->Component, new List<string>()); break;
                }

                this.PrintResNode(node);
                this.PrintNode(componentInfo.RootNode);

                switch (objectInfo->ComponentType)
                {
                    case ComponentType.TextInput:
                        var textInputComponent = (AtkComponentTextInput*)compNode->Component;
                        ImGui.Text($"InputBase Text1: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->AtkComponentInputBase.UnkText1.StringPtr))}");
                        ImGui.Text($"InputBase Text2: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->AtkComponentInputBase.UnkText2.StringPtr))}");
                        ImGui.Text($"Text1: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText1.StringPtr))}");
                        ImGui.Text($"Text2: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText2.StringPtr))}");
                        ImGui.Text($"Text3: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText3.StringPtr))}");
                        ImGui.Text($"Text4: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText4.StringPtr))}");
                        ImGui.Text($"Text5: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText5.StringPtr))}");
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
            ImGui.Text($"NodeID: {node->NodeID}");
            ImGui.SameLine();
            if (ImGui.SmallButton($"T:Visible##{(ulong)node:X}"))
            {
                node->Flags ^= 0x10;
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

        private bool DrawUnitListHeader(int index, uint count, ulong ptr, bool highlight)
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
            var stage = this.getAtkStageSingleton();

            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;

            var searchStr = this.searchInput;
            var searching = !string.IsNullOrEmpty(searchStr);

            for (var i = 0; i < UnitListCount; i++)
            {
                var headerDrawn = false;

                var highlight = this.selectedUnitBase != null && this.selectedInList[i];
                this.selectedInList[i] = false;
                var unitManager = &unitManagers[i];

                var unitBaseArray = &unitManager->AtkUnitEntries;

                var headerOpen = true;

                if (!searching)
                {
                    headerOpen = this.DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
                    headerDrawn = true;
                    noResults = false;
                }

                for (var j = 0; j < unitManager->Count && headerOpen; j++)
                {
                    var unitBase = unitBaseArray[j];
                    if (this.selectedUnitBase != null && unitBase == this.selectedUnitBase)
                    {
                        this.selectedInList[i] = true;
                        foundSelected = true;
                    }

                    var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));
                    if (searching)
                    {
                        if (name == null || !name.ToLower().Contains(searchStr.ToLower())) continue;
                    }

                    noResults = false;
                    if (!headerDrawn)
                    {
                        headerOpen = this.DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
                        headerDrawn = true;
                    }

                    if (headerOpen)
                    {
                        var visible = (unitBase->Flags & 0x20) == 0x20;
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
                        if (this.selectedUnitBase == null || unitBaseArray[j] != this.selectedUnitBase) continue;
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
            var par = node->ParentNode;
            while (par != null)
            {
                pos *= new Vector2(par->ScaleX, par->ScaleY);
                pos += new Vector2(par->X, par->Y);
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
                if ((node->Flags & (short)NodeFlags.Visible) != (short)NodeFlags.Visible) return false;
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

            ImGui.GetForegroundDrawList(ImGuiHelpers.MainViewport).AddRect(position, position + size, nodeVisible ? 0xFF00FF00 : 0xFF0000FF);
        }

        private void PrintOutValue(ulong addr, IEnumerable<string> path, Type type, object value)
        {
            if (type.IsPointer)
            {
                var val = (Pointer)value;
                var unboxed = Pointer.Unbox(val);
                if (unboxed != null)
                {
                    var unboxedAddr = (ulong)unboxed;
                    ClickToCopyText($"{(ulong)unboxed:X}");
                    if (this.beginModule > 0 && unboxedAddr >= this.beginModule && unboxedAddr <= this.endModule)
                    {
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, 0xffcbc0ff);
                        ClickToCopyText($"ffxiv_dx11.exe+{unboxedAddr - this.beginModule:X}");
                        ImGui.PopStyleColor();
                    }

                    try
                    {
                        var eType = type.GetElementType();
                        var ptrObj = Marshal.PtrToStructure(new IntPtr(unboxed), eType);
                        ImGui.SameLine();
                        this.PrintOutObject(ptrObj, (ulong)unboxed, new List<string>(path));
                    }
                    catch
                    {
                        // Ignored
                    }
                }
                else
                {
                    ImGui.Text("null");
                }
            }
            else
            {
                if (!type.IsPrimitive)
                {
                    this.PrintOutObject(value, addr, new List<string>(path));
                }
                else
                {
                    ImGui.Text($"{value}");
                }
            }
        }

        private void PrintOutObject(object obj, ulong addr, List<string> path, bool autoExpand = false)
        {
            if (this.endModule == 0 && this.beginModule == 0)
            {
                try
                {
                    var processModule = Process.GetCurrentProcess().MainModule;
                    if (processModule != null)
                    {
                        this.beginModule = (ulong)processModule.BaseAddress.ToInt64();
                        this.endModule = this.beginModule + (ulong)processModule.ModuleMemorySize;
                    }
                    else
                    {
                        this.endModule = 1;
                    }
                }
                catch
                {
                    this.endModule = 1;
                }
            }

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
            if (autoExpand)
            {
                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
            }

            if (ImGui.TreeNode($"{obj}##print-obj-{addr:X}-{string.Join("-", path)}"))
            {
                ImGui.PopStyleColor();
                foreach (var f in obj.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
                {
                    var fixedBuffer = (FixedBufferAttribute)f.GetCustomAttribute(typeof(FixedBufferAttribute));
                    if (fixedBuffer != null)
                    {
                        ImGui.Text($"fixed");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{f.FieldType.Name}");
                    }

                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1), $"{f.Name}: ");
                    ImGui.SameLine();

                    this.PrintOutValue(addr, new List<string>(path) { f.Name }, f.FieldType, f.GetValue(obj));
                }

                foreach (var p in obj.GetType().GetProperties())
                {
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{p.PropertyType.Name}");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                    ImGui.SameLine();

                    this.PrintOutValue(addr, new List<string>(path) { p.Name }, p.PropertyType, p.GetValue(obj));
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }
    }
}
