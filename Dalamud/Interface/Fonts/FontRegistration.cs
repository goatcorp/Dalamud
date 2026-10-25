using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Interface.Fonts.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>A plugin-registered font family backed by a <see cref="FontSource"/> or a wrapped family chain.</summary>
/// <remarks><ul>
/// <li>Registrations with equal sources, or equal adjustments and chains, compare equal and share one reference-counted font.</li>
/// <li>Once all references are unregistered, styles using it fall back to the default font and their
/// <see cref="ImFontRef.LoadTask"/> faults.</li>
/// <li>Cannot be serialized to JSON. Serializing a <see cref="TextStyle"/> that references a registration omits
/// <see cref="TextStyle.FamilyId"/>.</li>
/// </ul></remarks>
/// <seealso cref="IUiBuilder.RegisterFont"/>
/// <seealso cref="IUiBuilder.RegisterWrappedFont"/>
[JsonConverter(typeof(NotSerializable))]
public sealed class FontRegistration : IFontFamilyId, IEquatable<FontRegistration>
{
    private static readonly ModuleLog Log = ModuleLog.Create<FontRegistration>();

    private readonly int hashCode;
    private IReadOnlyList<IFontFaceId>? fonts;

    /// <summary>Initializes a new instance of the <see cref="FontRegistration"/> class, and starts reading the names
    /// of a font source, or waiting for the registrations in a chain.</summary>
    /// <param name="key">The source, or the adjustments and the chain of a wrapped font.</param>
    internal FontRegistration(RegistrationKey key)
    {
        this.Key = key;
        this.hashCode = key.GetHashCode();
        this.Names = (this.IsWrapped
                          ? WaitAll(this.Chain.SelectMany(static e => e.Registrations).Select(static r => r.LoadTask))
                          : Task.Run(this.ReadNames))
            .ObserveFaults();
    }

    /// <inheritdoc/>
    /// <value>For a font source, the family name read from the font, preferring the typographic family name and
    /// falling back to the file or resource name until loaded or on failure. For a wrapped font, the first family's
    /// name.</value>
    public LocalizedName Name =>
        this.IsWrapped
            ? this.Chain[0].Name
            : (this.Names.IsCompletedSuccessfully ? this.Names.GetResultSafely().Family : null)
              ?? (field ??= new(this.Source.Name));

    /// <summary>Gets the task that completes once the font's OpenType name table has been read.</summary>
    /// <remarks>For a font source, faults if the source cannot be opened or is not a valid OpenType font. Full face
    /// loading happens lazily when a <see cref="TextStyle"/> uses the registration. For a wrapped font, awaits
    /// <see cref="LoadTask"/> of all registrations in its chain.</remarks>
    /// <seealso cref="ImFontRef.LoadTask"/>
    public Task LoadTask => this.Names;

    /// <inheritdoc/>
    /// <remarks>A font source has one face. A wrapped font has the faces of the first family of its chain.</remarks>
    public IReadOnlyList<IFontFaceId> Fonts => this.IsWrapped ? this.Chain[0].Fonts : this.fonts ??= [new Face(this)];

    /// <inheritdoc/>
    string IFontFamilyId.ShortName =>
        this.IsWrapped
            ? $"Wrap[{string.Join(", ", this.Chain.Select(static e => e.ShortName))}]({this.Adjustments})"
            : this.Source.ToShortString();

    /// <inheritdoc/>
    string IFontFamilyId.DiagnosticKey => this.IsWrapped ? this.ToString() : this.Source.Key;

    /// <inheritdoc/>
    IEnumerable<FontRegistration> IFontFamilyId.Registrations =>
        this.Chain.SelectMany(static e => e.Registrations).Prepend(this);

    /// <summary>Gets a value indicating whether this registration wraps a font chain.</summary>
    internal bool IsWrapped => !this.Chain.IsEmpty;

    /// <summary>Gets the source of a font source registration.</summary>
    internal FontSource Source => this.Key.Source;

    /// <summary>Gets the adjustments of a wrapped font.</summary>
    internal FontAdjustments Adjustments => this.Key.Adjustments;

