using System.Collections.Generic;
using System.Diagnostics;

using Dalamud.Interface.Internal.ImGuiInternalStructs;

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
    /// Compare a snapshot to the current post-draw state and report any errors in a MessageBox dialog.
    /// </summary>
    /// <param name="source">The source of any problems, something to blame.</param>
    /// <param name="before">ImGui context snapshot.</param>
    private static void ReportProblems(string source, in ImGuiContextSnapshot before)
    {
        if (!AssertsEnabled)
        {
            return;
        }

        var cSnap = ImGuiContextSnapshot.TakeCurrent();
        var warnings = default(List<string>);

        if (cSnap.CurrentWindowStackSize != 1)
        {
            warnings = new();
            if (cSnap.CurrentWindowStackSize > 1)
            {
                warnings.Add(
                    $"Mismatched Begin/BeginChild vs End/EndChild calls: did you forget to call End/EndChild?\n\ncSnap.WindowStackSize = {cSnap.CurrentWindowStackSize}");
            }
            else
            {
                warnings.Add(
                    $"Mismatched Begin/BeginChild vs End/EndChild calls: did you call End/EndChild too much?\n\ncSnap.WindowStackSize = {cSnap.CurrentWindowStackSize}");
            }
        }

        if (before.ColorStackSize != cSnap.ColorStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to pop a color!\n\nBefore: {before.ColorStackSize}, after: {cSnap.ColorStackSize}");
        }

        if (before.StyleVarStackSize != cSnap.StyleVarStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to pop a style var!\n\nBefore: {before.StyleVarStackSize}, after: {cSnap.StyleVarStackSize}");
        }

        if (before.FontStackSize != cSnap.FontStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to pop a font!\n\nBefore: {before.FontStackSize}, after: {cSnap.FontStackSize}");
        }

        if (before.FocusScopeStackSize != cSnap.FocusScopeStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to pop a focus scope!\n\nBefore: {before.FocusScopeStackSize}, after: {cSnap.FocusScopeStackSize}");
        }

        if (before.ItemFlagsStackSize != cSnap.ItemFlagsStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to pop an item flag!\n\nBefore: {before.ItemFlagsStackSize}, after: {cSnap.ItemFlagsStackSize}");
        }

        if (before.GroupStackSize != cSnap.GroupStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to pop a group!\n\nBefore: {before.GroupStackSize}, after: {cSnap.GroupStackSize}");
        }

        if (before.BeginPopupStackSize != cSnap.BeginPopupStackSize)
        {
            warnings ??= new();
            warnings.Add(
                $"You forgot to end a popup!\n\nBefore: {before.BeginPopupStackSize}, after: {cSnap.BeginPopupStackSize}");
        }

        if (warnings is not null)
            ShowAssert(source, string.Join("\n", warnings));
    }

    private static void ShowAssert(string source, string message)
    {
        const MessageBoxType flags = MessageBoxType.Ok | MessageBoxType.IconError;
        const string caption = "You fucked up";
        message = $"{message}\n\nSource: {source}\n\nAsserts are now disabled. You may re-enable them.";

        _ = MessageBoxW(Process.GetCurrentProcess().MainWindowHandle, message, caption, flags);
        AssertsEnabled = false;
    }

    /// <summary>
    /// A snapshot of various ImGui context properties.
    /// </summary>
    public struct ImGuiContextSnapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImGuiContextSnapshot"/> struct.
        /// </summary>
        /// <param name="context">The context to capture from.</param>
        public ImGuiContextSnapshot(in ImGuiContext context)
        {
            this.CurrentWindowStackSize = context.CurrentWindowStack.Size;
            this.ColorStackSize = context.ColorStack.Size;
            this.StyleVarStackSize = context.StyleVarStack.Size;
            this.FontStackSize = context.FontStack.Size;
            this.FocusScopeStackSize = context.FocusScopeStack.Size;
            this.ItemFlagsStackSize = context.ItemFlagsStack.Size;
            this.GroupStackSize = context.GroupStack.Size;
            this.BeginPopupStackSize = context.BeginPopupStack.Size;
        }

        /// <summary>
        /// Gets the stack size for <see cref="ImGui.Begin(string)"/>.
        /// </summary>
        public int CurrentWindowStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for <see cref="ImGui.PushStyleColor(ImGuiNET.ImGuiCol,uint)"/>.
        /// </summary>
        public int ColorStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for <see cref="ImGui.PushStyleVar(ImGuiNET.ImGuiStyleVar,float)"/>.
        /// </summary>
        public int StyleVarStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for <see cref="ImGui.PushFont"/>.
        /// </summary>
        public int FontStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for PushFocusScope.
        /// </summary>
        public int FocusScopeStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for PushItemFlags.
        /// </summary>
        public int ItemFlagsStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for <see cref="ImGui.BeginGroup"/>.
        /// </summary>
        public int GroupStackSize { get; init; }

        /// <summary>
        /// Gets the stack size for <see cref="ImGui.BeginPopup(string)"/>.
        /// </summary>
        public int BeginPopupStackSize { get; init; }

        /// <summary>
        /// Takes a snapshot of the current ImGui context.
        /// </summary>
        /// <returns>The snapshot taken.</returns>
        public static ImGuiContextSnapshot TakeCurrent() => new(ImGuiContext.CurrentRef);
    }

    /// <summary>
    /// Calls <see cref="ImGuiManagedAsserts.ReportProblems"/> on dispose.
    /// </summary>
    public struct ScopedSnapshotProblemReporter : IDisposable
    {
        private readonly string source;
        private readonly ImGuiContextSnapshot snapshot;
        private bool cancelled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedSnapshotProblemReporter"/> struct.
        /// </summary>
        /// <param name="source">The source for display.</param>
        public ScopedSnapshotProblemReporter(string source)
        {
            this.source = source;
            this.snapshot = ImGuiContextSnapshot.TakeCurrent();
        }

        /// <summary>
        /// Cancel the reporting.
        /// </summary>
        public void Cancel() => this.cancelled = true;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.cancelled)
                return;

            ReportProblems(this.source, this.snapshot);
        }
    }
}
