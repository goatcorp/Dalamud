using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;
// using Dalamud.Hooking;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// This class represents the members of your party.
    /// </summary>
    public sealed partial class PartyList
    {
        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;

        // private bool isReady = false;
        // private IntPtr partyListBegin;
        // private Hook<PartyListUpdateDelegate> partyListUpdateHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyList"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        /// <param name="addressResolver">The ClientStateAddressResolver instance.</param>
        internal PartyList(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.address = addressResolver;
            this.dalamud = dalamud;
            // this.partyListUpdateHook = new Hook<PartyListUpdateDelegate>(Address.PartyListUpdate, new PartyListUpdateDelegate(PartyListUpdateDetour), this);
        }

        private delegate long PartyListUpdateDelegate(IntPtr structBegin, long param2, char param3);

        /// <summary>
        /// Gets the length of the PartyList.
        /// </summary>
        public int Length => 0; // !this.isReady ? 0 : Marshal.ReadByte(this.partyListBegin + 0xF0);

        /// <summary>
        /// Get the nth party member.
        /// </summary>
        /// <param name="index">Index of the party member.</param>
        /// <returns>The party member.</returns>
        public PartyMember this[int index]
        {
            get
            {
                return null;
                // if (!this.isReady)
                //     return null;
                // if (index >= this.Length)
                //     return null;
                // var tblIndex = this.partyListBegin + (index * 24);
                // var memberStruct = Marshal.PtrToStructure<Structs.PartyMember>(tblIndex);
                // return new PartyMember(this.dalamud.ClientState.Actors, memberStruct);
            }
        }

        /// <summary>
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            // TODO Fix for 5.3
            // this.partyListUpdateHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // if (!this.isReady)
            //     this.partyListUpdateHook.Dispose();
            // this.isReady = false;
        }

        // private long PartyListUpdateDetour(IntPtr structBegin, long param2, char param3)
        // {
        //     var result = this.partyListUpdateHook.Original(structBegin, param2, param3);
        //     this.partyListBegin = structBegin + 0xB48;
        //     this.partyListUpdateHook.Dispose();
        //     this.isReady = true;
        //     return result;
        // }
    }

    /// <summary>
    /// Implements IReadOnlyCollection, IEnumerable.
    /// </summary>
    public sealed partial class PartyList : IReadOnlyCollection<PartyMember>
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<PartyMember>.Count => this.Length;

        /// <inheritdoc/>
        public IEnumerator<PartyMember> GetEnumerator()
        {
            for (var i = 0; i < this.Length; i++)
            {
                if (this[i] != null)
                {
                    yield return this[i];
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>
    /// Implements ICollection.
    /// </summary>
    public sealed partial class PartyList : ICollection
    {
        /// <inheritdoc/>
        public int Count => this.Length;

        /// <inheritdoc/>
        public object SyncRoot => this;

        /// <inheritdoc/>
        public bool IsSynchronized => false;

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            for (var i = 0; i < this.Length; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }
    }
}
