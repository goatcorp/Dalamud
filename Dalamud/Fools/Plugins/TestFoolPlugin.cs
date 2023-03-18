namespace Dalamud.Fools.Plugins;

public class TestFoolPlugin : IFoolsPlugin
{
    public string Name => "TestFoolPlugin";

    public string Description => "TestFoolPlugin";

    public string InternalName => "TestFoolPlugin";

    public string Author => "NotNite";

    public TestFoolPlugin() { }

    public void Dispose() { }
}
