using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Addon;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// Class used to interface with the server info bar.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
internal sealed unsafe class DtrBar : IDisposable, IServiceType, IDtrBar
{
    private const uint BaseNodeId = 1000;

    private static readonly ModuleLog Log = new("DtrBar");
    
    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonEventManager uiEventManager = Service<AddonEventManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    private readonly AddonLifecycleEventListener dtrPostDrawListener;
    private readonly AddonLifecycleEventListener dtrPostRequestedUpdateListener;
    
    private readonly ConcurrentBag<DtrBarEntry> newEntries = new();
    private readonly List<DtrBarEntry> entries = new();

    private readonly Dictionary<uint, List<IAddonEventHandle>> eventHandles = new();
    
    private uint runningNodeIds = BaseNodeId;

    [ServiceManager.ServiceConstructor]
    private DtrBar()
    {
        this.dtrPostDrawListener = new AddonLifecycleEventListener(AddonEvent.PostDraw, "_DTR", this.OnDtrPostDraw);
        this.dtrPostRequestedUpdateListener = new AddonLifecycleEventListener(AddonEvent.PostRequestedUpdate, "_DTR", this.OnAddonRequestedUpdateDetour);

        this.addonLifecycle.RegisterListener(this.dtrPostDrawListener);
        this.addonLifecycle.RegisterListener(this.dtrPostRequestedUpdateListener);
        
        this.framework.Update += this.Update;

        this.configuration.DtrOrder ??= new List<string>();
        this.configuration.DtrIgnore ??= new List<string>();
        this.configuration.QueueSave();
    }

    /// <inheritdoc/>
    public DtrBarEntry Get(string title, SeString? text = null)
    {
        if (this.entries.Any(x => x.Title == title) || this.newEntries.Any(x => x.Title == title))
            throw new ArgumentException("An entry with the same title already exists.");

        var entry = new DtrBarEntry(title, null);
        entry.Text = text;

        // Add the entry to the end of the order list, if it's not there already.
        if (!this.configuration.DtrOrder!.Contains(title))
            this.configuration.DtrOrder!.Add(title);

        this.newEntries.Add(entry);

        return entry;
    }
    
