using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState
{
    public class PartyList : IReadOnlyCollection<Actor>, ICollection, IDisposable
    {
        private ClientStateAddressResolver Address { get; }
        private Dalamud dalamud;

        private delegate IntPtr ResolvePlaceholderActor(IntPtr param1, string PlaceholderText, byte param3, byte param4);
        private ResolvePlaceholderActor PlaceholderResolver;
        private IntPtr PlaceholderResolverObject;
        private bool isReady = false;
        private Task ResolveTask;

        public PartyList(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            Address = addressResolver;
            this.dalamud = dalamud;
            PlaceholderResolver = Marshal.GetDelegateForFunctionPointer<ResolvePlaceholderActor>(Address.ResolvePlaceholderText);
            PlaceholderResolverObject = IntPtr.Zero;
            ResolveTask = new Task( () => { SetupPlaceholderResolver(); } );
            ResolveTask.Start();
        }

        public void Enable()
        {
            // TODO Fix for 5.3
            // I don't think anything needs to be done here anymore?
        }

        public void Dispose()
        {
            isReady = false;
        }

        public Actor this[int index]
        {
            get {
                if (!isReady)
                    return null;
                if (index >= Length)
                    return null;
                IntPtr actorptr = GetActorFromPlaceholder("<"+(index + 1)+">");
                if (actorptr == IntPtr.Zero)
                    return null;
                var memberStruct = Marshal.PtrToStructure<Structs.Actor>(actorptr);
                return new Actor(actorptr, memberStruct, dalamud);
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

        public IEnumerator<Actor> GetEnumerator() {
            for (var i = 0; i < Length; i++) {
                if (this[i] != null) {
                    yield return this[i];
                }
            }
        }

        private async void SetupPlaceholderResolver()
        {
            while (PlaceholderResolverObject == IntPtr.Zero)
            {
                try
                {
                    IntPtr step2 = Marshal.ReadIntPtr(Address.PlaceholderResolverObject) + 8;
                    PlaceholderResolverObject = Marshal.ReadIntPtr(step2) + 0xE7D0;
                    isReady = true;
                }
                catch (Exception)
                {
                    PlaceholderResolverObject = IntPtr.Zero;
                    await Task.Delay(1000);
                    continue;
                }
            }
        }

        private IntPtr GetActorFromPlaceholder(string placeholder)
        {
            return PlaceholderResolver(PlaceholderResolverObject, placeholder, 1, 0);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /* Temporarily 0 or 8, until we can find a better way to get the exact number */
        public int Length => !isReady ? 0 : 8;

        int IReadOnlyCollection<Actor>.Count => Length;

        public int Count => Length;

        public object SyncRoot => this;

        public bool IsSynchronized => false;
    }
}
