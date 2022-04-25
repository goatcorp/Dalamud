using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Game.Gui.ContextMenus.OldStructs;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Class responsible for reading and writing to context menu data.
    /// </summary>
    internal unsafe class ContextMenuReaderWriter
    {
        private readonly OldAgentContextInterface* agentContextInterface;

        private int atkValueCount;
        private AtkValue* atkValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuReaderWriter"/> class.
        /// </summary>
        /// <param name="agentContextInterface">The AgentContextInterface to act upon.</param>
        /// <param name="atkValueCount">The number of ATK values to consider.</param>
        /// <param name="atkValues">Pointer to the array of ATK values.</param>
        public ContextMenuReaderWriter(OldAgentContextInterface* agentContextInterface, int atkValueCount, AtkValue* atkValues)
        {
            PluginLog.Warning($"{(IntPtr)atkValues:X}");

            this.agentContextInterface = agentContextInterface;
            this.atkValueCount = atkValueCount;
            this.atkValues = atkValues;
        }

        private enum SubContextMenuStructLayout
        {
            Main,
            Alternate,
        }

        /// <summary>
        /// Gets the number of AtkValues for this context menu.
        /// </summary>
        public int AtkValueCount => this.atkValueCount;

        /// <summary>
        /// Gets the AtkValues for this context menu.
        /// </summary>
        public AtkValue* AtkValues => this.atkValues;

        /// <summary>
        /// Gets the amount of items in the context menu.
        /// </summary>
        public int ContextMenuItemCount => this.atkValues[0].Int;

        /// <summary>
        /// Gets a value indicating whether the context menu has a title.
        /// </summary>
        public bool HasTitle
        {
            get
            {
                bool isStringType =
                    (int)this.atkValues[1].Type == 8
                    || (int)this.atkValues[1].Type == 38
                    || this.atkValues[1].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;

                return isStringType;
            }
        }

        /// <summary>
        /// Gets the title of the context menu.
        /// </summary>
        public SeString? Title
        {
            get
            {
                if (this.HasTitle && (&this.atkValues[1])->String != null)
                {
                    MemoryHelper.ReadSeStringNullTerminated((IntPtr)(&this.atkValues[1])->String, out var str);
                    return str;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the index of the first context menu item.
        /// </summary>
        public int FirstContextMenuItemIndex
        {
            get
            {
                if (this.HasTitle)
                {
                    return 8;
                }

                return 7;
            }
        }

        /// <summary>
        /// Gets the position of the context menu.
        /// </summary>
        public Vector2? Position
        {
            get
            {
                if (this.HasTitle) return new Vector2(this.atkValues[2].Int, this.atkValues[3].Int);

                return null;
            }
        }

        private int HasPreviousIndicatorFlagsIndex
        {
            get
            {
                if (this.HasTitle)
                {
                    return 6;
                }

                return 2;
            }
        }

        private int HasNextIndicatorFlagsIndex
        {
            get
            {
                if (this.HasTitle)
                {
                    return 5;
                }

                return 3;
            }
        }

        private int NameIndexOffset
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 1;
                }

                return 0;
            }
        }

        private int IsDisabledIndexOffset
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate)
                {
                    return 2;
                }

                return this.ContextMenuItemCount;
            }
        }

        /*
        /// <summary>
        /// 0x14000000 | action
        /// </summary>
        public int? MaskedActionIndexOffset
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate) return 3;

                return null;
            }
        }
        */

        private int SequentialAtkValuesPerContextMenuItem
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate) return 4;

                return 1;
            }
        }

        private int TotalDesiredAtkValuesPerContextMenuItem
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate) return 4;

                return 2;
            }
        }

        private bool IsInventoryContext
        {
            get
            {
                if ((IntPtr)this.agentContextInterface == (IntPtr)AgentInventoryContext.Instance())
                {
                    return true;
                }

                return false;
            }
        }

        private SubContextMenuStructLayout? StructLayout
        {
            get
            {
                if (this.HasTitle)
                {
                    if (this.atkValues[7].Int == 8)
                        return SubContextMenuStructLayout.Alternate;

                    if (this.atkValues[7].Int == 1) return SubContextMenuStructLayout.Main;
                }

                return null;
            }
        }

        private byte NoopAction
        {
            get
            {
                if (this.IsInventoryContext)
                    return 0xff;
                return 0x67;
            }
        }

        private byte OpenSubContextMenuAction
        {
            get
            {
                if (this.IsInventoryContext)
                {
                    // This is actually the action to open the Second Tier context menu and we just hack around it
                    return 0x31;
                }

                return 0x66;
            }
        }

        private byte? FirstUnhandledAction
        {
            get
            {
                if (this.StructLayout is SubContextMenuStructLayout.Alternate)
                    return 0x68;

                return null;
            }
        }

        /// <summary>
        /// Read the context menu from the agent.
        /// </summary>
        /// <returns>Read menu items.</returns>
        public GameContextMenuItem[]? Read()
        {
            var gameContextMenuItems = new List<GameContextMenuItem>();
            for (var contextMenuItemIndex = 0; contextMenuItemIndex < this.ContextMenuItemCount; contextMenuItemIndex++)
            {
                var contextMenuItemAtkValueBaseIndex = this.FirstContextMenuItemIndex + (contextMenuItemIndex * this.SequentialAtkValuesPerContextMenuItem);

                // Get the name
                var nameAtkValue = &this.atkValues[contextMenuItemAtkValueBaseIndex + this.NameIndexOffset];
                if (nameAtkValue->Type == 0)
                {
                    continue;
                }

                var name = MemoryHelper.ReadSeStringNullTerminated((IntPtr)nameAtkValue->String);

                // Get the enabled state. Note that SE stores this as IsDisabled, NOT IsEnabled (those heathens)
                var isEnabled = true;
                var isDisabledDefined = this.FirstContextMenuItemIndex + this.ContextMenuItemCount < this.AtkValueCount;
                if (isDisabledDefined)
                {
                    var isDisabledAtkValue = &this.atkValues[contextMenuItemAtkValueBaseIndex + this.IsDisabledIndexOffset];
                    isEnabled = isDisabledAtkValue->Int == 0;
                }

                // Get the action
                byte action;
                if (this.IsInventoryContext)
                {
                    var actions = &((OldAgentInventoryContext*)this.agentContextInterface)->Actions;
                    action = *(actions + contextMenuItemAtkValueBaseIndex);
                }
                else if (this.StructLayout is SubContextMenuStructLayout.Alternate)
                {
                    var redButtonActions = &((OldAgentContext*)this.agentContextInterface)->Items->RedButtonActions;
                    action = (byte)*(redButtonActions + contextMenuItemIndex);
                }
                else if (((OldAgentContext*)this.agentContextInterface)->Items != null)
                {
                    var actions = &((OldAgentContext*)this.agentContextInterface)->Items->Actions;
                    action = *(actions + contextMenuItemAtkValueBaseIndex);
                }
                else
                {
                    PluginLog.Warning("Context Menu action failed, Items pointer was unexpectedly null.");
                    return null;
                }

                // Get the has previous indicator flag
                var hasPreviousIndicatorFlagsAtkValue = &this.atkValues[this.HasPreviousIndicatorFlagsIndex];
                var hasPreviousIndicator = this.HasFlag(hasPreviousIndicatorFlagsAtkValue->UInt, contextMenuItemIndex);

                // Get the has next indicator flag
                var hasNextIndicatorFlagsAtkValue = &this.atkValues[this.HasNextIndicatorFlagsIndex];
                var hasNextIndicator = this.HasFlag(hasNextIndicatorFlagsAtkValue->UInt, contextMenuItemIndex);

                var indicator = ContextMenuItemIndicator.None;
                if (hasPreviousIndicator)
                {
                    indicator = ContextMenuItemIndicator.Previous;
                }
                else if (hasNextIndicator)
                {
                    indicator = ContextMenuItemIndicator.Next;
                }

                var gameContextMenuItem = new GameContextMenuItem(name, action)
                {
                    IsEnabled = isEnabled,
                    Indicator = indicator,
                };

                gameContextMenuItems.Add(gameContextMenuItem);
            }

            return gameContextMenuItems.ToArray();
        }

        /// <summary>
        /// Write items to the context menu.
        /// </summary>
        /// <param name="contextMenuItems">The items to write.</param>
        /// <param name="allowReallocate">Whether or not reallocation is allowed.</param>
        public void Write(IEnumerable<ContextMenuItem> contextMenuItems, bool allowReallocate = true)
        {
            if (allowReallocate)
            {
                var newAtkValuesCount = this.FirstContextMenuItemIndex + (contextMenuItems.Count() * this.TotalDesiredAtkValuesPerContextMenuItem);

                // Allocate the new array. We have to do a little dance with the first 8 bytes which represents the array count
                const int arrayCountSize = 8;
                var newAtkValuesArraySize = arrayCountSize + (Marshal.SizeOf<AtkValue>() * newAtkValuesCount);
                var newAtkValuesArray = MemoryHelper.GameAllocateUi((ulong)newAtkValuesArraySize);
                if (newAtkValuesArray == IntPtr.Zero)
                {
                    return;
                }

                var newAtkValues = (AtkValue*)(newAtkValuesArray + arrayCountSize);

                // Zero the memory, then copy the atk values up to the first context menu item atk value
                Marshal.Copy(new byte[newAtkValuesArraySize], 0, newAtkValuesArray, newAtkValuesArraySize);
                Buffer.MemoryCopy(this.atkValues, newAtkValues, newAtkValuesArraySize - arrayCountSize, (long)sizeof(AtkValue) * this.FirstContextMenuItemIndex);

                // Free the old array
                var oldArray = (IntPtr)this.atkValues - arrayCountSize;
                var oldArrayCount = *(ulong*)oldArray;
                var oldArraySize = arrayCountSize + ((ulong)sizeof(AtkValue) * oldArrayCount);
                MemoryHelper.GameFree(ref oldArray, oldArraySize);

                // Set the array count
                *(ulong*)newAtkValuesArray = (ulong)newAtkValuesCount;

                this.atkValueCount = newAtkValuesCount;
                this.atkValues = newAtkValues;
            }

            // Set the context menu item count
            const int contextMenuItemCountAtkValueIndex = 0;
            var contextMenuItemCountAtkValue = &this.atkValues[contextMenuItemCountAtkValueIndex];
            contextMenuItemCountAtkValue->UInt = (uint)contextMenuItems.Count();

            // Clear the previous arrow flags
            var hasPreviousIndicatorAtkValue = &this.atkValues[this.HasPreviousIndicatorFlagsIndex];
            hasPreviousIndicatorAtkValue->UInt = 0;

            // Clear the next arrow flags
            var hasNextIndiactorFlagsAtkValue = &this.atkValues[this.HasNextIndicatorFlagsIndex];
            hasNextIndiactorFlagsAtkValue->UInt = 0;

            for (var contextMenuItemIndex = 0; contextMenuItemIndex < contextMenuItems.Count(); ++contextMenuItemIndex)
            {
                var contextMenuItem = contextMenuItems.ElementAt(contextMenuItemIndex);

                var contextMenuItemAtkValueBaseIndex = this.FirstContextMenuItemIndex + (contextMenuItemIndex * this.SequentialAtkValuesPerContextMenuItem);

                // Set the name
                var nameAtkValue = &this.atkValues[contextMenuItemAtkValueBaseIndex + this.NameIndexOffset];
                nameAtkValue->ChangeType(ValueType.String);
                fixed (byte* nameBytesPtr = contextMenuItem.Name.Encode().NullTerminate())
                {
                    nameAtkValue->SetString(nameBytesPtr);
                }

                // Set the enabled state. Note that SE stores this as IsDisabled, NOT IsEnabled (those heathens)
                var disabledAtkValue = &this.atkValues[contextMenuItemAtkValueBaseIndex + this.IsDisabledIndexOffset];
                disabledAtkValue->ChangeType(ValueType.Int);
                disabledAtkValue->Int = contextMenuItem.IsEnabled ? 0 : 1;

                // Set the action
                byte action = 0;
                if (contextMenuItem is GameContextMenuItem gameContextMenuItem)
                {
                    action = gameContextMenuItem.SelectedAction;
                }
                else if (contextMenuItem is CustomContextMenuItem customContextMenuItem)
                {
                    action = this.NoopAction;
                }
                else if (contextMenuItem is OpenSubContextMenuItem openSubContextMenuItem)
                {
                    action = this.OpenSubContextMenuAction;
                }

                if (this.IsInventoryContext)
                {
                    var actions = &((OldAgentInventoryContext*)this.agentContextInterface)->Actions;
                    *(actions + this.FirstContextMenuItemIndex + contextMenuItemIndex) = action;
                }
                else if (this.StructLayout is SubContextMenuStructLayout.Alternate && this.FirstUnhandledAction != null)
                {
                    // Some weird placeholder goes here
                    var actions = &((OldAgentContext*)this.agentContextInterface)->Items->Actions;
                    *(actions + this.FirstContextMenuItemIndex + contextMenuItemIndex) = (byte)(this.FirstUnhandledAction.Value + contextMenuItemIndex);

                    // Make sure there's one of these function pointers for every item.
                    // The function needs to be the same, so we just copy the first one into every index.
                    var unkFunctionPointers = &((OldAgentContext*)this.agentContextInterface)->Items->UnkFunctionPointers;
                    *(unkFunctionPointers + this.FirstContextMenuItemIndex + contextMenuItemIndex) = *(unkFunctionPointers + this.FirstContextMenuItemIndex);

                    // The real action goes here
                    var redButtonActions = &((OldAgentContext*)this.agentContextInterface)->Items->RedButtonActions;
                    *(redButtonActions + contextMenuItemIndex) = action;
                }
                else if (((OldAgentContext*)this.agentContextInterface)->Items != null)
                {
                    // TODO: figure out why this branch is reached on inventory contexts and why Items is sometimes null.
                    var actions = &((OldAgentContext*)this.agentContextInterface)->Items->Actions;
                    *(actions + this.FirstContextMenuItemIndex + contextMenuItemIndex) = action;
                }
                else
                {
                    PluginLog.Warning("Context Menu action failed, Items pointer was unexpectedly null.");
                }

                if (contextMenuItem.Indicator == ContextMenuItemIndicator.Previous)
                {
                    this.SetFlag(ref hasPreviousIndicatorAtkValue->UInt, contextMenuItemIndex, true);
                }
                else if (contextMenuItem.Indicator == ContextMenuItemIndicator.Next)
                {
                    this.SetFlag(ref hasNextIndiactorFlagsAtkValue->UInt, contextMenuItemIndex, true);
                }
            }
        }

        private bool HasFlag(uint mask, int itemIndex)
        {
            return (mask & (1 << itemIndex)) > 0;
        }

        private void SetFlag(ref uint mask, int itemIndex, bool value)
        {
            mask &= ~(1U << itemIndex);

            if (value)
            {
                mask |= (uint)(1 << itemIndex);
            }
        }

        /*
        private void Log()
        {
            Log(this.atkValueCount, this.atkValues);
        }

        private static void Log(int atkValueCount, AtkValue* atkValues)
        {
            PluginLog.Debug($"ContextMenuReader.Log");

            for (int atkValueIndex = 0; atkValueIndex < atkValueCount; ++atkValueIndex)
            {
                var atkValue = &atkValues[atkValueIndex];

                object? value;
                switch (atkValue->Type)
                {
                    case ValueType.Int:
                        value = atkValue->Int;
                        break;
                    case ValueType.Bool:
                        value = atkValue->Byte;
                        break;
                    case ValueType.UInt:
                        value = atkValue->UInt;
                        break;
                    case ValueType.Float:
                        value = atkValue->Float;
                        break;
                    default:
                    {
                        if (atkValue->Type == ValueType.String
                            || (int)atkValue->Type == 38
                            || (int)atkValue->Type == 8)
                        {
                            value = MemoryHelper.ReadSeStringNullTerminated((IntPtr)atkValue->String);
                        }
                        else
                        {
                            value = $"{(IntPtr)atkValue->String:X}";
                        }

                        break;
                    }
                }

                PluginLog.Debug($"atkValues[{atkValueIndex}]={(IntPtr)atkValue:X}   {atkValue->Type}={value}");
            }
        }
        */
    }
}
