using Lumina.Text.Payloads;

namespace Dalamud.Game.Text;

/// <summary>
/// Represents a custom chat link payload used within the Dalamud framework to trigger plugin-specific commands or actions when clicked.
/// </summary>
public class DalamudLinkPayload
{
    /// <summary>
    /// Represents the custom <see cref="LinkMacroPayloadType"/> identifier reserved for Dalamud links.
    /// </summary>
    public const LinkMacroPayloadType LinkType = LinkMacroPayloadType.Terminator - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudLinkPayload"/> class with basic command details.
    /// </summary>
    /// <param name="commandId">The unique identifier for the command to be executed upon clicking the link.</param>
    /// <param name="pluginName">The internal name of the plugin associated with this link payload.</param>
    public DalamudLinkPayload(uint commandId, string pluginName)
    {
        this.CommandId = commandId;
        this.PluginName = pluginName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudLinkPayload"/> class with command details and additional custom payload data.
    /// </summary>
    /// <param name="commandId">The unique identifier for the command to be executed upon clicking the link.</param>
    /// <param name="pluginName">The internal name of the plugin associated with this link payload.</param>
    /// <param name="extra1">An optional integer parameter for custom payload data.</param>
    /// <param name="extra2">A second optional integer parameter for custom payload data.</param>
    /// <param name="extraString">An optional string parameter for custom payload data.</param>
    public DalamudLinkPayload(uint commandId, string pluginName, int extra1, int extra2, string extraString)
    {
        this.CommandId = commandId;
        this.PluginName = pluginName;
        this.Extra1 = extra1;
        this.Extra2 = extra2;
        this.ExtraString = extraString;
    }

    /// <summary>
    /// Gets the unique command identifier that determines which action or handler is triggered.
    /// </summary>
    public uint CommandId { get; init; }

    /// <summary>
    /// Gets the internal name of the plugin that owns or processes this link.
    /// </summary>
    public string PluginName { get; init; }

    /// <summary>
    /// Gets or sets additional integer metadata associated with the payload.
    /// </summary>
    public int Extra1 { get; set; }

    /// <summary>
    /// Gets or sets secondary additional integer metadata associated with the payload.
    /// </summary>
    public int Extra2 { get; set; }

    /// <summary>
    /// Gets or sets additional string metadata associated with the payload.
    /// </summary>
    public string ExtraString { get; set; }
}
