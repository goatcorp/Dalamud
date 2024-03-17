using Dalamud.Game.Config;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannedStringRenderer
{
    /// <summary>Initializes this instance of the <see cref="SpannedStringRenderer"/> class.</summary>
    /// <param name="rendererOptions">the initial parameters.</param>
    /// <param name="imGuiGlobalId">An ImGui global id, or 0 to ignore.</param>
    /// <param name="putDummyAfterRender">Whether to put a dummy after render.</param>
    /// <param name="drawListPtr">The target draw list.</param>
    /// <returns>A reference of this instance after the initialize operation is completed.</returns>
    public SpannedStringRenderer Initialize(
        in ISpannedStringRenderer.Options rendererOptions,
        uint imGuiGlobalId,
        bool putDummyAfterRender,
        ImDrawList* drawListPtr)
    {
        ThreadSafety.DebugAssertMainThread();

        this.rendered = false;
        this.options = new()
        {
            UseLinks = rendererOptions.UseLinks ?? true,
            UseControlCharacter = rendererOptions.ControlCharactersSpanParams.HasValue,
            UseWrapMarker = !string.IsNullOrEmpty(rendererOptions.WrapMarker),
            UseWrapMarkerParams = rendererOptions.WrapMarkerStyle is not null,
            PutDummyAfterRender = putDummyAfterRender,
            WordBreak = rendererOptions.WordBreak ?? WordBreakType.Normal,
            AcceptedNewLines = rendererOptions.AcceptedNewLines ?? NewLineType.All,
            ImGuiGlobalId = imGuiGlobalId,
            TabWidth = rendererOptions.TabWidth ?? -4,
            Scale = rendererOptions.Scale ?? ImGuiHelpers.GlobalScale,
            LineWrapWidth = rendererOptions.LineWrapWidth ?? ImGui.GetColumnWidth(),
            ControlCharactersSpanStyle = rendererOptions.ControlCharactersSpanParams ?? default,
            InitialSpanStyle = rendererOptions.InitialStyle ?? SpanStyle.FromContext,
            WrapMarker = rendererOptions.WrapMarker ?? string.Empty,
            WrapMarkerStyle = rendererOptions.WrapMarkerStyle ?? default,
            DrawListPtr = drawListPtr,
        };

        var gfdIndex = rendererOptions.GraphicFontIconMode ?? -1;
        if (gfdIndex < 0)
        {
            gfdIndex =
                Service<GameConfig>.Get().TryGet(SystemConfigOption.PadSelectButtonIcon, out uint iconTmp)
                    ? (int)iconTmp
                    : 0;
        }

        var gfdTexCount = this.factory.GfdTextures.Length;
        gfdIndex = ((gfdIndex % gfdTexCount) + gfdTexCount) % gfdTexCount;
        this.options.GfdTexture = this.factory.GfdTextures[gfdIndex];
        return this;
    }

    /// <summary>The resolved options for <see cref="SpannedStringRenderer"/>.</summary>
    private struct Options
    {
        public bool UseLinks;
        public bool UseControlCharacter;
        public bool UseWrapMarker;
        public bool UseWrapMarkerParams;
        public bool PutDummyAfterRender;
        public WordBreakType WordBreak;
        public NewLineType AcceptedNewLines;
        public IDalamudTextureWrap? GfdTexture;
        public uint ImGuiGlobalId;
        public float TabWidth;
        public float Scale;
        public float LineWrapWidth;
        public string WrapMarker;
        public SpanStyle ControlCharactersSpanStyle;
        public SpanStyle InitialSpanStyle;
        public SpanStyle WrapMarkerStyle;
        public ImDrawList* DrawListPtr;
    }
}
