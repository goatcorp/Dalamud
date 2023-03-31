using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Fools.Plugins;

public class YesSolicitingPlugin : IFoolsPlugin
{
    private readonly Framework framework;
    private readonly ChatGui chatGui;

    private readonly List<string> firstNames;
    private readonly List<string> lastNames;

    private long nextUpdate;

    public YesSolicitingPlugin()
    {
        this.framework = Service<Framework>.Get();
        this.framework.Update += this.OnUpdate;

        this.chatGui = Service<ChatGui>.Get();

        var dataManager = Service<DataManager>.Get();
        var charaMakeName = dataManager.GetExcelSheet<CharaMakeName>()!;

        this.firstNames = new List<string>();
        this.lastNames = new List<string>();

        for (uint i = 0; i < charaMakeName.RowCount; i++)
        {
            var row = charaMakeName.GetRow(i);
            if (row == null)
            {
                break;
            }

            // moon cats best cats, fight me
            var firstName = row.MiqoteMoonFemale.ToDalamudString().TextValue;
            var lastName = row.MiqoteMoonLastname.ToDalamudString().TextValue;

            if (firstName.Trim() == string.Empty || lastName.Trim() == string.Empty)
            {
                break;
            }

            this.firstNames.Add(firstName);
            this.lastNames.Add(lastName);
        }
    }

    public void Dispose()
    {
        this.framework.Update -= this.OnUpdate;
    }

    private void Emit()
    {
        var firstName = this.firstNames[Random.Shared.Next(0, this.firstNames.Count)];
        var lastName = this.lastNames[Random.Shared.Next(0, this.lastNames.Count)];

        var messages = new List<string>
        {
            // legally required to put "april fools" in each of these so someone doesn't go outing themselves
            "//**goat2023;PLUGIN SELLING FAST AND CHEAP;20 MINUTE WAIT TIME;APRIL FOOLS JOKE;https://goatcorp.github.io/;**//",
            "(GOATCORP.GITHUB.IO) Buy April Fools Joke, Cheap FFXIV Plugins, Fast shipping ~-!<L>@#",
            "Need plugins?|[GOATCORP.GITHUB.IO]|10mins Delivery time|CheapFast100%|[CODE:APRILFOOLS,2023%OFF]",
            "GOATCORP.GITHUB.IO - 10min Delivery time - Cheap - Fast - 100% - CODE:APRILFOOLS,2023%OFF",

            "Like to ERP? Join our Extraordinary Raid Party today!",
            "Bored? Hungry? Visit the Alternate Reality Plugins section today!",
            "Selling iTomestone 14 Pro - has world-first 0.5x mechanic zoom camera, /tell if interested",
            "buying gf 10k gil",
            "ULTIMATE TWENTY NINE HOUR RAID SESSION BEGINS NOW. WATCH LIVE AT twitch.tomestone/xXx_HARDCORE_GAMING_xXx",

            // Copilot wrote this joke and it was so funny I had to keep it
            "looking for group to clear ultimates with. i am 2000+ ilvl tank tell if interested",
            "Are you looking for a night out of fun? Want to meet new people? See things you've never seen before? Meet me at the back alley of Ul'dah at 10pm tonight! Bring a robe.",
        };

        var message = messages[Random.Shared.Next(0, messages.Count)];

        this.chatGui.PrintChat(new XivChatEntry
        {
            Name = $"[YesSoliciting] {firstName} {lastName}",
            Message = message,
            Type = XivChatType.Shout,
        });
    }

    private void OnUpdate(Framework fr)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now >= this.nextUpdate)
        {
            this.Emit();
            this.nextUpdate = now + (60 * 10);
        }
    }
}
