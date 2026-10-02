namespace Dalamud.NativeUi.Enums;

/// <summary>
/// Enumeration for key frame group types,
/// this represents the array index that stores these types of keyframes.
/// </summary>
#pragma warning disable CA1069
// Intentional duplicate entry, as the game itself treats that index as two separate things depending on node type.
internal enum KeyFrameGroupType
{
    /// <summary>
    /// Position.
    /// </summary>
    Position = 0,

    /// <summary>
    /// Rotation.
    /// </summary>
    Rotation = 1,

    /// <summary>
    /// Scale.
    /// </summary>
    Scale = 2,

    /// <summary>
    /// Alpha.
    /// </summary>
    Alpha = 3,

    /// <summary>
    /// Tint.
    /// </summary>
    Tint = 4,

    /// <summary>
    /// PartId.
    /// </summary>
    PartId = 5,

    /// <summary>
    /// TextColor.
    /// </summary>
    TextColor = 5,

    /// <summary>
    /// TextEdge.
    /// </summary>
    TextEdge = 6,

    /// <summary>
    /// TextLabel.
    /// </summary>
    TextLabel = 7,
}
#pragma warning restore CA1069
