using Lumina.Text.Payloads;

namespace Dalamud.Game.Text;

public class DalamudLinkPayload
{
    /// <summary>
    /// A custom LinkMacroPayloadType for a DalamudLink.
    /// </summary>
    public const LinkMacroPayloadType LinkType = (LinkMacroPayloadType)0x0F;

    public DalamudLinkPayload(uint commandId, string pluginName)
    {
        this.CommandId = commandId;
        this.PluginName = pluginName;
    }

    public DalamudLinkPayload(uint commandId, string pluginName, int extra1, int extra2, string extraString)
    {
        this.CommandId = commandId;
        this.PluginName = pluginName;
        this.Extra1 = extra1;
        this.Extra2 = extra2;
        this.ExtraString = extraString;
    }

    public uint CommandId { get; init; }

    public string PluginName { get; init; }

    public int Extra1 { get; set; }

    public int Extra2 { get; set; }

    public string ExtraString { get; set; }
}
