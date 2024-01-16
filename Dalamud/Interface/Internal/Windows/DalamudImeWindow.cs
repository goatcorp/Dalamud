using System.Numerics;

using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// A window for displaying IME details.
/// </summary>
internal unsafe class DalamudImeWindow : Window
{
    private const int ImePageSize = 9;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudImeWindow"/> class.
    /// </summary>
    public DalamudImeWindow()
        : base(
            "Dalamud IME",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground)
    {
        this.Size = default(Vector2);

        this.RespectCloseHotkey = false;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
    }

    /// <inheritdoc/>
    public override void PostDraw()
    {
        if (Service<DalamudIme>.GetNullable() is not { } ime)
            return;

        var viewport = ime.AssociatedViewport;
        if (viewport.NativePtr is null)
            return;

        var drawCand = ime.ImmCand.Count != 0;
        var drawConv = drawCand || ime.ShowPartialConversion;
        var drawIme = ime.InputModeIcon != 0;
        var imeIconFont = InterfaceManager.DefaultFont;

        var pad = ImGui.GetStyle().WindowPadding;
        var candTextSize = ImGui.CalcTextSize(ime.ImmComp == string.Empty ? " " : ime.ImmComp);

        var native = ime.ImmCandNative;
        var totalIndex = native.dwSelection + 1;
        var totalSize = native.dwCount;

        var pageStart = native.dwPageStart;
        var pageIndex = (pageStart / ImePageSize) + 1;
        var pageCount = (totalSize / ImePageSize) + 1;
        var pageInfo = $"{totalIndex}/{totalSize} ({pageIndex}/{pageCount})";
        
        // Calc the window size.
        var maxTextWidth = 0f;
        for (var i = 0; i < ime.ImmCand.Count; i++)
        {
            var textSize = ImGui.CalcTextSize($"{i + 1}. {ime.ImmCand[i]}");
            maxTextWidth = maxTextWidth > textSize.X ? maxTextWidth : textSize.X;
        }

        maxTextWidth = maxTextWidth > ImGui.CalcTextSize(pageInfo).X ? maxTextWidth : ImGui.CalcTextSize(pageInfo).X;
        maxTextWidth = maxTextWidth > ImGui.CalcTextSize(ime.ImmComp).X
                           ? maxTextWidth
                           : ImGui.CalcTextSize(ime.ImmComp).X;

        var numEntries = (drawCand ? ime.ImmCand.Count + 1 : 0) + 1 + (drawIme ? 1 : 0);
        var spaceY = ImGui.GetStyle().ItemSpacing.Y;
        var imeWindowHeight = (spaceY * (numEntries - 1)) + (candTextSize.Y * numEntries);
        var windowSize = new Vector2(maxTextWidth, imeWindowHeight) + (pad * 2);

        // 1. Figure out the expanding direction.
        var expandUpward = ime.CursorPos.Y + windowSize.Y > viewport.WorkPos.Y + viewport.WorkSize.Y;
        var windowPos = ime.CursorPos - pad;
        if (expandUpward)
        {
            windowPos.Y -= windowSize.Y - candTextSize.Y - (pad.Y * 2);
            if (drawIme)
                windowPos.Y += candTextSize.Y + spaceY;
        }
        else
        {
            if (drawIme)
                windowPos.Y -= candTextSize.Y + spaceY;
        }

        // 2. Contain within the viewport. Do not use clamp, as the target window might be too small.
        if (windowPos.X < viewport.WorkPos.X)
            windowPos.X = viewport.WorkPos.X;
        else if (windowPos.X + windowSize.X > viewport.WorkPos.X + viewport.WorkSize.X)
            windowPos.X = (viewport.WorkPos.X + viewport.WorkSize.X) - windowSize.X;
        if (windowPos.Y < viewport.WorkPos.Y)
            windowPos.Y = viewport.WorkPos.Y;
        else if (windowPos.Y + windowSize.Y > viewport.WorkPos.Y + viewport.WorkSize.Y)
            windowPos.Y = (viewport.WorkPos.Y + viewport.WorkSize.Y) - windowSize.Y;

        var cursor = windowPos + pad;

        // Draw the ime window.
        var drawList = ImGui.GetForegroundDrawList(viewport);

        // Draw the background rect for candidates.
        if (drawCand)
        {
            Vector2 candRectLt, candRectRb;
            if (!expandUpward)
            {
                candRectLt = windowPos + candTextSize with { X = 0 } + pad with { X = 0 };
                candRectRb = windowPos + windowSize;
                if (drawIme)
                    candRectLt.Y += spaceY + candTextSize.Y;
            }
            else
            {
                candRectLt = windowPos;
                candRectRb = windowPos + (windowSize - candTextSize with { X = 0 } - pad with { X = 0 });
                if (drawIme)
                    candRectRb.Y -= spaceY + candTextSize.Y;
            }

            drawList.AddRectFilled(
                candRectLt,
                candRectRb,
                ImGui.GetColorU32(ImGuiCol.WindowBg),
                ImGui.GetStyle().WindowRounding);
        }

        if (!expandUpward && drawIme)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        imeIconFont.RenderChar(
                            drawList,
                            imeIconFont.FontSize,
                            cursor + new Vector2(dx, dy),
                            ImGui.GetColorU32(ImGuiCol.WindowBg),
                            ime.InputModeIcon);
                    }
                }
            }

            imeIconFont.RenderChar(
                drawList,
                imeIconFont.FontSize,
                cursor,
                ImGui.GetColorU32(ImGuiCol.Text),
                ime.InputModeIcon);
            cursor.Y += candTextSize.Y + spaceY;
        }

        if (!expandUpward && drawConv)
        {
            DrawTextBeingConverted();
            cursor.Y += candTextSize.Y + spaceY;

            // Add a separator.
            drawList.AddLine(cursor, cursor + new Vector2(maxTextWidth, 0), ImGui.GetColorU32(ImGuiCol.Separator));
        }

        if (drawCand)
        {
            // Add the candidate words.
            for (var i = 0; i < ime.ImmCand.Count; i++)
            {
                var selected = i == (native.dwSelection % ImePageSize);
                var color = ImGui.GetColorU32(ImGuiCol.Text);
                if (selected)
                    color = ImGui.GetColorU32(ImGuiCol.NavHighlight);

                drawList.AddText(cursor, color, $"{i + 1}. {ime.ImmCand[i]}");
                cursor.Y += candTextSize.Y + spaceY;
            }

            // Add a separator
            drawList.AddLine(cursor, cursor + new Vector2(maxTextWidth, 0), ImGui.GetColorU32(ImGuiCol.Separator));

            // Add the pages infomation.
            drawList.AddText(cursor, ImGui.GetColorU32(ImGuiCol.Text), pageInfo);
            cursor.Y += candTextSize.Y + spaceY;
        }

        if (expandUpward && drawConv)
        {
            // Add a separator.
            drawList.AddLine(cursor, cursor + new Vector2(maxTextWidth, 0), ImGui.GetColorU32(ImGuiCol.Separator));

            DrawTextBeingConverted();
            cursor.Y += candTextSize.Y + spaceY;
        }

        if (expandUpward && drawIme)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        imeIconFont.RenderChar(
                            drawList,
                            imeIconFont.FontSize,
                            cursor + new Vector2(dx, dy),
                            ImGui.GetColorU32(ImGuiCol.WindowBg),
                            ime.InputModeIcon);
                    }
                }
            }

            imeIconFont.RenderChar(
                drawList,
                imeIconFont.FontSize,
                cursor,
                ImGui.GetColorU32(ImGuiCol.Text),
                ime.InputModeIcon);
        }

        return;

        void DrawTextBeingConverted()
        {
            // Draw the text background.
            drawList.AddRectFilled(
                cursor - (pad / 2),
                cursor + candTextSize + (pad / 2),
                ImGui.GetColorU32(ImGuiCol.WindowBg));

            // If only a part of the full text is marked for conversion, then draw background for the part being edited.
            if (ime.PartialConversionFrom != 0 || ime.PartialConversionTo != ime.ImmComp.Length)
            {
                var part1 = ime.ImmComp[..ime.PartialConversionFrom];
                var part2 = ime.ImmComp[..ime.PartialConversionTo];
                var size1 = ImGui.CalcTextSize(part1);
                var size2 = ImGui.CalcTextSize(part2);
                drawList.AddRectFilled(
                    cursor + size1 with { Y = 0 },
                    cursor + size2,
                    ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
            }

            // Add the text being converted.
            drawList.AddText(cursor, ImGui.GetColorU32(ImGuiCol.Text), ime.ImmComp);
        
            // Draw the caret inside the composition string.
            if (DalamudIme.ShowCursorInInputText)
            {
                var partBeforeCaret = ime.ImmComp[..ime.CompositionCursorOffset];
                var sizeBeforeCaret = ImGui.CalcTextSize(partBeforeCaret);
                drawList.AddLine(
                    cursor + sizeBeforeCaret with { Y = 0 },
                    cursor + sizeBeforeCaret,
                    ImGui.GetColorU32(ImGuiCol.Text));
            }
        }
    }
}
