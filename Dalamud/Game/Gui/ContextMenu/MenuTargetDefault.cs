using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Network.Structures.InfoProxy;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Target information on a default context menu.
/// </summary>
public sealed unsafe class MenuTargetDefault : MenuTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuTargetDefault"/> class.
    /// </summary>
    /// <param name="context">The agent associated with the context menu.</param>
    internal MenuTargetDefault(AgentContext* context)
    {
        this.Context = context;
    }

    /// <summary>
    /// Gets the name of the target.
    /// </summary>
    public string TargetName => this.Context->TargetName.ToString();

    /// <summary>
    /// Gets the object id of the target.
    /// </summary>
    public ulong TargetObjectId => this.Context->TargetObjectId;

    /// <summary>
    /// Gets the target object.
    /// </summary>
    public GameObject? TargetObject => Service<ObjectTable>.Get().SearchById(this.TargetObjectId);

    /// <summary>
    /// Gets the content id of the target.
    /// </summary>
    public ulong TargetContentId => this.Context->TargetContentId;

    /// <summary>
    /// Gets the home world id of the target.
    /// </summary>
    public ExcelResolver<World> TargetHomeWorld => new((uint)this.Context->TargetHomeWorldId);

    /// <summary>
    /// Gets the currently targeted character. Only shows up for specific targets, like friends, party finder listings, or party members.
    /// Just because this is <see langword="null"/> doesn't mean the target isn't a character.
    /// </summary>
    public CharacterData? TargetCharacter
    {
        get
        {
            var target = this.Context->CurrentContextMenuTarget;
            if (target != null)
                return new(target);
            return null;
        }
    }

    private AgentContext* Context { get; }
}
