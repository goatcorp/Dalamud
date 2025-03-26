using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

using Dalamud.Utility;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.DragDrop;

/// <summary> Implements the IDropTarget interface to interact with external drag and dropping. </summary>
internal partial class DragDropManager : DragDropManager.IDropTarget
{
    private int lastUpdateFrame = -1;
    private DragDropInterop.ModifierKeys lastKeyState = DragDropInterop.ModifierKeys.MK_NONE;

    /// <summary> Create the drag and drop formats we accept. </summary>
    private FORMATETC formatEtc =
        new()
        {
            cfFormat = (short)DragDropInterop.ClipboardFormat.CF_HDROP,
            ptd = nint.Zero,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            tymed = TYMED.TYMED_HGLOBAL,
        };

    /// <summary>
    /// Invoked whenever a drag and drop process drags files into any FFXIV-related viewport.
    /// </summary>
    /// <param name="pDataObj"> The drag and drop data. </param>
    /// <param name="grfKeyState"> The mouse button used to drag as well as key modifiers. </param>
    /// <param name="pt"> The global cursor position. </param>
    /// <param name="pdwEffect"> Effects that can be used with this drag and drop process. </param>
    public void DragEnter(IDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect)
    {
        this.IsDragging = true;
        this.lastKeyState = UpdateIo((DragDropInterop.ModifierKeys)grfKeyState, true);

        if (pDataObj.QueryGetData(ref this.formatEtc) != 0)
        {
            pdwEffect = 0;
        }
        else
        {
            pdwEffect &= (uint)DragDropInterop.DropEffects.Copy;
            (this.Files, this.Directories) = this.GetPaths(pDataObj);
            this.Extensions = this.Files.Select(Path.GetExtension).Where(p => !p.IsNullOrEmpty()).Distinct().ToHashSet();
        }

        Log.Debug("[DragDrop] Entering external Drag and Drop with {KeyState} at {PtX}, {PtY} and with {N} files.", (DragDropInterop.ModifierKeys)grfKeyState, pt.X, pt.Y, this.Files.Count + this.Directories.Count);
    }

    /// <summary> Invoked every windows update-frame as long as the drag and drop process keeps hovering over an FFXIV-related viewport. </summary>
    /// <param name="grfKeyState"> The mouse button used to drag as well as key modifiers. </param>
    /// <param name="pt"> The global cursor position. </param>
    /// <param name="pdwEffect"> Effects that can be used with this drag and drop process. </param>
    /// <remarks> Can be invoked more often than once a XIV frame, so we are keeping track of frames to skip unnecessary updates. </remarks>
    public void DragOver(uint grfKeyState, POINTL pt, ref uint pdwEffect)
    {
        var frame = ImGui.GetFrameCount();
        if (frame != this.lastUpdateFrame)
        {
            this.lastUpdateFrame = frame;
            this.lastKeyState = UpdateIo((DragDropInterop.ModifierKeys)grfKeyState, false);
            pdwEffect &= (uint)DragDropInterop.DropEffects.Copy;
            Log.Verbose("[DragDrop] External Drag and Drop with {KeyState} at {PtX}, {PtY}.", (DragDropInterop.ModifierKeys)grfKeyState, pt.X, pt.Y);
        }
    }

    /// <summary> Invoked whenever a drag and drop process that hovered over any FFXIV-related viewport leaves all FFXIV-related viewports. </summary>
    public void DragLeave()
    {
        this.IsDragging = false;
        this.Files = Array.Empty<string>();
        this.Directories = Array.Empty<string>();
        this.Extensions = new HashSet<string>();
        MouseDrop(this.lastKeyState);
        Log.Debug("[DragDrop] Leaving external Drag and Drop.");
    }

    /// <summary> Invoked whenever a drag process ends by dropping over any FFXIV-related viewport. </summary>
    /// <param name="pDataObj"> The drag and drop data. </param>
    /// <param name="grfKeyState"> The mouse button used to drag as well as key modifiers. </param>
    /// <param name="pt"> The global cursor position. </param>
    /// <param name="pdwEffect"> Effects that can be used with this drag and drop process. </param>
    public void Drop(IDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect)
    {
        MouseDrop(this.lastKeyState);
        this.lastDropFrame = ImGui.GetFrameCount();
        this.IsDragging = false;
        if (this.HasPaths)
        {
            pdwEffect &= (uint)DragDropInterop.DropEffects.Copy;
        }
        else
        {
            pdwEffect = 0;
        }

        Log.Debug("[DragDrop] Dropping {N} files with {KeyState} at {PtX}, {PtY}.", this.Files.Count + this.Directories.Count, (DragDropInterop.ModifierKeys)grfKeyState, pt.X, pt.Y);
    }

