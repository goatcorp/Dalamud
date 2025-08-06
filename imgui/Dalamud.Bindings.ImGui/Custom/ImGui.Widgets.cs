using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool Begin(ImU8String name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        fixed (bool* openPtr = &open)
        {
            var r = ImGuiNative.Begin(namePtr, openPtr, flags) != 0;
            name.Dispose();
            return r;
        }
    }

    public static bool Begin(ImU8String name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.Begin(namePtr, null, flags) != 0;
            name.Dispose();
            return r;
        }
    }

    public static bool BeginChild(
        ImU8String strId, Vector2 size = default, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginChild(strIdPtr, size, border ? (byte)1 : (byte)0, flags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginChild(
        uint id, Vector2 size = default, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None) =>
        ImGuiNative.BeginChild(id, size, border ? (byte)1 : (byte)0, flags) != 0;

    public static bool BeginCombo(
        ImU8String label, ImU8String previewValue, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* previewValuePtr = &previewValue.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginCombo(labelPtr, previewValuePtr, flags) != 0;
            label.Dispose();
            previewValue.Dispose();
            return r;
        }
    }

    public static bool BeginListBox(ImU8String label, Vector2 size = default)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginListBox(labelPtr, size) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool BeginMenu(ImU8String label, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginMenu(labelPtr, enabled ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool BeginPopup(ImU8String strId, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginPopup(strIdPtr, flags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginPopupContextItem(
        ImU8String strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonDefault)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginPopupContextItem(strIdPtr, popupFlags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginPopupContextWindow(
        ImU8String strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonDefault)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginPopupContextWindow(strIdPtr, popupFlags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginPopupContextVoid(
        ImU8String strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonDefault)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginPopupContextVoid(strIdPtr, popupFlags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginPopupModal(
        ImU8String name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        fixed (bool* openPtr = &open)
        {
            var r = ImGuiNative.BeginPopupModal(namePtr, openPtr, flags) != 0;
            name.Dispose();
            return r;
        }
    }

    public static bool BeginPopupModal(ImU8String name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginPopupModal(namePtr, null, flags) != 0;
            name.Dispose();
            return r;
        }
    }

    public static bool BeginTabBar(ImU8String strId, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginTabBar(strIdPtr, flags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginTabItem(
        ImU8String label, ref bool pOpen, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* pOpenPtr = &pOpen)
        {
            var r = ImGuiNative.BeginTabItem(labelPtr, pOpenPtr, flags) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool BeginTabItem(ImU8String label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginTabItem(labelPtr, null, flags) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool BeginTable(
        ImU8String strId, int column, ImGuiTableFlags flags = ImGuiTableFlags.None, Vector2 outerSize = default,
        float innerWidth = 0.0f)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.BeginTable(strIdPtr, column, flags, outerSize, innerWidth) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool Button(ImU8String label, Vector2 size = default)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.Button(labelPtr, size) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool Checkbox(ImU8String label, ref bool v)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* vPtr = &v)
        {
            var r = ImGuiNative.Checkbox(labelPtr, vPtr) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool CheckboxFlags<T>(ImU8String label, ref T flags, T flagsValue)
        where T : IBinaryInteger<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var allOn = (flags & flagsValue) == flagsValue;
            var anyOn = !T.IsZero(flags & flagsValue);
            bool pressed;
            if (!allOn && anyOn)
            {
                var g = GetCurrentContext();
                var backupItemFlags = g.CurrentItemFlags;
                g.CurrentItemFlags |= ImGuiItemFlags.MixedValue;
                pressed = ImGuiNative.Checkbox(labelPtr, &allOn) != 0;
                g.CurrentItemFlags = backupItemFlags;
            }
            else
            {
                pressed = ImGuiNative.Checkbox(labelPtr, &allOn) != 0;
            }

            if (pressed)
            {
                if (allOn)
                    flags |= flagsValue;
                else
                    flags &= ~flagsValue;
            }

            label.Dispose();
            return pressed;
        }
    }

    public static bool CollapsingHeader(
        ImU8String label, ref bool visible, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* visiblePtr = &visible)
        {
            var r = ImGuiNative.CollapsingHeader(labelPtr, visiblePtr, flags) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool CollapsingHeader(ImU8String label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.CollapsingHeader(labelPtr, null, flags) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool ColorButton(
        ImU8String descId, in Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None,
        Vector2 size = default)
    {
        fixed (byte* descIdPtr = &descId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.ColorButton(descIdPtr, col, flags, size) != 0;
            descId.Dispose();
            return r;
        }
    }

    public static void Columns(int count = 1, ImU8String id = default, bool border = true)
    {
        fixed (byte* idPtr = &id.GetPinnableNullTerminatedReference())
            ImGuiNative.Columns(count, idPtr, border ? (byte)1 : (byte)0);
        id.Dispose();
    }

    public static bool DebugCheckVersionAndDataLayout(
        ImU8String versionStr, nuint szIo, nuint szStyle, nuint szVec2, nuint szVec4, nuint szDrawVert,
        nuint szDrawIdx)
    {
        fixed (byte* versionPtr = &versionStr.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.DebugCheckVersionAndDataLayout(
                        versionPtr,
                        szIo,
                        szStyle,
                        szVec2,
                        szVec4,
                        szDrawVert,
                        szDrawIdx) != 0;
            versionStr.Dispose();
            return r;
        }
    }

    public static void DebugTextEncoding(ImU8String text)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
        {
            ImGuiNative.DebugTextEncoding(textPtr);
            text.Dispose();
        }
    }

    public static bool Draw(ImGuiTextFilterPtr self, ImU8String label = default, float width = 0.0f)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference("Filter (inc,-exc)"u8))
        {
            var r = ImGuiNative.Draw(self.Handle, labelPtr, width) != 0;
            label.Dispose();
            return r;
        }
    }

    public static ImGuiTextFilterPtr ImGuiTextFilter(ImU8String defaultFilter = default)
    {
        fixed (byte* defaultFilterPtr = &defaultFilter.GetPinnableNullTerminatedReference("\0"u8))
        {
            var r = ImGuiNative.ImGuiTextFilter(defaultFilterPtr);
            defaultFilter.Dispose();
            return r;
        }
    }

    public static ImGuiTextRangePtr ImGuiTextRange() => ImGuiNative.ImGuiTextRange();

    public static ImGuiTextRangePtr ImGuiTextRange(ReadOnlySpan<byte> text)
    {
        fixed (byte* textPtr = text)
            return ImGuiNative.ImGuiTextRange(textPtr, textPtr + text.Length);
    }

    public static bool InvisibleButton(
        ImU8String strId, Vector2 size, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.InvisibleButton(strIdPtr, size, flags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool IsDataType(ImGuiPayloadPtr self, ImU8String type)
    {
        fixed (byte* typePtr = &type.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.IsDataType(self.Handle, typePtr) != 0;
            type.Dispose();
            return r;
        }
    }

    public static bool IsPopupOpen(ImU8String strId, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.IsPopupOpen(strIdPtr, flags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static void LoadIniSettingsFromDisk(ImU8String iniFilename)
    {
        fixed (byte* iniFilenamePtr = &iniFilename.GetPinnableNullTerminatedReference())
            ImGuiNative.LoadIniSettingsFromDisk(iniFilenamePtr);
        iniFilename.Dispose();
    }

    public static void LoadIniSettingsFromMemory(ImU8String iniData)
    {
        fixed (byte* iniDataPtr = iniData)
            ImGuiNative.LoadIniSettingsFromMemory(iniDataPtr, (nuint)iniData.Length);
        iniData.Dispose();
    }

    public static void LogToFile(int autoOpenDepth = -1, ImU8String filename = default)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
            ImGuiNative.LogToFile(autoOpenDepth, filenamePtr);
        filename.Dispose();
    }

    public static bool MenuItem(
        ImU8String label, ImU8String shortcut, bool selected = false, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* shortcutPtr = &shortcut.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.MenuItem(
                        labelPtr,
                        shortcutPtr,
                        selected ? (byte)1 : (byte)0,
                        enabled ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            shortcut.Dispose();
            return r;
        }
    }

    public static bool MenuItem(ImU8String label, ImU8String shortcut, ref bool selected, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* shortcutPtr = &shortcut.GetPinnableNullTerminatedReference())
        fixed (bool* selectedPtr = &selected)
        {
            var r = ImGuiNative.MenuItem(labelPtr, shortcutPtr, selectedPtr, enabled ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            shortcut.Dispose();
            return r;
        }
    }

    public static bool MenuItem(ImU8String label, ref bool selected, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* selectedPtr = &selected)
        {
            var r = ImGuiNative.MenuItem(labelPtr, null, selectedPtr, enabled ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool MenuItem(ImU8String label, bool selected = false, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.MenuItem(
                        labelPtr,
                        null,
                        selected ? (byte)1 : (byte)0,
                        enabled ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            return r;
        }
    }

    public static void OpenPopup(ImU8String strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
            ImGuiNative.OpenPopup(strIdPtr, popupFlags);
        strId.Dispose();
    }

    public static void OpenPopup(uint id, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None) =>
        ImGuiNative.OpenPopup(id, popupFlags);

    public static void OpenPopupOnItemClick(
        ImU8String strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonDefault)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
            ImGuiNative.OpenPopupOnItemClick(strIdPtr, popupFlags);
        strId.Dispose();
    }

    public static void ProgressBar(float fraction, Vector2 sizeArg, ImU8String overlay = default)
    {
        fixed (byte* overlayPtr = &overlay.GetPinnableNullTerminatedReference())
            ImGuiNative.ProgressBar(fraction, sizeArg, overlayPtr);
        overlay.Dispose();
    }

    public static void ProgressBar(float fraction, ImU8String overlay = default)
    {
        fixed (byte* overlayPtr = &overlay.GetPinnableNullTerminatedReference())
            ImGuiNative.ProgressBar(fraction, new(-float.MinValue, 0), overlayPtr);
        overlay.Dispose();
    }

    public static bool RadioButton(ImU8String label, bool active)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.RadioButton(labelPtr, active ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool RadioButton<T>(ImU8String label, ref T v, T vButton)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var pressed = ImGuiNative.RadioButton(
                              labelPtr,
                              EqualityComparer<T>.Default.Equals(v, vButton) ? (byte)1 : (byte)0) != 0;
            if (pressed)
                v = vButton;
            return pressed;
        }
    }

    public static void SaveIniSettingsToDisk(ImU8String iniFilename)
    {
        fixed (byte* iniPtr = &iniFilename.GetPinnableNullTerminatedReference())
            ImGuiNative.SaveIniSettingsToDisk(iniPtr);
    }

    public static bool Selectable(
        ImU8String label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
        Vector2 size = default)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.Selectable(labelPtr, selected ? (byte)1 : (byte)0, flags, size) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool Selectable(
        ImU8String label, ref bool selected, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
        Vector2 size = default)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* selectedPtr = &selected)
        {
            var r = ImGuiNative.Selectable(labelPtr, selectedPtr, flags, size) != 0;
            label.Dispose();
            return r;
        }
    }

    public static void SetClipboardText(ImU8String text)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImGuiNative.SetClipboardText(textPtr);
        text.Dispose();
    }

    public static bool SetDragDropPayload(ImU8String type, ReadOnlySpan<byte> data, ImGuiCond cond)
    {
        fixed (byte* typePtr = &type.GetPinnableNullTerminatedReference())
        fixed (byte* dataPtr = data)
        {
            var r = ImGuiNative.SetDragDropPayload(typePtr, dataPtr, (nuint)data.Length, cond) != 0;
            type.Dispose();
            return r;
        }
    }

    public static void SetTabItemClosed(ImU8String tabOrDockedWindowLabel)
    {
        fixed (byte* tabItemPtr = &tabOrDockedWindowLabel.GetPinnableNullTerminatedReference())
            ImGuiNative.SetTabItemClosed(tabItemPtr);
        tabOrDockedWindowLabel.Dispose();
    }

    public static void SetWindowCollapsed(bool collapsed, ImGuiCond cond = ImGuiCond.None) =>
        ImGuiNative.SetWindowCollapsed(collapsed ? (byte)1 : (byte)0, cond);

    public static void SetWindowCollapsed(ImU8String name, bool collapsed, ImGuiCond cond = ImGuiCond.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
            ImGuiNative.SetWindowCollapsed(namePtr, collapsed ? (byte)1 : (byte)0, cond);
        name.Dispose();
    }

    public static void SetWindowFocus() => ImGuiNative.SetWindowFocus();

    public static void SetWindowFocus(ImU8String name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
            ImGuiNative.SetWindowFocus(namePtr);
        name.Dispose();
    }

    public static void SetWindowPos(Vector2 pos, ImGuiCond cond = ImGuiCond.None) =>
        ImGuiNative.SetWindowPos(pos, cond);

    public static void SetWindowPos(ImU8String name, Vector2 pos, ImGuiCond cond = ImGuiCond.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
            ImGuiNative.SetWindowPos(namePtr, pos, cond);
        name.Dispose();
    }

    public static void SetWindowSize(Vector2 size, ImGuiCond cond = ImGuiCond.None) =>
        ImGuiNative.SetWindowSize(size, cond);

    public static void SetWindowSize(ImU8String name, Vector2 size, ImGuiCond cond = ImGuiCond.None)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
            ImGuiNative.SetWindowSize(namePtr, size, cond);
        name.Dispose();
    }

    public static void ShowFontSelector(ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiNative.ShowFontSelector(labelPtr);
        label.Dispose();
    }

    public static bool ShowStyleSelector(ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.ShowStyleSelector(labelPtr) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool SmallButton(ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.SmallButton(labelPtr) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool TabItemButton(ImU8String label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.TabItemButton(labelPtr, flags) != 0;
            label.Dispose();
            return r;
        }
    }

    public static void TableHeader(ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiNative.TableHeader(labelPtr);
        label.Dispose();
    }

    public static void TableSetupColumn(
        ImU8String label, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, float initWidthOrWeight = 0.0f,
        uint userId = 0)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiNative.TableSetupColumn(labelPtr, flags, initWidthOrWeight, userId);
        label.Dispose();
    }

    public static void TreePush(ImU8String strId)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
            ImGuiNative.TreePush(strIdPtr);
        strId.Dispose();
    }

    public static void TreePush(nint ptrId) => ImGuiNative.TreePush((void*)ptrId);
    public static void TreePush(void* ptrId) => ImGuiNative.TreePush(ptrId);

    public static void Value<T>(ImU8String prefix, in T value)
    {
        prefix.AppendLiteral(": ");
        prefix.AppendFormatted(value);
        fixed (byte* prefixPtr = prefix)
        {
            ImGuiNative.TextUnformatted(prefixPtr, prefixPtr + prefix.Length);
            prefix.Dispose();
        }
    }

    // public static void Value(AutoUtf8Buffer prefix, float value) => Value(prefix, value, default);

    public static void Value(ImU8String prefix, float value, ImU8String floatFormat = default)
    {
        fixed (byte* prefixPtr = &prefix.GetPinnableNullTerminatedReference())
        fixed (byte* floatPtr = &floatFormat.GetPinnableNullTerminatedReference())
        {
            ImGuiNative.Value(prefixPtr, value, floatPtr);
            prefix.Dispose();
            floatFormat.Dispose();
        }
    }
}
