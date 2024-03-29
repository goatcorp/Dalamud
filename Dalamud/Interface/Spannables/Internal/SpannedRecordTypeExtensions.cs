namespace Dalamud.Interface.Spannables.Internal;

/// <summary>Extension methods for <see cref="SpannedRecordType"/>.</summary>
internal static class SpannedRecordTypeExtensions
{
    /// <summary>Determines if the given type is an object.</summary>
    /// <param name="type">The type.</param>
    /// <returns>Whether it is.</returns>
    public static bool IsObject(this SpannedRecordType type) =>
        type is SpannedRecordType.ObjectIcon
            or SpannedRecordType.ObjectTexture
            or SpannedRecordType.ObjectNewLine
            or SpannedRecordType.ObjectSpannable;
}
