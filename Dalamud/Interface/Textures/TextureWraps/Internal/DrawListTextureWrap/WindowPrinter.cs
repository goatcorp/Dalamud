using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using ImGuiNET;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <inheritdoc cref="IDrawListTextureWrap"/>
internal sealed unsafe partial class DrawListTextureWrap
{
    /// <inheritdoc/>
    public void ResizeAndDrawWindow(ReadOnlySpan<char> windowName, Vector2 scale)
    {
        ref var window = ref ImGuiWindow.FindWindowByName(windowName);
        if (Unsafe.IsNullRef(ref window))
            throw new ArgumentException("Window not found", nameof(windowName));

        this.Size = window.Size;

        var numDrawList = CountDrawList(ref window);
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
        AddWindowToDrawData(ref window, ref drawLists);
        for (var i = 0; i < numDrawList; i++)
        {
            drawData.TotalVtxCount += drawData.CmdLists[i]->VtxBuffer.Size;
            drawData.TotalIdxCount += drawData.CmdLists[i]->IdxBuffer.Size;
        }

        this.Draw(drawData);

        return;

        static bool IsWindowActiveAndVisible(scoped in ImGuiWindow window) =>
            window.Active != 0 && window.Hidden == 0;

        static void AddWindowToDrawData(scoped ref ImGuiWindow window, ref ImDrawList** wptr)
        {
            switch (window.DrawList.CmdBuffer.Size)
            {
                case 0:
                case 1 when window.DrawList.CmdBuffer[0].ElemCount == 0 &&
                            window.DrawList.CmdBuffer[0].UserCallback == 0:
                    break;
                default:
                    *wptr++ = window.DrawList;
                    break;
            }

            for (var i = 0; i < window.DC.ChildWindows.Size; i++)
            {
                ref var child = ref *(ImGuiWindow*)window.DC.ChildWindows[i];
                if (IsWindowActiveAndVisible(in child)) // Clipped children may have been marked not active
                    AddWindowToDrawData(ref child, ref wptr);
            }
        }

        static int CountDrawList(scoped ref ImGuiWindow window)
        {
            var res = window.DrawList.CmdBuffer.Size switch
            {
                0 => 0,
                1 when window.DrawList.CmdBuffer[0].ElemCount == 0 &&
                       window.DrawList.CmdBuffer[0].UserCallback == 0 => 0,
                _ => 1,
            };
            for (var i = 0; i < window.DC.ChildWindows.Size; i++)
                res += CountDrawList(ref *(ImGuiWindow*)window.DC.ChildWindows[i]);
            return res;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x448)]
    private struct ImGuiWindow
    {
        [FieldOffset(0x048)]
        public Vector2 Pos;

        [FieldOffset(0x050)]
        public Vector2 Size;

        [FieldOffset(0x0CB)]
        public byte Active;

        [FieldOffset(0x0D2)]
        public byte Hidden;

        [FieldOffset(0x118)]
        public ImGuiWindowTempData DC;

        [FieldOffset(0x2C0)]
        public ImDrawListPtr DrawList;

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable SA1300
        public static extern ImGuiWindow* igCustom_FindWindowByName(byte* inherit);
#pragma warning restore SA1300

        public static ref ImGuiWindow FindWindowByName(ReadOnlySpan<char> name)
        {
            var nb = Encoding.UTF8.GetByteCount(name);
            var buf = stackalloc byte[nb + 1];
            buf[Encoding.UTF8.GetBytes(name, new(buf, nb))] = 0;

            return ref *igCustom_FindWindowByName(buf);
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xF0)]
        public struct ImGuiWindowTempData
        {
            [FieldOffset(0x98)]
            public ImVector<nint> ChildWindows;
        }
    }
}