    /// <inheritdoc/>
    public void Remove(string title)
    {
        if (this.entries.FirstOrDefault(entry => entry.Title == title) is { } dtrBarEntry)
        {
            dtrBarEntry.Remove();
        }
    }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        this.addonLifecycle.UnregisterListener(this.dtrPostDrawListener);
        this.addonLifecycle.UnregisterListener(this.dtrPostRequestedUpdateListener);

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
        this.HandleAddedNodes();

        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null || dtr->RootNode->ChildNode == null) return;

        // The collision node on the DTR element is always the width of its content
        if (dtr->UldManager.NodeList == null) return;

        // If we have an unmodified DTR but still have entries, we need to
        // work to reset our state.
        if (!this.CheckForDalamudNodes())
            this.RecreateNodes();

        var collisionNode = dtr->GetNodeById(17);
        if (collisionNode == null) return;

        // If we are drawing backwards, we should start from the right side of the collision node. That is,
        // collisionNode->X + collisionNode->Width.
        var runningXPos = this.configuration.DtrSwapDirection
                              ? collisionNode->X + collisionNode->Width
                              : collisionNode->X;

        foreach (var data in this.entries)
        {
            var isHide = this.configuration.DtrIgnore!.Any(x => x == data.Title) || !data.Shown;

            if (data is { Dirty: true, Added: true, Text: not null, TextNode: not null })
            {
                var node = data.TextNode;
                node->SetText(data.Text.Encode());
                ushort w = 0, h = 0;

                if (!isHide)
                {
                    node->GetTextDrawSize(&w, &h, node->NodeText.StringPtr);
                    node->AtkResNode.SetWidth(w);
                }

                node->AtkResNode.ToggleVisibility(!isHide);

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
                    data.TextNode->AtkResNode.SetPositionFloat(runningXPos + this.configuration.DtrSpacing, 2);
                    runningXPos += elementWidth;
                }
                else
                {
                    runningXPos -= elementWidth;
                    data.TextNode->AtkResNode.SetPositionFloat(runningXPos, 2);
                }
            }
            else
            {
                // If we want the node hidden, shift it up, to prevent collision conflicts
                data.TextNode->AtkResNode.SetY(-collisionNode->Height * dtr->RootNode->ScaleX);
            }
        }
    }

    private void HandleAddedNodes()
    {
        if (this.newEntries.Any())
        {
            foreach (var newEntry in this.newEntries)
            {
                newEntry.TextNode = this.MakeNode(++this.runningNodeIds);
                this.entries.Add(newEntry);
            }
            
            this.newEntries.Clear();
            this.ApplySort();
        }
    }
    
    private void OnDtrPostDraw(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addon = (AtkUnitBase*)addonInfo.Addon;

        this.UpdateNodePositions(addon);
            
        if (!this.configuration.DtrSwapDirection)
        {
            var targetSize = (ushort)this.CalculateTotalSize();
            var sizeDelta = MathF.Round((targetSize - addon->RootNode->Width) * addon->RootNode->ScaleX);
                
            if (addon->RootNode->Width != targetSize)
            {
                addon->RootNode->SetWidth(targetSize);
                addon->SetX((short)(addon->GetX() - sizeDelta));
                    
                // force a RequestedUpdate immediately to force the game to right-justify it immediately.
                addon->OnUpdate(AtkStage.GetSingleton()->GetNumberArrayData(), AtkStage.GetSingleton()->GetStringArrayData());
            }
        }
    }
    
    private void UpdateNodePositions(AtkUnitBase* addon)
    {
        // If we grow to the right, we need to left-justify the original elements.
        // else if we grow to the left, the game right-justifies it for us.
        if (this.configuration.DtrSwapDirection)
        {
            var targetSize = (ushort)this.CalculateTotalSize();
            addon->RootNode->SetWidth(targetSize);
            var sizeOffset = addon->GetNodeById(17)->GetX();
            
            var node = addon->RootNode->ChildNode;
            while (node is not null)
            {
                if (node->NodeID < 1000 && node->IsVisible)
                {
                    node->SetX(node->GetX() - sizeOffset);
                }
            
                node = node->PrevSiblingNode;
            }
        }
    }

    private void OnAddonRequestedUpdateDetour(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addon = (AtkUnitBase*)addonInfo.Addon;
        
        this.UpdateNodePositions(addon);
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
        if (this.entries.Any())
        {
            this.eventHandles.Clear();
        }
        
        foreach (var entry in this.entries)
        {
            entry.TextNode = this.MakeNode(++this.runningNodeIds);
            entry.Added = false;
        }
    }

    // Calculates the total width the dtr bar should be
    private float CalculateTotalSize()
    {
        var addon = this.GetDtr();
        if (addon is null || addon->RootNode is null || addon->UldManager.NodeList is null) return 0;

        var totalSize = 0.0f;
        
        foreach (var index in Enumerable.Range(0, addon->UldManager.NodeListCount))
        {
            var node = addon->UldManager.NodeList[index];
            
            // Node 17 is the default CollisionNode that fits over the existing elements
            if (node->NodeID is 17) totalSize += node->Width;
            
            // Node > 1000, are our custom nodes
            if (node->NodeID is > 1000 && node->IsVisible) totalSize += node->Width + this.configuration.DtrSpacing;
        }

        return totalSize;
    }

    private bool AddNode(AtkTextNode* node)
    {
        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null || dtr->UldManager.NodeList == null || node == null) return false;

        this.eventHandles.TryAdd(node->AtkResNode.NodeID, new List<IAddonEventHandle>());
        this.eventHandles[node->AtkResNode.NodeID].AddRange(new List<IAddonEventHandle>
        {
            this.uiEventManager.AddEvent(AddonEventManager.DalamudInternalKey, (nint)dtr, (nint)node, AddonEventType.MouseOver, this.DtrEventHandler),
            this.uiEventManager.AddEvent(AddonEventManager.DalamudInternalKey, (nint)dtr, (nint)node, AddonEventType.MouseOut, this.DtrEventHandler),
            this.uiEventManager.AddEvent(AddonEventManager.DalamudInternalKey, (nint)dtr, (nint)node, AddonEventType.MouseClick, this.DtrEventHandler),
        });
        
        var lastChild = dtr->RootNode->ChildNode;
        while (lastChild->PrevSiblingNode != null) lastChild = lastChild->PrevSiblingNode;
        Log.Debug($"Found last sibling: {(ulong)lastChild:X}");
        lastChild->PrevSiblingNode = (AtkResNode*)node;
        node->AtkResNode.ParentNode = lastChild->ParentNode;
        node->AtkResNode.NextSiblingNode = lastChild;

        dtr->RootNode->ChildCount = (ushort)(dtr->RootNode->ChildCount + 1);
        Log.Debug("Set last sibling of DTR and updated child count");

        dtr->UldManager.UpdateDrawNodeList();
        dtr->UpdateCollisionNodeList(false);
        Log.Debug("Updated node draw list");
        return true;
    }

    private void RemoveNode(AtkTextNode* node)
    {
        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null || dtr->UldManager.NodeList == null || node == null) return;

        this.eventHandles[node->AtkResNode.NodeID].ForEach(handle => this.uiEventManager.RemoveEvent(AddonEventManager.DalamudInternalKey, handle));
        this.eventHandles[node->AtkResNode.NodeID].Clear();

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
        dtr->UpdateCollisionNodeList(false);
        Log.Debug("Updated node draw list");
    }

    private AtkTextNode* MakeNode(uint nodeId)
    {
        var newTextNode = AtkUldManager.CreateAtkTextNode();
        if (newTextNode == null)
        {
            Log.Debug("Failed to allocate memory for AtkTextNode");
            return null;
        }

        newTextNode->AtkResNode.NodeID = nodeId;
        newTextNode->AtkResNode.Type = NodeType.Text;
        newTextNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop | NodeFlags.Enabled | NodeFlags.RespondToMouse | NodeFlags.HasCollision | NodeFlags.EmitsEvents;
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

        newTextNode->TextColor = new ByteColor { R = 255, G = 255, B = 255, A = 255 };
        newTextNode->EdgeColor = new ByteColor { R = 142, G = 106, B = 12, A = 255 };

        return newTextNode;
    }
    
    private void DtrEventHandler(AddonEventType atkEventType, IntPtr atkUnitBase, IntPtr atkResNode)
    {
        var addon = (AtkUnitBase*)atkUnitBase;
        var node = (AtkResNode*)atkResNode;

        if (this.entries.FirstOrDefault(entry => entry.TextNode == node) is not { } dtrBarEntry) return;

        if (dtrBarEntry is { Tooltip: not null })
        {
            switch (atkEventType)
            {
                case AddonEventType.MouseOver:
                    AtkStage.GetSingleton()->TooltipManager.ShowTooltip(addon->ID, node, dtrBarEntry.Tooltip.Encode());
                    break;
                
                case AddonEventType.MouseOut:
                    AtkStage.GetSingleton()->TooltipManager.HideTooltip(addon->ID);
                    break;
            }
        }

        if (dtrBarEntry is { OnClick: not null })
        {
            switch (atkEventType)
            {
                case AddonEventType.MouseOver:
                    this.uiEventManager.SetCursor(AddonCursorType.Clickable);
                    break;
                
                case AddonEventType.MouseOut:
                    this.uiEventManager.ResetCursor();
                    break;
                
                case AddonEventType.MouseClick:
                    dtrBarEntry.OnClick.Invoke();
                    break;
            }
        }
    }
}

/// <summary>
/// Plugin-scoped version of a AddonEventManager service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IDtrBar>]
#pragma warning restore SA1015
internal class DtrBarPluginScoped : IDisposable, IServiceType, IDtrBar
{
    [ServiceManager.ServiceDependency]
    private readonly DtrBar dtrBarService = Service<DtrBar>.Get();

    private readonly Dictionary<string, DtrBarEntry> pluginEntries = new();

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var entry in this.pluginEntries)
        {
            entry.Value.Remove();
        }
        
        this.pluginEntries.Clear();
    }
    
    /// <inheritdoc/>
    public DtrBarEntry Get(string title, SeString? text = null)
    {
        // If we already have a known entry for this plugin, return it.
        if (this.pluginEntries.TryGetValue(title, out var existingEntry)) return existingEntry;

        return this.pluginEntries[title] = this.dtrBarService.Get(title, text);
    }
    
    /// <inheritdoc/>
    public void Remove(string title)
    {
        if (this.pluginEntries.TryGetValue(title, out var existingEntry))
        {
            existingEntry.Remove();
            this.pluginEntries.Remove(title);
        }
    }
}
