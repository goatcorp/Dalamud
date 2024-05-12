using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Represents an active notification.</summary>
internal sealed partial class ActiveNotification
{
    /// <summary>Draws this notification.</summary>
    /// <param name="width">The maximum width of the notification window.</param>
    /// <param name="offsetY">The offset from the bottom.</param>
    /// <returns>The height of the notification.</returns>
    public float Draw(float width, float offsetY)
    {
        var opacity =
            Math.Clamp(
                (float)(this.hideEasing.IsRunning
                            ? (this.hideEasing.IsDone || ReducedMotions ? 0 : 1f - this.hideEasing.Value)
                            : (this.showEasing.IsDone || ReducedMotions ? 1 : this.showEasing.Value)),
                0f,
                1f);
        if (opacity <= 0)
            return 0;

        var actionWindowHeight =
            // Content
            ImGui.GetTextLineHeight() +
            // Top and bottom padding
            (NotificationConstants.ScaledWindowPadding * 2);

        var viewport = ImGuiHelpers.MainViewport;
        var viewportPos = viewport.WorkPos;
        var viewportSize = viewport.WorkSize;

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(NotificationConstants.ScaledWindowPadding));
        unsafe
        {
            ImGui.PushStyleColor(
                ImGuiCol.WindowBg,
                *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg) * new Vector4(
                    1f,
                    1f,
                    1f,
                    NotificationConstants.BackgroundOpacity));
        }

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowPos(
            (viewportPos + viewportSize) -
            new Vector2(NotificationConstants.ScaledViewportEdgeMargin) -
            new Vector2(0, offsetY),
            ImGuiCond.Always,
            Vector2.One);
        ImGui.SetNextWindowSizeConstraints(
            new(width, actionWindowHeight),
            new(
                width,
                !this.underlyingNotification.Minimized || this.expandoEasing.IsRunning
                    ? float.MaxValue
                    : actionWindowHeight));
        ImGui.Begin(
            $"##NotifyMainWindow{this.Id}",
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoSavedSettings);
        ImGui.PushID(this.Id.GetHashCode());

        var isFocused = ImGui.IsWindowFocused();
        var isHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
        var isTakingKeyboardInput = isFocused && ImGui.GetIO().WantTextInput;
        var warrantsExtension =
            this.ExtensionDurationSinceLastInterest > TimeSpan.Zero
            && (isHovered || isTakingKeyboardInput);

        this.EffectiveExpiry = this.CalculateEffectiveExpiry(ref warrantsExtension);

        if (!isTakingKeyboardInput && !isHovered && isFocused)
        {
            ImGui.SetWindowFocus(null);
            isFocused = false;
        }

        if (DateTime.Now > this.EffectiveExpiry)
            this.DismissNow(NotificationDismissReason.Timeout);

        if (this.ExtensionDurationSinceLastInterest > TimeSpan.Zero && warrantsExtension)
            this.lastInterestTime = DateTime.Now;

        this.DrawWindowBackgroundProgressBar();
        this.DrawTopBar(width, actionWindowHeight, isHovered, warrantsExtension);
        if (!this.underlyingNotification.Minimized && !this.expandoEasing.IsRunning)
        {
            this.DrawContentAndActions(width, actionWindowHeight);
        }
        else if (this.expandoEasing.IsRunning)
        {
            var easedValue = ReducedMotions ? 1f : (float)this.expandoEasing.Value;
            if (this.underlyingNotification.Minimized)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity * (1f - easedValue));
            else
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity * easedValue);
            this.DrawContentAndActions(width, actionWindowHeight);
            ImGui.PopStyleVar();
        }

        if (isFocused)
            this.DrawFocusIndicator();
        this.DrawExpiryBar(warrantsExtension);

        if (ImGui.IsWindowHovered())
        {
            if (this.Click is null)
            {
                if (this.UserDismissable && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    this.DismissNow(NotificationDismissReason.Manual);
            }
            else
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                    || ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                    || ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                    this.InvokeClick();
            }
        }

        var windowSize = ImGui.GetWindowSize();
        ImGui.PopID();
        ImGui.End();

        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);

        return windowSize.Y;
    }

    /// <summary>Calculates the effective expiry, taking ImGui window state into account.</summary>
    /// <param name="warrantsExtension">Notification will not dismiss while this paramter is <c>true</c>.</param>
    /// <returns>The calculated effective expiry.</returns>
    /// <remarks>Expected to be called BETWEEN <see cref="ImGui.Begin(string)"/> and <see cref="ImGui.End"/>.</remarks>
    private DateTime CalculateEffectiveExpiry(ref bool warrantsExtension)
    {
        DateTime expiry;
        var initialDuration = this.InitialDuration;
        var expiryInitial =
            initialDuration == TimeSpan.MaxValue
                ? DateTime.MaxValue
                : this.CreatedAt + initialDuration;

        var extendDuration = this.ExtensionDurationSinceLastInterest;
        if (warrantsExtension)
        {
            expiry = DateTime.MaxValue;
        }
        else
        {
            var expiryExtend =
                extendDuration == TimeSpan.MaxValue
                    ? DateTime.MaxValue
                    : this.lastInterestTime + extendDuration;

            expiry = expiryInitial > expiryExtend ? expiryInitial : expiryExtend;
            if (expiry < this.extendedExpiry)
                expiry = this.extendedExpiry;
        }

        var he = this.HardExpiry;
        if (he < expiry)
        {
            expiry = he;
            warrantsExtension = false;
        }

        return expiry;
    }

    private void DrawWindowBackgroundProgressBar()
    {
        var elapsed = 0f;
        var colorElapsed = 0f;
        float progress;

        if (ReducedMotions)
        {
            progress = this.Progress;
        }
        else
        {
            progress = Math.Clamp(this.ProgressEased, 0f, 1f);

            elapsed =
                (float)(((DateTime.Now - this.CreatedAt).TotalMilliseconds %
                         NotificationConstants.ProgressWaveLoopDuration) /
                        NotificationConstants.ProgressWaveLoopDuration);
            elapsed /= NotificationConstants.ProgressWaveIdleTimeRatio;

            colorElapsed = elapsed < NotificationConstants.ProgressWaveLoopMaxColorTimeRatio
                               ? elapsed / NotificationConstants.ProgressWaveLoopMaxColorTimeRatio
                               : ((NotificationConstants.ProgressWaveLoopMaxColorTimeRatio * 2) - elapsed) /
                                 NotificationConstants.ProgressWaveLoopMaxColorTimeRatio;

            elapsed = Math.Clamp(elapsed, 0f, 1f);
            colorElapsed = Math.Clamp(colorElapsed, 0f, 1f);
            colorElapsed = MathF.Sin(colorElapsed * (MathF.PI / 2f));

            if (progress >= 1f)
                elapsed = colorElapsed = 0f;
        }

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var rb = windowPos + windowSize;
        var midp = windowPos + windowSize with { X = windowSize.X * progress * elapsed };
        var rp = windowPos + windowSize with { X = windowSize.X * progress };

        ImGui.PushClipRect(windowPos, rb, false);
        ImGui.GetWindowDrawList().AddRectFilled(
            windowPos,
            midp,
            ImGui.GetColorU32(
                Vector4.Lerp(
                    NotificationConstants.BackgroundProgressColorMin,
                    NotificationConstants.BackgroundProgressColorMax,
                    colorElapsed)));
        ImGui.GetWindowDrawList().AddRectFilled(
            midp with { Y = 0 },
            rp,
            ImGui.GetColorU32(NotificationConstants.BackgroundProgressColorMin));
        ImGui.PopClipRect();
    }

    private void DrawFocusIndicator()
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        ImGui.PushClipRect(windowPos, windowPos + windowSize, false);
        ImGui.GetWindowDrawList().AddRect(
            windowPos,
            windowPos + windowSize,
            ImGui.GetColorU32(NotificationConstants.FocusBorderColor * new Vector4(1f, 1f, 1f, ImGui.GetStyle().Alpha)),
            0f,
            ImDrawFlags.None,
            NotificationConstants.FocusIndicatorThickness);
        ImGui.PopClipRect();
    }

    private void DrawTopBar(float width, float height, bool drawActionButtons, bool warrantsExtension)
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();

        var rtOffset = new Vector2(width, 0);
        using (Service<InterfaceManager>.Get().IconFontHandle?.Push())
        {
            ImGui.PushClipRect(windowPos, windowPos + windowSize with { Y = height }, false);

            if (!drawActionButtons)
                this.DrawExpiryPie(warrantsExtension, new(width - height, 0), new(height));

            if (this.UserDismissable)
            {
                if (this.DrawIconButton(FontAwesomeIcon.Times, rtOffset, height, drawActionButtons))
                    this.DismissNow(NotificationDismissReason.Manual);
                rtOffset.X -= height;
            }

            if (this.underlyingNotification.Minimized)
            {
                if (this.DrawIconButton(FontAwesomeIcon.ChevronDown, rtOffset, height, drawActionButtons))
                    this.Minimized = false;
            }
            else
            {
                if (this.DrawIconButton(FontAwesomeIcon.ChevronUp, rtOffset, height, drawActionButtons))
                    this.Minimized = true;
            }

            rtOffset.X -= height;
            ImGui.PopClipRect();
        }

        float relativeOpacity;
        if (this.expandoEasing.IsRunning && !ReducedMotions)
        {
            relativeOpacity =
                this.underlyingNotification.Minimized
                    ? 1f - (float)this.expandoEasing.Value
                    : (float)this.expandoEasing.Value;
        }
        else
        {
            relativeOpacity = this.underlyingNotification.Minimized ? 0f : 1f;
        }

        if (drawActionButtons)
            ImGui.PushClipRect(windowPos, windowPos + rtOffset with { Y = height }, false);
        else
            ImGui.PushClipRect(windowPos, windowPos + windowSize with { Y = height }, false);

        if (relativeOpacity > 0)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * relativeOpacity);
            ImGui.SetCursorPos(new(NotificationConstants.ScaledWindowPadding));
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.WhenTextColor);
            ImGui.TextUnformatted(
                ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
                    ? this.CreatedAt.LocAbsolute()
                    : ReducedMotions
                        ? this.CreatedAt.LocRelativePastLong(TimeSpan.FromSeconds(15))
                        : this.CreatedAt.LocRelativePastLong(TimeSpan.FromSeconds(5)));
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }

        if (relativeOpacity < 1)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * (1f - relativeOpacity));

            var agoText =
                ReducedMotions
                    ? this.CreatedAt.LocRelativePastShort(TimeSpan.FromSeconds(15))
                    : this.CreatedAt.LocRelativePastShort(TimeSpan.FromSeconds(5));
            var agoSize = ImGui.CalcTextSize(agoText);
            ImGui.SetCursorPos(new(width - ((height + agoSize.X) / 2f), NotificationConstants.ScaledWindowPadding));
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.WhenTextColor);
            ImGui.TextUnformatted(agoText);
            ImGui.PopStyleColor();

            this.DrawIcon(
                new(NotificationConstants.ScaledWindowPadding),
                new(height - (2 * NotificationConstants.ScaledWindowPadding)));
            ImGui.PushClipRect(
                windowPos + new Vector2(height, 0),
                windowPos + new Vector2(width - height, height),
                true);
            ImGui.SetCursorPos(new(height, NotificationConstants.ScaledWindowPadding));
            ImGui.TextUnformatted(this.EffectiveMinimizedText);
            ImGui.PopClipRect();

            ImGui.PopStyleVar();
        }

        ImGui.PopClipRect();
    }

    private bool DrawIconButton(FontAwesomeIcon icon, Vector2 rt, float size, bool drawActionButtons)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        if (!drawActionButtons)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0f);
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.CloseTextColor);

        ImGui.SetCursorPos(rt - new Vector2(size, 0));
        var r = ImGui.Button(icon.ToIconString(), new(size));

        ImGui.PopStyleColor(2);
        if (!drawActionButtons)
            ImGui.PopStyleVar();
        ImGui.PopStyleVar();
        return r;
    }

    private void DrawContentAndActions(float width, float actionWindowHeight)
    {
        var textColumnX = (NotificationConstants.ScaledWindowPadding * 2) + NotificationConstants.ScaledIconSize;
        var textColumnWidth = width - textColumnX - NotificationConstants.ScaledWindowPadding;
        var textColumnOffset = new Vector2(textColumnX, actionWindowHeight);

        this.DrawIcon(
            new(NotificationConstants.ScaledWindowPadding, actionWindowHeight),
            new(NotificationConstants.ScaledIconSize));

        textColumnOffset.Y += this.DrawTitle(textColumnOffset, textColumnWidth);
        textColumnOffset.Y += NotificationConstants.ScaledComponentGap;

        this.DrawContentBody(textColumnOffset, textColumnWidth);

        if (this.DrawActions is null)
            return;

        var userActionOffset = new Vector2(
            NotificationConstants.ScaledWindowPadding,
            ImGui.GetCursorPosY() + NotificationConstants.ScaledComponentGap);
        ImGui.SetCursorPos(userActionOffset);
        this.InvokeDrawActions(
            userActionOffset,
            new(width - NotificationConstants.ScaledWindowPadding, float.MaxValue));
    }

    private void DrawIcon(Vector2 minCoord, Vector2 size)
    {
        var maxCoord = minCoord + size;
        var iconColor = this.Type.ToColor();

        if (NotificationUtilities.DrawIconFrom(minCoord, maxCoord, this.IconTextureTask))
            return;

        if (this.Icon?.DrawIcon(minCoord, maxCoord, iconColor) is true)
            return;

        if (NotificationUtilities.DrawIconFrom(
                minCoord,
                maxCoord,
                this.Type.ToChar(),
                Service<NotificationManager>.Get().IconFontAwesomeFontHandle,
                iconColor))
            return;

        if (NotificationUtilities.DrawIconFrom(minCoord, maxCoord, this.initiatorPlugin))
            return;

        NotificationUtilities.DrawIconFromDalamudLogo(minCoord, maxCoord);
    }

    private float DrawTitle(Vector2 minCoord, float width)
    {
        ImGui.PushTextWrapPos(minCoord.X + width);

        ImGui.SetCursorPos(minCoord);
        if ((this.Title ?? this.Type.ToTitle()) is { } title)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.TitleTextColor);
            ImGui.TextUnformatted(title);
            ImGui.PopStyleColor();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.BlameTextColor);
        ImGui.SetCursorPos(minCoord with { Y = ImGui.GetCursorPosY() });
        ImGui.TextUnformatted(this.InitiatorString);
        ImGui.PopStyleColor();

        ImGui.PopTextWrapPos();
        return ImGui.GetCursorPosY() - minCoord.Y;
    }

    private void DrawContentBody(Vector2 minCoord, float width)
    {
        ImGui.SetCursorPos(minCoord);
        ImGui.PushTextWrapPos(minCoord.X + width);
        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.BodyTextColor);
        ImGui.TextUnformatted(this.Content);
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();
    }

    private void DrawExpiryPie(bool warrantsExtension, Vector2 offset, Vector2 size)
    {
        if (!Service<DalamudConfiguration>.Get().ReduceMotions ?? false)
            return;

        // circle here; 0 means 0deg; 1 means 360deg
        float fillStartCw, fillEndCw;
        if (this.DismissReason is not null)
        {
            fillStartCw = fillEndCw = 0f;
        }
        else if (warrantsExtension)
        {
            fillStartCw = fillEndCw = 0f;
        }
        else if (this.EffectiveExpiry == DateTime.MaxValue)
        {
            if (this.ShowIndeterminateIfNoExpiry)
            {
                // draw
                var elapsed = (float)(((DateTime.Now - this.CreatedAt).TotalMilliseconds %
                                       NotificationConstants.IndeterminatePieLoopDuration) /
                                      NotificationConstants.IndeterminatePieLoopDuration);
                fillStartCw = elapsed;
                fillEndCw = elapsed + 0.2f + (MathF.Sin(elapsed * MathF.PI) * 0.2f);
            }
            else
            {
                // do not draw
                fillStartCw = fillEndCw = 0f;
            }
        }
        else
        {
            fillStartCw = 1f - (float)((this.EffectiveExpiry - DateTime.Now).TotalMilliseconds /
                                       (this.EffectiveExpiry - this.lastInterestTime).TotalMilliseconds);
            fillEndCw = 1f;
        }

        if (fillStartCw > fillEndCw)
            (fillStartCw, fillEndCw) = (fillEndCw, fillStartCw);

        if (fillStartCw == 0 && fillEndCw == 0)
            return;
       
        var radius = Math.Min(size.X, size.Y) / 3f;
        var ifrom = fillStartCw * MathF.PI * 2;
        var ito = fillEndCw * MathF.PI * 2;

        var nseg = MathF.Ceiling(2 * MathF.PI * radius);
        var step = (MathF.PI * 2) / nseg;

        var center = ImGui.GetWindowPos() + offset + (size / 2);
        var color = ImGui.GetColorU32(this.Type.ToColor() * new Vector4(1, 1, 1, 0.2f));

        var prevOff = center + (radius * new Vector2(MathF.Sin(ifrom), -MathF.Cos(ifrom)));
        Span<Vector2> verts = stackalloc Vector2[(int)MathF.Ceiling(((ito - ifrom) / step) + 3)];
        var vertPtr = 0;
        verts[vertPtr++] = center;
        verts[vertPtr++] = prevOff;

        var cur = ifrom + step;
        for (; cur < ito; cur += step)
        {
            var curOff = center + (radius * new Vector2(MathF.Sin(cur), -MathF.Cos(cur)));
            if (Vector2.DistanceSquared(prevOff, curOff) >= 1)
                verts[vertPtr++] = prevOff = curOff;
        }

        var lastOff = center + (radius * new Vector2(MathF.Sin(ito), -MathF.Cos(ito)));
        if (Vector2.DistanceSquared(prevOff, lastOff) >= 1)
            verts[vertPtr++] = lastOff;
        unsafe
        {
            var dlist = ImGui.GetWindowDrawList().NativePtr;
            fixed (Vector2* pvert = verts)
                ImGuiNative.ImDrawList_AddConvexPolyFilled(dlist, pvert, vertPtr, color);
        }
    }

    private void DrawExpiryBar(bool warrantsExtension)
    {
        if (Service<DalamudConfiguration>.Get().ReduceMotions ?? false)
            return;

        float barL, barR;
        if (this.DismissReason is not null)
        {
            var v = this.hideEasing.IsDone || ReducedMotions ? 0f : 1f - (float)this.hideEasing.Value;
            var midpoint = (this.prevProgressL + this.prevProgressR) / 2f;
            var length = (this.prevProgressR - this.prevProgressL) / 2f;
            barL = midpoint - (length * v);
            barR = midpoint + (length * v);
        }
        else if (warrantsExtension)
        {
            barL = 0f;
            barR = 1f;
            this.prevProgressL = barL;
            this.prevProgressR = barR;
        }
        else if (this.EffectiveExpiry == DateTime.MaxValue)
        {
            if (this.ShowIndeterminateIfNoExpiry)
            {
                var elapsed = (float)(((DateTime.Now - this.CreatedAt).TotalMilliseconds %
                                       NotificationConstants.IndeterminateProgressbarLoopDuration) /
                                      NotificationConstants.IndeterminateProgressbarLoopDuration);
                barL = Math.Max(elapsed - (1f / 3), 0f) / (2f / 3);
                barR = Math.Min(elapsed, 2f / 3) / (2f / 3);
                barL = MathF.Pow(barL, 3);
                barR = 1f - MathF.Pow(1f - barR, 3);
                this.prevProgressL = barL;
                this.prevProgressR = barR;
            }
            else
            {
                this.prevProgressL = barL = 0f;
                this.prevProgressR = barR = 1f;
            }
        }
        else
        {
            barL = 1f - (float)((this.EffectiveExpiry - DateTime.Now).TotalMilliseconds /
                                (this.EffectiveExpiry - this.lastInterestTime).TotalMilliseconds);
            barR = 1f;
            this.prevProgressL = barL;
            this.prevProgressR = barR;
        }

        barR = Math.Clamp(barR, 0f, 1f);

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        ImGui.PushClipRect(windowPos, windowPos + windowSize, false);
        ImGui.GetWindowDrawList().AddRectFilled(
            windowPos + new Vector2(
                windowSize.X * barL,
                windowSize.Y - NotificationConstants.ScaledExpiryProgressBarHeight),
            windowPos + windowSize with { X = windowSize.X * barR },
            ImGui.GetColorU32(this.Type.ToColor()));
        ImGui.PopClipRect();
    }
}
