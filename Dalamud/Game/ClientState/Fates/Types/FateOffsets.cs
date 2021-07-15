using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Game.ClientState.Fates.Types
{
    /// <summary>
    /// Memory offsets for the <see cref="Fate"/> type.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the offset usage instead.")]
    public static class FateOffsets
    {
        public const int FateId = 0x18;
        public const int StartTimeEpoch = 0x20;
        public const int Duration = 0x28;
        public const int Name = 0xC0;
        public const int State = 0x3AC;
        public const int Progress = 0x3B8;
        public const int Level = 0x3F9;
        public const int Position = 0x450;
        public const int Territory = 0x74E;
    }
}
