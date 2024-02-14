using System.Diagnostics;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Forces ImGui::RenderWindowDecorations to use the default font.
/// Fixes dock node draw using shared data across different draw lists.
/// TODO: figure out how to synchronize ImDrawList::_Data and ImDrawList::Push/PopTextureID across different instances.
/// It might be better to just special-case that particular function,
/// as no other code touches ImDrawList that is irrelevant to the global shared state,
/// with the exception of Dock... functions which are called from ImGui::NewFrame,
/// which are guaranteed to use the global default font.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class ImGuiRenderWindowDecorationsForceFont : IServiceType, IDisposable
{
    private const int CImGuiRenderWindowDecorationsOffset = 0x461B0;
    private const int CImGuiWindowDockIsActiveOffset = 0x401;

    private readonly Hook<ImGuiRenderWindowDecorationsDelegate> hook;

    [ServiceManager.ServiceConstructor]
    private ImGuiRenderWindowDecorationsForceFont(InterfaceManager.InterfaceManagerWithScene imws)
    {
        // Effectively waiting for ImGui to become available.
        Debug.Assert(ImGuiHelpers.IsImGuiInitialized, "IMWS initialized but IsImGuiInitialized is false?");

        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        this.hook = Hook<ImGuiRenderWindowDecorationsDelegate>.FromAddress(
            cimgui + CImGuiRenderWindowDecorationsOffset,
            this.ImGuiRenderWindowDecorationsDetour);
        this.hook.Enable();
    }

    private delegate void ImGuiRenderWindowDecorationsDelegate(
        nint window,
        nint titleBarRectPtr,
        byte titleBarIsHighlight,
        byte handleBordersAndResizeGrips,
        int resizeGripCount,
        nint resizeGripColPtr,
        float resizeGripDrawSize);

    /// <inheritdoc/>
    public void Dispose() => this.hook.Dispose();

    private unsafe void ImGuiRenderWindowDecorationsDetour(
        nint window,
        nint titleBarRectPtr,
        byte titleBarIsHighlight,
        byte handleBordersAndResizeGrips,
        int resizeGripCount,
        nint resizeGripColPtr,
        float resizeGripDrawSize)
    {
        using (
            ((byte*)window)![CImGuiWindowDockIsActiveOffset] != 0
                ? Service<InterfaceManager>.Get().DefaultFontHandle?.Push()
                : null)
        {
            this.hook.Original(
                window,
                titleBarRectPtr,
                titleBarIsHighlight,
                handleBordersAndResizeGrips,
                resizeGripCount,
                resizeGripColPtr,
                resizeGripDrawSize);
        }
    }
}
