using System.Globalization;
using System.Text;

using Newtonsoft.Json;

namespace Dalamud.Common.Game;

/// <summary>
/// A GameVersion object contains give hierarchical numeric components: year, month,
/// day, major and minor. All components may be unspecified, which is represented
/// internally as a -1. By definition, an unspecified component matches anything
/// (both unspecified and specified), and an unspecified component is "less than" any
/// specified component. It will also equal the string "any" if all components are
/// unspecified. The value can be retrieved from the ffxivgame.ver file in your game
/// installation directory.
/// </summary>
[Serializable]
public sealed class GameVersion : ICloneable, IComparable, IComparable<GameVersion>, IEquatable<GameVersion>
{
    private static readonly GameVersion AnyVersion = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="version">Version string to parse.</param>
    public GameVersion(string version)
    {
        var ver = Parse(version);
        this.Year = ver.Year;
        this.Month = ver.Month;
        this.Day = ver.Day;
        this.Major = ver.Major;
        this.Minor = ver.Minor;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    [JsonConstructor]
    public GameVersion(int year, int month, int day, int major, int minor) : this(year, month, day, major)
    {
        if ((this.Minor = minor) < 0)
            throw new ArgumentOutOfRangeException(nameof(minor));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    /// <param name="major">The major version.</param>
    public GameVersion(int year, int month, int day, int major) : this(year, month, day)
    {
        if ((this.Major = major) < 0)
            throw new ArgumentOutOfRangeException(nameof(major));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    public GameVersion(int year, int month, int day) : this(year, month)
    {
        if ((this.Day = day) < 0)
            throw new ArgumentOutOfRangeException(nameof(day));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    public GameVersion(int year, int month) : this(year)
    {
        if ((this.Month = month) < 0)
            throw new ArgumentOutOfRangeException(nameof(month));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    public GameVersion(int year)
    {
        if ((this.Year = year) < 0)
            throw new ArgumentOutOfRangeException(nameof(year));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    public GameVersion()
    {
    }

    /// <summary>
    /// Gets the default "any" game version.
    /// </summary>
    public static GameVersion Any => AnyVersion;

    /// <summary>
    /// Gets the year component.
    /// </summary>
    [JsonRequired]
    public int Year { get; } = -1;

    /// <summary>
    /// Gets the month component.
    /// </summary>
    [JsonRequired]
    public int Month { get; } = -1;

    /// <summary>
    /// Gets the day component.
    /// </summary>
    [JsonRequired]
    public int Day { get; } = -1;

    /// <summary>
    /// Gets the major version component.
    /// </summary>
    [JsonRequired]
    public int Major { get; } = -1;

    /// <summary>
    /// Gets the minor version component.
    /// </summary>
    [JsonRequired]
    public int Minor { get; } = -1;

    public static implicit operator GameVersion(string ver)
    {
        return Parse(ver);
    }

    public static bool operator ==(GameVersion? v1, GameVersion? v2)
    {
        if (v1 is null)
        {
            return v2 is null;
        }

        return v2 is not null && v1.Equals(v2);
    }

    public static bool operator !=(GameVersion v1, GameVersion v2)
    {
        return !(v1 == v2);
    }

    public static bool operator <(GameVersion v1, GameVersion v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        return v1.CompareTo(v2) < 0;
    }

    public static bool operator <=(GameVersion v1, GameVersion v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        return v1.CompareTo(v2) <= 0;
    }

    public static bool operator >(GameVersion v1, GameVersion v2)
    {
        return v2 < v1;
    }

    public static bool operator >=(GameVersion v1, GameVersion v2)
    {
        return v2 <= v1;
    }

    public static GameVersion operator +(GameVersion v1, TimeSpan v2)
    {
        ArgumentNullException.ThrowIfNull(v1);

        if (v1.Year == -1 || v1.Month == -1 || v1.Day == -1)
            return v1;

        var date = new DateTime(v1.Year, v1.Month, v1.Day) + v2;

        return new GameVersion(date.Year, date.Month, date.Day, v1.Major, v1.Minor);
    }

    public static GameVersion operator -(GameVersion v1, TimeSpan v2)
    {
        ArgumentNullException.ThrowIfNull(v1);

        if (v1.Year == -1 || v1.Month == -1 || v1.Day == -1)
            return v1;

        var date = new DateTime(v1.Year, v1.Month, v1.Day) - v2;

        return new GameVersion(date.Year, date.Month, date.Day, v1.Major, v1.Minor);
    }

    /// <summary>
    /// Parse a version string. YYYY.MM.DD.majr.minr or "any".
    /// </summary>
    /// <param name="input">Input to parse.</param>
    /// <returns>GameVersion object.</returns>
    public static GameVersion Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.ToLower(CultureInfo.InvariantCulture) == "any")
            return Any;

        var parts = input.Split('.');
        var tplParts = parts.Select(
            p =>
            {
                var result = int.TryParse(p, out var value);
                return (result, value);
            }).ToArray();

        if (tplParts.Any(t => !t.result))
            throw new FormatException("Bad formatting");

        var intParts = tplParts.Select(t => t.value).ToArray();
        var len = intParts.Length;

        return len switch
        {
            1 => new GameVersion(intParts[0]),
            2 => new GameVersion(intParts[0], intParts[1]),
            3 => new GameVersion(intParts[0], intParts[1], intParts[2]),
            4 => new GameVersion(intParts[0], intParts[1], intParts[2], intParts[3]),
            5 => new GameVersion(intParts[0], intParts[1], intParts[2], intParts[3], intParts[4]),
            _ => throw new ArgumentException("Too many parts"),
        };
    }

    /// <summary>
    /// Try to parse a version string. YYYY.MM.DD.majr.minr or "any".
    /// </summary>
    /// <param name="input">Input to parse.</param>
    /// <param name="result">GameVersion object.</param>
    /// <returns>Success or failure.</returns>
    public static bool TryParse(string input, out GameVersion result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            result = null!;
            return false;
        }
    }

    /// <inheritdoc/>
    public object Clone() => new GameVersion(this.Year, this.Month, this.Day, this.Major, this.Minor);

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            GameVersion value => this.CompareTo(value),
            _ => throw new ArgumentException("Argument must be a GameVersion", nameof(obj)),
        };
    }

    /// <inheritdoc/>
    public int CompareTo(GameVersion? value)
    {
        if (value == null)
            return 1;

        if (this == value)
            return 0;

        if (this == AnyVersion)
            return 1;

        if (value == AnyVersion)
            return -1;

        if (this.Year != value.Year)
            return this.Year > value.Year ? 1 : -1;

        if (this.Month != value.Month)
            return this.Month > value.Month ? 1 : -1;

        if (this.Day != value.Day)
            return this.Day > value.Day ? 1 : -1;

        if (this.Major != value.Major)
            return this.Major > value.Major ? 1 : -1;

        if (this.Minor != value.Minor)
            return this.Minor > value.Minor ? 1 : -1;

        // This should never happen
        return 0;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is GameVersion value && this.Equals(value);
    }

    /// <inheritdoc/>
    public bool Equals(GameVersion? value)
    {
        if (value == null)
        {
            return false;
        }

        return
            (this.Year == value.Year) &&
            (this.Month == value.Month) &&
            (this.Day == value.Day) &&
            (this.Major == value.Major) &&
            (this.Minor == value.Minor);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode?view=net-8.0#notes-to-inheritors
        return HashCode.Combine(this.Year, this.Month, this.Day, this.Major, this.Minor);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (this.Year == -1 &&
            this.Month == -1 &&
            this.Day == -1 &&
            this.Major == -1 &&
            this.Minor == -1)
            return "any";

        return new StringBuilder()
               .Append($"{(this.Year == -1 ? 0 : this.Year):D4}.")
               .Append($"{(this.Month == -1 ? 0 : this.Month):D2}.")
               .Append($"{(this.Day == -1 ? 0 : this.Day):D2}.")
               .Append($"{(this.Major == -1 ? 0 : this.Major):D4}.")
               .Append($"{(this.Minor == -1 ? 0 : this.Minor):D4}")
               .ToString();
    }
}
