using System.Text;

namespace Dalamud.Interface.Fonts;

/// <summary>An OpenType feature and its value, such as <c>new("liga", 0)</c> to turn standard ligatures off.</summary>
/// <param name="Tag">The tag, its first character in the lowest byte.</param>
/// <param name="Value">The value. 0 turns the feature off, 1 turns it on, and others pick an alternate.</param>
public readonly record struct FontFeature(uint Tag, uint Value)
{
    /// <summary>Initializes a new instance of the <see cref="FontFeature"/> struct.</summary>
    /// <param name="tag">The tag of up to four ASCII characters, such as <c>"smcp"</c>.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentException">If the tag is not of 1 to 4 ASCII characters.</exception>
    public FontFeature(string tag, uint value = 1)
        : this(
            tag is { Length: >= 1 and <= 4 } && Ascii.IsValid(tag)
                ? BitConverter.ToUInt32(Encoding.ASCII.GetBytes(tag.PadRight(4)))
                : throw new ArgumentException($"\"{tag}\" is not an OpenType feature tag.", nameof(tag)),
            value)
    {
    }

    /// <summary>Gets the tag as text.</summary>
    public string TagName => Encoding.ASCII.GetString(BitConverter.GetBytes(this.Tag)).TrimEnd();
}
