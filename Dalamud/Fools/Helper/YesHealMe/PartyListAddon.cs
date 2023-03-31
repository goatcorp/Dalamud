using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Fools.Helper.YesHealMe;

public unsafe class PartyListAddon : IEnumerable<PartyListAddonData>, IDisposable
{
    private readonly List<PartyListAddonData> addonData = new();

    public PartyListAddon()
    {
        Service<Framework>.Get().Update += this.OnFrameworkUpdate;
    }

    private static AddonPartyList* PartyList => (AddonPartyList*)Service<GameGui>.Get()?.GetAddonByName("_PartyList");
    private static bool DataAvailable => PartyList != null && PartyList->AtkUnitBase.RootNode != null;

    public void Dispose()
    {
        Service<Framework>.Get().Update -= this.OnFrameworkUpdate;
    }

    public IEnumerator<PartyListAddonData> GetEnumerator()
    {
        return this.addonData.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        this.addonData.Clear();
        if (!DataAvailable || PartyList->MemberCount <= 0)
        {
            return;
        }

        foreach (var index in Enumerable.Range(0, PartyList->MemberCount))
        {
            var playerCharacter = HudHelper.GetPlayerCharacter(index);
            var userInterface = PartyList->PartyMember[index];

            this.addonData.Add(new PartyListAddonData
            {
                PlayerCharacter = playerCharacter,
                UserInterface = userInterface,
            });
        }
    }
}
