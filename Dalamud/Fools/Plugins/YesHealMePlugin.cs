using Dalamud.Fools.Helper.YesHealMe;
using Dalamud.Interface;

namespace Dalamud.Fools.Plugins;

public class YesHealMePlugin : IFoolsPlugin
{
    private readonly FontManager fontManager;
    private readonly PartyListAddon partyListAddon = new();
    private int iconId = 1;

    public YesHealMePlugin()
    {
        const string nameSpace = "fools+YesHealMe";
        var uiBuilder = new UiBuilder(nameSpace);
        this.fontManager = new FontManager(uiBuilder);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.fontManager.Dispose();
        this.partyListAddon.Dispose();
        IconCache.Cleanup();
    }

    public void DrawUi()
    {
        YesHealMePluginWindow.Draw(this.partyListAddon, this.fontManager, ref this.iconId);
    }
}
