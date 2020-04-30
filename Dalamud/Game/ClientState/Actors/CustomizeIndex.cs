using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Actors
{
    /// <summary>
    /// This enum describes the indices of the Customize array.
    /// </summary>
    // TODO: This may need some rework since it may not be entirely accurate (stolen from Sapphire)
    public enum CustomizeIndex {
        Race = 0x00,
        Gender = 0x01,
        Tribe = 0x04,
        Height = 0x03,
        ModelType = 0x02, // Au Ra: changes horns/tails, everything else: seems to drastically change appearance (flip between two sets, odd/even numbers). sometimes retains hairstyle and other features
        FaceType = 0x05,
        HairStyle = 0x06,
        HasHighlights = 0x07, // negative to enable, positive to disable
        SkinColor = 0x08,
        EyeColor = 0x09, // color of character's right eye
        HairColor = 0x0A, // main color
        HairColor2 = 0x0B, // highlights color
        FaceFeatures = 0x0C, // seems to be a toggle, (-odd and +even for large face covering), opposite for small
        FaceFeaturesColor = 0x0D,
        Eyebrows = 0x0E,
        EyeColor2 = 0x0F, // color of character's left eye
        EyeShape = 0x10,
        NoseShape = 0x11,
        JawShape = 0x12,
        LipStyle = 0x13, // lip colour depth and shape (negative values around -120 darker/more noticeable, positive no colour)
        LipColor = 0x14,
        RaceFeatureSize = 0x15,
        RaceFeatureType = 0x16, // negative or out of range tail shapes for race result in no tail (e.g. Au Ra has max of 4 tail shapes), incorrect value can crash client
        BustSize = 0x17, // char creator allows up to max of 100, i set to 127 cause who wouldnt but no visible difference
        Facepaint = 0x18,
        FacepaintColor = 0x19,
    }
}
