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
        new Duty("Dishes", i => $"{i} dish(es) to be cleaned"),
        new Duty("Taxes", _ => "Taxes need to be filed"),
        new Duty("Pets", i => $"{i} dog(s) waiting to be pet"),
        new Duty("Garbage", i => $"{i} garbage bag(s) to be put out"),
        new Duty("Bank", i => $"{i} bill(s) waiting payment"),
        new Duty("Hydration", i => $"{i} glasses(s) of water remaining to reach Full Hydration"),

        // new Duty("FINAL FANTASY XIV", i => $"At least {i} minute(s) left on your sub... maybe. Time is relative."),
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
