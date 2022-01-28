using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Gui.ContextMenus
{
    internal unsafe class ContextMenuReaderWriter
    {
        private readonly AgentContextInterface* agentContextInterface;

        private int atkValueCount;
        private AtkValue* atkValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuReaderWriter"/> class.
        /// </summary>
        /// <param name="agentContextInterface">The AgentContextInterface to act upon.</param>
        /// <param name="atkValueCount">The number of ATK values to consider.</param>
        /// <param name="atkValues">Pointer to the array of ATK values.</param>
        public ContextMenuReaderWriter(AgentContextInterface* agentContextInterface, int atkValueCount, AtkValue* atkValues)
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

        public int AtkValueCount => this.atkValueCount;

        public AtkValue* AtkValues => this.atkValues;

        public int ContextMenuItemCount => this.atkValues[0].Int;

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

        public int HasPreviousIndicatorFlagsIndex
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

        public int HasNextIndicatorFlagsIndex
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

        public int NameIndexOffset
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

        public int IsDisabledIndexOffset
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

        public int SequentialAtkValuesPerContextMenuItem
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate) return 4;

                return 1;
            }
        }

        public int TotalDesiredAtkValuesPerContextMenuItem
        {
            get
            {
                if (this.HasTitle && this.StructLayout == SubContextMenuStructLayout.Alternate) return 4;

                return 2;
            }
        }

        public Vector2? Position
        {
            get
            {
                if (this.HasTitle) return new Vector2(this.atkValues[2].Int, this.atkValues[3].Int);

                return null;
            }
        }

        public unsafe bool IsInventoryContext
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
                if (HasTitle)
                {
                    if (this.atkValues[7].Int == 8)
                    {
                        return SubContextMenuStructLayout.Alternate;
                    }
                    else if (this.atkValues[7].Int == 1)
                    {
                        return SubContextMenuStructLayout.Main;
                    }
                }

                return null;
            }
        }

        public byte NoopAction
        {
            get
            {
                if (IsInventoryContext)
                {
                    return 0xff;
                }
                else
                {
                    return 0x67;
                }
            }
        }

        public byte OpenSubContextMenuAction
        {
            get
            {
                if (IsInventoryContext)
                {
                    // This is actually the action to open the Second Tier context menu and we just hack around it
                    return 0x31;
                }
                else
                {
                    return 0x66;
                }
            }
        }

        public byte? FirstUnhandledAction
        {
            get
            {
                if (this.StructLayout is SubContextMenuStructLayout.Alternate) return 0x68;

                return null;
            }
        }

        /// <summary>
        /// Read the context menu from the agent.
        /// </summary>
        /// <returns>Read menu items.</returns>
        public GameContextMenuItem[] Read()
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
                    var actions = &((AgentInventoryContext*)this.agentContextInterface)->Actions;
                    action = *(actions + contextMenuItemAtkValueBaseIndex);
                }
                else if (this.StructLayout is SubContextMenuStructLayout.Alternate)
                {
                    var redButtonActions = &((AgentContext*)this.agentContextInterface)->Items->RedButtonActions;
                    action = (byte)*(redButtonActions + contextMenuItemIndex);
                }
                else
                {
                    var actions = &((AgentContext*)this.agentContextInterface)->Items->Actions;
                    action = *(actions + contextMenuItemAtkValueBaseIndex);
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
                Buffer.MemoryCopy(this.atkValues, newAtkValues, newAtkValuesArraySize - arrayCountSize, (long)sizeof(AtkValue) * FirstContextMenuItemIndex);

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
                    var actions = &((AgentInventoryContext*)this.agentContextInterface)->Actions;
                    *(actions + this.FirstContextMenuItemIndex + contextMenuItemIndex) = action;
                }
                else if (this.StructLayout is SubContextMenuStructLayout.Alternate && this.FirstUnhandledAction != null)
                {
                    // Some weird placeholder goes here
                    var actions = &((AgentContext*)this.agentContextInterface)->Items->Actions;
                    *(actions + this.FirstContextMenuItemIndex + contextMenuItemIndex) = (byte)(this.FirstUnhandledAction.Value + contextMenuItemIndex);

                    // Make sure there's one of these function pointers for every item.
                    // The function needs to be the same, so we just copy the first one into every index.
                    var unkFunctionPointers = &((AgentContext*)this.agentContextInterface)->Items->UnkFunctionPointers;
                    *(unkFunctionPointers + this.FirstContextMenuItemIndex + contextMenuItemIndex) = *(unkFunctionPointers + this.FirstContextMenuItemIndex);

                    // The real action goes here
                    var redButtonActions = &((AgentContext*)this.agentContextInterface)->Items->RedButtonActions;
                    *(redButtonActions + contextMenuItemIndex) = action;
                }
                else
                {
                    var actions = &((AgentContext*)this.agentContextInterface)->Items->Actions;
                    *(actions + this.FirstContextMenuItemIndex + contextMenuItemIndex) = action;
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

        public void Log()
        {
            Log(this.atkValueCount, this.atkValues);
        }

        public static void Log(int atkValueCount, AtkValue* atkValues)
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
    }
}
