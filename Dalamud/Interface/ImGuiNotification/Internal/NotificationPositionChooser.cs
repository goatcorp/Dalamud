using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>
/// Class responsible for drawing UI that lets users choose the position of notifications.
/// </summary>
internal class NotificationPositionChooser
{
    private readonly DalamudConfiguration configuration;
    private readonly Vector2 previousAnchorPosition;

    private Vector2 currentAnchorPosition;

    /// <summary>
    /// Prevents immediate closure from opening with a gamepad due to button click in the settings window.
    /// </summary>
    private bool waitForRelease;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPositionChooser"/> class.
    /// </summary>
    /// <param name="configuration">The configuration we are reading or writing from.</param>
    public NotificationPositionChooser(DalamudConfiguration configuration)
    {
        this.configuration = configuration;
        this.previousAnchorPosition = configuration.NotificationAnchorPosition;
        this.currentAnchorPosition = this.previousAnchorPosition;
        this.waitForRelease = ImGui.IsKeyDown(ImGuiKey.GamepadFaceDown);
    }

    /// <summary>
    /// Gets or sets an action that is invoked when the user makes a selection.
    /// </summary>
    public event Action? SelectionMade;

    /// <summary>
    /// Draw the chooser UI.
    /// </summary>
    public void Draw()
    {
        using var style1 = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 0f);
        using var style2 = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 0f);
        using var color = ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

        var viewport = ImGuiHelpers.MainViewport;
        var viewportSize = viewport.Size;
        var viewportPos = viewport.Pos;

        ImGui.SetNextWindowFocus();
        ImGui.SetNextWindowPos(viewportPos);
        ImGui.SetNextWindowSize(viewportSize);
        ImGuiHelpers.ForceNextWindowMainViewport();

        ImGui.SetNextWindowBgAlpha(0.6f);

        ImGui.Begin(
            "###NotificationPositionChooser"u8,
            ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav);

        var gamepadState = Service<GamepadState>.Get();
        var io = ImGui.GetIO();

        if (io.MouseDelta != Vector2.Zero)
        {
            var adjustedMousePos = ImGui.GetMousePos() - viewportPos;
            this.currentAnchorPosition = adjustedMousePos / viewportSize;
        }

        var isGamepadActive = gamepadState.EnableGamepadNav;
        if (isGamepadActive && gamepadState.LeftStick != Vector2.Zero)
        {
            var gamepadDelta = gamepadState.LeftStick * new Vector2(1, -1) / 100f;
            this.currentAnchorPosition += gamepadDelta * io.DeltaTime * 1.5f;
        }

        this.currentAnchorPosition = Vector2.Clamp(this.currentAnchorPosition, Vector2.Zero, Vector2.One);

        DrawPreview(this.previousAnchorPosition, 0.3f);
        DrawPreview(this.currentAnchorPosition, 1f);

        if (this.waitForRelease && !ImGui.IsKeyDown(ImGuiKey.GamepadFaceDown))
        {
            this.waitForRelease = false;
        }

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) || (!this.waitForRelease && ImGui.IsKeyPressed(ImGuiKey.GamepadFaceRight)))
        {
            this.SelectionMade.InvokeSafely();
        }
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || (!this.waitForRelease && ImGui.IsKeyPressed(ImGuiKey.GamepadFaceDown)))
        {
            this.configuration.NotificationAnchorPosition = this.currentAnchorPosition;
            this.configuration.QueueSave();

            this.SelectionMade.InvokeSafely();
        }

        // In the middle of the screen, draw some instructions
        var instructions = isGamepadActive
            ? Locs.InstructionsGamepad.Value
            : Locs.InstructionMouse.Value;

        var dl = ImGui.GetWindowDrawList();
        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            var instructionSize = ImGui.CalcTextSize(instruction);
            var instructionPos = new Vector2(
                ImGuiHelpers.MainViewport.Size.X / 2 - instructionSize.X / 2,
                ImGuiHelpers.MainViewport.Size.Y / 2 - instructionSize.Y / 2 + i * instructionSize.Y);
            instructionPos += viewportPos;
            dl.AddText(instructionPos, 0xFFFFFFFF, instruction);
        }

        ImGui.End();
    }

    private static void DrawPreview(Vector2 anchorPosition, float borderAlpha)
    {
        var dl = ImGui.GetWindowDrawList();
        var width = NotificationManager.CalculateNotificationWidth();
        var height = 100f * ImGuiHelpers.GlobalScale;
        var smallBoxHeight = height * 0.4f;
        var edgeMargin = NotificationConstants.ScaledViewportEdgeMargin;
        var spacing = 10f * ImGuiHelpers.GlobalScale;

        var viewport = ImGuiHelpers.MainViewport;
        var viewportSize = viewport.Size;
        var viewportPos = viewport.Pos;
        var borderColor = ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, borderAlpha));
        var borderThickness = 4.0f * ImGuiHelpers.GlobalScale;
        var borderRounding = 4.0f * ImGuiHelpers.GlobalScale;
        var backgroundColor = new Vector4(0, 0, 0, 0.5f); // Semi-transparent black

        // Calculate positions based on the snap position
        Vector2 topLeft, bottomRight, smallTopLeft, smallBottomRight;

        var snapPos = NotificationManager.ChooseSnapDirection(anchorPosition);
        if (snapPos is NotificationSnapDirection.Top or NotificationSnapDirection.Bottom)
        {
            // Calculate X position - same logic for top and bottom
            var xPos = (viewportSize.X - width) * anchorPosition.X;
            xPos = Math.Max(edgeMargin, Math.Min(viewportSize.X - width - edgeMargin, xPos));

            if (snapPos == NotificationSnapDirection.Top)
            {
                // For top position: big box at top, small box below it
                var yPos = edgeMargin;
                topLeft = new Vector2(xPos, yPos);
                bottomRight = new Vector2(xPos + width, yPos + height);

                smallTopLeft = new Vector2(xPos, yPos + height + spacing);
                smallBottomRight = new Vector2(xPos + width, yPos + height + spacing + smallBoxHeight);
            }
            else
            {
                // For bottom position: big box at bottom, small box above it
                var yPos = viewportSize.Y - height - edgeMargin;
                topLeft = new Vector2(xPos, yPos);
                bottomRight = new Vector2(xPos + width, yPos + height);

                smallTopLeft = new Vector2(xPos, yPos - smallBoxHeight - spacing);
                smallBottomRight = new Vector2(xPos + width, yPos - spacing);
            }
        }
        else
        {
            // For left and right positions, boxes are still stacked vertically (one above the other)
            // Only the horizontal position changes

            // Calculate Y position based on unit offset - used for both left and right positions
            var yPos = (viewportSize.Y - height) * anchorPosition.Y;
            yPos = Math.Max(edgeMargin, Math.Min(viewportSize.Y - height - edgeMargin, yPos));

            if (snapPos == NotificationSnapDirection.Left)
            {
                // For left position: boxes are at the left edge of the screen
                var xPos = edgeMargin;

                if (anchorPosition.Y > 0.5f)
                {
                    // Small box on top
                    smallTopLeft = new Vector2(xPos, yPos - smallBoxHeight - spacing);
                    smallBottomRight = new Vector2(xPos + width, yPos - spacing);

                    // Big box below
                    topLeft = new Vector2(xPos, yPos);
                    bottomRight = new Vector2(xPos + width, yPos + height);
                }
                else
                {
                    // Big box on top
                    topLeft = new Vector2(xPos, yPos);
                    bottomRight = new Vector2(xPos + width, yPos + height);

                    // Small box below
                    smallTopLeft = new Vector2(xPos, yPos + height + spacing);
                    smallBottomRight = new Vector2(xPos + width, yPos + height + spacing + smallBoxHeight);
                }
            }
            else
            {
                // For right position: boxes are at the right edge of the screen
                var xPos = viewportSize.X - width - edgeMargin;

                if (anchorPosition.Y > 0.5f)
                {
                    // Small box on top
                    smallTopLeft = new Vector2(xPos, yPos - smallBoxHeight - spacing);
                    smallBottomRight = new Vector2(xPos + width, yPos - spacing);

                    // Big box below
                    topLeft = new Vector2(xPos, yPos);
                    bottomRight = new Vector2(xPos + width, yPos + height);
                }
                else
                {
                    // Big box on top
                    topLeft = new Vector2(xPos, yPos);
                    bottomRight = new Vector2(xPos + width, yPos + height);

                    // Small box below
                    smallTopLeft = new Vector2(xPos, yPos + height + spacing);
                    smallBottomRight = new Vector2(xPos + width, yPos + height + spacing + smallBoxHeight);
                }
            }
        }

        topLeft += viewportPos;
        bottomRight += viewportPos;
        smallTopLeft += viewportPos;
        smallBottomRight += viewportPos;

        // Draw the big box
        dl.AddRectFilled(topLeft, bottomRight, ImGui.ColorConvertFloat4ToU32(backgroundColor), borderRounding, ImDrawFlags.RoundCornersAll);
        dl.AddRect(topLeft, bottomRight, borderColor, borderRounding, ImDrawFlags.RoundCornersAll, borderThickness);

        // Draw the small box
        dl.AddRectFilled(smallTopLeft, smallBottomRight, ImGui.ColorConvertFloat4ToU32(backgroundColor), borderRounding, ImDrawFlags.RoundCornersAll);
        dl.AddRect(smallTopLeft, smallBottomRight, borderColor, borderRounding, ImDrawFlags.RoundCornersAll, borderThickness);
    }

    private static class Locs
    {
        public static readonly Lazy<string[]> InstructionsGamepad = new(() =>
            Loc.Localize("NotificationPositionChooser.InstructionsGamepad", "Drag your mouse or use the Left Stick to move notifications.\nLeft-click or press A/Cross to select the position.\nRight-click or press B/Circle to cancel.").Split('\n'));

        public static readonly Lazy<string[]> InstructionMouse = new(() =>
            Loc.Localize("NotificationPositionChooser.InstructionMouse", "Drag to move the notifications to where you would like them to appear.\nLeft-click to select the position.\nRight-click to close without making changes.").Split('\n'));
    }
}
