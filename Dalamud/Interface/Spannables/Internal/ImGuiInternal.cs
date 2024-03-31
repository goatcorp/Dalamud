using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Internal;

/// <summary>A portion of imgui_internal.h.</summary>
internal static class ImGuiInternals
{
    /// <summary>Adds an item. Params: (RectF* boundary, uint id, RectF* navBoundary, flags) -&gt; bool.</summary>
    public static readonly unsafe delegate* unmanaged<RectVector4*, uint, RectVector4*, ImGuiItemFlags, byte>
        ImGuiItemAdd;

    /// <summary>Tests if item is hoverable.</summary>
    public static readonly unsafe delegate* unmanaged<RectVector4*, uint, byte> ImGuiItemHoverable;

    /// <summary>Cancels nav move request.</summary>
    public static readonly unsafe delegate* unmanaged<void> ImGuiNavMoveRequestCancel;

    /// <summary>Sets the active item. Params: (uint id, ImGuiWindow* window).</summary>
    public static readonly unsafe delegate* unmanaged<uint, nint, void> ImGuiSetActiveId;

    /// <summary>Sets the focused item. Params: (uint id, ImGuiWindow* window).</summary>
    public static readonly unsafe delegate* unmanaged<uint, nint, void> ImGuiSetFocusedId;

    /// <summary>Sets the hovered item. Params: (uint id).</summary>
    public static readonly unsafe delegate* unmanaged<uint, void> ImGuiSetHoveredId;

    private const int CImGuiItemAddOffset = 0x3c0a0;
    private const int CImGuiItemHoverable = 0x3c200;
    private const int CImGuiNavMoveRequestCancel = 0x3dc80;
    private const int CImGuiSetActiveIdOffset = 0x483f0;
    private const int CImGuiSetFocusIdOffset = 0x48D40;
    private const int CImGuiSetHoverIdOffset = 0x48e80;

    static unsafe ImGuiInternals()
    {
        _ = ImGui.GetCurrentContext();

        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        ImGuiItemAdd =
            (delegate* unmanaged<RectVector4*, uint, RectVector4*, ImGuiItemFlags, byte>)(cimgui + CImGuiItemAddOffset);
        ImGuiItemHoverable = (delegate* unmanaged<RectVector4*, uint, byte>)(cimgui + CImGuiItemHoverable);
        ImGuiNavMoveRequestCancel = (delegate* unmanaged<void>)(cimgui + CImGuiNavMoveRequestCancel);
        ImGuiSetActiveId = (delegate* unmanaged<uint, nint, void>)(cimgui + CImGuiSetActiveIdOffset);
        ImGuiSetFocusedId = (delegate* unmanaged<uint, nint, void>)(cimgui + CImGuiSetFocusIdOffset);
        ImGuiSetHoveredId = (delegate* unmanaged<uint, void>)(cimgui + CImGuiSetHoverIdOffset);
    }

    /// <summary>Transient per-window flags, reset at the beginning of the frame. For child window, inherited from
    /// parent on first Begin(). This is going to be exposed in imgui.h when stabilized enough.</summary>
    // See imgui_internal.h
    [Flags]
    public enum ImGuiItemFlags
    {
        /// <summary>Nothing.</summary>
        None = 0,

        /// <summary>Disable keyboard tabbing (FIXME: should merge with _NoNav.)</summary>
        NoTabStop = 1 << 0,

        /// <summary>Button() will return true multiple times based on io.KeyRepeatDelay and io.KeyRepeatRate settings.
        /// </summary>
        ButtonRepeat = 1 << 1,

        /// <summary>Disable interactions but doesn't affect visuals. See BeginDisabled()/EndDisabled().
        /// See https://github.com/ocornut/imgui/issues/211.</summary>
        Disabled = 1 << 2,

        /// <summary>Disable keyboard/gamepad directional navigation (FIXME: should merge with _NoTabStop.)</summary>
        NoNav = 1 << 3,

