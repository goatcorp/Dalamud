using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <inheritdoc cref="IDrawListTextureWrap"/>
internal sealed unsafe partial class DrawListTextureWrap
{
    /// <inheritdoc/>
    public void ResizeAndDrawWindow(ReadOnlySpan<char> windowName, Vector2 scale)
    {
        var window = ImGuiP.FindWindowByName(windowName);
        if (window.IsNull)
            throw new ArgumentException("Window not found", nameof(windowName));

        this.Size = window.Size;

        var numDrawList = CountDrawList(window);
        var drawLists = stackalloc ImDrawList*[numDrawList];
        var drawData = new ImDrawData
        {
            Valid = 1,
            CmdListsCount = numDrawList,
            TotalIdxCount = 0,
            TotalVtxCount = 0,
            CmdLists = drawLists,
            DisplayPos = window.Pos,
            DisplaySize = window.Size,
            FramebufferScale = scale,
        };
        AddWindowToDrawData(window, ref drawLists);
        for (var i = 0; i < numDrawList; i++)
        {
            drawData.TotalVtxCount += drawData.CmdLists[i]->VtxBuffer.Size;
            drawData.TotalIdxCount += drawData.CmdLists[i]->IdxBuffer.Size;
        }

        this.Draw(drawData);

        return;

        static bool IsWindowActiveAndVisible(ImGuiWindowPtr window) => window is { Active: true, Hidden: false };

        static void AddWindowToDrawData(ImGuiWindowPtr window, ref ImDrawList** wptr)
        {
            switch (window.DrawList.CmdBuffer.Size)
            {
                case 0:
                case 1 when window.DrawList.CmdBuffer[0].ElemCount == 0 &&
                            window.DrawList.CmdBuffer[0].UserCallback == null:
                    break;
                default:
                    *wptr++ = window.DrawList;
                    break;
            }

            for (var i = 0; i < window.DC.ChildWindows.Size; i++)
            {
                var child = window.DC.ChildWindows[i];
                if (IsWindowActiveAndVisible(child)) // Clipped children may have been marked not active
                    AddWindowToDrawData(child, ref wptr);
            }
        }

        static int CountDrawList(ImGuiWindowPtr window)
        {
            var res = window.DrawList.CmdBuffer.Size switch
            {
                0 => 0,
                1 when window.DrawList.CmdBuffer[0].ElemCount == 0 &&
                       window.DrawList.CmdBuffer[0].UserCallback == null => 0,
                _ => 1,
            };
            for (var i = 0; i < window.DC.ChildWindows.Size; i++)
                res += CountDrawList(window.DC.ChildWindows[i]);
            return res;
        }
    }
}
