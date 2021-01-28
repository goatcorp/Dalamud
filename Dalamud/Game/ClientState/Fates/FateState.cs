#pragma warning disable 1591

namespace Dalamud.Game.ClientState.Fates
{
    public enum FateState : byte
    {
        Running = 0x02,
        Ended = 0x04,
        Failed = 0x05,
        Preparation = 0x07,
        WaitingForEnd = 0x08
    }
}
