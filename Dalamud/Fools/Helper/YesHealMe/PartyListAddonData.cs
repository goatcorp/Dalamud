using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace NoTankYou.DataModels;

public readonly unsafe struct PartyListAddonData
{
    private static readonly Dictionary<uint, Stopwatch> TimeSinceLastTargetable = new();

    public AddonPartyList.PartyListMemberStruct UserInterface { get; init; }
    public PlayerCharacter? PlayerCharacter { get; init; }

    private bool Targetable => UserInterface.PartyMemberComponent->OwnerNode->AtkResNode.Color.A != 0x99;
    
    public bool IsTargetable()
    {
        if (PlayerCharacter is null) return false;

        TimeSinceLastTargetable.TryAdd(PlayerCharacter.ObjectId, Stopwatch.StartNew());
        var stopwatch = TimeSinceLastTargetable[PlayerCharacter.ObjectId];
            
        if (Targetable)
        {
            // Returns true if the party member has been targetable for 2second or more
            return stopwatch.Elapsed >= TimeSpan.FromSeconds(2);
        }
        else
        {
            // Returns false, and continually resets the stopwatch
            stopwatch.Restart();
            return false;
        }
    }
}