        /// <summary>Disable item being a candidate for default focus (e.g. used by title bar items.)</summary>
        NoNavDefaultFocus = 1 << 4,

        /// <summary>Disable MenuItem/Selectable() automatically closing their popup window.</summary>
        SelectableDontClosePopup = 1 << 5,

        /// <summary>[BETA] Represent a mixed/indeterminate value, generally multi-selection where values differ.
        /// Currently only supported by Checkbox() (later should support all sorts of widgets).</summary>
        MixedValue = 1 << 6,

        /// <summary>[ALPHA] Allow hovering interactions but underlying value is not changed.</summary>
        ReadOnly = 1 << 7,

        /// <summary>[WIP] Auto-activate input mode when tab focused. Currently only used and supported by a few items
        /// before it becomes a generic feature.</summary>
        Inputable = 1 << 8,
    }

    /// <summary>Storage for LastItem data.</summary>
    [Flags]
    public enum ImGuiItemStatusFlags
    {
        /// <summary>Nothing.</summary>
        None = 0,

        /// <summary>Mouse position is within item rectangle (does NOT mean that the window is in correct z-order and
        /// can be hovered!, this is only one part of the most-common IsItemHovered test.)</summary>
        HoveredRect = 1 << 0,

        /// <summary>g.LastItemData.DisplayRect is valid.</summary>
        HasDisplayRect = 1 << 1,

        /// <summary>Value exposed by item was edited in the current frame (should match the bool return value of most
        /// widgets.)</summary>
        Edited = 1 << 2,

        /// <summary>Set when Selectable(), TreeNode() reports toggling a selection. We can't report "Selected", only
        /// state changes, in order to easily handle clipping with less issues.</summary>
        ToggledSelection = 1 << 3,

        /// <summary>Set when TreeNode() reports toggling their open state.</summary>
        ToggledOpen = 1 << 4,

        /// <summary>Set if the widget/group is able to provide data for the ImGuiItemStatusFlags_Deactivated flag.
        /// </summary>
        HasDeactivated = 1 << 5,

        /// <summary>Only valid if ImGuiItemStatusFlags_HasDeactivated is set.</summary>
        Deactivated = 1 << 6,

        /// <summary>Override the HoveredWindow test to allow cross-window hover testing.</summary>
        HoveredWindow = 1 << 7,

        /// <summary>Set when the Focusable item just got focused by Tabbing (FIXME: to be removed soon.)</summary>
        [Obsolete]
        FocusedByTabbing = 1 << 8,
    }

#pragma warning disable SA1600
#pragma warning disable SA1602
    public enum ImGuiInputEventType
    {
        None = 0,
        MousePos,
        MouseWheel,
        MouseButton,
        MouseViewport,
        Key,
        Text,
        Focus,

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "it just is")]
        COUNT,
    }

    public enum ImGuiInputSource
    {
        None = 0,
        Mouse,
        Keyboard,
        Gamepad,
        Clipboard, // Currently only used by InputText()
        Nav, // Stored in g.ActiveIdSource only

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "it just is")]
        COUNT,
    }
