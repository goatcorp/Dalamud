using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;

namespace Dalamud.Game.Gui.Dtr
{
    /// <summary>
    /// Class used to interface with the server info bar.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public unsafe class DtrBar : IDisposable
    {
        /// <summary>
        /// The amount of padding between Server Info UI elements.
        /// </summary>
        private const int ElementPadding = 30;

        private List<DtrBarEntry> entries = new();
        private uint runningNodeIds = 1000;

        /// <summary>
        /// Initializes a new instance of the <see cref="DtrBar"/> class.
        /// </summary>
        public DtrBar()
        {
            Service<Framework>.Get().Update += this.Update;
        }

        /// <summary>
        /// Get a DTR bar entry.
        /// This allows you to add your own text, and users to sort it.
        /// </summary>
        /// <param name="title">A user-friendly name for sorting.</param>
        /// <param name="text">The text the entry shows.</param>
        /// <returns>The entry object used to update, hide and remove the entry.</returns>
        /// <exception cref="ArgumentException">Thrown when an entry with the specified title exists.</exception>
        public DtrBarEntry Get(string title, SeString? text = null)
        {
            if (this.entries.Any(x => x.Title == title))
                throw new ArgumentException("An entry with the same title already exists.");

            var node = this.MakeNode(++this.runningNodeIds);
            var entry = new DtrBarEntry(title, node);
            entry.Text = text;

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
            Service<Framework>.Get().Update -= this.Update;
        }

        /// <summary>
        /// Check whether an entry with the specified title exists.
        /// </summary>
        /// <param name="title">The title to check for.</param>
        /// <returns>Whether or not an entry with that title is registered.</returns>
        internal bool HasEntry(string title) => this.entries.Any(x => x.Title == title);

        private static AtkUnitBase* GetDtr() => (AtkUnitBase*)Service<GameGui>.Get().GetAddonByName("_DTR", 1).ToPointer();

        private void Update(Framework unused)
        {
            var dtr = GetDtr();
            if (dtr == null) return;

            foreach (var data in this.entries.Where(d => d.ShouldBeRemoved))
            {
                this.RemoveNode(data.TextNode);
            }

            this.entries.RemoveAll(d => d.ShouldBeRemoved);

            // The collision node on the DTR element is always the width of its content
            var collisionNode = dtr->UldManager.NodeList[1];
            var runningXPos = collisionNode->X;

            for (var i = 0; i < this.entries.Count; i++)
            {
                var data = this.entries[i];

                if (data.Dirty && data.Added && data.Text != null && data.TextNode != null)
                {
                    var node = data.TextNode;
                    node->SetText(data.Text?.Encode());
                    ushort w = 0, h = 0;

                    if (!data.Shown)
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

                if (data.Shown)
                {
                    runningXPos -= data.TextNode->AtkResNode.Width + ElementPadding;
                    data.TextNode->AtkResNode.SetPositionFloat(runningXPos, 2);
                }

                this.entries[i] = data;
            }
        }

        private bool AddNode(AtkTextNode* node)
        {
            var dtr = GetDtr();
            if (dtr == null || dtr->RootNode == null || node == null) return false;

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
            var dtr = GetDtr();
            if (dtr == null || dtr->RootNode == null || node == null) return false;

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
            newTextNode->AtkResNode.Flags = (short)(NodeFlags.AnchorLeft | NodeFlags.AnchorTop);
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

        private void ApplySort()
        {
            var configuration = Service<DalamudConfiguration>.Get();
            if (configuration.DtrOrder == null)
            {
                configuration.DtrOrder = new List<string>();
                configuration.Save();
            }

            // Sort the current entry list, based on the order in the configuration.
            var ordered = configuration.DtrOrder.Select(entry => this.entries.FirstOrDefault(x => x.Title == entry)).Where(value => value != null).ToList();

            // Add entries that weren't sorted to the end of the list.
            if (ordered.Count != this.entries.Count)
            {
                ordered.AddRange(this.entries.Where(x => ordered.All(y => y.Title != x.Title)));
            }

            // Update the order list for new entries.
            configuration.DtrOrder.Clear();
            foreach (var dtrEntry in ordered)
            {
                configuration.DtrOrder.Add(dtrEntry.Title);
            }

            this.entries = ordered;
        }
    }
}
