using System.Numerics;

using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Dalamud.Bindings.ImGui;

using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying AtkArrayData.
/// </summary>
internal unsafe class AtkArrayDataBrowserWidget : IDataWindowWidget
{
    private readonly Type numberType = typeof(NumberArrayType);
    private readonly Type stringType = typeof(StringArrayType);
    private readonly Type extendType = typeof(ExtendArrayType);

    private int selectedNumberArray;
    private int selectedStringArray;
    private int selectedExtendArray;

    private string searchTerm = string.Empty;
    private bool hideUnsetStringArrayEntries = false;
    private bool hideUnsetExtendArrayEntries = false;
    private bool showTextAddress = false;
    private bool showMacroString = false;

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "atkarray" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Atk Array Data";

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        using var tabs = ImRaii.TabBar("AtkArrayDataTabs");
        if (!tabs) return;

        this.DrawNumberArrayTab();
        this.DrawStringArrayTab();
        this.DrawExtendArrayTab();
    }

    private void DrawArrayList(Type? arrayType, int arrayCount, short* arrayKeys, AtkArrayData** arrays, ref int selectedIndex)
    {
        using var table = ImRaii.Table("ArkArrayTable", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var hasSearchTerm = !string.IsNullOrEmpty(this.searchTerm);

        for (var arrayIndex = 0; arrayIndex < arrayCount; arrayIndex++)
        {
            var inUse = arrayKeys[arrayIndex] != -1;

            var rowsFound = 0;

            if (hasSearchTerm && arrayType == typeof(StringArrayType))
            {
                if (!inUse)
                    continue;

                var stringArrayData = (StringArrayData*)arrays[arrayIndex];
                for (var rowIndex = 0; rowIndex < arrays[arrayIndex]->Size; rowIndex++)
                {
                    var isNull = (nint)stringArrayData->StringArray[rowIndex] == 0;
                    if (isNull)
                        continue;

                    if (new ReadOnlySeStringSpan(stringArrayData->StringArray[rowIndex]).ExtractText().Contains(this.searchTerm, StringComparison.InvariantCultureIgnoreCase))
                        rowsFound++;
                }

                if (rowsFound == 0)
                    continue;
            }

            using var disabled = ImRaii.Disabled(!inUse);
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            if (ImGui.Selectable($"#{arrayIndex}", selectedIndex == arrayIndex, ImGuiSelectableFlags.SpanAllColumns))
                selectedIndex = arrayIndex;

            ImGui.TableNextColumn(); // Type
            if (arrayType != null && Enum.IsDefined(arrayType, arrayIndex))
            {
                ImGui.TextUnformatted(Enum.GetName(arrayType, arrayIndex));
            }
            else if (inUse && arrays[arrayIndex]->SubscribedAddonsCount > 0)
            {
                var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

                for (var j = 0; j < arrays[arrayIndex]->SubscribedAddonsCount; j++)
                {
                    if (arrays[arrayIndex]->SubscribedAddons[j] == 0)
                        continue;

                    using (ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF))
                        ImGui.TextUnformatted(raptureAtkUnitManager->GetAddonById(arrays[arrayIndex]->SubscribedAddons[j])->NameString);
                    break;
                }
            }

            ImGui.TableNextColumn(); // Size
            if (inUse)
                ImGui.TextUnformatted((rowsFound > 0 ? rowsFound : arrays[arrayIndex]->Size).ToString());
        }
    }

    private void DrawArrayHeader(Type? arrayType, string type, int index, AtkArrayData* array)
    {
        ImGui.TextUnformatted($"{type} Array #{index}");

        if (arrayType != null && Enum.IsDefined(arrayType, index))
        {
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted($" ({Enum.GetName(arrayType, index)})");
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("–");
        ImGui.SameLine();
        ImGui.TextUnformatted("Address: ");
        ImGui.SameLine(0, 0);
        WidgetUtil.DrawCopyableText($"0x{(nint)array:X}", "Copy address");

        if (array->SubscribedAddonsCount > 0)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("–");
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF))
                ImGui.TextUnformatted($"{array->SubscribedAddonsCount} Subscribed Addon" + (array->SubscribedAddonsCount > 1 ? 's' : string.Empty));

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (tooltip)
                {
                    var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

                    for (var j = 0; j < array->SubscribedAddonsCount; j++)
                    {
                        if (array->SubscribedAddons[j] == 0)
                            continue;

                        ImGui.TextUnformatted(raptureAtkUnitManager->GetAddonById(array->SubscribedAddons[j])->NameString);
                    }
                }
            }
        }
    }

    private void DrawNumberArrayTab()
    {
        var atkArrayDataHolder = RaptureAtkModule.Instance()->AtkArrayDataHolder;

        using var tab = ImRaii.TabItem("Number Arrays");
        if (!tab) return;

        this.DrawArrayList(
            this.numberType,
            atkArrayDataHolder.NumberArrayCount,
            atkArrayDataHolder.NumberArrayKeys,
            (AtkArrayData**)atkArrayDataHolder.NumberArrays,
            ref this.selectedNumberArray);

        if (this.selectedNumberArray >= atkArrayDataHolder.NumberArrayCount || atkArrayDataHolder.NumberArrayKeys[this.selectedNumberArray] == -1)
            this.selectedNumberArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var array = atkArrayDataHolder.NumberArrays[this.selectedNumberArray];
        this.DrawArrayHeader(this.numberType, "Number", this.selectedNumberArray, (AtkArrayData*)array);

        using var table = ImRaii.Table("NumberArrayDataTable", 7, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Integer", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Short", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Byte", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Float", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Hex", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(7, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < array->Size; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            var ptr = &array->IntArray[i];

            ImGui.TableNextColumn(); // Address
            WidgetUtil.DrawCopyableText($"0x{(nint)ptr:X}", "Copy entry address");

            ImGui.TableNextColumn(); // Integer
            WidgetUtil.DrawCopyableText((*ptr).ToString(), "Copy value");

            ImGui.TableNextColumn(); // Short
            WidgetUtil.DrawCopyableText((*(short*)ptr).ToString(), "Copy as short");

            ImGui.TableNextColumn(); // Byte
            WidgetUtil.DrawCopyableText((*(byte*)ptr).ToString(), "Copy as byte");

            ImGui.TableNextColumn(); // Float
            WidgetUtil.DrawCopyableText((*(float*)ptr).ToString(), "Copy as float");

            ImGui.TableNextColumn(); // Hex
            WidgetUtil.DrawCopyableText($"0x{array->IntArray[i]:X2}", "Copy Hex");
        }
    }

    private void DrawStringArrayTab()
    {
        using var tab = ImRaii.TabItem("String Arrays");
        if (!tab) return;

        var atkArrayDataHolder = RaptureAtkModule.Instance()->AtkArrayDataHolder;

        using (var sidebarchild = ImRaii.Child("StringArraySidebar", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings))
        {
            if (sidebarchild)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##TextSearch", "Search...", ref this.searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

                this.DrawArrayList(
                    this.stringType,
                    atkArrayDataHolder.StringArrayCount,
                    atkArrayDataHolder.StringArrayKeys,
                    (AtkArrayData**)atkArrayDataHolder.StringArrays,
                    ref this.selectedStringArray);
            }
        }

        if (this.selectedStringArray >= atkArrayDataHolder.StringArrayCount || atkArrayDataHolder.StringArrayKeys[this.selectedStringArray] == -1)
            this.selectedStringArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var array = atkArrayDataHolder.StringArrays[this.selectedStringArray];
        this.DrawArrayHeader(this.stringType, "String", this.selectedStringArray, (AtkArrayData*)array);
        ImGui.Checkbox("Hide unset entries##HideUnsetStringArrayEntriesCheckbox", ref this.hideUnsetStringArrayEntries);
        ImGui.SameLine();
        ImGui.Checkbox("Show text address##WordWrapCheckbox", ref this.showTextAddress);
        ImGui.SameLine();
        ImGui.Checkbox("Show macro string##RenderStringsCheckbox", ref this.showMacroString);

        using var table = ImRaii.Table("StringArrayDataTable", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn(this.showTextAddress ? "Text Address" : "Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Managed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        var hasSearchTerm = !string.IsNullOrEmpty(this.searchTerm);

        for (var i = 0; i < array->Size; i++)
        {
            var isNull = (nint)array->StringArray[i] == 0;
            if (isNull && this.hideUnsetStringArrayEntries)
                continue;

            if (hasSearchTerm)
            {
                if (isNull)
                    continue;

                if (!new ReadOnlySeStringSpan(array->StringArray[i]).ExtractText().Contains(this.searchTerm, StringComparison.InvariantCultureIgnoreCase))
                    continue;
            }

            using var disabledColor = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isNull);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            ImGui.TableNextColumn(); // Address
            if (this.showTextAddress)
            {
                if (!isNull)
                    WidgetUtil.DrawCopyableText($"0x{(nint)array->StringArray[i]:X}", "Copy text address");
            }
            else
            {
                WidgetUtil.DrawCopyableText($"0x{(nint)(&array->StringArray[i]):X}", "Copy entry address");
            }

            ImGui.TableNextColumn(); // Managed
            if (!isNull)
            {
                ImGui.TextUnformatted(((nint)array->StringArray[i] != 0 && array->ManagedStringArray[i] == array->StringArray[i]).ToString());
            }

            ImGui.TableNextColumn(); // Text
            if (!isNull)
            {
                if (this.showMacroString)
                {
                    WidgetUtil.DrawCopyableText(new ReadOnlySeStringSpan(array->StringArray[i]).ToString(), "Copy text");
                }
                else
                {
                    ImGuiHelpers.SeStringWrapped(new ReadOnlySeStringSpan(array->StringArray[i]));
                }
            }
        }
    }

    private void DrawExtendArrayTab()
    {
        using var tab = ImRaii.TabItem("Extend Arrays");
        if (!tab) return;

        var atkArrayDataHolder = RaptureAtkModule.Instance()->AtkArrayDataHolder;

        this.DrawArrayList(
            this.extendType,
            atkArrayDataHolder.ExtendArrayCount,
            atkArrayDataHolder.ExtendArrayKeys,
            (AtkArrayData**)atkArrayDataHolder.ExtendArrays,
            ref this.selectedExtendArray);

        if (this.selectedExtendArray >= atkArrayDataHolder.ExtendArrayCount || atkArrayDataHolder.ExtendArrayKeys[this.selectedExtendArray] == -1)
            this.selectedExtendArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);

        var array = atkArrayDataHolder.ExtendArrays[this.selectedExtendArray];
        this.DrawArrayHeader(null, "Extend", this.selectedExtendArray, (AtkArrayData*)array);
        ImGui.Checkbox("Hide unset entries##HideUnsetExtendArrayEntriesCheckbox", ref this.hideUnsetExtendArrayEntries);

        using var table = ImRaii.Table("ExtendArrayDataTable", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Pointer", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < array->Size; i++)
        {
            var isNull = (nint)array->DataArray[i] == 0;
            if (isNull && this.hideUnsetExtendArrayEntries)
                continue;

            using var disabledColor = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isNull);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            ImGui.TableNextColumn(); // Address
            WidgetUtil.DrawCopyableText($"0x{(nint)(&array->DataArray[i]):X}", "Copy entry address");

            ImGui.TableNextColumn(); // Pointer
            if (!isNull)
                WidgetUtil.DrawCopyableText($"0x{(nint)array->DataArray[i]:X}", "Copy address");
        }
    }
}
