using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiScene;

namespace Dalamud.Fools.Helper.YesHealMe;

public class IconCache : IDisposable
{
    private const string IconFilePath = "ui/icon/{0:D3}000/{1:D6}_hr1.tex";

    private static IconCache? internalInstance;
    private readonly Dictionary<uint, TextureWrap?> iconTextures = new();
    public static IconCache Instance => internalInstance ??= new IconCache();

    public void Dispose()
    {
        foreach (var texture in this.iconTextures.Values)
        {
            texture?.Dispose();
        }

        this.iconTextures.Clear();
    }

    public static void Cleanup()
    {
        internalInstance?.Dispose();
    }

    private void LoadIconTexture(uint iconId)
    {
        Task.Run(() =>
        {
            try
            {
                var path = IconFilePath.Format(iconId / 1000, iconId);
                var tex = Service<DataManager>.Get().GetImGuiTexture(path);

                if (tex is not null && tex.ImGuiHandle != nint.Zero)
                {
                    this.iconTextures[iconId] = tex;
                }
                else
                {
                    tex?.Dispose();
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Failed loading texture for icon {iconId} - {ex.Message}");
            }
        });
    }

    public TextureWrap? GetIcon(uint iconId)
    {
        if (this.iconTextures.TryGetValue(iconId, out var value))
        {
            return value;
        }

        this.iconTextures.Add(iconId, null);
        this.LoadIconTexture(iconId);

        return this.iconTextures[iconId];
    }
}
