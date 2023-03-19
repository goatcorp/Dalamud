using System;

namespace Dalamud.Fools;

public interface IFoolsPlugin : IDisposable
{
    public void DrawUi() { }
}
