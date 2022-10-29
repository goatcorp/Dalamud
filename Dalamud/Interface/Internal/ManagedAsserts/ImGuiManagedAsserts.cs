using System.Diagnostics;

using ImGuiNET;

using static Dalamud.NativeFunctions;

namespace Dalamud.Interface.Internal.ManagedAsserts;

/// <summary>
/// Report ImGui problems with a MessageBox dialog.
/// </summary>
internal static class ImGuiManagedAsserts
{
    /// <summary>
    /// Gets or sets a value indicating whether asserts are enabled for ImGui.
    /// </summary>
    public static bool AssertsEnabled { get; set; }

    /// <summary>
    /// Create a snapshot of the current ImGui context.
    /// Should be called before rendering an ImGui frame.
    /// </summary>
    /// <returns>A snapshot of the current context.</returns>
    public static unsafe ImGuiContextSnapshot GetSnapshot()
    {
        var contextPtr = ImGui.GetCurrentContext();

        var styleVarStack = *((int*)contextPtr + ImGuiContextOffsets.StyleVarStackOffset);    // ImVector.Size
        var colorStack = *((int*)contextPtr + ImGuiContextOffsets.ColorStackOffset);          // ImVector.Size
        var fontStack = *((int*)contextPtr + ImGuiContextOffsets.FontStackOffset);            // ImVector.Size
        var popupStack = *((int*)contextPtr + ImGuiContextOffsets.BeginPopupStackOffset);     // ImVector.Size
        var windowStack = *((int*)contextPtr + ImGuiContextOffsets.CurrentWindowStackOffset); // ImVector.Size

        return new ImGuiContextSnapshot
        {
            StyleVarStackSize = styleVarStack,
            ColorStackSize = colorStack,
            FontStackSize = fontStack,
            BeginPopupStackSize = popupStack,
            WindowStackSize = windowStack,
        };
    }

    /// <summary>
    /// Compare a snapshot to the current post-draw state and report any errors in a MessageBox dialog.
    /// </summary>
    /// <param name="source">The source of any problems, something to blame.</param>
    /// <param name="before">ImGui context snapshot.</param>
    public static void ReportProblems(string source, ImGuiContextSnapshot before)
    {
        // TODO: Needs to be updated for ImGui 1.88
        return;

#pragma warning disable CS0162
        if (!AssertsEnabled)
        {
            return;
        }

        var cSnap = GetSnapshot();

        if (before.StyleVarStackSize != cSnap.StyleVarStackSize)
        {
            ShowAssert(source, $"You forgot to pop a style var!\n\nBefore: {before.StyleVarStackSize}, after: {cSnap.StyleVarStackSize}");
            return;
        }

        if (before.ColorStackSize != cSnap.ColorStackSize)
        {
            ShowAssert(source, $"You forgot to pop a color!\n\nBefore: {before.ColorStackSize}, after: {cSnap.ColorStackSize}");
            return;
        }

        if (before.FontStackSize != cSnap.FontStackSize)
        {
            ShowAssert(source, $"You forgot to pop a font!\n\nBefore: {before.FontStackSize}, after: {cSnap.FontStackSize}");
            return;
        }

        if (before.BeginPopupStackSize != cSnap.BeginPopupStackSize)
        {
            ShowAssert(source, $"You forgot to end a popup!\n\nBefore: {before.BeginPopupStackSize}, after: {cSnap.BeginPopupStackSize}");
            return;
        }

        if (cSnap.WindowStackSize != 1)
        {
            if (cSnap.WindowStackSize > 1)
            {
                ShowAssert(source, $"Mismatched Begin/BeginChild vs End/EndChild calls: did you forget to call End/EndChild?\n\ncSnap.WindowStackSize = {cSnap.WindowStackSize}");
            }
            else
            {
                ShowAssert(source, $"Mismatched Begin/BeginChild vs End/EndChild calls: did you call End/EndChild too much?\n\ncSnap.WindowStackSize = {cSnap.WindowStackSize}");
            }
        }
#pragma warning restore CS0162
    }

    private static void ShowAssert(string source, string message)
    {
        var caption = $"You fucked up";
        message = $"{message}\n\nSource: {source}\n\nAsserts are now disabled. You may re-enable them.";
        var flags = MessageBoxType.Ok | MessageBoxType.IconError;

        _ = MessageBoxW(Process.GetCurrentProcess().MainWindowHandle, message, caption, flags);
        AssertsEnabled = false;
    }

    /// <summary>
    /// A snapshot of various ImGui context properties.
    /// </summary>
    public class ImGuiContextSnapshot
    {
        /// <summary>
        /// Gets the ImGui style var stack size.
        /// </summary>
        public int StyleVarStackSize { get; init; }

        /// <summary>
        /// Gets the ImGui color stack size.
        /// </summary>
        public int ColorStackSize { get; init; }

        /// <summary>
        /// Gets the ImGui font stack size.
        /// </summary>
        public int FontStackSize { get; init; }

        /// <summary>
        /// Gets the ImGui begin popup stack size.
        /// </summary>
        public int BeginPopupStackSize { get; init; }

        /// <summary>
        /// Gets the ImGui window stack size.
        /// </summary>
        public int WindowStackSize { get; init; }
    }
}
