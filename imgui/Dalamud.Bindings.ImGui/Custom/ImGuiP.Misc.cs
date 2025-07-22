using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public unsafe partial class ImGuiP
{
    public static bool ArrowButtonEx(
        Utf8Buffer strId, ImGuiDir dir, Vector2 sizeArg, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ArrowButtonEx(strIdPtr, dir, sizeArg, flags) != 0;
            strId.Dispose();
            return r;
        }
    }

    public static bool BeginChildEx(Utf8Buffer name, uint id, Vector2 sizeArg, bool border, ImGuiWindowFlags flags)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginChildEx(namePtr, id, sizeArg, border ? (byte)1 : (byte)0, flags) != 0;
            name.Dispose();
            return r;
        }
    }

    public static void BeginColumns(Utf8Buffer strId, int count, ImGuiOldColumnFlags flags = ImGuiOldColumnFlags.None)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
            ImGuiPNative.BeginColumns(strIdPtr, count, flags);
        strId.Dispose();
    }

    public static bool BeginMenuEx(Utf8Buffer label, Utf8Buffer icon = default, bool enabled = true)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* iconPtr = &icon.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginMenuEx(labelPtr, iconPtr, enabled ? (byte)1 : (byte)0) != 0;
            label.Dispose();
            icon.Dispose();
            return r;
        }
    }

    public static bool BeginTableEx(
        Utf8Buffer name, uint id, int columnsCount, ImGuiTableFlags flags = ImGuiTableFlags.None,
        Vector2 outerSize = default, float innerWidth = 0.0f)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginTableEx(namePtr, id, columnsCount, flags, outerSize, innerWidth) != 0;
            name.Dispose();
            return r;
        }
    }

    public static bool BeginViewportSideBar(
        Utf8Buffer name, ImGuiViewportPtr viewport, ImGuiDir dir, float size, ImGuiWindowFlags windowFlags)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.BeginViewportSideBar(namePtr, viewport, dir, size, windowFlags) != 0;
            name.Dispose();
            return r;
        }
    }

    public static bool ButtonEx(
        Utf8Buffer label, Vector2 sizeArg = default, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ButtonEx(labelPtr, sizeArg, flags) != 0;
            label.Dispose();
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

    public static void ColorTooltip(Utf8Buffer text, ReadOnlySpan<float> col, ImGuiColorEditFlags flags)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
        fixed (float* colPtr = col)
            ImGuiPNative.ColorTooltip(textPtr, colPtr, flags);
        text.Dispose();
    }

    public static ImGuiWindowSettingsPtr CreateNewWindowSettings(Utf8Buffer name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.CreateNewWindowSettings(namePtr);
            name.Dispose();
            return r;
        }
    }

    public static void Custom_StbTextMakeUndoReplace(
        ImGuiInputTextStatePtr str, int where, int oldLength, int newLength) =>
        ImGuiPNative.Custom_StbTextMakeUndoReplace(str, where, oldLength, newLength);

    public static void Custom_StbTextUndo(ImGuiInputTextStatePtr str) => ImGuiPNative.Custom_StbTextUndo(str);

    public static bool DataTypeApplyFromText<T>(Utf8Buffer buf, ImGuiDataType dataType, T data, Utf8Buffer format)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* bufPtr = &buf.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.DataTypeApplyFromText(bufPtr, dataType, &data, formatPtr) != 0;
            format.Dispose();
            buf.Dispose();
            return r;
        }
    }

    public static void DebugNodeDockNode(ImGuiDockNodePtr node, Utf8Buffer label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeDockNode(node, labelPtr);
        label.Dispose();
    }

    public static void DebugNodeDrawList(
        ImGuiWindowPtr window, ImGuiViewportPPtr viewport, ImDrawListPtr drawList, Utf8Buffer label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeDrawList(window, viewport, drawList, labelPtr);
        label.Dispose();
    }

    public static void DebugNodeStorage(ImGuiStoragePtr storage, Utf8Buffer label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeStorage(storage, labelPtr);
        label.Dispose();
    }

    public static void DebugNodeTabBar(ImGuiTabBarPtr tabBar, Utf8Buffer label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeTabBar(tabBar, labelPtr);
        label.Dispose();
    }

    public static void DebugNodeWindow(ImGuiWindowPtr window, Utf8Buffer label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeWindow(window, labelPtr);
        label.Dispose();
    }

    public static void DebugNodeWindowsList(scoped in ImVector<ImGuiWindowPtr> windows, Utf8Buffer label)
    {
        fixed (ImVector<ImGuiWindowPtr>* windowsPtr = &windows)
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
            ImGuiPNative.DebugNodeWindowsList(windowsPtr, labelPtr);
        label.Dispose();
    }

    public static void DockBuilderCopyWindowSettings(Utf8Buffer srcName, Utf8Buffer dstName)
    {
        fixed (byte* srcNamePtr = &srcName.GetPinnableNullTerminatedReference())
        fixed (byte* dstNamePtr = &dstName.GetPinnableNullTerminatedReference())
            ImGuiPNative.DockBuilderCopyWindowSettings(srcNamePtr, dstNamePtr);
        srcName.Dispose();
        dstName.Dispose();
    }

    public static void DockBuilderDockWindow(Utf8Buffer windowName, uint nodeId)
    {
        fixed (byte* windowNamePtr = &windowName.GetPinnableNullTerminatedReference())
            ImGuiPNative.DockBuilderDockWindow(windowNamePtr, nodeId);
        windowName.Dispose();
    }

    public static bool DragBehavior(
        uint id, ImGuiDataType dataType, void* pV, float vSpeed, void* pMin, void* pMax, Utf8Buffer format,
        ImGuiSliderFlags flags)
    {
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.DragBehavior(id, dataType, pV, vSpeed, pMin, pMax, formatPtr, flags) != 0;
            format.Dispose();
            return r;
        }
    }

    public static ImGuiWindowSettingsPtr FindOrCreateWindowSettings(Utf8Buffer name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.FindOrCreateWindowSettings(namePtr);
            name.Dispose();
            return r;
        }
    }

    public static ImGuiSettingsHandlerPtr FindSettingsHandler(Utf8Buffer typeName)
    {
        fixed (byte* typeNamePtr = &typeName.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.FindSettingsHandler(typeNamePtr);
            typeName.Dispose();
            return r;
        }
    }

    public static ImGuiWindowPtr FindWindowByName(Utf8Buffer name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.FindWindowByName(namePtr);
            name.Dispose();
            return r;
        }
    }

    public static uint GetColumnsID(Utf8Buffer strId, int count)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.GetColumnsID(strIdPtr, count);
            strId.Dispose();
            return r;
        }
    }

    public static uint GetIDWithSeed(Utf8Buffer strId, uint seed)
    {
        fixed (byte* strIdPtr = &strId.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.GetIDWithSeed(strIdPtr, strIdPtr + strId.Length, seed);
            strId.Dispose();
            return r;
        }
    }

    public static void* ImFileLoadToMemory(
        Utf8Buffer filename, Utf8Buffer mode, out nuint outFileSize, int paddingBytes = 0)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        fixed (byte* modePtr = &mode.GetPinnableNullTerminatedReference())
        fixed (nuint* outFileSizePtr = &outFileSize)
        {
            var r = ImGuiPNative.ImFileLoadToMemory(filenamePtr, modePtr, outFileSizePtr, paddingBytes);
            filename.Dispose();
            mode.Dispose();
            return r;
        }
    }

    public static void* ImFileLoadToMemory(Utf8Buffer filename, Utf8Buffer mode, int paddingBytes = 0)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        fixed (byte* modePtr = &mode.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ImFileLoadToMemory(filenamePtr, modePtr, null, paddingBytes);
            filename.Dispose();
            mode.Dispose();
            return r;
        }
    }

    public static ImFileHandle ImFileOpen(Utf8Buffer filename, Utf8Buffer mode)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        fixed (byte* modePtr = &mode.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ImFileOpen(filenamePtr, modePtr);
            filename.Dispose();
            mode.Dispose();
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

    public static void ImFormatStringToTempBuffer(byte** outBuf, byte** outBufEnd, Utf8Buffer fmt)
    {
        fixed (byte* fmtPtr = &fmt.GetPinnableNullTerminatedReference())
            ImGuiPNative.ImFormatStringToTempBuffer(outBuf, outBufEnd, fmtPtr);
        fmt.Dispose();
    }

    public static ImGuiWindowPtr ImGuiWindow(ImGuiContextPtr context, Utf8Buffer name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.ImGuiWindow(context, namePtr);
            name.Dispose();
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
        Utf8Buffer label, Utf8Buffer icon = default, Utf8Buffer shortcut = default, bool selected = false,
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
            label.Dispose();
            icon.Dispose();
            shortcut.Dispose();
            return r;
        }
    }

    public static void RemoveSettingsHandler(Utf8Buffer typeName)
    {
        fixed (byte* typeNamePtr = &typeName.GetPinnableNullTerminatedReference())
            ImGuiPNative.RemoveSettingsHandler(typeNamePtr);
        typeName.Dispose();
    }

    public static bool SliderBehavior<T>(
        ImRect bb, uint id, ImGuiDataType dataType, scoped ref T value, T min, T max, Utf8Buffer format,
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
            format.Dispose();
            return r;
        }
    }

    public static Vector2 TabItemCalcSize(Utf8Buffer label, bool hasCloseButton)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            Vector2 v;
            ImGuiPNative.TabItemCalcSize(&v, labelPtr, hasCloseButton ? (byte)1 : (byte)0);
            return v;
        }
    }

    public static bool TabItemEx(
        ImGuiTabBarPtr tabBar, Utf8Buffer label, ref bool open, ImGuiTabItemFlags flags, ImGuiWindowPtr dockedWindow)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (bool* openPtr = &open)
        {
            var r = ImGuiPNative.TabItemEx(tabBar, labelPtr, openPtr, flags, dockedWindow) != 0;
            label.Dispose();
            return r;
        }
    }

    public static void TabItemLabelAndCloseButton(
        ImDrawListPtr drawList, ImRect bb, ImGuiTabItemFlags flags, Vector2 framePadding, Utf8Buffer label, uint tabId,
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

        label.Dispose();
    }

    public static bool TempInputScalar<T>(
        ImRect bb, uint id, Utf8Buffer label, ImGuiDataType dataType, scoped ref T data, Utf8Buffer format, T min,
        T max)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = &data)
        {
            var r = ImGuiPNative.TempInputScalar(bb, id, labelPtr, dataType, dataPtr, formatPtr, &min, &max) != 0;
            label.Dispose();
            return r;
        }
    }

    public static bool TreeNodeBehavior(uint id, ImGuiTreeNodeFlags flags, Utf8Buffer label)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiPNative.TreeNodeBehavior(id, flags, labelPtr, labelPtr + label.Length) != 0;
            label.Dispose();
            return r;
        }
    }
}
