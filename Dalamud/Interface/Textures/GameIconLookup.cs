using System.Text;

using Dalamud.Game;

namespace Dalamud.Interface.Textures;

/// <summary>Represents a lookup for a game icon.</summary>
public readonly record struct GameIconLookup
{
    /// <summary>Initializes a new instance of the <see cref="GameIconLookup"/> struct.</summary>
    /// <param name="iconId">The icon ID.</param>
    /// <param name="itemHq">Whether the HQ icon is requested, where HQ is in the context of items.</param>
    /// <param name="hiRes">Whether the high-resolution icon is requested.</param>
    /// <param name="language">The language of the icon to load.</param>
    public GameIconLookup(uint iconId, bool itemHq = false, bool hiRes = true, ClientLanguage? language = null)
    {
        this.IconId = iconId;
        this.ItemHq = itemHq;
        this.HiRes = hiRes;
        this.Language = language;
    }
    
    /// <summary>Gets the icon ID.</summary>
    public uint IconId { get; init; }

    /// <summary>Gets a value indicating whether the HQ icon is requested, where HQ is in the context of items.</summary>
    public bool ItemHq { get; init; }

    /// <summary>Gets a value indicating whether the high-resolution icon is requested.</summary>
    public bool HiRes { get; init; }

    /// <summary>Gets the language of the icon to load.</summary>
    /// <remarks>
    /// <para><c>null</c> will use the active game language.</para>
    /// <para>If the specified resource does not have variants per language, the language-neutral texture will be used.
    /// </para>
    /// </remarks>
    public ClientLanguage? Language { get; init; }

    public static implicit operator GameIconLookup(int iconId) => new(checked((uint)iconId));

    public static implicit operator GameIconLookup(uint iconId) => new(iconId);

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(nameof(GameIconLookup)).Append('(').Append(this.IconId);
        if (this.ItemHq)
            sb.Append(", HQ");
        if (this.HiRes)
            sb.Append(", HR1");
        if (this.Language is not null)
            sb.Append(", ").Append(Enum.GetName(this.Language.Value));
        return sb.Append(')').ToString();
    }
}
