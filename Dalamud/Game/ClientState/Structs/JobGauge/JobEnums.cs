using System;

#pragma warning disable SA1402 // File may only contain a single type

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    #region AST

    /// <summary>
    /// AST Divination seal types.
    /// </summary>
    public enum SealType : byte
    {
        /// <summary>
        /// No seal.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Sun seal.
        /// </summary>
        SUN = 1,

        /// <summary>
        /// Moon seal.
        /// </summary>
        MOON = 2,

        /// <summary>
        /// Celestial seal.
        /// </summary>
        CELESTIAL = 3,
    }

    /// <summary>
    /// AST Arcanum (card) types.
    /// </summary>
    public enum CardType : byte
    {
        /// <summary>
        /// No card.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// The Balance card.
        /// </summary>
        BALANCE = 1,

        /// <summary>
        /// The Bole card.
        /// </summary>
        BOLE = 2,

        /// <summary>
        /// The Arrow card.
        /// </summary>
        ARROW = 3,

        /// <summary>
        /// The Spear card.
        /// </summary>
        SPEAR = 4,

        /// <summary>
        /// The Ewer card.
        /// </summary>
        EWER = 5,

        /// <summary>
        /// The Spire card.
        /// </summary>
        SPIRE = 6,

        /// <summary>
        /// The Lord of Crowns card.
        /// </summary>
        LORD = 0x70,

        /// <summary>
        /// The Lady of Crowns card.
        /// </summary>
        LADY = 0x80,
    }

    #endregion

    #region BRD

    /// <summary>
    /// BRD Current Song types.
    /// </summary>
    public enum CurrentSong : byte
    {
        /// <summary>
        /// No song is active type.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Mage's Ballad type.
        /// </summary>
        MAGE = 5,

        /// <summary>
        /// Army's Paeon type.
        /// </summary>
        ARMY = 0xA,

        /// <summary>
        /// The Wanderer's Minuet type.
        /// </summary>
        WANDERER = 0xF,
    }

    #endregion

    #region DRG

    /// <summary>
    /// DRG Blood of the Dragon state types.
    /// </summary>
    public enum BOTDState : byte
    {
        /// <summary>
        /// Inactive type.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Blood of the Dragon is active.
        /// </summary>
        BOTD = 1,

        /// <summary>
        /// Life of the Dragon is active.
        /// </summary>
        LOTD = 2,
    }

    #endregion

    #region NIN

    /// <summary>
    /// NIN Mudra types.
    /// </summary>
    public enum Mudras : byte
    {
        /// <summary>
        /// Ten mudra.
        /// </summary>
        TEN = 1,

        /// <summary>
        /// Chi mudra.
        /// </summary>
        CHI = 2,

        /// <summary>
        /// Jin mudra.
        /// </summary>
        JIN = 3,
    }

    #endregion

    #region SAM

    /// <summary>
    /// Samurai Sen types.
    /// </summary>
    [Flags]
    public enum Sen : byte
    {
        /// <summary>
        /// No Sen.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Setsu Sen type.
        /// </summary>
        SETSU = 1 << 0,

        /// <summary>
        /// Getsu Sen type.
        /// </summary>
        GETSU = 1 << 1,

        /// <summary>
        /// Ka Sen type.
        /// </summary>
        KA = 1 << 2,
    }

    #endregion

    #region SCH

    /// <summary>
    /// SCH Dismissed fairy types.
    /// </summary>
    public enum DismissedFairy : byte
    {
        /// <summary>
        /// Dismissed fairy is Eos.
        /// </summary>
        EOS = 6,

        /// <summary>
        /// Dismissed fairy is Selene.
        /// </summary>
        SELENE = 7,
    }

    #endregion

    #region SMN

    /// <summary>
    /// SMN summoned pet types.
    /// </summary>
    public enum SummonPet : byte
    {
        /// <summary>
        /// No pet.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// The summoned pet Ifrit.
        /// </summary>
        IFRIT = 3,

        /// <summary>
        /// The summoned pet Titan.
        /// </summary>
        TITAN = 4,

        /// <summary>
        /// The summoned pet Garuda.
        /// </summary>
        GARUDA = 5,
    }

    /// <summary>
    /// SMN summoned pet glam types.
    /// </summary>
    public enum PetGlam : byte
    {
        /// <summary>
        /// No pet glam.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Emerald carbuncle pet glam.
        /// </summary>
        EMERALD = 1,

        /// <summary>
        /// Topaz carbuncle pet glam.
        /// </summary>
        TOPAZ = 2,

        /// <summary>
        /// Ruby carbuncle pet glam.
        /// </summary>
        RUBY = 3,
    }

    #endregion
}

#pragma warning restore SA1402 // File may only contain a single type
