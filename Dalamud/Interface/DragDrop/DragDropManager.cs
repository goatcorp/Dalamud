using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Interface.DragDrop;

internal partial class DragDropManager : IDisposable, IDragDropManager
{
    private readonly UiBuilder uiBuilder;

    public DragDropManager(UiBuilder uiBuilder)
        => this.uiBuilder = uiBuilder;

    public void Enable()
    {
        if (this.ServiceAvailable)
        {
            return;
        }

        try
        {
            var ret2 = DragDropInterop.RegisterDragDrop(this.uiBuilder.WindowHandlePtr, this);
            Marshal.ThrowExceptionForHR(ret2);
            this.ServiceAvailable = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Could not create windows drag and drop utility:\n{ex}");
        }
    }

    public void Dispose()
    {
        if (!this.ServiceAvailable)
        {
            return;
        }


    }

    public bool ServiceAvailable { get; internal set; }

    public bool IsDragging { get; private set; }

    public IReadOnlyList<string> Files { get; private set; } = Array.Empty<string>();

    public IReadOnlySet<string> Extensions { get; private set; } = new HashSet<string>();

    public IReadOnlyList<string> Directories { get; private set; } = Array.Empty<string>();

    /// <inheritdoc cref="IDragDropManager.CreateImGuiSource(string, Func{IDragDropManager, bool}, Func{IDragDropManager, bool})"/>
    public void CreateImGuiSource(string label, Func<IDragDropManager, bool> validityCheck, Func<IDragDropManager, bool> tooltipBuilder)
    {
        if (!this.IsDragging && !this.IsDropping()) return;
        if (!validityCheck(this) || !ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceExtern)) return;

        ImGui.SetDragDropPayload(label, nint.Zero, 0);
        if (this.CheckTooltipFrame(out var frame) && tooltipBuilder(this))
        {
            this.lastTooltipFrame = frame;
        }

        ImGui.EndDragDropSource();
    }

    /// <inheritdoc cref="IDragDropManager.CreateImGuiTarget"/>
    public bool CreateImGuiTarget(string label, out IReadOnlyList<string> files, out IReadOnlyList<string> directories)
    {
        files = Array.Empty<string>();
        directories = Array.Empty<string>();
        if (!this.IsDragging || !ImGui.BeginDragDropTarget()) return false;

        unsafe
        {
            if (ImGui.AcceptDragDropPayload(label, ImGuiDragDropFlags.AcceptBeforeDelivery).NativePtr != null && this.IsDropping())
            {
                this.lastDropFrame = -2;
                files = this.Files;
                directories = this.Directories;
                return true;
            }
        }

        ImGui.EndDragDropTarget();
        return false;
    }

    private int lastDropFrame = -2;
    private int lastTooltipFrame = -1;

    private bool CheckTooltipFrame(out int frame)
    {
        frame = ImGui.GetFrameCount();
        return this.lastTooltipFrame < frame;
    }

    private bool IsDropping()
    {
        var frame = ImGui.GetFrameCount();
        return this.lastDropFrame == frame || this.lastDropFrame == frame - 1;
    }
}
