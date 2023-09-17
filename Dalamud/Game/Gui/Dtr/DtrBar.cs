using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;

namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// Class used to interface with the server info bar.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IDtrBar>]
#pragma warning restore SA1015
internal sealed unsafe class DtrBar : IDisposable, IServiceType, IDtrBar
{
    private const uint BaseNodeId = 1000;

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private List<DtrBarEntry> entries = new();
    private uint runningNodeIds = BaseNodeId;

    [ServiceManager.ServiceConstructor]
    private DtrBar()
    {
        this.framework.Update += this.Update;

        this.configuration.DtrOrder ??= new List<string>();
        this.configuration.DtrIgnore ??= new List<string>();
        this.configuration.QueueSave();
    }

    /// <inheritdoc/>
    public DtrBarEntry Get(string title, SeString? text = null)
    {
        if (this.entries.Any(x => x.Title == title))
            throw new ArgumentException("An entry with the same title already exists.");

        var node = this.MakeNode(++this.runningNodeIds);
        var entry = new DtrBarEntry(title, node);
        entry.Text = text;

        // Add the entry to the end of the order list, if it's not there already.
        if (!this.configuration.DtrOrder!.Contains(title))
            this.configuration.DtrOrder!.Add(title);
        this.entries.Add(entry);
        this.ApplySort();

        return entry;
    }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        foreach (var entry in this.entries)
            this.RemoveNode(entry.TextNode);

