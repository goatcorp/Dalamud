using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.GameFonts;

/// <summary>
/// ABI-compatible wrapper for <see cref="IFontHandle"/>.
/// </summary>
[Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
public sealed class GameFontHandle : IFontHandle
{
    private readonly IFontHandle.IInternal fontHandle;
    private readonly FontAtlasFactory fontAtlasFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontHandle"/> class.
    /// </summary>
    /// <param name="fontHandle">The wrapped <see cref="IFontHandle"/>.</param>
    /// <param name="fontAtlasFactory">An instance of <see cref="FontAtlasFactory"/>.</param>
    internal GameFontHandle(IFontHandle.IInternal fontHandle, FontAtlasFactory fontAtlasFactory)
    {
        this.fontHandle = fontHandle;
        this.fontAtlasFactory = fontAtlasFactory;
    }

    /// <inheritdoc />
    public event Action<IFontHandle> ImFontChanged
    {
        add => this.fontHandle.ImFontChanged += value;
        remove => this.fontHandle.ImFontChanged -= value;
    }

    /// <inheritdoc />
    public Exception? LoadException => this.fontHandle.LoadException;

    /// <inheritdoc />
    public bool Available => this.fontHandle.Available;

    /// <inheritdoc cref="IFontHandle.IInternal.ImFont"/>
    [Obsolete($"Use {nameof(Push)}, and then use {nameof(ImGui.GetFont)} instead.", false)]
    public ImFontPtr ImFont => this.fontHandle.ImFont;

    /// <summary>
    /// Gets the font style. Only applicable for <see cref="GameFontHandle"/>.
    /// </summary>
    [Obsolete("If you use this, let the fact that you use this be known at Dalamud Discord.", false)]
    public GameFontStyle Style => ((GamePrebakedFontHandle)this.fontHandle).FontStyle;

    /// <summary>
    /// Gets the relevant <see cref="FdtReader"/>.<br />
    /// <br />
    /// Only applicable for game fonts. Otherwise it will throw.
    /// </summary>
    [Obsolete("If you use this, let the fact that you use this be known at Dalamud Discord.", false)]
    public FdtReader FdtReader => this.fontAtlasFactory.GetFdtReader(this.Style.FamilyAndSize)!;

    /// <inheritdoc />
    public void Dispose() => this.fontHandle.Dispose();

    /// <inheritdoc />
    public IFontHandle.ImFontLocked Lock() => this.fontHandle.Lock();

    /// <summary>
    /// Pushes the font.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that can be used to pop the font on dispose.</returns>
    public IDisposable Push() => this.fontHandle.Push();

    /// <inheritdoc/>
    IFontHandle.FontPopper IFontHandle.Push() => this.fontHandle.Push();

    /// <inheritdoc />
    public Task<IFontHandle> WaitAsync() => this.fontHandle.WaitAsync();

    /// <summary>
    /// Creates a new <see cref="GameFontLayoutPlan.Builder"/>.<br />
    /// <br />
    /// Only applicable for game fonts. Otherwise it will throw.
    /// </summary>
    /// <param name="text">Text.</param>
    /// <returns>A new builder for GameFontLayoutPlan.</returns>
    [Obsolete("If you use this, let the fact that you use this be known at Dalamud Discord.", false)]
    public GameFontLayoutPlan.Builder LayoutBuilder(string text) => new(this.ImFont, this.FdtReader, text);

    /// <summary>
    /// Draws text.
    /// </summary>
    /// <param name="text">Text to draw.</param>
    [Obsolete("If you use this, let the fact that you use this be known at Dalamud Discord.", false)]
    public void Text(string text)
    {
        if (!this.Available)
        {
            ImGui.TextUnformatted(text);
        }
        else
        {
            var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
            pos.X -= ImGui.GetScrollX();
            pos.Y -= ImGui.GetScrollY();

            var layout = this.LayoutBuilder(text).Build();
            layout.Draw(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
            ImGui.Dummy(new Vector2(layout.Width, layout.Height));
        }
    }

    /// <summary>
    /// Draws text in given color.
    /// </summary>
    /// <param name="col">Color.</param>
    /// <param name="text">Text to draw.</param>
    [Obsolete("If you use this, let the fact that you use this be known at Dalamud Discord.", false)]
    public void TextColored(Vector4 col, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, col);
        this.Text(text);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Draws disabled text.
    /// </summary>
    /// <param name="text">Text to draw.</param>
    [Obsolete("If you use this, let the fact that you use this be known at Dalamud Discord.", false)]
    public void TextDisabled(string text)
    {
        unsafe
        {
            this.TextColored(*ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled), text);
        }
    }
}
