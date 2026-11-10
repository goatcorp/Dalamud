using System.Collections.Generic;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// A sub part in the <see cref="TimelineBuilder"/> system.
/// This specific builder will build framesets, these are sets of keyframe animations or sets of animation labels.
/// </summary>
/// <remarks>
/// A single node can have both labels and keyframes, however, the labels will only control children animations.
/// Animations can only be run and defined by parent nodes, it's not possible to self contain everything you need to animation.
/// </remarks>
internal class FrameSetBuilder(TimelineBuilder parent, int startFrameId, int endFrameId)
{
    private readonly List<TimelineKeyFrame> animationKeyFrames = [];
    private readonly List<TimelineKeyFrame> labelKeyFrames = [];

    /// <summary>
    /// Adds a prebuilt set of keyframes to this builder.
    /// </summary>
    /// <param name="keyFrame">Array of frames to start with.</param>
    /// <returns>A builder for adding keyframes.</returns>
    public FrameSetBuilder AddFrame(params TimelineKeyFrame[] keyFrame)
    {
        foreach (var frame in keyFrame)
        {
            switch (frame.GroupType)
            {
                case AtkTimelineKeyGroupType.Label:
                    this.labelKeyFrames.Add(frame);
                    break;

                case AtkTimelineKeyGroupType.Float2:
                case AtkTimelineKeyGroupType.Float:
                case AtkTimelineKeyGroupType.Byte:
                case AtkTimelineKeyGroupType.NodeTint:
                case AtkTimelineKeyGroupType.UShort:
                case AtkTimelineKeyGroupType.RGB:
                case AtkTimelineKeyGroupType.Short:
                case AtkTimelineKeyGroupType.None:
                default:
                    this.animationKeyFrames.Add(frame);
                    break;
            }
        }

        return this;
    }

    /// <summary>
    /// Adds an empty keyframe.
    /// </summary>
    /// <param name="frameId">Frame id to add keyframes to.</param>
    /// <returns>A builder for adding keyframes.</returns>
    public FrameSetBuilder AddEmptyFrame(int frameId)
    {
        this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
        {
            FrameIndex = frameId, GroupType = AtkTimelineKeyGroupType.None,
        });

        return this;
    }

    /// <summary>
    /// Adds a keyframe with the specified properties.
    /// </summary>
    /// <param name="frameId">Frame index.</param>
    /// <param name="position">Position Offset from nodes original position.</param>
    /// <param name="alpha">Transparency, range from 0 to 255, if 0 the node will not be visible.</param>
    /// <param name="addColor">RGB Color to add, range from 0 to 255.</param>
    /// <param name="multiplyColor">RGB Multiply color, range from 0 to 100, 0 will make the node fully black.</param>
    /// <param name="rotation">Rotation in radians.</param>
    /// <param name="scale">Scale.</param>
    /// <param name="textColor">Text Color.</param>
    /// <param name="textOutlineColor">Outline Color.</param>
    /// <param name="partId">PartId, this is smoothly transitioned to change which part an image node shows over time.</param>
    /// <param name="interpolation">What kind of interpolation to use.</param>
    /// <param name="rotationDegrees">Rotation in degrees.</param>
    /// <returns>A builder for adding keyframes.</returns>
    public FrameSetBuilder AddFrame(
        int frameId, Vector2? position = null, byte? alpha = null, Vector3? addColor = null, Vector3? multiplyColor = null,
        float? rotation = null, Vector2? scale = null, Vector3? textColor = null, Vector3? textOutlineColor = null, uint? partId = null, AtkTimelineInterpolation? interpolation = null,
        float? rotationDegrees = null)
        {
        if (position is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Position = position.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (alpha is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Alpha = alpha.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (addColor is not null || multiplyColor is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, AddColor = addColor ?? new Vector3(0.0f, 0.0f, 0.0f), MultiplyColor = multiplyColor ?? new Vector3(100.0f, 100.0f, 100.0f), Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (rotation is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Rotation = rotation.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (rotationDegrees is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Rotation = rotationDegrees.Value * MathF.PI / 180.0f, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (scale is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, Scale = scale.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (textColor is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, TextColor = textColor.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (textOutlineColor is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, TextEdgeColor = textOutlineColor.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        if (partId is not null)
        {
            this.animationKeyFrames.Add(new TimelineAnimationKeyFrame
            {
                FrameIndex = frameId, PartId = partId.Value, Interpolation = interpolation ?? AtkTimelineInterpolation.Linear,
            });
        }

        return this;
    }

    /// <summary>
    /// Adds an animation label.
    /// </summary>
    /// <remarks>
    /// Labels define ranges of keyframes and their behavior when at the end of the range.
    /// </remarks>
    /// <param name="frameId">Frame id to add keyframes to.</param>'
    /// <param name="labelId">Label id to set.</param>
    /// <param name="jumpBehavior">Jump behavior.</param>
    /// <param name="labelTarget">Jump target.</param>
    /// <returns>A builder for adding keyframes.</returns>
    public FrameSetBuilder AddLabel(int frameId, int labelId, AtkTimelineJumpBehavior jumpBehavior, int labelTarget)
    {
        this.labelKeyFrames.Add(new TimelineLabelSetKeyFrame
        {
            FrameIndex = frameId,
            GroupType = AtkTimelineKeyGroupType.Label,
            JumpBehavior = jumpBehavior,
            LabelId = labelId,
            JumpLabelId = labelTarget,
        });

        return this;
    }

    /// <summary>
    /// Adds a standardized label pair that will run to completion and stop when played.
    /// </summary>
    /// <param name="frameStart">Start frame index.</param>
    /// <param name="frameStop">End frame index.</param>
    /// <param name="labelId">Label id.</param>
    /// <returns>A builder for adding keyframes.</returns>
    public FrameSetBuilder AddLabelPair(int frameStart, int frameStop, int labelId)
    {
        this.labelKeyFrames.Add(new TimelineLabelSetKeyFrame
        {
            FrameIndex = frameStart,
            GroupType = AtkTimelineKeyGroupType.Label,
            JumpBehavior = AtkTimelineJumpBehavior.Start,
            LabelId = labelId,
        });

        this.labelKeyFrames.Add(new TimelineLabelSetKeyFrame
        {
            FrameIndex = frameStop,
            GroupType = AtkTimelineKeyGroupType.Label,
            JumpBehavior = AtkTimelineJumpBehavior.PlayOnce,
            LabelId = 0,
            JumpLabelId = 0,
        });

        return this;
    }

    /// <summary>
    /// Begins am individual keyframe builder.
    /// </summary>
    /// <param name="frame">Frame id to add keyframes to.</param>
    /// <returns>A builder for adding keyframes.</returns>
    public KeyFrameBuilder BeginFrameBuilder(int frame)
        => new(this, frame);

    /// <summary>
    /// Ends this frame sets building, and populates the parents labels and keyframes.
    /// </summary>
    /// <returns>Parent timeline builder.</returns>
    public TimelineBuilder EndFrameSet()
    {
        if (this.labelKeyFrames.Count != 0)
        {
            parent.LabelSets.Add(new TimelineLabelSet
            {
                StartFrameId = startFrameId, EndFrameId = endFrameId, Labels = this.labelKeyFrames,
            });
        }

        if (this.animationKeyFrames.Count != 0)
        {
            parent.Animations.Add(new TimelineAnimation
            {
                StartFrameId = startFrameId, EndFrameId = endFrameId, KeyFrames = this.animationKeyFrames,
            });
        }

        return parent;
    }
}
