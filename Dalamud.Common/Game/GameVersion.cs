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
    [JsonConstructor]
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
    public GameVersion(int year, int month, int day, int major, int minor)
    {
        if ((this.Year = year) < 0)
            throw new ArgumentOutOfRangeException(nameof(year));

        if ((this.Month = month) < 0)
            throw new ArgumentOutOfRangeException(nameof(month));

        if ((this.Day = day) < 0)
            throw new ArgumentOutOfRangeException(nameof(day));

        if ((this.Major = major) < 0)
            throw new ArgumentOutOfRangeException(nameof(major));

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
    public GameVersion(int year, int month, int day, int major)
    {
        if ((this.Year = year) < 0)
            throw new ArgumentOutOfRangeException(nameof(year));

        if ((this.Month = month) < 0)
            throw new ArgumentOutOfRangeException(nameof(month));

        if ((this.Day = day) < 0)
            throw new ArgumentOutOfRangeException(nameof(day));

        if ((this.Major = major) < 0)
            throw new ArgumentOutOfRangeException(nameof(major));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    public GameVersion(int year, int month, int day)
    {
        if ((this.Year = year) < 0)
            throw new ArgumentOutOfRangeException(nameof(year));

        if ((this.Month = month) < 0)
            throw new ArgumentOutOfRangeException(nameof(month));

        if ((this.Day = day) < 0)
            throw new ArgumentOutOfRangeException(nameof(day));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameVersion"/> class.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    public GameVersion(int year, int month)
    {
        if ((this.Year = year) < 0)
            throw new ArgumentOutOfRangeException(nameof(year));

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
    public int Year { get; } = -1;

    /// <summary>
    /// Gets the month component.
    /// </summary>
    public int Month { get; } = -1;

    /// <summary>
    /// Gets the day component.
    /// </summary>
    public int Day { get; } = -1;

    /// <summary>
    /// Gets the major version component.
    /// </summary>
    public int Major { get; } = -1;

    /// <summary>
    /// Gets the minor version component.
    /// </summary>
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
        if (v1 is null)
            throw new ArgumentNullException(nameof(v1));

        return v1.CompareTo(v2) < 0;
    }

    public static bool operator <=(GameVersion v1, GameVersion v2)
    {
        if (v1 is null)
            throw new ArgumentNullException(nameof(v1));

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
        if (v1 == null)
            throw new ArgumentNullException(nameof(v1));

        if (v1.Year == -1 || v1.Month == -1 || v1.Day == -1)
            return v1;

        var date = new DateTime(v1.Year, v1.Month, v1.Day) + v2;

        return new GameVersion(date.Year, date.Month, date.Day, v1.Major, v1.Minor);
    }

    public static GameVersion operator -(GameVersion v1, TimeSpan v2)
    {
        if (v1 == null)
            throw new ArgumentNullException(nameof(v1));

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
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.ToLower(CultureInfo.InvariantCulture) == "any")
            return new GameVersion();

        var parts = input.Split('.');
        var tplParts = parts.Select(p =>
        {
            var result = int.TryParse(p, out var value);
            return (result, value);
        }).ToArray();

        if (tplParts.Any(t => !t.result))
            throw new FormatException("Bad formatting");

        var intParts = tplParts.Select(t => t.value).ToArray();
        var len = intParts.Length;

        if (len == 1)
            return new GameVersion(intParts[0]);
        else if (len == 2)
            return new GameVersion(intParts[0], intParts[1]);
        else if (len == 3)
            return new GameVersion(intParts[0], intParts[1], intParts[2]);
        else if (len == 4)
            return new GameVersion(intParts[0], intParts[1], intParts[2], intParts[3]);
        else if (len == 5)
            return new GameVersion(intParts[0], intParts[1], intParts[2], intParts[3], intParts[4]);
        else
            throw new ArgumentException("Too many parts");
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
        if (obj == null)
            return 1;

        if (obj is GameVersion value)
        {
            return this.CompareTo(value);
        }
        else
        {
            throw new ArgumentException("Argument must be a GameVersion");
        }
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

        return 0;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not GameVersion value)
            return false;

        return this.Equals(value);
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
        var accumulator = 0;

        // This might be horribly wrong, but it isn't used heavily.
        accumulator |= this.Year.GetHashCode();
        accumulator |= this.Month.GetHashCode();
        accumulator |= this.Day.GetHashCode();
        accumulator |= this.Major.GetHashCode();
        accumulator |= this.Minor.GetHashCode();

        return accumulator;
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
               .Append(string.Format("{0:D4}.", this.Year == -1 ? 0 : this.Year))
               .Append(string.Format("{0:D2}.", this.Month == -1 ? 0 : this.Month))
               .Append(string.Format("{0:D2}.", this.Day == -1 ? 0 : this.Day))
               .Append(string.Format("{0:D4}.", this.Major == -1 ? 0 : this.Major))
               .Append(string.Format("{0:D4}", this.Minor == -1 ? 0 : this.Minor))
               .ToString();
    }
}
