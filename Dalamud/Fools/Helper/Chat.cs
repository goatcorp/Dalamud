using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Dalamud.Fools.Helper;

// Copied from KamiLib's Chat
// https://github.com/MidoriKami/KamiLib/blob/master/ChatCommands/Chat.cs
internal static class Chat
{
    public static void Print(string pluginName, string tag, string message) => Service<ChatGui>.Get().Print(GetBaseString(pluginName, tag, message).BuiltString);

    private static SeStringBuilder GetBaseString(string pluginName, string tag, string message, DalamudLinkPayload? payload = null)
    {
        if (payload is null)
        {
            return new SeStringBuilder()
                   .AddUiForeground($"[{pluginName}] ", 45)
                   .AddUiForeground($"[{tag}] ", 62)
                   .AddText(message);
        }
        else
        {
            return new SeStringBuilder()
                   .AddUiForeground($"[{pluginName}] ", 45)
                   .AddUiForeground($"[{tag}] ", 62)
                   .Add(payload)
                   .AddUiForeground(message, 35)
                   .Add(RawPayload.LinkTerminator);
        }
    }
}
