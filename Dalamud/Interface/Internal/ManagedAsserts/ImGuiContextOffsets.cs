namespace Dalamud.Interface.Internal.ManagedAsserts;

/// <summary>
/// Offsets to various data in ImGui context.
/// </summary>
/// <remarks>
/// Last updated for ImGui 1.83.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the unsage instead.")]
internal static class ImGuiContextOffsets
{
    public const int CurrentWindowStackOffset = 0x73A;

    public const int ColorStackOffset = 0x79C;

    public const int StyleVarStackOffset = 0x7A0;

    public const int FontStackOffset = 0x7A4;

    public const int BeginPopupStackOffset = 0x7B8;

    public const int TextStateOffset = 0x4588;
}
