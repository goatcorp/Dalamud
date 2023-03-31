using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Dalamud.Fools.Helper.YesHealMe;

public static unsafe class HudHelper
{
    private static AgentHUD* AgentHud => AgentModule.Instance()->GetAgentHUD();

    public static PlayerCharacter? GetPlayerCharacter(int index)
    {
        // Sorta temporary, waiting for ClientStructs to merge a fixed size array for this element
        var partyMemberList = AgentHud->PartyMemberList;
        var targetOffset = index * sizeof(HudPartyMember);
        var targetAddress = partyMemberList + targetOffset;
        var hudData = (HudPartyMember*)targetAddress;

        var targetPlayer = hudData->ObjectId;

        return GetPlayer(targetPlayer);
    }

    private static PlayerCharacter? GetPlayer(uint objectId)
    {
        var result = Service<ObjectTable>.Get().SearchById(objectId);

        if (result?.GetType() == typeof(PlayerCharacter))
        {
            return result as PlayerCharacter;
        }

        return null;
    }
}
