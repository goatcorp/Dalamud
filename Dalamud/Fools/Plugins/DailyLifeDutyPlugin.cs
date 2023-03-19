using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Fools.Helper;
using Dalamud.Game;

namespace Dalamud.Fools.Plugins;

public class DailyLifeDutyPlugin : IFoolsPlugin
{
    private const string PluginName = "DailyLifeDuty";

    private static readonly List<Duty> Duties = new[]
    {
        new Duty("Dishes", i => $"{i} dishes to be cleaned"),
        new Duty("Taxes", _ => "Taxes need to be filed"),
        new Duty("Pets", i => $"{i} dogs waiting to be pet"),
        new Duty("Garbage", i => $"{i} garbage bags to be put out"),
    }.ToList();

    private long lastMessage;

    public DailyLifeDutyPlugin()
    {
        Service<Framework>.Get().Update += this.OnUpdate;
        this.EmitDutyReminder();
    }

    public void Dispose()
    {
        Service<Framework>.Get().Update -= this.OnUpdate;
    }

    private void OnUpdate(Framework framework)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - this.lastMessage > 60 * 5)
        {
            this.EmitDutyReminder();
        }
    }

    private void EmitDutyReminder()
    {
        var duty = Duties[Random.Shared.Next(Duties.Count)];
        Chat.Print(PluginName, duty.Tag, duty.Message(Random.Shared.Next(20)));
        this.lastMessage = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private class Duty
    {
        public Duty(string tag, Func<int, string> message)
        {
            this.Tag = tag;
            this.Message = message;
        }

        internal string Tag { get; init; }

        internal Func<int, string> Message { get; init; }
    }
}
