using System.Diagnostics.CodeAnalysis;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Draw event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableDrawEventArgs : SpannableEventArgs
{
    /// <summary>Gets the draw arguments from the spannable invoker.</summary>
    public ImDrawListPtr DrawListPtr { get; private set; }

    /// <summary>Initializes the drawing properties.</summary>
    /// <param name="drawListPtr">Pointer to a draw list.</param>
    public void InitializeDrawEvent(ImDrawListPtr drawListPtr) => this.DrawListPtr = drawListPtr;
}
