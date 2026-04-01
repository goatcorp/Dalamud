using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Game.Player;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.Fools26;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Lazy")]
internal class Fools26VerifyWindow : Window, IDisposable
{
    private const float MovementThreshold = 0.01f;
    private const double ColorTransitionDurationSeconds = 0.20;

    private readonly uint[] possibleEmoteRowId = [70, 69, 72, 76];

    private readonly Stopwatch verifyTimer;

    private readonly VerifyStep[] steps =
    [
        new("Keep your character's face in frame.", 5, false),
        new("Please do the emote listed below.", 3, true),
        new("Thank you!", 4, false),
    ];

    private readonly string[] adjectives =
    [
        "lovely",
        "charming",
        "dashing",
        "elegant",
        "graceful",
        "radiant",
        "stunning",
        "awesome",
        "intimidating",
        "regal",
        "majestic",
        "fierce",
        "chaotic",
        "mysterious",
        "menacing",
        "deranged",
        "unstoppable",
        "legendary",
        "parmesan",
        "great",
        "infamous",
        "notorious",
        "quirky",
        "captivating",
        "enchanting",
        "bewitching",
        "blinding",
        "verified",
    ];

    private int currentStep;
    private bool progressing;
    private InterruptReason? interruptReason;
    private int lastCapturedStep = -1;

    private uint selectedEmote;

    private Vector3? lastCameraPos;
    private Vector3? lastCharacterPos;
    private Vector2? lastWindowPos;
    private DateTime lastMovedAt = DateTime.MinValue;

    private Vector4 displayedFillColor = new Vector4(0, 0, 0, 0);
    private Vector4 displayedBorderColor = new Vector4(0, 0, 0, 0);
    private Vector4 targetFillColor = new Vector4(0, 0, 0, 0);
    private Vector4 targetBorderColor = new Vector4(0, 0, 0, 0);
    private Vector4 prevFillColor = new Vector4(0, 0, 0, 0);
    private Vector4 prevBorderColor = new Vector4(0, 0, 0, 0);
    private DateTime colorTransitionStarted = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fools26VerifyWindow"/> class.
    /// </summary>
    public Fools26VerifyWindow()
        : base("Dalamud Character Verification", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.BgAlpha = 0;
        this.Size = new Vector2(300, 400);

        this.verifyTimer = new Stopwatch();

        this.ForceMainWindow = true;
        this.RespectCloseHotkey = false;
        this.AllowClickthrough = false;
        this.AllowPinning = false;
    }

    private enum InterruptReason
    {
        KeepInFrame,
        TurnToCamera,
        ZoomIn,
        KeepEmote,
        Glasses,
        HoldStill,
        ExitGPose,
    }

    private enum EStateResult
    {
        InFrame,
        OutOfFrame,
        TurnedAway,
        TooFar,
        MovedRecently,
        HasGlasses,
        NoCharacter,
        InGPose,
    }

    public IDalamudTextureWrap? Screenshot { get; private set; }

    public string? Rating { get; private set; }

