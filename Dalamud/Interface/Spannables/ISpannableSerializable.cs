namespace Dalamud.Interface.Spannables;

/// <summary>Something that can be used to serialize states of a spannable.</summary>
// TODO: test this, and think more on if this is useful
public interface ISpannableSerializable
{
    /// <summary>Serializes the state of this spannable, if this spannable is stateful.</summary>
    /// <param name="buffer">The optional buffer to write out the serialized data to.</param>
    /// <returns>A positive number if there is state to serialize; <c>0</c> if not.</returns>
    int SerializeState(Span<byte> buffer);

    /// <summary>Deserializes the state of this spannable, if this spannable is stateful.</summary>
    /// <param name="buffer">The buffer to read state from.</param>
    /// <param name="consumed">The number of bytes read.</param>
    /// <returns><c>true</c> on success.</returns>
    bool TryDeserializeState(ReadOnlySpan<byte> buffer, out int consumed);
}
