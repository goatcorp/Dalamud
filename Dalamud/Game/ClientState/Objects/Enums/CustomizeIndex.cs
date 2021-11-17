namespace Dalamud.Game.ClientState.Objects.Enums;

/// <summary>
/// This enum describes the indices of the Customize array.
/// </summary>
// TODO: This may need some rework since it may not be entirely accurate (stolen from Sapphire)
public enum CustomizeIndex
{
    /// <summary>
    /// The race of the character.
    /// </summary>
    Race = 0x00,

    /// <summary>
    /// The gender of the character.
    /// </summary>
    Gender = 0x01,

    /// <summary>
    /// The tribe of the character.
    /// </summary>
    Tribe = 0x04,

    /// <summary>
    /// The height of the character.
    /// </summary>
    Height = 0x03,

    /// <summary>
    /// The model type of the character.
    /// </summary>
    ModelType = 0x02, // Au Ra: changes horns/tails, everything else: seems to drastically change appearance (flip between two sets, odd/even numbers). sometimes retains hairstyle and other features

    /// <summary>
    /// The face type of the character.
    /// </summary>
    FaceType = 0x05,

    /// <summary>
    /// The hair of the character.
    /// </summary>
    HairStyle = 0x06,

    /// <summary>
    /// Whether or not the character has hair highlights.
    /// </summary>
    HasHighlights = 0x07, // negative to enable, positive to disable

    /// <summary>
    /// The skin color of the character.
    /// </summary>
    SkinColor = 0x08,

    /// <summary>
    /// The eye color of the character.
    /// </summary>
    EyeColor = 0x09, // color of character's right eye

    /// <summary>
    /// The hair color of the character.
    /// </summary>
    HairColor = 0x0A, // main color

    /// <summary>
    /// The highlights hair color of the character.
    /// </summary>
    HairColor2 = 0x0B, // highlights color

    /// <summary>
    /// The face features of the character.
    /// </summary>
    FaceFeatures = 0x0C, // seems to be a toggle, (-odd and +even for large face covering), opposite for small

    /// <summary>
    /// The color of the face features of the character.
    /// </summary>
    FaceFeaturesColor = 0x0D,

    /// <summary>
    /// The eyebrows of the character.
    /// </summary>
    Eyebrows = 0x0E,

    /// <summary>
    /// The 2nd eye color of the character.
    /// </summary>
    EyeColor2 = 0x0F, // color of character's left eye

    /// <summary>
    /// The eye shape of the character.
    /// </summary>
    EyeShape = 0x10,

    /// <summary>
    /// The nose shape of the character.
    /// </summary>
    NoseShape = 0x11,

    /// <summary>
    /// The jaw shape of the character.
    /// </summary>
    JawShape = 0x12,

    /// <summary>
    /// The lip style of the character.
    /// </summary>
    LipStyle = 0x13, // lip colour depth and shape (negative values around -120 darker/more noticeable, positive no colour)

    /// <summary>
    /// The lip color of the character.
    /// </summary>
    LipColor = 0x14,

    /// <summary>
    /// The race feature size of the character.
    /// </summary>
    RaceFeatureSize = 0x15,

    /// <summary>
    /// The race feature type of the character.
    /// </summary>
    RaceFeatureType = 0x16, // negative or out of range tail shapes for race result in no tail (e.g. Au Ra has max of 4 tail shapes), incorrect value can crash client

    /// <summary>
    /// The bust size of the character.
    /// </summary>
    BustSize = 0x17, // char creator allows up to max of 100, i set to 127 cause who wouldnt but no visible difference

    /// <summary>
    /// The face paint of the character.
    /// </summary>
    Facepaint = 0x18,

    /// <summary>
    /// The face paint color of the character.
    /// </summary>
    FacepaintColor = 0x19,
}
