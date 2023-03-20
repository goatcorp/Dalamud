using System.Linq;
using Dalamud.Logging;
using NoTankYou.System;

namespace Dalamud.Fools.Plugins;

public class YesHealMePlugin: IFoolsPlugin
{
    private PartyListAddon partyListAddon;

    public YesHealMePlugin()
    {
        partyListAddon = new PartyListAddon();
    }

    public void DrawUi()
    {
        foreach (var partyMember in this.partyListAddon.Select(pla => pla.PlayerCharacter).Where(pc => pc is not null))
        {
            if (partyMember.CurrentHp < partyMember.MaxHp)
            {
                // Do things here
            }
        }
    }


    public void Dispose()
    {
        this.partyListAddon.Dispose();
    }
}
