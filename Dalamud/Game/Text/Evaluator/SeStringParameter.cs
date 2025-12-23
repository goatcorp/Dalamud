using System.Globalization;

using Lumina.Text.ReadOnly;

using DSeString = Dalamud.Game.Text.SeStringHandling.SeString;

namespace Dalamud.Game.Text.Evaluator;

/// <summary>
/// A wrapper for a local parameter, holding either a number or a string.
/// </summary>
public readonly struct SeStringParameter
{
    private readonly uint num;
    private readonly ReadOnlySeString str;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeStringParameter"/> struct for a number parameter.
    /// </summary>
    /// <param name="value">The number value.</param>
    public SeStringParameter(uint value)
    {
        this.num = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeStringParameter"/> struct for a string parameter.
    /// </summary>
    /// <param name="value">The string value.</param>
    public SeStringParameter(ReadOnlySeString value)
    {
        this.str = value;
        this.IsString = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeStringParameter"/> struct for a string parameter.
    /// </summary>
    /// <param name="value">The string value.</param>
    public SeStringParameter(string value)
    {
        this.str = new ReadOnlySeString(value);
        this.IsString = true;
    }

    /// <summary>
    /// Gets a value indicating whether the backing type of this parameter is a string.
    /// </summary>
    public bool IsString { get; }

    /// <summary>
    /// Gets a numeric value.
    /// </summary>
    public uint UIntValue =>
        !this.IsString
            ? this.num
            : uint.TryParse(this.str.ExtractText(), out var value) ? value : 0;

    /// <summary>
    /// Gets a string value.
    /// </summary>
    public ReadOnlySeString StringValue =>
        this.IsString ? this.str : new(this.num.ToString("D", CultureInfo.InvariantCulture));

    public static implicit operator SeStringParameter(int value) => new((uint)value);

    public static implicit operator SeStringParameter(uint value) => new(value);

    public static implicit operator SeStringParameter(ReadOnlySeString value) => new(value);

    public static implicit operator SeStringParameter(ReadOnlySeStringSpan value) => new(new ReadOnlySeString(value));

    public static implicit operator SeStringParameter(DSeString value) => new(new ReadOnlySeString(value.Encode()));

    public static implicit operator SeStringParameter(string value) => new(value);

    public static implicit operator SeStringParameter(ReadOnlySpan<byte> value) => new(value);
}
