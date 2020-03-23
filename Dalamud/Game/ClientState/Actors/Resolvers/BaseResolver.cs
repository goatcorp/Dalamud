using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    public abstract class BaseResolver {
        protected Dalamud dalamud;

        public BaseResolver(Dalamud dalamud) {
            this.dalamud = dalamud;
        }
    }
}
