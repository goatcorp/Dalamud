using Dalamud.Configuration.Internal;
using Dalamud.Game.Chat;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Text;

/// <summary>
/// This interface represents a single chat message sent from a plugin.
/// </summary>
public interface IPrintableChatMessage : IChatMessage
{
    /// <summary>
    /// Gets or sets a value indicating whether new message sounds should be silenced or not.
    /// </summary>
    bool Silent { get; set; }
}

/// <inheritdoc />
public sealed class PrintableChatMessage : IPrintableChatMessage
{
    /// <inheritdoc />
    public XivChatType LogKind { get; set; } = Service<DalamudConfiguration>.Get().GeneralChatType;

    /// <inheritdoc />
    public XivChatRelationKind SourceKind { get; set; }

    /// <inheritdoc />
    public XivChatRelationKind TargetKind { get; set; }

    /// <inheritdoc />
    public ReadOnlySeString Sender { get; set; }

    /// <inheritdoc />
    public ReadOnlySeString Message { get; set; }

    /// <inheritdoc />
    public int Timestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether new message sounds should be silenced or not.
    /// </summary>
    public bool Silent { get; set; }
}
