using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Hooking;

namespace Dalamud.Game.ClientState
{
    public class PartyList : IReadOnlyCollection<PartyMember>, ICollection, IDisposable
    {
        private ClientStateAddressResolver Address { get; }

        private Dalamud dalamud;

        private delegate long PartyListUpdateDelegate(IntPtr structBegin, long param2, char param3);

        private Hook<PartyListUpdateDelegate> partyListUpdateHook;
        private IntPtr partyListBegin;
        private bool isReady = false;

        public PartyList(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.Address = addressResolver;
            this.dalamud = dalamud;
            // this.partyListUpdateHook = new Hook<PartyListUpdateDelegate>(Address.PartyListUpdate, new PartyListUpdateDelegate(PartyListUpdateDetour), this);
        }

        public void Enable()
        {
            // TODO Fix for 5.3
            // this.partyListUpdateHook.Enable();
        }

        public void Dispose()
        {
            // if (!this.isReady)
            //     this.partyListUpdateHook.Dispose();
            this.isReady = false;
        }

        private long PartyListUpdateDetour(IntPtr structBegin, long param2, char param3)
        {
            var result = this.partyListUpdateHook.Original(structBegin, param2, param3);
            this.partyListBegin = structBegin + 0xB48;
            this.partyListUpdateHook.Dispose();
            this.isReady = true;
            return result;
        }

        public PartyMember this[int index]
        {
            get
            {
                if (!this.isReady)
                    return null;
                if (index >= this.Length)
                    return null;
                var tblIndex = this.partyListBegin + (index * 24);
                var memberStruct = Marshal.PtrToStructure<Structs.PartyMember>(tblIndex);
                return new PartyMember(this.dalamud.ClientState.Actors, memberStruct);
            }
        }

        public void CopyTo(Array array, int index)
        {
            for (var i = 0; i < this.Length; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

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

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Length => !this.isReady ? 0 : Marshal.ReadByte(this.partyListBegin + 0xF0);

        int IReadOnlyCollection<PartyMember>.Count => this.Length;

        public int Count => this.Length;

        public object SyncRoot => this;

        public bool IsSynchronized => false;
    }
}
