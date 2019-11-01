using System;

namespace Dalamud.Game.ClientState.Structs.JobGauge {
    public enum SealType : byte {
        NONE = 0,
        SUN,
        MOON,
        CELESTIAL
    }

    public enum CardType : byte {
        NONE = 0,
        BALANCE,
        BOLE,
        ARROW,
        SPEAR,
        EWER,
        SPIRE,
        LORD = 0x70,
        LADY = 0x80
    }

    public enum SummonPet : byte {
        NONE = 0,
        IFRIT = 3,
        TITAN,
        GARUDA
    }

    public enum PetGlam : byte {
        NONE = 0,
        EMERALD,
        TOPAZ,
        RUBY
    }

    [Flags]
    public enum Sen : byte {
        NONE = 0,
        SETSU = 1 << 0,
        GETSU = 1 << 1,
        KA = 1 << 2
    }

    public enum BOTDState : byte {
        NONE = 0,
        BOTD,
        LOTD
    }

    public enum CurrentSong : byte {
        MAGE = 5,
        ARMY = 0xA,
        WANDERER = 0xF
    }

    public enum DismissedFairy : byte {
        EOS = 6,
        SELENE
    }

    public enum Mudras : byte {
        TEN = 1,
        CHI = 2,
        JIN = 3
    }
}
