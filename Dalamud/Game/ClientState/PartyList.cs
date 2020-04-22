using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            Address = addressResolver;
            this.dalamud = dalamud;
            partyListUpdateHook = new Hook<PartyListUpdateDelegate>(Address.PartyListUpdate, new PartyListUpdateDelegate(PartyListUpdateDetour), this);
        }

        public void Enable()
        {
            partyListUpdateHook.Enable();
        }

        public void Dispose()
        {
            if (!this.isReady)
                partyListUpdateHook.Dispose();
            isReady = false;
        }

        private long PartyListUpdateDetour(IntPtr structBegin, long param2, char param3)
        {
            var result = partyListUpdateHook.Original(structBegin, param2, param3);
            partyListBegin = structBegin + 0xB48;
            partyListUpdateHook.Dispose();
            isReady = true;
            return result;
        }
        public PartyMember this[int index]
        {
            get
            {
                if (!this.isReady)
                    return null;
                if (index >= Length)
                    return null;
                var tblIndex = partyListBegin + index * 24;
                var memberStruct = Marshal.PtrToStructure<Structs.PartyMember>(tblIndex);
                return new PartyMember(dalamud.ClientState.Actors, memberStruct);
            }
        }

        public void CopyTo(Array array, int index)
        {
            for (var i = 0; i < Length; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        private class PartyListEnumerator : IEnumerator<PartyMember>
        {
            private readonly PartyList party;
            private int currentIndex;

            public PartyListEnumerator(PartyList list)
            {
                party = list;
            }

            public bool MoveNext()
            {
                currentIndex++;
                return currentIndex != party.Length;
            }

            public void Reset()
            {
                currentIndex = 0;
            }

            public PartyMember Current => this.party[this.currentIndex];

            object IEnumerator.Current => Current;

            // Required by IEnumerator<T> even though we have nothing we want to dispose here.
            public void Dispose() {}
        }

        public IEnumerator<PartyMember> GetEnumerator() {
            return new PartyListEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int Length => !this.isReady ? 0 : Marshal.ReadByte(partyListBegin + 0xF0);

        int IReadOnlyCollection<PartyMember>.Count => Length;

        public int Count => Length;

        public object SyncRoot => this;

        public bool IsSynchronized => false;
    }
}
