using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;
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
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// Class used to interface with the server info bar.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DtrBar : IInternalDisposableService, IDtrBar
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
    private readonly AddonLifecycleEventListener dtrPreFinalizeListener;

    private readonly ConcurrentBag<DtrBarEntry> newEntries = new();
    private readonly List<DtrBarEntry> entries = new();

    private readonly Dictionary<uint, List<IAddonEventHandle>> eventHandles = new();

    private Utf8String* emptyString;
    
    private uint runningNodeIds = BaseNodeId;
    private float entryStartPos = float.NaN;

    [ServiceManager.ServiceConstructor]
    private DtrBar()
    {
        this.dtrPostDrawListener = new AddonLifecycleEventListener(AddonEvent.PostDraw, "_DTR", this.FixCollision);
        this.dtrPostRequestedUpdateListener = new AddonLifecycleEventListener(AddonEvent.PostRequestedUpdate, "_DTR", this.FixCollision);
        this.dtrPreFinalizeListener = new AddonLifecycleEventListener(AddonEvent.PreFinalize, "_DTR", this.PreFinalize);

        this.addonLifecycle.RegisterListener(this.dtrPostDrawListener);
        this.addonLifecycle.RegisterListener(this.dtrPostRequestedUpdateListener);
        this.addonLifecycle.RegisterListener(this.dtrPreFinalizeListener);
        
        this.framework.Update += this.Update;

        this.configuration.DtrOrder ??= new List<string>();
        this.configuration.DtrIgnore ??= new List<string>();
        this.configuration.QueueSave();
    }

    /// <summary>
    /// Event type fired each time a DtrEntry was removed.
    /// </summary>
    /// <param name="title">The title of the bar entry.</param>
    internal delegate void DtrEntryRemovedDelegate(string title);

    /// <summary>
    /// Event fired each time a DtrEntry was removed.
    /// </summary>
    internal event DtrEntryRemovedDelegate? DtrEntryRemoved;

    /// <inheritdoc/>
    public IReadOnlyList<IReadOnlyDtrBarEntry> Entries => this.entries;
    
    /// <inheritdoc/>
    public IDtrBarEntry Get(string title, SeString? text = null)
    {
        if (this.entries.Any(x => x.Title == title) || this.newEntries.Any(x => x.Title == title))
            throw new ArgumentException("An entry with the same title already exists.");

        var entry = new DtrBarEntry(this.configuration, title, null);
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
    void IInternalDisposableService.DisposeService()
    {
        this.addonLifecycle.UnregisterListener(this.dtrPostDrawListener);
        this.addonLifecycle.UnregisterListener(this.dtrPostRequestedUpdateListener);
        this.addonLifecycle.UnregisterListener(this.dtrPreFinalizeListener);

        foreach (var entry in this.entries)
            this.RemoveEntry(entry);

        this.entries.Clear();
        this.framework.Update -= this.Update;

        if (this.emptyString != null)
        {
            this.emptyString->Dtor();
            this.emptyString = null;
        }
    }

    /// <summary>
    /// Remove nodes marked as "should be removed" from the bar.
    /// </summary>
    internal void HandleRemovedNodes()
    {
        foreach (var data in this.entries)
        {
            if (data.ShouldBeRemoved)
            {
                this.RemoveEntry(data);
                this.DtrEntryRemoved?.Invoke(data.Title);
            }
        }

        this.entries.RemoveAll(d => d.ShouldBeRemoved);
    }

    /// <summary>
    /// Remove native resources for the specified entry.
    /// </summary>
    /// <param name="toRemove">The resources to remove.</param>
    internal void RemoveEntry(DtrBarEntry toRemove)
    {
        this.RemoveNode(toRemove.TextNode);

        if (toRemove.Storage != null)
        {
            toRemove.Storage->Dtor(true);
            toRemove.Storage = null;
        }
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
        if (!this.CheckForDalamudNodes(dtr))
            this.RecreateNodes();

        var collisionNode = dtr->GetNodeById(17);
        if (collisionNode == null) return;

        // We haven't calculated the native size yet, so we don't know where to start positioning.
        if (float.IsNaN(this.entryStartPos)) return;

        var runningXPos = this.entryStartPos;

        foreach (var data in this.entries)
        {
            if (!data.Added)
            {
                data.Added = this.AddNode(data.TextNode);
                data.Dirty = true;
            }

            var isHide = !data.Shown || data.UserHidden;
            var node = data.TextNode;
            var nodeHidden = !node->AtkResNode.IsVisible();

            if (!isHide)
            {
                if (nodeHidden)
                    node->AtkResNode.ToggleVisibility(true);

                if (data is { Added: true, Text: not null, TextNode: not null } && (data.Dirty || nodeHidden))
                {
                    if (data.Storage == null)
                    {
                        data.Storage = Utf8String.CreateEmpty();
                    }

                    data.Storage->SetString(data.Text.EncodeWithNullTerminator());
                    node->SetText(data.Storage->StringPtr);

                    ushort w = 0, h = 0;
                    node->GetTextDrawSize(&w, &h, node->NodeText.StringPtr);
                    node->AtkResNode.SetWidth(w);
                }

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
            else if (!nodeHidden)
            {
                // If we want the node hidden, shift it up, to prevent collision conflicts
                node->AtkResNode.SetYFloat(-collisionNode->Height * dtr->RootNode->ScaleX);
                node->AtkResNode.ToggleVisibility(false);
            }

            data.Dirty = false;
        }
    }

    private void HandleAddedNodes()
    {
        if (!this.newEntries.IsEmpty)
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
    
    private void FixCollision(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addon = (AtkUnitBase*)addonInfo.Addon;
        if (addon->RootNode is null || addon->UldManager.NodeList is null) return;

        float minX = addon->RootNode->Width;
        var additionalWidth = 0;
        AtkResNode* collisionNode = null;

        foreach (var index in Enumerable.Range(0, addon->UldManager.NodeListCount))
        {
            var node = addon->UldManager.NodeList[index];
            if (node->IsVisible())
            {
                var nodeId = node->NodeId;
                var nodeType = node->Type;

                if (nodeType == NodeType.Collision)
                {
                    collisionNode = node;
                }
                else if (nodeId >= BaseNodeId)
                {
                    // Dalamud-created node
                    additionalWidth += node->Width + this.configuration.DtrSpacing;
                }
                else if ((nodeType == NodeType.Res || (ushort)nodeType >= 1000) &&
                         (node->ChildNode == null || node->ChildNode->IsVisible()))
                {
                    // Native top-level node. These are are either res nodes or button components.
                    // Both the node and its child (if it has one) must be visible for the node to be counted.
                    minX = MathF.Min(minX, node->X);
                }
            }
        }

        if (collisionNode == null) return;

        var nativeWidth = addon->RootNode->Width - (int)minX;
        var targetX = minX - (this.configuration.DtrSwapDirection ? 0 : additionalWidth);
        var targetWidth = (ushort)(nativeWidth + additionalWidth);

        if (collisionNode->Width != targetWidth || Math.Abs(collisionNode->X - targetX) > 0.0001)
        {
            collisionNode->SetWidth(targetWidth);
            collisionNode->SetXFloat(targetX);
        }

        // If we are drawing backwards, we should start from the right side of the native nodes.
        this.entryStartPos = this.configuration.DtrSwapDirection ? minX + nativeWidth : minX;
    }

    private void PreFinalize(AddonEvent type, AddonArgs args)
    {
        this.entryStartPos = float.NaN;
    }

    /// <summary>
    /// Checks if there are any Dalamud nodes in the DTR.
    /// </summary>
    /// <returns>True if there are nodes with an ID > 1000.</returns>
    private bool CheckForDalamudNodes(AtkUnitBase* dtr)
    {
        for (var i = 0; i < dtr->UldManager.NodeListCount; i++)
        {
            if (dtr->UldManager.NodeList[i]->NodeId > 1000)
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

    private bool AddNode(AtkTextNode* node)
    {
        var dtr = this.GetDtr();
        if (dtr == null || dtr->RootNode == null || dtr->UldManager.NodeList == null || node == null) return false;

        this.eventHandles.TryAdd(node->AtkResNode.NodeId, new List<IAddonEventHandle>());
        this.eventHandles[node->AtkResNode.NodeId].AddRange(new List<IAddonEventHandle>
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

        this.eventHandles[node->AtkResNode.NodeId].ForEach(handle => this.uiEventManager.RemoveEvent(AddonEventManager.DalamudInternalKey, handle));
        this.eventHandles[node->AtkResNode.NodeId].Clear();

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
        var newTextNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>(); // AtkUldManager.CreateAtkTextNode();
        if (newTextNode == null)
        {
            Log.Debug("Failed to allocate memory for AtkTextNode");
            return null;
        }

        newTextNode->AtkResNode.NodeId = nodeId;
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

        if (this.emptyString == null)
            this.emptyString = Utf8String.FromString(" ");
        
        newTextNode->SetText(this.emptyString->StringPtr);

        newTextNode->TextColor = new ByteColor { R = 255, G = 255, B = 255, A = 255 };
        newTextNode->EdgeColor = new ByteColor { R = 142, G = 106, B = 12, A = 255 };

        // ICreatable was restored, this may be necessary if AtkUldManager.CreateAtkTextNode(); is used instead of Create<T>
        // // Memory is filled with random data after being created, zero out some things to avoid issues.
        // newTextNode->UnkPtr_1 = null;
        // newTextNode->SelectStart = 0;
        // newTextNode->SelectEnd = 0;
        // newTextNode->FontCacheHandle = 0;
        // newTextNode->CharSpacing = 0;
        // newTextNode->BackgroundColor = new ByteColor { R = 0, G = 0, B = 0, A = 0 };
        // newTextNode->TextId = 0;
        // newTextNode->SheetType = 0;

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
                    AtkStage.Instance()->TooltipManager.ShowTooltip(addon->Id, node, dtrBarEntry.Tooltip.Encode());
                    break;
                
                case AddonEventType.MouseOut:
                    AtkStage.Instance()->TooltipManager.HideTooltip(addon->Id);
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
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IDtrBar>]
#pragma warning restore SA1015
internal class DtrBarPluginScoped : IInternalDisposableService, IDtrBar
{
    [ServiceManager.ServiceDependency]
    private readonly DtrBar dtrBarService = Service<DtrBar>.Get();

    private readonly Dictionary<string, IDtrBarEntry> pluginEntries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DtrBarPluginScoped"/> class.
    /// </summary>
    internal DtrBarPluginScoped()
    {
        this.dtrBarService.DtrEntryRemoved += this.OnDtrEntryRemoved;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IReadOnlyDtrBarEntry> Entries => this.dtrBarService.Entries;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.dtrBarService.DtrEntryRemoved -= this.OnDtrEntryRemoved;

        foreach (var entry in this.pluginEntries)
        {
            entry.Value.Remove();
        }
        
        this.pluginEntries.Clear();
    }

    /// <inheritdoc/>
    public IDtrBarEntry Get(string title, SeString? text = null)
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

    private void OnDtrEntryRemoved(string title)
    {
        this.pluginEntries.Remove(title);
    }
}
