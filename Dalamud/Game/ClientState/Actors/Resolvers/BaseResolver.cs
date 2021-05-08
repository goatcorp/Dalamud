namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    public abstract class BaseResolver
    {
        protected Dalamud dalamud;

        public BaseResolver(Dalamud dalamud)
        {
            this.dalamud = dalamud;
        }
    }
}
