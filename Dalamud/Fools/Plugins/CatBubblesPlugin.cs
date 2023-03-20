using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace Dalamud.Fools.Plugins;

public class CatBubblesPlugin : IFoolsPlugin
{
    // Plugin

    private ClientState ClientState;

    public CatBubblesPlugin()
    {
        ClientState = Service<ClientState>.Get();

        var sigscanner = Service<SigScanner>.Get();

        var openAddr = sigscanner.ScanText("E8 ?? ?? ?? ?? C7 43 ?? ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ??");
        BalloonOpen = Marshal.GetDelegateForFunctionPointer<BalloonOpenDelegate>(openAddr);

        var updateAddr = sigscanner.ScanText("48 85 D2 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 57 48 83 EC 20 8B 41 0C");
        BalloonUpdateHook = Hook<BalloonUpdateDelegate>.FromAddress(updateAddr, BalloonUpdateDetour);
        BalloonUpdateHook.Enable();

        Timer.Elapsed += OnTimerElapsed;
        Timer.Interval = Rng.Next(3, 8) * 1000;
        Timer.Start();
    }

    public void Dispose()
    {
        Timer.Elapsed -= OnTimerElapsed;
        Timer.Stop();
        
        BalloonUpdateHook.Disable();
        BalloonUpdateHook.Dispose();
    }

    private void OnTimerElapsed(object sender, object e)
    {
        EngageCatMode = true;
        Timer.Interval = Rng.Next(35, 150) * 1000;
    }

    // meow :3

    private bool EngageCatMode = false;

    private readonly Timer Timer = new();
    private readonly Random Rng = new();

    private readonly List<string> strs1 = new() { "mrrp", "nya", "mew", "meow", "mraow", "purr" };
    private readonly List<string> strs2 = new() { ":3", ":3c", "=^-^=" };
    private readonly List<string> strs3 = new() { "zxcvbnm,./`-=", "qweasdzxc", "fghjkl;mnbvcxz", "plokmijnuhkjgs" };

    private string GetRandStr(List<string> list)
    {
        var x = Rng.Next(list.Count);
        return list[x];
    }

    private string GenerateCatSpeak()
    {
        var items = new List<string>();

        var itemCt = Rng.Next(1, 10) + 1;

        int lastGen = -1;
        bool hasEmoted = false;
        for (var i = 0; i < itemCt; i++)
        {
            var isLast = i == itemCt - 1;
            
            var r = i == 0 ? 0 : Rng.Next(0, 3);
            switch (r)
            {
                case 0:
                    items.Add(GetRandStr(strs1));
                    break;
                case 1:
                    if (hasEmoted && !isLast) goto case default;
                    var item = GetRandStr(strs2);
                    if (lastGen == 0) item = ' ' + item;
                    if (!isLast) item += ' ';
                    items.Add(item);
                    hasEmoted = true;
                    break;
                case 2:
                    if (isLast && lastGen != 1) goto case 1;
                    if (lastGen != 0) goto case default;
                    items.Add(" ");
                    break;
                default:
                    items.Add(GetRandStr(strs3));
                    break;
            }

            lastGen = r;
        }
        
        return string.Join("", items);
    }

    private delegate nint BalloonOpenDelegate(nint a1, nint a2, string a3, bool a4);
    private BalloonOpenDelegate BalloonOpen;

    private delegate nint BalloonUpdateDelegate(nint a1, nint a2, nint a3, nint a4);
    private Hook<BalloonUpdateDelegate> BalloonUpdateHook = null!;

    private unsafe nint BalloonUpdateDetour(nint a1, nint a2, nint a3, nint a4)
    {
        var balloon = (Balloon*)a1;
        if (EngageCatMode && a2 == ClientState.LocalPlayer?.Address && balloon->State == BalloonState.Inactive)
        {
            var text = GenerateCatSpeak();
            balloon->Text.SetString(text);
            balloon->State = BalloonState.Active;
            balloon->Type = BalloonType.Timer;
            balloon->PlayTimer = 5f;
            BalloonOpen(a1, a2, text, balloon->UnkBool == 1);

            EngageCatMode = false;
        }

        return BalloonUpdateHook.Original(a1, a2, a3, a4);
    }
}