    private static DragDropInterop.ModifierKeys UpdateIo(DragDropInterop.ModifierKeys keys, bool entering)
    {
        var io = ImGui.GetIO();
        void UpdateMouse(int mouseIdx)
        {
            if (entering)
            {
                io.MouseDownDuration[mouseIdx] = 1f;
            }

            io.MouseDown[mouseIdx] = true;
            io.AddMouseButtonEvent(mouseIdx, true);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_LBUTTON))
        {
            UpdateMouse(0);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_RBUTTON))
        {
            UpdateMouse(1);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_MBUTTON))
        {
            UpdateMouse(2);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_CONTROL))
        {
            io.KeyCtrl = true;
            io.AddKeyEvent(ImGuiKey.LeftCtrl, true);
        }
        else
        {
            io.KeyCtrl = false;
            io.AddKeyEvent(ImGuiKey.LeftCtrl, false);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_ALT))
        {
            io.KeyAlt = true;
            io.AddKeyEvent(ImGuiKey.LeftAlt, true);
        }
        else
        {
            io.KeyAlt = false;
            io.AddKeyEvent(ImGuiKey.LeftAlt, false);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_SHIFT))
        {
            io.KeyShift = true;
            io.AddKeyEvent(ImGuiKey.LeftShift, true);
        }
        else
        {
            io.KeyShift = false;
            io.AddKeyEvent(ImGuiKey.LeftShift, false);
        }

        return keys;
    }

    private static void MouseDrop(DragDropInterop.ModifierKeys keys)
    {
        var io = ImGui.GetIO();
        void UpdateMouse(int mouseIdx)
        {
            io.AddMouseButtonEvent(mouseIdx, false);
            io.MouseDown[mouseIdx] = false;
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_LBUTTON))
        {
            UpdateMouse(0);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_RBUTTON))
        {
            UpdateMouse(1);
        }

        if (keys.HasFlag(DragDropInterop.ModifierKeys.MK_MBUTTON))
        {
            UpdateMouse(2);
        }
    }

    private (string[] Files, string[] Directories) GetPaths(IDataObject data)
    {
        if (!this.IsDragging)
        {
            return (Array.Empty<string>(), Array.Empty<string>());
        }

        try
        {
            data.GetData(ref this.formatEtc, out var stgMedium);
            var numFiles = DragDropInterop.DragQueryFileW(stgMedium.unionmember, uint.MaxValue, new StringBuilder(), 0);
            var files = new string[numFiles];
            var sb = new StringBuilder(1024);
            var directoryCount = 0;
            var fileCount = 0;
            for (var i = 0u; i < numFiles; ++i)
            {
                sb.Clear();
                var ret = DragDropInterop.DragQueryFileW(stgMedium.unionmember, i, sb, sb.Capacity);
                if (ret >= sb.Capacity)
                {
                    sb.Capacity = ret + 1;
                    ret = DragDropInterop.DragQueryFileW(stgMedium.unionmember, i, sb, sb.Capacity);
                }

                if (ret > 0 && ret < sb.Capacity)
                {
                    var s = sb.ToString();
                    if (Directory.Exists(s))
                    {
                        files[^(++directoryCount)] = s;
                    }
                    else
                    {
                        files[fileCount++] = s;
                    }
                }
            }

            var fileArray = fileCount > 0 ? files.Take(fileCount).ToArray() : Array.Empty<string>();
            var directoryArray = directoryCount > 0 ? files.TakeLast(directoryCount).Reverse().ToArray() : Array.Empty<string>();

            return (fileArray, directoryArray);
        }
        catch (Exception ex)
        {
            Log.Error($"Error obtaining data from drag & drop:\n{ex}");
        }

        return (Array.Empty<string>(), Array.Empty<string>());
    }
}
