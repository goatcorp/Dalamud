using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.UI;
using NoTankYou.DataModels;
using NoTankYou.Utilities;

namespace NoTankYou.System;

public unsafe class PartyListAddon : IEnumerable<PartyListAddonData>, IDisposable
{
    public record PartyFramePositionInfo(Vector2 Position, Vector2 Size, Vector2 Scale);
    public IEnumerator<PartyListAddonData> GetEnumerator() => addonData.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    private static AddonPartyList* PartyList => (AddonPartyList*) Service<GameGui>.Get()?.GetAddonByName("_PartyList");
    public static bool DataAvailable => PartyList != null && PartyList->AtkUnitBase.RootNode != null;

    private readonly List<PartyListAddonData> addonData = new();

    public PartyListAddon()
    {
        Service<Framework>.Get().Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Service<Framework>.Get().Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        addonData.Clear();
        if (!DataAvailable) return;
        if (PartyList->MemberCount <= 0) return;

        foreach (var index in Enumerable.Range(0, PartyList->MemberCount))
        {
            var playerCharacter = HudHelper.GetPlayerCharacter(index);
            var userInterface = PartyList->PartyMember[index];

            addonData.Add(new PartyListAddonData
            {
                PlayerCharacter = playerCharacter,
                UserInterface = userInterface,
            });
        }
    }

    public static PartyFramePositionInfo GetPositionInfo()
    {
        // Resource Node (id 9) contains a weird offset for the actual list elements
        var rootNode = PartyList->AtkUnitBase.RootNode;
        var addonBasePosition = new Vector2(rootNode->X, rootNode->Y);
        var scale = new Vector2(rootNode->ScaleX, rootNode->ScaleY);

        var partyListNode = PartyList->AtkUnitBase.GetNodeById(9);
        var partyListPositionOffset = new Vector2(partyListNode->X, partyListNode->Y) * scale;
        var partyListSize = new Vector2(partyListNode->Width, partyListNode->Height);

        return new PartyFramePositionInfo(addonBasePosition + partyListPositionOffset, partyListSize * scale, scale);
    }
}
