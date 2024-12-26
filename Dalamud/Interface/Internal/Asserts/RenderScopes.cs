using System.Diagnostics;

namespace Dalamud.Interface.Internal.Asserts;

public static class RenderScopes
{
    private static RenderScopeFrame currentFrame = new();
    private static RenderScopeFrame lastFrame = new();

    public static RenderScopeFrame GetLastFrame() => lastFrame;

    public static RenderScopeFrame GetCurrentFrame() => currentFrame;

    public static void NewFrame()
    {
        //Debug.Assert(currentFrame.IsRoot, "NewFrame() but we didn't pop all the way to .");
    }

    public class RenderScopeFrame
    {

    }
}
