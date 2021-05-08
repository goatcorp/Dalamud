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
        SUN,

        /// <summary>
        /// Moon seal.
        /// </summary>
        MOON,

        /// <summary>
        /// Celestial seal.
        /// </summary>
        CELESTIAL,
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
        BALANCE,

        /// <summary>
        /// The Bole card.
        /// </summary>
        BOLE,

        /// <summary>
        /// The Arrow card.
        /// </summary>
        ARROW,

        /// <summary>
        /// The Spear card.
        /// </summary>
        SPEAR,

        /// <summary>
        /// The Ewer card.
        /// </summary>
        EWER,

        /// <summary>
        /// The Spire card.
        /// </summary>
        SPIRE,

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

    public enum SummonPet : byte
    {
        NONE = 0,
        IFRIT = 3,
        TITAN,
        GARUDA,
    }

    public enum PetGlam : byte
    {
        NONE = 0,
        EMERALD,
        TOPAZ,
        RUBY,
    }

    [Flags]
    public enum Sen : byte
    {
        NONE = 0,
        SETSU = 1 << 0,
        GETSU = 1 << 1,
        KA = 1 << 2,
    }

    public enum BOTDState : byte
    {
        NONE = 0,
        BOTD,
        LOTD,
    }

    public enum CurrentSong : byte
    {
        MAGE = 5,
        ARMY = 0xA,
        WANDERER = 0xF,
    }

    public enum DismissedFairy : byte
    {
        EOS = 6,
        SELENE,
    }

    public enum Mudras : byte
    {
        TEN = 1,
        CHI = 2,
        JIN = 3,
    }
}

#pragma warning restore SA1402 // File may only contain a single type
