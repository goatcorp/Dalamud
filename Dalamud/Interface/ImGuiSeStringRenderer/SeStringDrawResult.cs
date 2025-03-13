using System.Linq;
using System.Numerics;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Represents the result of a rendered interactable SeString.</summary>
public ref struct SeStringDrawResult
{
    private Payload? lazyPayload;

    /// <summary>Gets the visible size of the text rendered/to be rendered.</summary>
    public Vector2 Size { get; init; }

    /// <summary>Gets a value indicating whether a payload or the whole text has been clicked.</summary>
    public bool Clicked { get; init; }

    /// <summary>Gets the offset of the interacted payload, or <c>-1</c> if none.</summary>
    public int InteractedPayloadOffset { get; init; }

    /// <summary>Gets the interacted payload envelope, or <see cref="ReadOnlySpan{T}.Empty"/> if none.</summary>
    public ReadOnlySpan<byte> InteractedPayloadEnvelope { get; init; }

    /// <summary>Gets the interacted payload, or <c>null</c> if none.</summary>
    public Payload? InteractedPayload =>
        this.lazyPayload ??=
            this.InteractedPayloadEnvelope.IsEmpty
                ? default
                : SeString.Parse(this.InteractedPayloadEnvelope).Payloads.FirstOrDefault();
}