#pragma warning restore SA1602
#pragma warning restore SA1600

    /// <summary>Last item data.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiLastItemData
    {
        /// <summary>ID of the item.</summary>
        public uint Id;

        /// <summary>Item flags.</summary>
        public ImGuiItemFlags InFlags;

        /// <summary>Item status flags.</summary>
        public ImGuiItemStatusFlags StatusFlags;

        /// <summary>Full rectangle.</summary>
        public RectVector4 Rect;

        /// <summary>Navigation scoring rectangle (not displayed.)</summary>
        public RectVector4 NavRect;

        /// <summary>Display rectangle (only if <see cref="ImGuiItemStatusFlags.HasDisplayRect"/> is set.)</summary>
        public RectVector4 DisplayRect;
    }

    /// <summary>A portion of ImGuiContext.</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ImGuiContext
    {
        /// <summary>Past input events processed in NewFrame().
        /// This is to allow domain-specific application to access e.g mouse/pen trail.</summary>
        [FieldOffset(0x3900)]
        public ImVector InputEventsTrail;

        /// <summary>Pointer to the currently active ImGui window.</summary>
        [FieldOffset(0x3ff0)]
        public nint CurrentWindow; // type: ImGuiWindow*

        /// <summary>Pointer to the currently hovered ImGui window.</summary>
        [FieldOffset(0x3ff8)]
        public nint HoveredWindow; // type: ImGuiWindow*

        /// <summary>ID of the hovered item.</summary>
        [FieldOffset(0x4030)]
        public uint HoveredId;

        /// <summary>Whether the item specified by <see cref="HoveredId"/> allows overlapping.</summary>
        [FieldOffset(0x4038)]
        public byte HoveredIdAllowOverlap;

        /// <summary>Whether the item specified by <see cref="HoveredId"/> is using the mouse wheel.</summary>
        [FieldOffset(0x4039)]
        public byte HoveredIdUsingMouseWheel;

        /// <summary>Whether the item specified by <see cref="HoveredId"/> is disabled.</summary>
        [FieldOffset(0x403b)]
        public byte HoveredIdDisabled;

        /// <summary>ID of the active item.</summary>
        [FieldOffset(0x4044)]
        public uint ActiveId;

        /// <summary>Whether the item specified by <see cref="ActiveId"/> is using the mouse wheel.</summary>
        [FieldOffset(0x4088)]
        public byte ActiveIdUsingMouseWheel;

        /// <summary>Whether the item specified by <see cref="ActiveId"/> allows overlapping.</summary>
        [FieldOffset(0x4091)]
        public byte ActiveIdAllowOverlap;

        /// <summary>Last item data.</summary>
        [FieldOffset(0x40c0)]
        public ImGuiLastItemData LastItemData;

        /// <summary>ID of the focused item.</summary>
        [FieldOffset(0x4280)]
        public uint NavId;

        /// <summary>Gets the reference of the current instance.</summary>
        public static unsafe ref ImGuiContext Instance => ref *(ImGuiContext*)ImGui.GetCurrentContext();
    }

#pragma warning disable SA1600
#pragma warning disable SA1602
    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventMousePos
    {
        public float PosX;
        public float PosY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventMouseWheel
    {
        public float WheelX;
        public float WheelY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventMouseButton
    {
        public int Button;
        public byte Down;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventMouseViewport
    {
        public uint HoveredViewportID;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventKey
    {
        public ImGuiKey Key;
        public byte Down;
        public float AnalogValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventText
    {
        public uint Char;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImGuiInputEventAppFocused
    {
        public bool Focused;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ImGuiInputEvent
    {
        [FieldOffset(0)]
        public ImGuiInputEventType Type;

        [FieldOffset(4)]
        public ImGuiInputSource Source;

        /// <summary>Valid if Type == ImGuiInputEventType_MousePos.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventMousePos MousePos;

        /// <summary>Valid if Type == ImGuiInputEventType_MouseWheel.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventMouseWheel MouseWheel;

        /// <summary>Valid if Type == ImGuiInputEventType_MouseButton.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventMouseButton MouseButton;

        /// <summary>Valid if Type == ImGuiInputEventType_MouseViewport.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventMouseViewport MouseViewport;

        /// <summary>Valid if Type == ImGuiInputEventType_Key.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventKey Key;

        /// <summary>Valid if Type == ImGuiInputEventType_Text.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventText Text;

        /// <summary>Valid if Type == ImGuiInputEventType_Focus.</summary>
        [FieldOffset(8)]
        public ImGuiInputEventAppFocused AppFocused;

        [FieldOffset(20)]
        public byte AddedByTestEngine;
    }
#pragma warning restore SA1602
#pragma warning restore SA1600
}
