using System.Numerics;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// A window for displaying IME details.
    /// </summary>
    internal unsafe class IMEWindow : Window
    {
        private const int ImePageSize = 9;

        /// <summary>
        /// Initializes a new instance of the <see cref="IMEWindow"/> class.
        /// </summary>
        public IMEWindow()
            : base("Dalamud IME", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground)
        {
            this.Size = new Vector2(100, 200);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.RespectCloseHotkey = false;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            if (this.IsOpen && Service<KeyState>.Get()[VirtualKey.SHIFT]) Service<DalamudInterface>.Get().CloseImeWindow();
            var ime = Service<DalamudIME>.GetNullable();

            if (ime == null || !ime.IsEnabled)
            {
                ImGui.Text("IME is unavailable.");
                return;
            }

            //ImGui.Text($"{ime.GetCursorPos()}");
            //ImGui.Text($"{ImGui.GetWindowViewport().WorkSize}");
        }

        /// <inheritdoc/>
        public override void PostDraw()
        {
            if (this.IsOpen && Service<KeyState>.Get()[VirtualKey.SHIFT]) Service<DalamudInterface>.Get().CloseImeWindow();
            var ime = Service<DalamudIME>.GetNullable();

            if (ime == null || !ime.IsEnabled)
                return;

            var maxTextWidth = 0f;
            var textHeight = ImGui.CalcTextSize(ime.ImmComp).Y;

            var native = ime.ImmCandNative;
            var totalIndex = native.Selection + 1;
            var totalSize = native.Count;

            var pageStart = native.PageStart;
            var pageIndex = (pageStart / ImePageSize) + 1;
            var pageCount = (totalSize / ImePageSize) + 1;
            var pageInfo = $"{totalIndex}/{totalSize} ({pageIndex}/{pageCount})";

            // Calc the window size
            for (var i = 0; i < ime.ImmCand.Count; i++)
            {
                var textSize = ImGui.CalcTextSize($"{i + 1}. {ime.ImmCand[i]}");
                maxTextWidth = maxTextWidth > textSize.X ? maxTextWidth : textSize.X;
            }

            maxTextWidth = maxTextWidth > ImGui.CalcTextSize(pageInfo).X ? maxTextWidth : ImGui.CalcTextSize(pageInfo).X;
            maxTextWidth = maxTextWidth > ImGui.CalcTextSize(ime.ImmComp).X ? maxTextWidth : ImGui.CalcTextSize(ime.ImmComp).X;

            var imeWindowWidth = maxTextWidth + (2 * ImGui.GetStyle().WindowPadding.X);
            var imeWindowHeight = (textHeight * (ime.ImmCand.Count + 2)) + (5 * (ime.ImmCand.Count - 1)) + (2 * ImGui.GetStyle().WindowPadding.Y);

            // Calc the window pos
            var cursorPos = ime.GetCursorPos();
            var imeWindowMinPos = new Vector2(cursorPos.X, cursorPos.Y);
            var imeWindowMaxPos = new Vector2(imeWindowMinPos.X + imeWindowWidth, imeWindowMinPos.Y + imeWindowHeight);
            var gameWindowSize = ImGui.GetWindowViewport().WorkSize;

            var offset = new Vector2(
                imeWindowMaxPos.X - gameWindowSize.X > 0 ? imeWindowMaxPos.X - gameWindowSize.X : 0,
                imeWindowMaxPos.Y - gameWindowSize.Y > 0 ? imeWindowMaxPos.Y - gameWindowSize.Y : 0);
            imeWindowMinPos -= offset;
            imeWindowMaxPos -= offset;

            var nextDrawPosY = imeWindowMinPos.Y;
            var drawAreaPosX = imeWindowMinPos.X + ImGui.GetStyle().WindowPadding.X;

            // Draw the ime window
            var drawList = ImGui.GetForegroundDrawList();
            // Draw the background rect
            drawList.AddRectFilled(imeWindowMinPos, imeWindowMaxPos, ImGui.GetColorU32(ImGuiCol.WindowBg), ImGui.GetStyle().WindowRounding);
            // Add component text
            drawList.AddText(new Vector2(drawAreaPosX, nextDrawPosY), ImGui.GetColorU32(ImGuiCol.Text), ime.ImmComp);
            nextDrawPosY += textHeight + ImGui.GetStyle().ItemSpacing.Y;
            // Add separator
            drawList.AddLine(new Vector2(drawAreaPosX, nextDrawPosY), new Vector2(drawAreaPosX + maxTextWidth, nextDrawPosY), ImGui.GetColorU32(ImGuiCol.Separator));
            // Add candidate words
            for (var i = 0; i < ime.ImmCand.Count; i++)
            {
                var selected = i == (native.Selection % ImePageSize);
                var color = ImGui.GetColorU32(ImGuiCol.Text);
                if (selected)
                    color = ImGui.GetColorU32(ImGuiCol.NavHighlight);

                drawList.AddText(new Vector2(drawAreaPosX, nextDrawPosY), color, $"{i + 1}. {ime.ImmCand[i]}");
                nextDrawPosY += textHeight + ImGui.GetStyle().ItemSpacing.Y;
            }

            // Add separator
            drawList.AddLine(new Vector2(drawAreaPosX, nextDrawPosY), new Vector2(drawAreaPosX + maxTextWidth, nextDrawPosY), ImGui.GetColorU32(ImGuiCol.Separator));
            // Add pages infomation
            drawList.AddText(new Vector2(drawAreaPosX, nextDrawPosY), ImGui.GetColorU32(ImGuiCol.Text), pageInfo);
        }
    }
}
