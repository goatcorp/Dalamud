using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.ClientState.Customize;

/// <summary>
/// This collection represents customization data a <see cref="ICharacter"/> has.
/// </summary>
public interface ICustomizeData
{
    /// <summary>
    /// Gets the current race.
    /// E.g., Miqo'te, Aura.
    /// </summary>
    public byte Race { get; }

    /// <summary>
    /// Gets the current sex.
    /// </summary>
    public byte Sex { get; }

    /// <summary>
    /// Gets the current body type.
    /// </summary>
    public byte BodyType { get; }

    /// <summary>
    /// Gets the current height (0 to 100).
    /// </summary>
    public byte Height { get; }

    /// <summary>
    /// Gets the current tribe.
    /// E.g., Seeker of the Sun, Keeper of the Moon.
    /// </summary>
    public byte Tribe { get; }

    /// <summary>
    /// Gets the current face (1 to 4).
    /// </summary>
    public byte Face { get; }

    /// <summary>
    /// Gets the current hairstyle.
    /// </summary>
    public byte Hairstyle { get; }

    /// <summary>
    /// Gets the current skin color.
    /// </summary>
    public byte SkinColor { get; }

    /// <summary>
    /// Gets the current color of the left eye.
    /// </summary>
    public byte EyeColorLeft { get; }

    /// <summary>
    /// Gets the current color of the right eye.
    /// </summary>
    public byte EyeColorRight { get; }

    /// <summary>
    /// Gets the current main hair color.
    /// </summary>
    public byte HairColor { get; }

    /// <summary>
    /// Gets the current highlight hair color.
    /// </summary>
    public byte HighlightsColor { get; }

    /// <summary>
    /// Gets the current tattoo color.
    /// </summary>
    public byte TattooColor { get; }

    /// <summary>
    /// Gets the current eyebrow type.
    /// </summary>
    public byte Eyebrows { get; }

    /// <summary>
    /// Gets the current nose type.
    /// </summary>
    public byte Nose { get; }

    /// <summary>
    /// Gets the current jaw type.
    /// </summary>
    public byte Jaw { get; }

    /// <summary>
    /// Gets the current lip color fur pattern.
    /// </summary>
    public byte LipColorFurPattern { get; }

    /// <summary>
    /// Gets the current muscle mass value.
    /// </summary>
    public byte MuscleMass { get; }

    /// <summary>
    /// Gets the current tail type (1 to 4).
    /// </summary>
    public byte TailShape { get; }

    /// <summary>
    /// Gets the current bust size (0 to 100).
    /// </summary>
    public byte BustSize { get; }

    /// <summary>
    /// Gets the current color of the face paint.
    /// </summary>
    public byte FacePaintColor { get; }

    /// <summary>
    /// Gets a value indicating whether highlight color is used.
    /// </summary>
    public bool Highlights { get; }

    /// <summary>
    /// Gets a value indicating whether this facial feature is used.
    /// </summary>
    public bool FacialFeature1 { get; }

    /// <inheritdoc cref="FacialFeature1"/>
    public bool FacialFeature2 { get; }

    /// <inheritdoc cref="FacialFeature1"/>
    public bool FacialFeature3 { get; }

    /// <inheritdoc cref="FacialFeature1"/>
    public bool FacialFeature4 { get; }

    /// <inheritdoc cref="FacialFeature1"/>
    public bool FacialFeature5 { get; }

    /// <inheritdoc cref="FacialFeature1"/>
    public bool FacialFeature6 { get; }

    /// <inheritdoc cref="FacialFeature1"/>
    public bool FacialFeature7 { get; }

    /// <summary>
    /// Gets a value indicating whether the legacy tattoo is used.
    /// </summary>
    public bool LegacyTattoo { get; }

    /// <summary>
    /// Gets the current eye shape type.
    /// </summary>
    public byte EyeShape { get; }

    /// <summary>
    /// Gets a value indicating whether small iris is used.
    /// </summary>
    public bool SmallIris { get; }

    /// <summary>
    /// Gets the current mouth type.
    /// </summary>
    public byte Mouth { get; }

    /// <summary>
    /// Gets a value indicating whether lipstick is used.
    /// </summary>
    public bool Lipstick { get; }