    public override void OnOpen()
    {
        base.OnOpen();

        this.currentStep = 0;
        this.progressing = false;
        this.interruptReason = null;
        this.lastCapturedStep = -1;
        this.Screenshot = null;
        this.Rating = null;

        this.lastCameraPos = null;
        this.lastCharacterPos = null;
        this.lastWindowPos = null;
        this.lastMovedAt = DateTime.MinValue;

        this.Screenshot?.Dispose();
        this.Screenshot = null;
        this.Rating = null;

        this.selectedEmote = this.possibleEmoteRowId[new Random().Next(this.possibleEmoteRowId.Length)];
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2f);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
    }

        /// <inheritdoc/>
    public override void Draw()
    {
        var step = this.steps[this.currentStep];
        var characterState = this.GetCharacterState();

        var currentEmote = Service<DataManager>.Get().Excel.GetSheet<Emote>().GetRow(this.selectedEmote);
        var applicableActionTimeline = currentEmote.ActionTimeline[0].RowId;

        var doingEmote = !step.DoEmote || characterState.CurrentTimeline == applicableActionTimeline;
        this.progressing = characterState.Result == EStateResult.InFrame && doingEmote;

        if (characterState.Result == EStateResult.MovedRecently)
        {
            this.progressing = false;
        }

        if (doingEmote && this.lastCapturedStep != this.currentStep &&
            this.verifyTimer.Elapsed.TotalSeconds > 1f)
        {
            var min = ImGui.GetWindowPos();
            var max = min + ImGui.GetWindowSize() - new Vector2(0, 20);
            this.CaptureScreenshot(min, max);
            this.lastCapturedStep = this.currentStep;
        }

        if (!this.verifyTimer.IsRunning)
        {
            if (characterState.Result is EStateResult.OutOfFrame or EStateResult.NoCharacter)
            {
                this.interruptReason = InterruptReason.KeepInFrame;
            }
            else if (characterState.Result == EStateResult.MovedRecently)
            {
                this.interruptReason = InterruptReason.HoldStill;
            }
            else if (characterState.Result == EStateResult.TurnedAway)
            {
                this.interruptReason = InterruptReason.TurnToCamera;
            }
            else if (characterState.Result == EStateResult.TooFar)
            {
                this.interruptReason = InterruptReason.ZoomIn;
            }
            else if (characterState.Result == EStateResult.HasGlasses)
            {
                this.interruptReason = InterruptReason.Glasses;
            }
            else if (characterState.Result == EStateResult.InGPose)
            {
                this.interruptReason = InterruptReason.ExitGPose;
            }
            else if (step.DoEmote && !doingEmote)
            {
                this.interruptReason = InterruptReason.KeepEmote;
            }
            else
            {
                this.interruptReason = null;
            }
        }

        if (this.verifyTimer.IsRunning)
        {
            var timeLeft = step.Duration - this.verifyTimer.Elapsed.TotalSeconds;
            if (timeLeft <= 0)
            {
                this.verifyTimer.Reset();

                this.currentStep++;
                if (this.currentStep >= this.steps.Length)
                {
                    this.Finish();
                }
            }
            else if (!this.progressing && timeLeft >= 0.25) // be a little graceful and not pause if we're almost done
            {
                this.verifyTimer.Reset();

                if (characterState.Result is EStateResult.OutOfFrame or EStateResult.NoCharacter)
                {
                    this.interruptReason = InterruptReason.KeepInFrame;
                }
                else if (characterState.Result == EStateResult.MovedRecently)
                {
                    this.interruptReason = InterruptReason.HoldStill;
                }
                else if (characterState.Result == EStateResult.TurnedAway)
                {
                    this.interruptReason = InterruptReason.TurnToCamera;
                }
                else if (characterState.Result == EStateResult.TooFar)
                {
                    this.interruptReason = InterruptReason.ZoomIn;
                }
                else if (characterState.Result == EStateResult.HasGlasses)
                {
                    this.interruptReason = InterruptReason.Glasses;
                }
                else if (characterState.Result == EStateResult.InGPose)
                {
                    this.interruptReason = InterruptReason.ExitGPose;
                }
                else
                {
                    // TODO: this is a little harsh, maybe we should not do this
                    this.interruptReason = InterruptReason.KeepEmote;
                }
            }
        }
        else
        {
            if (this.interruptReason is not null && this.progressing)
            {
                this.interruptReason = null;
                this.verifyTimer.Start();
            }
            else if (this.interruptReason is null && this.progressing && !this.verifyTimer.IsRunning)
            {
                this.verifyTimer.Start();
            }
        }

        var style = ImGui.GetStyle();
        var fontSize = ImGui.GetFontSize();
        var windowSize = ImGui.GetWindowSize();

        var headerSize = fontSize
                         + (fontSize + (style.FramePadding.Y * 2))
                         + style.ItemSpacing.Y * 3;

        const float uiAlpha = 1f;
        ImGui.SetNextWindowBgAlpha(uiAlpha);
        var redColor = new Vector4(0.07f, 0, 0, 1);
        using (ImRaii.PushColor(ImGuiCol.ChildBg, redColor, /* this.interruptReason is not null */ false))
        {
            using (var header = ImRaii.Child("##header", new Vector2(0, headerSize)))
            {
                if (header.Success)
                {
                    ImGui.Spacing();
                    ImGuiHelpers.CenteredText(step.Description);

                    var progress = (float)(this.verifyTimer.Elapsed.TotalSeconds / step.Duration);
                    var progressSize = windowSize.X / 2;
                    ImGuiHelpers.CenterCursorFor(progressSize);
                    ImGui.ProgressBar(EaseInOutCubic(progress), new Vector2(progressSize, 0));
                    ImGui.Spacing();

                    float EaseInOutCubic(float x)
                    {
                        return x < 0.5f ? 4 * x * x * x : 1 - (float)Math.Pow(-2 * x + 2, 3) / 2;
                    }
                }
            }

            var footerSize = fontSize + style.ItemSpacing.Y * 4;
            ImGui.SetCursorPosY(windowSize.Y - footerSize);
            ImGui.SetNextWindowBgAlpha(uiAlpha);
            using (var footer = ImRaii.Child("##footer", new Vector2(0, footerSize)))
            {
                if (footer.Success)
                {
                    var statusText = this.interruptReason switch
                    {
                        InterruptReason.KeepInFrame => "Please center your character's face.",
                        InterruptReason.HoldStill => "Please hold the camera still.",
                        InterruptReason.TurnToCamera => "Please turn toward the camera.",
                        InterruptReason.ZoomIn => "Please zoom in a little.",
                        InterruptReason.KeepEmote => "Please do a {0} emote.".Format(currentEmote.TextCommand.Value.Command.ExtractText()),
                        InterruptReason.Glasses => "Please remove your glasses.",
                        InterruptReason.ExitGPose => "Please exit Group Pose.",
                        _ => this.verifyTimer.IsRunning ? "Verifying..." : "Waiting...",
                    };

                    ImGui.Spacing();
                    ImGuiHelpers.CenteredText(statusText);
                    ImGui.Spacing();
                }
            }

            var drawList = ImGui.GetWindowDrawList();
            var center = ImGui.GetWindowPos() + (windowSize / 2f);
            center += new Vector2(0, 20 * ImGuiHelpers.GlobalScale); // shift up a bit to account for the header text
            var radius = Math.Min(windowSize.X, windowSize.Y) * 0.37f;
            var childBg = this.interruptReason is not null
                ? redColor
                : style.Colors[(int)ImGuiCol.ChildBg];
            var fill = childBg with { W = uiAlpha };
            var borderColor = this.interruptReason is not null ? ImGuiColors.DalamudOrange : ImGuiColors.HealerGreen;
            this.DrawInvertedCircle(drawList, center, radius, fill, borderColor, 10f, 3f);
        }
    }

    public void Dispose()
    {
        this.verifyTimer.Stop();
        this.Screenshot?.Dispose();
    }

    private static Vector4 Lerp(Vector4 a, Vector4 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Vector4(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t);
    }

    private static bool Vector4Equal(Vector4 a, Vector4 b, float eps = 0.0025f)
    {
        return MathF.Abs(a.X - b.X) <= eps
               && MathF.Abs(a.Y - b.Y) <= eps
               && MathF.Abs(a.Z - b.Z) <= eps
               && MathF.Abs(a.W - b.W) <= eps;
    }

    private unsafe (Vector3? Position, Quaternion? Rotation) GetHeadPose(Character* character)
    {
        const string headBone = "j_kao";
        if (character is null) return (null, null);
        var characterBase = character->GetCharacterBase();
        if (characterBase is null) return (null, null);

        var characterPos = (Vector3)character->Position;

        Vector3? headPos = null;
        Quaternion? headRot = null;

        for (var skeletonIdx = 0; skeletonIdx < characterBase->Skeleton->PartialSkeletonCount; skeletonIdx++)
        {
            var partialSkeleton = characterBase->Skeleton->PartialSkeletons[skeletonIdx];
            for (var animatedSkeletonIdx = 0; animatedSkeletonIdx < 2; animatedSkeletonIdx++)
            {
                var animatedSkeleton = partialSkeleton.GetHavokAnimatedSkeleton(animatedSkeletonIdx);
                if (animatedSkeleton == null) continue;
                var pose = partialSkeleton.GetHavokPose(animatedSkeletonIdx);
                if (pose == null) continue;

                for (var boneIdx = 0; boneIdx < animatedSkeleton->Skeleton->Bones.Length; boneIdx++)
                {
                    var bone = animatedSkeleton->Skeleton->Bones[boneIdx];
                    if (bone.Name.String == headBone)
                    {
                        var translation = pose->ModelPose.Data[boneIdx].Translation;
                        headPos = new Vector3(translation.X, translation.Y, translation.Z) + characterPos;

                        var r = pose->ModelPose.Data[boneIdx].Rotation;
                        var boneQ = new Quaternion(r.X, r.Y, r.Z, r.W);
                        var yaw = character->Rotation;
                        var charQ = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
                        headRot = Quaternion.Multiply(charQ, boneQ);
                        headRot = Quaternion.Normalize(headRot.Value);

                        break;
                    }
                }

                if (headPos.HasValue) break;
            }

            if (headPos.HasValue) break;
        }

        return (headPos, headRot);
    }

    private unsafe (EStateResult Result, uint? CurrentTimeline) GetCharacterState()
    {
        var gameGui = Service<GameGui>.Get();
        var objectTable = Service<ObjectTable>.Get();
        var clientState = Service<ClientState>.Get();

        if (clientState.IsGPosing) return (EStateResult.InGPose, null);

        var cc = CameraManager.Instance()->GetActiveCamera();
        if (cc is null) return (EStateResult.NoCharacter, null);

        var camPos = cc->CameraBase.SceneCamera.Position;

        var dalamudCharacter = objectTable.LocalPlayer;
        if (dalamudCharacter is null) return (EStateResult.NoCharacter, null);

        var character = (Character*)dalamudCharacter.Address;
        if (character is null) return (EStateResult.NoCharacter, null);

        var camPosVec = (Vector3)camPos;
        var currentCharPos = (Vector3)character->Position;
        var currentWindowPos = ImGui.GetWindowPos();

        // recently moved state
        {
            var moved = false;
            if (this.lastCameraPos is null || Vector3.Distance(this.lastCameraPos.Value, camPosVec) > MovementThreshold)
                moved = true;
            else if (this.lastCharacterPos is null ||
                     Vector3.Distance(this.lastCharacterPos.Value, currentCharPos) > MovementThreshold)
                moved = true;
            else if (this.lastWindowPos is null ||
                     Vector2.Distance(this.lastWindowPos.Value, currentWindowPos) > MovementThreshold)
                moved = true;

            if (moved)
            {
                this.lastMovedAt = DateTime.UtcNow;
            }
        }

        this.lastCameraPos = camPosVec;
        this.lastCharacterPos = currentCharPos;
        this.lastWindowPos = currentWindowPos;

        var result = EStateResult.OutOfFrame;
        var headPose = this.GetHeadPose(character);
        var bonePos = headPose.Position;
        var headRot = headPose.Rotation;
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        if (bonePos != null)
        {
            if (gameGui.WorldToScreen(bonePos.Value, out var pos))
            {
                if (pos.X >= windowPos.X
                    && pos.Y >= windowPos.Y
                    && pos.X < (windowPos.X + windowSize.X)
                    && pos.Y < (windowPos.Y + windowSize.Y))
                {
                    result = EStateResult.InFrame;
                }

                if (result == EStateResult.InFrame)
                {
                    const float headFacingAngleDeg = 20f; // tolerance in degrees

                    var toCamera = Vector3.Normalize(camPosVec - bonePos.Value);
                    var headForward = Vector3.Normalize(Vector3.Transform(new Vector3(1, 0, 0), headRot!.Value));
                    var dot = Vector3.Dot(headForward, toCamera);
                    var thresholdCos = MathF.Cos(headFacingAngleDeg * MathF.PI / 180f);
                    var facing = dot >= thresholdCos;

                    if (!facing)
                    {
                        result = EStateResult.TurnedAway;
                    }
                }

                if (result == EStateResult.InFrame)
                {
                    var distance = Vector3.Distance(camPos, bonePos.Value);
                    const float maxDistance = 3.8f;
                    if (distance > maxDistance)
                    {
                        result = EStateResult.TooFar;
                    }
                }
            }
        }

        if (result == EStateResult.InFrame)
        {
            foreach (var glassesId in character->DrawData.GlassesIds)
            {
                if (glassesId == 0)
                    continue;

                result = EStateResult.HasGlasses;
            }
        }

        if (result == EStateResult.InFrame && this.lastMovedAt != DateTime.MinValue && (DateTime.UtcNow - this.lastMovedAt).TotalSeconds <= 1f)
        {
            result = EStateResult.MovedRecently;
        }

        // 2 = face
        var currentExpression = character->Timeline.TimelineSequencer.TimelineIds[2];
        return (result, currentExpression);
    }

    private void CaptureScreenshot(Vector2 min, Vector2 max)
    {
        var vp = ImGui.GetMainViewport();

        Service<TextureManager>.Get().CreateFromImGuiViewportAsync(new ImGuiViewportTextureArgs()
        {
            TakeBeforeImGuiRender = true,
            ViewportId = ImGui.GetMainViewport().ID,
            Uv0 = min / vp.Size,
            Uv1 = max / vp.Size,
        }, null, "april fools screenshot").ContinueWith(t => { this.Screenshot = t.Result; });
    }

    private void Finish()
    {
        var dalamudCharacter = Service<ObjectTable>.Get().LocalPlayer;
        if (dalamudCharacter is not null)
        {
            this.Rating = this.adjectives[dalamudCharacter.Customize.Sum(x => x) % this.adjectives.Length];
        }

        this.IsOpen = false;

        Service<DalamudInterface>.Get().OpenFools26Result();

        var config = Service<DalamudConfiguration>.Get();
        config.Fools26CompletedContentIds.Add(Service<PlayerState>.Get().ContentId);
    }

    private void DrawInvertedCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 fillColor,
        Vector4 borderColor, float borderGap, float borderThickness, int scanStep = 1)
    {
        if (!Vector4Equal(this.targetFillColor, fillColor) || !Vector4Equal(this.targetBorderColor, borderColor))
        {
            this.prevFillColor = this.displayedFillColor;
            this.prevBorderColor = this.displayedBorderColor;
            this.targetFillColor = fillColor;
            this.targetBorderColor = borderColor;
            this.colorTransitionStarted = DateTime.UtcNow;
        }

        var t = 1f;
        if (this.colorTransitionStarted != DateTime.MinValue)
        {
            var elapsed = (DateTime.UtcNow - this.colorTransitionStarted).TotalSeconds;
            if (elapsed < ColorTransitionDurationSeconds)
            {
                t = (float)(elapsed / ColorTransitionDurationSeconds);
            }
            else
            {
                t = 1f;
                this.colorTransitionStarted = DateTime.MinValue;
            }
        }

        var currentFill = Lerp(this.prevFillColor, this.targetFillColor, t);
        var currentBorder = Lerp(this.prevBorderColor, this.targetBorderColor, t);

        this.displayedFillColor = currentFill;
        this.displayedBorderColor = currentBorder;

        var fillU32 = ImGui.ColorConvertFloat4ToU32(currentFill);

        var wp = ImGui.GetWindowPos();
        var ws = ImGui.GetWindowSize();
        var windowRectMin = wp;
        var windowRectMax = wp + ws;

        var left = center.X - radius;
        var right = center.X + radius;
        var top = center.Y - radius;
        var bottom = center.Y + radius;

        if (left > windowRectMin.X)
            drawList.AddRectFilled(windowRectMin, new Vector2(left, windowRectMax.Y), fillU32);
        if (right < windowRectMax.X)
            drawList.AddRectFilled(new Vector2(right, windowRectMin.Y), windowRectMax, fillU32);

        var topClipMin = new Vector2(Math.Max(left, windowRectMin.X), windowRectMin.Y);
        var topClipMax = new Vector2(Math.Min(right, windowRectMax.X), Math.Max(windowRectMin.Y, top));
        if (topClipMax.Y > topClipMin.Y)
            drawList.AddRectFilled(topClipMin, topClipMax, fillU32);

        var bottomClipMin = new Vector2(Math.Max(left, windowRectMin.X), Math.Min(bottom, windowRectMax.Y));
        var bottomClipMax = new Vector2(Math.Min(right, windowRectMax.X), windowRectMax.Y);
        if (bottomClipMax.Y > bottomClipMin.Y)
            drawList.AddRectFilled(bottomClipMin, bottomClipMax, fillU32);

        var step = Math.Max(1, scanStep);
        var yStart = (int)Math.Max(windowRectMin.Y, Math.Floor(top));
        var yEnd = (int)Math.Min(windowRectMax.Y, Math.Ceiling(bottom));
        for (var y = yStart; y < yEnd; y += step)
        {
            var yf = y + 0.5f; // center of the scan strip
            var dy = yf - center.Y;
            var r2 = radius * radius;
            var dx = 0f;
            var dy2 = dy * dy;
            if (dy2 < r2)
                dx = (float)Math.Sqrt(r2 - dy2);

            var segLeft = center.X - dx;
            var segRight = center.X + dx;

            var fillLeft = Math.Max(windowRectMin.X, segLeft);
            var fillRight = Math.Min(windowRectMax.X, segRight);

            var yTop = y;
            var yBottom = Math.Min(y + step, windowRectMax.Y);

            if (fillLeft > windowRectMin.X)
                drawList.AddRectFilled(new Vector2(windowRectMin.X, yTop), new Vector2(fillLeft, yBottom), fillU32);

            if (fillRight < windowRectMax.X)
                drawList.AddRectFilled(new Vector2(fillRight, yTop), new Vector2(windowRectMax.X, yBottom), fillU32);
        }

        var borderU32 = ImGui.ColorConvertFloat4ToU32(currentBorder);
        drawList.AddCircle(center, radius + borderGap, borderU32, borderThickness);
    }

    private record VerifyStep(string Description, double Duration, bool DoEmote);
}