        this.entries.Clear();
        this.framework.Update -= this.Update;
    }

    /// <summary>
    /// Remove nodes marked as "should be removed" from the bar.
    /// </summary>
    internal void HandleRemovedNodes()
    {
        foreach (var data in this.entries.Where(d => d.ShouldBeRemoved))
        {
            this.RemoveNode(data.TextNode);
        }

        this.entries.RemoveAll(d => d.ShouldBeRemoved);
    }

    /// <summary>
    /// Check whether an entry with the specified title exists.
    /// </summary>
    /// <param name="title">The title to check for.</param>
    /// <returns>Whether or not an entry with that title is registered.</returns>
    internal bool HasEntry(string title) => this.entries.Any(x => x.Title == title);

    /// <summary>
    /// Dirty the DTR bar entry with the specified title.
    /// </summary>
    /// <param name="title">Title of the entry to dirty.</param>
    /// <returns>Whether the entry was found.</returns>
    internal bool MakeDirty(string title)
    {
        var entry = this.entries.FirstOrDefault(x => x.Title == title);
        if (entry == null)
            return false;

        entry.Dirty = true;
        return true;
    }

    /// <summary>
    /// Reapply the DTR entry ordering from <see cref="DalamudConfiguration"/>.
    /// </summary>
    internal void ApplySort()
    {
        // Sort the current entry list, based on the order in the configuration.
        var positions = this.configuration
                            .DtrOrder!
                            .Select(entry => (entry, index: this.configuration.DtrOrder!.IndexOf(entry)))
                            .ToDictionary(x => x.entry, x => x.index);

        this.entries.Sort((x, y) =>
        {
            var xPos = positions.TryGetValue(x.Title, out var xIndex) ? xIndex : int.MaxValue;
            var yPos = positions.TryGetValue(y.Title, out var yIndex) ? yIndex : int.MaxValue;
            return xPos.CompareTo(yPos);
        });
    }

    private AtkUnitBase* GetDtr() => (AtkUnitBase*)this.gameGui.GetAddonByName("_DTR").ToPointer();

    private void Update(IFramework unused)
    {
        this.HandleRemovedNodes();

        var dtr = this.GetDtr();
        if (dtr == null) return;

        // The collision node on the DTR element is always the width of its content
        if (dtr->UldManager.NodeList == null) return;

        // If we have an unmodified DTR but still have entries, we need to
        // work to reset our state.
        if (!this.CheckForDalamudNodes())
            this.RecreateNodes();

        var collisionNode = dtr->UldManager.NodeList[1];
        if (collisionNode == null) return;

        // If we are drawing backwards, we should start from the right side of the collision node. That is,
        // collisionNode->X + collisionNode->Width.
        var runningXPos = this.configuration.DtrSwapDirection
                              ? collisionNode->X + collisionNode->Width
                              : collisionNode->X;

        for (var i = 0; i < this.entries.Count; i++)
        {
            var data = this.entries[i];
            var isHide = this.configuration.DtrIgnore!.Any(x => x == data.Title) || !data.Shown;

            if (data.Dirty && data.Added && data.Text != null && data.TextNode != null)
            {
                var node = data.TextNode;
                node->SetText(data.Text?.Encode());
                ushort w = 0, h = 0;

                if (isHide)
                {
                    node->AtkResNode.ToggleVisibility(false);
                }
                else
                {
                    node->AtkResNode.ToggleVisibility(true);
                    node->GetTextDrawSize(&w, &h, node->NodeText.StringPtr);
                    node->AtkResNode.SetWidth(w);
                }

                data.Dirty = false;
            }

            if (!data.Added)
            {
                data.Added = this.AddNode(data.TextNode);
            }

            if (!isHide)
            {
                var elementWidth = data.TextNode->AtkResNode.Width + this.configuration.DtrSpacing;

                if (this.configuration.DtrSwapDirection)
                {
                    data.TextNode->AtkResNode.SetPositionFloat(runningXPos, 2);
                    runningXPos += elementWidth;
                }
                else
                {
                    runningXPos -= elementWidth;
                    data.TextNode->AtkResNode.SetPositionFloat(runningXPos, 2);
                }
            }

            this.entries[i] = data;
        }
    }

    /// <summary>
    /// Checks if there are any Dalamud nodes in the DTR.
    /// </summary>
    /// <returns>True if there are nodes with an ID > 1000.</returns>
    private bool CheckForDalamudNodes()
    {
        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null) return false;

        for (var i = 0; i < dtr->UldManager.NodeListCount; i++)
        {
            if (dtr->UldManager.NodeList[i]->NodeID > 1000)
                return true;
        }

        return false;
    }

    private void RecreateNodes()
    {
        this.runningNodeIds = BaseNodeId;
        foreach (var entry in this.entries)
        {
            entry.TextNode = this.MakeNode(++this.runningNodeIds);
            entry.Added = false;
        }
    }

    private bool AddNode(AtkTextNode* node)
    {
        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null || dtr->UldManager.NodeList == null || node == null) return false;

        var lastChild = dtr->RootNode->ChildNode;
        while (lastChild->PrevSiblingNode != null) lastChild = lastChild->PrevSiblingNode;
        Log.Debug($"Found last sibling: {(ulong)lastChild:X}");
        lastChild->PrevSiblingNode = (AtkResNode*)node;
        node->AtkResNode.ParentNode = lastChild->ParentNode;
        node->AtkResNode.NextSiblingNode = lastChild;

        dtr->RootNode->ChildCount = (ushort)(dtr->RootNode->ChildCount + 1);
        Log.Debug("Set last sibling of DTR and updated child count");

        dtr->UldManager.UpdateDrawNodeList();
        Log.Debug("Updated node draw list");
        return true;
    }

    private bool RemoveNode(AtkTextNode* node)
    {
        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null || dtr->UldManager.NodeList == null || node == null) return false;

        var tmpPrevNode = node->AtkResNode.PrevSiblingNode;
        var tmpNextNode = node->AtkResNode.NextSiblingNode;

        // if (tmpNextNode != null)
        tmpNextNode->PrevSiblingNode = tmpPrevNode;
        if (tmpPrevNode != null)
            tmpPrevNode->NextSiblingNode = tmpNextNode;
        node->AtkResNode.Destroy(true);

        dtr->RootNode->ChildCount = (ushort)(dtr->RootNode->ChildCount - 1);
        Log.Debug("Set last sibling of DTR and updated child count");
        dtr->UldManager.UpdateDrawNodeList();
        Log.Debug("Updated node draw list");
        return true;
    }

    private AtkTextNode* MakeNode(uint nodeId)
    {
        var newTextNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
        if (newTextNode == null)
        {
            Log.Debug("Failed to allocate memory for text node");
            return null;
        }

        IMemorySpace.Memset(newTextNode, 0, (ulong)sizeof(AtkTextNode));
        newTextNode->Ctor();

        newTextNode->AtkResNode.NodeID = nodeId;
        newTextNode->AtkResNode.Type = NodeType.Text;
        newTextNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
        newTextNode->AtkResNode.DrawFlags = 12;
        newTextNode->AtkResNode.SetWidth(22);
        newTextNode->AtkResNode.SetHeight(22);
        newTextNode->AtkResNode.SetPositionFloat(-200, 2);

        newTextNode->LineSpacing = 12;
        newTextNode->AlignmentFontType = 5;
        newTextNode->FontSize = 14;
        newTextNode->TextFlags = (byte)TextFlags.Edge;
        newTextNode->TextFlags2 = 0;

        newTextNode->SetText(" ");

        newTextNode->TextColor.R = 255;
        newTextNode->TextColor.G = 255;
        newTextNode->TextColor.B = 255;
        newTextNode->TextColor.A = 255;

        newTextNode->EdgeColor.R = 142;
        newTextNode->EdgeColor.G = 106;
        newTextNode->EdgeColor.B = 12;
        newTextNode->EdgeColor.A = 255;

        return newTextNode;
    }
}