    /// <summary>
    /// Gets the current face paint type.
    /// </summary>
    public byte FacePaint { get; }

    /// <summary>
    /// Gets a value indicating whether face paint reversed is used.
    /// </summary>
    public bool FacePaintReversed { get; }
}

/// <inheritdoc/>
internal readonly unsafe struct CustomizeData : ICustomizeData
{
    /// <summary>
    /// Gets or sets the address of the customize data struct in memory.
    /// </summary>
    public readonly nint Address;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomizeData"/> struct.
    /// </summary>
    /// <param name="address">Address of the status list.</param>
    internal CustomizeData(nint address)
    {
        this.Address = address;
    }

    /// <inheritdoc/>
    public byte Race => this.Struct->Race;

    /// <inheritdoc/>
    public byte Sex => this.Struct->Sex;

    /// <inheritdoc/>
    public byte BodyType => this.Struct->BodyType;

    /// <inheritdoc/>
    public byte Height => this.Struct->Height;

    /// <inheritdoc/>
    public byte Tribe => this.Struct->Tribe;

    /// <inheritdoc/>
    public byte Face => this.Struct->Face;

    /// <inheritdoc/>
    public byte Hairstyle => this.Struct->Hairstyle;

    /// <inheritdoc/>
    public byte SkinColor => this.Struct->SkinColor;

    /// <inheritdoc/>
    public byte EyeColorLeft => this.Struct->EyeColorLeft;

    /// <inheritdoc/>
    public byte EyeColorRight => this.Struct->EyeColorRight;

    /// <inheritdoc/>
    public byte HairColor => this.Struct->HairColor;

    /// <inheritdoc/>
    public byte HighlightsColor => this.Struct->HighlightsColor;

    /// <inheritdoc/>
    public byte TattooColor => this.Struct->TattooColor;

    /// <inheritdoc/>
    public byte Eyebrows => this.Struct->Eyebrows;

    /// <inheritdoc/>
    public byte Nose => this.Struct->Nose;

    /// <inheritdoc/>
    public byte Jaw => this.Struct->Jaw;

    /// <inheritdoc/>
    public byte LipColorFurPattern => this.Struct->LipColorFurPattern;

    /// <inheritdoc/>
    public byte MuscleMass => this.Struct->MuscleMass;

    /// <inheritdoc/>
    public byte TailShape => this.Struct->TailShape;

    /// <inheritdoc/>
    public byte BustSize => this.Struct->BustSize;

    /// <inheritdoc/>
    public byte FacePaintColor => this.Struct->FacePaintColor;

    /// <inheritdoc/>
    public bool Highlights => this.Struct->Highlights;

    /// <inheritdoc/>
    public bool FacialFeature1 => this.Struct->FacialFeature1;

    /// <inheritdoc/>
    public bool FacialFeature2 => this.Struct->FacialFeature2;

    /// <inheritdoc/>
    public bool FacialFeature3 => this.Struct->FacialFeature3;

    /// <inheritdoc/>
    public bool FacialFeature4 => this.Struct->FacialFeature4;

    /// <inheritdoc/>
    public bool FacialFeature5 => this.Struct->FacialFeature5;

    /// <inheritdoc/>
    public bool FacialFeature6 => this.Struct->FacialFeature6;

    /// <inheritdoc/>
    public bool FacialFeature7 => this.Struct->FacialFeature7;

    /// <inheritdoc/>
    public bool LegacyTattoo => this.Struct->LegacyTattoo;

    /// <inheritdoc/>
    public byte EyeShape => this.Struct->EyeShape;

    /// <inheritdoc/>
    public bool SmallIris => this.Struct->SmallIris;

    /// <inheritdoc/>
    public byte Mouth => this.Struct->Mouth;

    /// <inheritdoc/>
    public bool Lipstick => this.Struct->Lipstick;

    /// <inheritdoc/>
    public byte FacePaint => this.Struct->FacePaint;

    /// <inheritdoc/>
    public bool FacePaintReversed => this.Struct->FacePaintReversed;

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    internal FFXIVClientStructs.FFXIV.Client.Game.Character.CustomizeData* Struct =>
        (FFXIVClientStructs.FFXIV.Client.Game.Character.CustomizeData*)this.Address;
}
