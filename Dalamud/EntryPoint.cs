using CoreHook;

namespace Dalamud
{
    public sealed class EntryPoint : IEntryPoint
    {
        public EntryPoint(IContext context, string rootDirectory) { }

        public void Run(IContext context, string rootDirectory)
        {
            // Current goal is to make just enough to run this function and see if it works. (as a proof of concept.. thing.)
        }
    }
}
