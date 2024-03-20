using ImGuiNET;

namespace Dalamud.Interface.Utility.Raii;

// Push an arbitrary amount of fonts into an object that are all popped when it is disposed.
// If condition is false, no font is pushed.
public static partial class ImRaii
{
    public static Font PushFont(ImFontPtr font, bool condition = true)
        => condition ? new Font().Push(font) : new Font();

    // Push the default font if any other font is currently pushed.
    public static Font DefaultFont()
        => new Font().Push(Font.DefaultPushed, Font.FontPushCounter > 0);

    public sealed class Font : IDisposable
    {
        internal static int        FontPushCounter = 0;
        internal static ImFontPtr DefaultPushed;

        private int count;

        public Font()
            => this.count = 0;

        public Font Push(ImFontPtr font, bool condition = true)
        {
            if (condition)
            {
                if (FontPushCounter++ == 0)
                    DefaultPushed = ImGui.GetFont();
                ImGui.PushFont(font);
                ++this.count;
            }

            return this;
        }

        public void Pop(int num = 1)
        {
            num             =  Math.Min(num, this.count);
            this.count          -= num;
            FontPushCounter -= num;
            while (num-- > 0)
                ImGui.PopFont();
        }

        public void Dispose()
            => this.Pop(this.count);
    }
}
