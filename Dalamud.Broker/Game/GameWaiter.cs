namespace Dalamud.Broker.Game;

internal sealed class GameWaiter
{
    public GameWaiter()
    {
        new Thread(ThreadMain)
            {
                IsBackground = true,
            }
            .Start();
        // TODO
    }

    private void ThreadMain()
    {
        
    }
}