    /// <summary>Gets the families of a wrapped font, or empty for a font source.</summary>
    internal ImmutableArray<IFontFamilyId> Chain => this.Key.Chain.Array;

    /// <summary>Gets the identity key used for equality and registry lookup.</summary>
    internal RegistrationKey Key { get; }

    /// <summary>Gets or sets the active reference count while registered.</summary>
    /// <remarks>Mutated only under <see cref="FontRegistry"/>'s lock.</remarks>
    internal int References { get; set; }

    /// <summary>Gets a value indicating whether this registration is currently active.</summary>
    internal bool IsRegistered => FontRegistry.IsRegistered(this);

    /// <summary>Gets the task that reads a font source's names, or awaits chain registrations for a wrapped font.</summary>
    /// <value>The family and face names, with <see langword="null"/> for missing names or wrapped fonts.</value>
    internal Task<(LocalizedName? Family, LocalizedName? Face)> Names { get; }

    public static bool operator ==(FontRegistration? left, FontRegistration? right) => Equals(left, right);

    public static bool operator !=(FontRegistration? left, FontRegistration? right) => !Equals(left, right);

    /// <inheritdoc/>
    /// <returns>0 for a font source. The best match of the first family of its chain for a wrapped font.</returns>
    public int FindBestMatch(FontStyle style) => this.IsWrapped ? this.Chain[0].FindBestMatch(style) : 0;

    /// <inheritdoc/>
    public bool Equals(FontRegistration? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (this.hashCode == other.hashCode && this.Key.Equals(other.Key)));

    /// <inheritdoc/>
    void IFontFamilyId.AppendTo(FontComposer composer)
    {
        if (!this.IsRegistered)
            composer.ReportFailure(this, new InvalidOperationException("The font is not registered."));
        else if (this.IsWrapped)
            composer.AddComposite(this, this.Chain, this.Adjustments);
        else
            composer.AddFace(this);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is FontRegistration other && this.Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => this.hashCode;

    /// <inheritdoc/>
    public override string ToString() =>
        this.IsWrapped
            ? $"{nameof(FontRegistration)}:{((IFontFamilyId)this).ShortName}"
            : $"{nameof(FontRegistration)}:{this.Source}";

    private static async Task<(LocalizedName? Family, LocalizedName? Face)> WaitAll(IEnumerable<Task> tasks)
    {
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return default;
    }

    private (LocalizedName? Family, LocalizedName? Face) ReadNames()
    {
        try
        {
            using var stream = this.Source.Open();
            return OpenTypeNames.Read(stream, this.Source.FontNo);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Font {Source} could not be read", this.Source);
            throw;
        }
    }

    /// <summary>Identity key for a <see cref="FontRegistration"/>.</summary>
    /// <param name="Source">The source, or default for a wrapped font.</param>
    /// <param name="Adjustments">The adjustments of a wrapped font.</param>
    /// <param name="Chain">The families of a wrapped font, or empty for a font source.</param>
    internal readonly record struct RegistrationKey(
        FontSource Source,
        FontAdjustments Adjustments,
        EquatableArray<IFontFamilyId> Chain);

    /// <summary>The face of a font source registration.</summary>
    /// <param name="registration">The registration.</param>
    internal sealed class Face(FontRegistration registration) : IFontFaceId
    {
        /// <summary>Gets the registration.</summary>
        public FontRegistration Registration { get; } = registration;

        /// <inheritdoc/>
        public LocalizedName Name =>
            (this.Registration.Names.IsCompletedSuccessfully ? this.Registration.Names.GetResultSafely().Face : null)
            ?? (field ??= new(nameof(FontStyle.Default)));

        /// <inheritdoc/>
        public IFontFamilyId Family => this.Registration;

        /// <inheritdoc/>
        public FontStyle Style => FontStyle.Default;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Face other && this.Registration.Equals(other.Registration);

        /// <inheritdoc/>
        public override int GetHashCode() => this.Registration.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => this.Registration.ToString();
    }

    private sealed class NotSerializable : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(
            JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
            throw new NotSupportedException();

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
            throw new JsonSerializationException($"{value?.GetType().Name} cannot be serialized.");
    }
}
