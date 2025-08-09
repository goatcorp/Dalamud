using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public unsafe partial class ImGuiP
{
    public static bool ArrowButtonEx(
        ImU8String strId, ImGuiDir dir, Vector2 sizeArg, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ArrowButtonEx(strIdPtr, dir, sizeArg, flags) != 0;
            strId.Recycle();
            return r;
        }
    }

    public static bool BeginChildEx(ImU8String name, uint id, Vector2 sizeArg, bool border, ImGuiWindowFlags flags)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginChildEx(namePtr, id, sizeArg, border ? (byte)1 : (byte)0, flags) != 0;
            name.Recycle();
            return r;
        }
    }

    public static void BeginColumns(ImU8String strId, int count, ImGuiOldColumnFlags flags = ImGuiOldColumnFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
            ImGuiPNative.BeginColumns(strIdPtr, count, flags);
        strId.Recycle();
    }

    public static bool BeginMenuEx(ImU8String label, ImU8String icon = default, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* iconPtr = &icon.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginMenuEx(labelPtr, iconPtr, enabled ? (byte)1 : (byte)0) != 0;
            label.Recycle();
            icon.Recycle();
            return r;
        }
    }

    public static bool BeginTableEx(
        ImU8String name, uint id, int columnsCount, ImGuiTableFlags flags = ImGuiTableFlags.None,
        Vector2 outerSize = default, float innerWidth = 0.0f)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginTableEx(namePtr, id, columnsCount, flags, outerSize, innerWidth) != 0;
            name.Recycle();
            return r;
        }
    }

    public static bool BeginViewportSideBar(
        ImU8String name, ImGuiViewportPtr viewport, ImGuiDir dir, float size, ImGuiWindowFlags windowFlags)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginViewportSideBar(namePtr, viewport, dir, size, windowFlags) != 0;
            name.Recycle();
            return r;
        }
    }

    public static bool ButtonEx(
        ImU8String label, Vector2 sizeArg = default, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ButtonEx(labelPtr, sizeArg, flags) != 0;
            label.Recycle();
            return r;
        }
    }

    public static void ColorEditOptionsPopup(ReadOnlySpan<float> col, ImGuiColorEditFlags flags)
    {
        fixed (float* colPtr = col)
            ImGuiPNative.ColorEditOptionsPopup(colPtr, flags);
    }

    public static void ColorPickerOptionsPopup(ReadOnlySpan<float> refCol, ImGuiColorEditFlags flags)
    {
        fixed (float* refColPtr = refCol)
            ImGuiPNative.ColorPickerOptionsPopup(refColPtr, flags);
    }

    public static void ColorTooltip(ImU8String text, ReadOnlySpan<float> col, ImGuiColorEditFlags flags)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
        fixed (float* colPtr = col)
            ImGuiPNative.ColorTooltip(textPtr, colPtr, flags);
        text.Recycle();
    }

    public static ImGuiWindowSettingsPtr CreateNewWindowSettings(ImU8String name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.CreateNewWindowSettings(namePtr);
            name.Recycle();
            return r;
        }
    }

    public static void Custom_StbTextMakeUndoReplace(
        ImGuiInputTextStatePtr str, int where, int oldLength, int newLength) =>
        ImGuiPNative.Custom_StbTextMakeUndoReplace(str, where, oldLength, newLength);

    public static void Custom_StbTextUndo(ImGuiInputTextStatePtr str) => ImGuiPNative.Custom_StbTextUndo(str);

    public static bool DataTypeApplyFromText<T>(ImU8String buf, ImGuiDataType dataType, T data, ImU8String format)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* bufPtr = &buf.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.DataTypeApplyFromText(bufPtr, dataType, &data, formatPtr) != 0;
            format.Recycle();
            buf.Recycle();
            return r;
        }
    }

    public static void DebugNodeDockNode(ImGuiDockNodePtr node, ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeDockNode(node, labelPtr);
        label.Recycle();
    }

    public static void DebugNodeDrawList(
        ImGuiWindowPtr window, ImGuiViewportPPtr viewport, ImDrawListPtr drawList, ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeDrawList(window, viewport, drawList, labelPtr);
        label.Recycle();
    }

    public static void DebugNodeStorage(ImGuiStoragePtr storage, ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeStorage(storage, labelPtr);
        label.Recycle();
    }

    public static void DebugNodeTabBar(ImGuiTabBarPtr tabBar, ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeTabBar(tabBar, labelPtr);
        label.Recycle();
    }

    public static void DebugNodeWindow(ImGuiWindowPtr window, ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeWindow(window, labelPtr);
        label.Recycle();
    }

    public static void DebugNodeWindowsList(scoped in ImVector<ImGuiWindowPtr> windows, ImU8String label)
    {
        fixed (ImVector<ImGuiWindowPtr>* windowsPtr = &windows)
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeWindowsList(windowsPtr, labelPtr);
        label.Recycle();
    }

    public static void DockBuilderCopyWindowSettings(ImU8String srcName, ImU8String dstName)
    {
        fixed (byte* srcNamePtr = &srcName.GetPinnableNullTerminatedReference())
        fixed (byte* dstNamePtr = &dstName.GetPinnableNullTerminatedReference())
            ImGuiPNative.DockBuilderCopyWindowSettings(srcNamePtr, dstNamePtr);
        srcName.Recycle();
        dstName.Recycle();
    }

    public static void DockBuilderDockWindow(ImU8String windowName, uint nodeId)
    {
        fixed (byte* windowNamePtr = &windowName.GetPinnableNullTerminatedReference())
            ImGuiPNative.DockBuilderDockWindow(windowNamePtr, nodeId);
        windowName.Recycle();
    }

    public static bool DragBehavior(
        uint id, ImGuiDataType dataType, void* pV, float vSpeed, void* pMin, void* pMax, ImU8String format,
        ImGuiSliderFlags flags)
    {
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.DragBehavior(id, dataType, pV, vSpeed, pMin, pMax, formatPtr, flags) != 0;
            format.Recycle();
            return r;
        }
    }

    public static ImGuiWindowSettingsPtr FindOrCreateWindowSettings(ImU8String name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.FindOrCreateWindowSettings(namePtr);
            name.Recycle();
            return r;
        }
    }

    public static ImGuiSettingsHandlerPtr FindSettingsHandler(ImU8String typeName)
    {
        fixed (byte* typeNamePtr = &typeName.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.FindSettingsHandler(typeNamePtr);
            typeName.Recycle();
            return r;
        }
    }

    public static ImGuiWindowPtr FindWindowByName(ImU8String name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.FindWindowByName(namePtr);
            name.Recycle();
            return r;
        }
    }

    public static uint GetColumnsID(ImU8String strId, int count)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.GetColumnsID(strIdPtr, count);
            strId.Recycle();
            return r;
        }
    }

    public static uint GetIDWithSeed(ImU8String strId, uint seed)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.GetIDWithSeed(strIdPtr, strIdPtr + strId.Length, seed);
            strId.Recycle();
            return r;
        }
    }

    public static void* ImFileLoadToMemory(
        ImU8String filename, ImU8String mode, out nuint outFileSize, int paddingBytes = 0)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        fixed (byte* modePtr = &mode.GetPinnableNullTerminatedReference())
        fixed (nuint* outFileSizePtr = &outFileSize)
        {
            var r = ImGuiPNative.ImFileLoadToMemory(filenamePtr, modePtr, outFileSizePtr, paddingBytes);
            filename.Recycle();
            mode.Recycle();
            return r;
        }
    }

    public static void* ImFileLoadToMemory(ImU8String filename, ImU8String mode, int paddingBytes = 0)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        fixed (byte* modePtr = &mode.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ImFileLoadToMemory(filenamePtr, modePtr, null, paddingBytes);
            filename.Recycle();
            mode.Recycle();
            return r;
        }
    }

    public static ImFileHandle ImFileOpen(ImU8String filename, ImU8String mode)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        fixed (byte* modePtr = &mode.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ImFileOpen(filenamePtr, modePtr);
            filename.Recycle();
            mode.Recycle();
            return r;
        }
    }

    public static void ImFontAtlasBuildMultiplyRectAlpha8(
        ReadOnlySpan<byte> table, ReadOnlySpan<byte> pixels, int x, int y, int w, int h, int stride)
    {
        fixed (byte* tablePtr = table)
        fixed (byte* pixelsPtr = pixels)
            ImGuiPNative.ImFontAtlasBuildMultiplyRectAlpha8(tablePtr, pixelsPtr, x, y, w, h, stride);
    }

    public static void ImFontAtlasBuildRender32bppRectFromString(
        ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, ReadOnlySpan<byte> inStr, byte inMarkerChar,
        uint inMarkerPixelValue)
    {
        fixed (byte* inStrPtr = inStr)
        {
            ImGuiPNative.ImFontAtlasBuildRender32bppRectFromString(
                atlas,
                textureIndex,
                x,
                y,
                w,
                h,
                inStrPtr,
                inMarkerChar,
                inMarkerPixelValue);
        }
    }

    public static void ImFontAtlasBuildRender8bppRectFromString(
        ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, ReadOnlySpan<byte> inStr, byte inMarkerChar,
        byte inMarkerPixelValue)
    {
        fixed (byte* inStrPtr = inStr)
        {
            ImGuiPNative.ImFontAtlasBuildRender8bppRectFromString(
                atlas,
                textureIndex,
                x,
                y,
                w,
                h,
                inStrPtr,
                inMarkerChar,
                inMarkerPixelValue);
        }
    }

    public static void ImFormatStringToTempBuffer(byte** outBuf, byte** outBufEnd, ImU8String fmt)
    {
        fixed (byte* fmtPtr = &fmt.GetPinnableNullTerminatedReference())
            ImGuiPNative.ImFormatStringToTempBuffer(outBuf, outBufEnd, fmtPtr);
        fmt.Recycle();
    }

    public static ImGuiWindowPtr ImGuiWindow(ImGuiContextPtr context, ImU8String name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ImGuiWindow(context, namePtr);
            name.Recycle();
            return r;
        }
    }

    // public static byte* ImParseFormatFindEnd(byte* format)
    // public static byte* ImParseFormatFindStart(byte* format)
    // public static int ImParseFormatPrecision(byte* format, int defaultValue)
    // public static byte* ImStrchrRange(byte* strBegin, byte* strEnd, byte c)
    // public static byte* ImStrdup(byte* str)
    // public static byte* ImStrdupcpy(byte* dst, nuint* pDstSize, byte* str)
    // public static int ImStricmp(byte* str1, byte* str2)
    // public static byte* ImStristr(byte* haystack, byte* haystackEnd, byte* needle, byte* needleEnd)
    // public static int ImStrlenW(ushort* str)
    // public static void ImStrncpy(byte* dst, byte* src, nuint count)
    // public static int ImStrnicmp(byte* str1, byte* str2, nuint count)
    // public static byte* ImStrSkipBlank(byte* str)
    // public static void ImStrTrimBlanks(byte* str)
    // public static int ImTextCharFromUtf8(uint* outChar, byte* inText, byte* inTextEnd)
    // public static int ImTextCountCharsFromUtf8(byte* inText, byte* inTextEnd)
    // public static int ImTextCountUtf8BytesFromChar(byte* inText, byte* inTextEnd)

    public static void LogSetNextTextDecoration(byte* prefix, byte* suffix) =>
        ImGuiPNative.LogSetNextTextDecoration(prefix, suffix);

    public static bool MenuItemEx(
        ImU8String label, ImU8String icon = default, ImU8String shortcut = default, bool selected = false,
        bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* iconPtr = &icon.GetPinnableNullTerminatedReference())
        fixed (byte* shortcutPtr = &shortcut.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.MenuItemEx(
                        labelPtr,
                        iconPtr,
                        shortcutPtr,
                        selected ? (byte)1 : (byte)0,
                        enabled ? (byte)1 : (byte)0) != 0;
            label.Recycle();
            icon.Recycle();
            shortcut.Recycle();
            return r;
        }
    }

    public static void RemoveSettingsHandler(ImU8String typeName)
    {
        fixed (byte* typeNamePtr = &typeName.GetPinnableNullTerminatedReference())
            ImGuiPNative.RemoveSettingsHandler(typeNamePtr);
        typeName.Recycle();
    }

    public static bool SliderBehavior<T>(
        ImRect bb, uint id, ImGuiDataType dataType, scoped ref T value, T min, T max, ImU8String format,
        ImGuiSliderFlags flags, ImRectPtr outGrabBb)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* valuePtr = &value)
        {
            var r = ImGuiPNative.SliderBehavior(
                        bb,
                        id,
                        dataType,
                        valuePtr,
                        &min,
                        &max,
                        formatPtr,
                        flags,
                        outGrabBb) != 0;
            format.Recycle();
            return r;
        }
    }

    public static Vector2 TabItemCalcSize(ImU8String label, bool hasCloseButton)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            Vector2 v;
            ImGuiPNative.TabItemCalcSize(&v, labelPtr, hasCloseButton ? (byte)1 : (byte)0);
            return v;
        }
    }

    public static bool TabItemEx(
        ImGuiTabBarPtr tabBar, ImU8String label, ref bool open, ImGuiTabItemFlags flags, ImGuiWindowPtr dockedWindow)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* openPtr = &open)
        {
            var r = ImGuiPNative.TabItemEx(tabBar, labelPtr, openPtr, flags, dockedWindow) != 0;
            label.Recycle();
            return r;
        }
    }

    public static void TabItemLabelAndCloseButton(
        ImDrawListPtr drawList, ImRect bb, ImGuiTabItemFlags flags, Vector2 framePadding, ImU8String label, uint tabId,
        uint closeButtonId, bool isContentsVisible, out bool justClosed, out bool textClipped)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* justClosedPtr = &justClosed)
        fixed (bool* textClippedPtr = &textClipped)
        {
            ImGuiPNative.TabItemLabelAndCloseButton(
                drawList,
                bb,
                flags,
                framePadding,
                labelPtr,
                tabId,
                closeButtonId,
                isContentsVisible ? (byte)1 : (byte)0,
                justClosedPtr,
                textClippedPtr);
        }

        label.Recycle();
    }

    public static bool TempInputScalar<T>(
        ImRect bb, uint id, ImU8String label, ImGuiDataType dataType, scoped ref T data, ImU8String format, T min,
        T max)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = &data)
        {
            var r = ImGuiPNative.TempInputScalar(bb, id, labelPtr, dataType, dataPtr, formatPtr, &min, &max) != 0;
            label.Recycle();
            return r;
        }
    }

    public static bool TreeNodeBehavior(uint id, ImGuiTreeNodeFlags flags, ImU8String label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.TreeNodeBehavior(id, flags, labelPtr, labelPtr + label.Length) != 0;
            label.Recycle();
            return r;
        }
    }
}
