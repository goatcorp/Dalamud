using System;
using Dalamud.Interface;
using Dalamud.Interface.GameFonts;

namespace Dalamud.Fools.Helper.YesHealMe;

public class FontManager : IDisposable
{
    public GameFontHandle GameFont { get; }

    public FontManager(UiBuilder uiBuilder)
    {
        this.GameFont = uiBuilder.GetGameFontHandle( new GameFontStyle( GameFontFamily.Axis, 52.0f ) );
    }

    public void Dispose()
    {
        this.GameFont.Dispose();
    }
}
