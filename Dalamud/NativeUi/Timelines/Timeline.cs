using System.Collections.Generic;
using System.Numerics;

using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Managed representation of a built timeline.
/// </summary>
internal unsafe class Timeline : IDisposable
{
    private readonly TimelineResource internalTimelineResource;

    /// <summary>
    /// Initializes a new instance of the <see cref="Timeline"/> class.
    /// </summary>
    public Timeline()
    {
        this.InternalTimeline = (AtkTimeline*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTimeline), 8);

        this.internalTimelineResource = new TimelineResource();
        this.InternalTimeline->Resource = this.internalTimelineResource.InternalResource;
        this.InternalTimeline->LabelResource = null;
        this.InternalTimeline->ActiveAnimation = null;
        this.InternalTimeline->OwnerNode = null;
    }

    /// <summary>
    /// Sets the timeline animations used for this Timeline.
    /// </summary>
    public List<TimelineAnimation> Animations
    {
        set => this.internalTimelineResource.Animations = value;
    }

    /// <summary>
    /// Sets the label sets used for this Timeline.
    /// </summary>
    public List<TimelineLabelSet> LabelSets
    {
        set => this.internalTimelineResource.LabelSets = value;
    }

    /// <summary>
    /// Gets the pointer to the allocated timeline data.
    /// </summary>
    internal AtkTimeline* InternalTimeline { get; private set; }

    /// <summary>
    /// Gets or sets the pointer to the owner node.
    /// </summary>
    internal AtkResNode* OwnerNode
    {
        get => this.InternalTimeline->OwnerNode;
        set => this.InternalTimeline->OwnerNode = value;
    }

    /// <summary>
    /// Gets or sets the frametime.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal float FrameTime
    {
        get => this.InternalTimeline->FrameTime;
        set => this.InternalTimeline->FrameTime = value;
    }

    /// <summary>
    /// Gets or sets the parent frametime.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal float ParentFrameTime
    {
        get => this.InternalTimeline->ParentFrameTime;
        set => this.InternalTimeline->ParentFrameTime = value;
    }

    /// <summary>
    /// Gets or sets the Label Frame Idx Duration.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal int LabelFrameIdxDuration
    {
        get => this.InternalTimeline->LabelFrameIdxDuration;
        set => this.InternalTimeline->LabelFrameIdxDuration = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the label frame end id.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal int LabelEndFrameIdx
    {
        get => this.InternalTimeline->LabelEndFrameIdx;
        set => this.InternalTimeline->LabelEndFrameIdx = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the active label by id.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal int ActiveLabelId
    {
        get => this.InternalTimeline->ActiveLabelId;
        set => this.InternalTimeline->ActiveLabelId = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the timeline mask.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal AtkTimelineMask Mask
    {
        get => this.InternalTimeline->Mask;
        set => this.InternalTimeline->Mask = value;
    }

    /// <summary>
    /// Gets or sets the timeline flags.
    /// </summary>
    /// <returns>
    /// This is controlled by the game, setting will likely be ignored.
    /// </returns>
    internal AtkTimelineFlags Flags
    {
        get => this.InternalTimeline->Flags;
        set => this.InternalTimeline->Flags = value;
    }

    /// <summary>
    /// Plays the specified animation via label ID.
    /// </summary>
    /// <param name="labelId">The label ID to play.</param>
    /// <param name="force">Force the animation to restart even if it was already playing.</param>
    public void PlayAnimation(int labelId, bool force = false)
        => this.PlayAnimation(AtkTimelineJumpBehavior.Start, labelId, force);

    /// <summary>
    /// Plays the specified animation via label ID, with option to force it to restart if it's already running.
    /// </summary>
    /// <param name="behavior">Jump behavior.</param>
    /// <param name="labelId">Label id.</param>
    /// <param name="force">If it should force the animation to restart if it's already running.</param>
    public void PlayAnimation(AtkTimelineJumpBehavior behavior, int labelId, bool force = false)
    {
        if (this.InternalTimeline is null) return;

        if (this.InternalTimeline->ActiveLabelId != labelId || force)
        {
            this.InternalTimeline->PlayAnimation(behavior, (ushort)labelId);
        }
    }

    /// <summary>
    /// Stops any active animation by invoking labelId 0.
    /// </summary>
    public void StopAnimation()
    {
        if (this.InternalTimeline is null) return;

        this.InternalTimeline->PlayAnimation(AtkTimelineJumpBehavior.Start, 0);
    }

    /// <summary>
    /// Helper for updating a specific keyframes values.
    /// </summary>
    /// <param name="frameId">Frame id.</param>
    /// <param name="groupType">Group type.</param>
    /// <param name="position">Position.</param>
    /// <param name="alpha">Alpha.</param>
    /// <param name="addColor">Add Color.</param>
    /// <param name="multiplyColor">Multiply Color.</param>
    /// <param name="rotation">Rotation in radians.</param>
    /// <param name="scale">Scale.</param>
    /// <param name="textColor">Text color.</param>
    /// <param name="textOutlineColor">Text Outline color.</param>
    /// <param name="partId">Part Id.</param>
    /// <param name="interpolation">Interpolation method.</param>
    public void UpdateKeyFrame(
        int frameId, KeyFrameGroupType groupType, Vector2? position = null, byte? alpha = null, Vector3? addColor = null, Vector3? multiplyColor = null,
        float? rotation = null, Vector2? scale = null, Vector3? textColor = null, Vector3? textOutlineColor = null, uint? partId = null, AtkTimelineInterpolation? interpolation = null)
    {
        var keyFrame = this.GetKeyFrame(groupType, frameId);
        if (keyFrame is null) return;

        if (position is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Position = position.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (alpha is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Alpha = alpha.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (addColor is not null || multiplyColor is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, AddColor = addColor ?? new Vector3(0.0f, 0.0f, 0.0f), MultiplyColor = multiplyColor ?? new Vector3(100.0f, 100.0f, 100.0f), Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (rotation is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Rotation = rotation.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (scale is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Scale = scale.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (textColor is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, TextColor = textColor.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (textOutlineColor is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, TextEdgeColor = textOutlineColor.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }

        if (partId is not null)
        {
            *keyFrame = new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, PartId = partId.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.internalTimelineResource.Dispose();

        IMemorySpace.Free(this.InternalTimeline);
        this.InternalTimeline = null;
    }

    private AtkTimelineKeyFrame* GetKeyFrame(KeyFrameGroupType type, int frameIndex)
    {
        var animation = this.GetAnimationForFrameId(frameIndex);
        if (animation is null) return null;

        var keyGroup = animation->KeyGroups.GetPointer((int)type);
        for (var i = 0; i < keyGroup->KeyFrameCount; i++)
        {
            var keyFrame = &keyGroup->KeyFrames[i];

            if (keyFrame->FrameIdx == frameIndex)
            {
                return keyFrame;
            }
        }

        return null;
    }

    private AtkTimelineAnimation* GetAnimationForFrameId(int frameId)
    {
        if (this.InternalTimeline is null) return null;
        if (this.InternalTimeline->Resource is null) return null;

        for (var index = 0; index < this.InternalTimeline->Resource->AnimationCount; index++)
        {
            var animation = &this.InternalTimeline->Resource->Animations[index];

            if (animation->StartFrameIdx <= frameId && frameId <= animation->EndFrameIdx)
                return animation;
        }

        return null;
    }
}
