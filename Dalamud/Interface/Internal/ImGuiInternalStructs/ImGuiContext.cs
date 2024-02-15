using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using ImGuiNET;

namespace Dalamud.Interface.Internal.ImGuiInternalStructs;

/// <summary>
/// Offsets to various data in ImGui context.
/// </summary>
/// <remarks>
/// Last updated for ImGui 1.88.
/// </remarks>
[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "See ImGui source code")]
[StructLayout(LayoutKind.Explicit, Size = 0x58A0)]
internal struct ImGuiContext
{
    [FieldOffset(0x3FC0)]
    public ImVector CurrentWindowStack; // ImGuiWindowStackData

    [FieldOffset(0x4190)]
    public ImVector ColorStack; // ImGuiColorMod

    [FieldOffset(0x41A0)]
    public ImVector StyleVarStack; // ImGuiStyleMod

    [FieldOffset(0x41B0)]
    public ImVector<ImFontPtr> FontStack;

    [FieldOffset(0x41C0)]
    public ImVector<uint> FocusScopeStack; // ImGuiID

    [FieldOffset(0x41D0)]
    public ImVector<uint> ItemFlagsStack; // ImGuiItemFlags

    [FieldOffset(0x41E0)]
    public ImVector GroupStack; // ImGuiGroupData

    // Note: not really a "stack"; we probably don't care about this one
    [FieldOffset(0x41F0)]
    public ImVector OpenPopupStack; // ImGuiPopupData

    [FieldOffset(0x4200)]
    public ImVector BeginPopupStack; // ImGuiPopupData

    [FieldOffset(0x4588)]
    public ImGuiInputTextState TextState;

    public static unsafe ImGuiContext* CurrentPtr => (ImGuiContext*)ImGui.GetCurrentContext();

    public static unsafe ref ImGuiContext CurrentRef => ref *CurrentPtr;
}
