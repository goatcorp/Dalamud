namespace Dalamud.Interface.SpannedStrings;

/// <summary>Delegate for use with
/// <see cref="ISpannedStringBuilder{TReturn}.AppendCallback(SpannedStringCallbackDelegate?, float, out int)"/>.
/// </summary>
/// <param name="args">The arguments.</param>
public delegate void SpannedStringCallbackDelegate(in SpannedStringCallbackArgs args);
