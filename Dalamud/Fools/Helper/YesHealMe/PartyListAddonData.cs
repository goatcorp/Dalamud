using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Fools.Helper.YesHealMe;

public readonly struct PartyListAddonData
{
    public AddonPartyList.PartyListMemberStruct UserInterface { get; init; }
    public PlayerCharacter? PlayerCharacter { get; init; }
}
