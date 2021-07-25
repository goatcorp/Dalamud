using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// Memory offsets for the <see cref="Actor"/> type and all that inherit from it.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the offset usage instead.")]
    public static class ActorOffsets
    {
        // GameObject(Actor)
        // GameObject :: Character
        // GameObject :: Character :: BattleChara
        // GameObject :: Character :: Companion

        public const int Name = 0x30;
        public const int ActorId = 0x74;
        public const int DataId = 0x80;
        public const int OwnerId = 0x84;
        public const int ObjectKind = 0x8C;
        public const int SubKind = 0x8D;
        public const int IsFriendly = 0x8E;
        public const int YalmDistanceFromObjectX = 0x90;
        public const int TargetStatus = 0x91;
        public const int YalmDistanceFromObjectY = 0x92;
        public const int Position = 0xA0;
        public const int Rotation = 0xB0;
        public const int HitboxRadius = 0xC0;
        // End GameObject 0x1A0

        public const int CurrentHp = 0x1C4;
        public const int MaxHp = 0x1C8;
        public const int CurrentMp = 0x1CC;
        public const int MaxMp = 0x1D0;
        public const int CurrentGp = 0x1D4;
        public const int MaxGp = 0x1D6;
        public const int CurrentCp = 0x1D8;
        public const int MaxCp = 0x1DA;
        public const int ClassJob = 0x1E2;
        public const int Level = 0x1E3;
        public const int PlayerCharacterTargetActorId = 0x230;
        public const int Customize = 0x1898;
        public const int CompanyTag = 0x18B2;
        public const int BattleNpcTargetActorId = 0x18D8;
        public const int NameId = 0x1940;
        public const int CurrentWorld = 0x195C;
        public const int HomeWorld = 0x195E;
        public const int StatusFlags = 0x19A0;
        // End Character 0x19B0
        // End Companion 0x19C0

        public const int UIStatusEffects = 0x19F8;
        public const int IsCasting = 0x1B80;
        public const int IsCasting2 = 0x1B82;
        public const int CurrentCastSpellActionId = 0x1B84;
        public const int CurrentCastTargetActorId = 0x1B90;
        public const int CurrentCastTime = 0x1BB4;
        public const int TotalCastTime = 0x1BB8;
        // End BattleChara 0x2C00
    }
}
