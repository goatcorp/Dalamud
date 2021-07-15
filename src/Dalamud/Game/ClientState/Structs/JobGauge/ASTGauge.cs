using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory AST job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ASTGauge
    {
        [FieldOffset(4)]
        private CardType card;

        [FieldOffset(5)]
        private unsafe fixed byte seals[3];

        /// <summary>
        /// Gets the currently drawn <see cref="CardType"/>.
        /// </summary>
        /// <returns>Currently drawn <see cref="CardType"/>.</returns>
        public CardType DrawnCard() => this.card;

        /// <summary>
        /// Check if a <see cref="SealType"/> is currently active on the divination gauge.
        /// </summary>
        /// <param name="seal">The <see cref="SealType"/> to check for.</param>
        /// <returns>If the given Seal is currently divined.</returns>
        public unsafe bool ContainsSeal(SealType seal)
        {
            if (this.seals[0] == (byte)seal) return true;
            if (this.seals[1] == (byte)seal) return true;
            if (this.seals[2] == (byte)seal) return true;
            return false;
        }
    }
}
