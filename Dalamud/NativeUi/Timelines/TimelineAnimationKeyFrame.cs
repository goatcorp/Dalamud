using System.Numerics;

using Dalamud.NativeUi.Enums;
using Dalamud.NativeUi.Extensions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Adaptor class for easily setting keyframe internal values. Not intended for external use.
/// </summary>
internal class TimelineAnimationKeyFrame : TimelineKeyFrame
{
    private readonly NodeTint nodeTint = new();

    /// <summary>
    /// Gets or sets x/Y position that the node will be set to.
    /// </summary>
    /// <remarks>
    /// This is not an offset from original position.
    /// </remarks>
    public Vector2 Position
    {
        get => new(this.Value.Float2.Item1, this.Value.Float2.Item2);
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                Float2 = new StdPair<float, float>(value.X, value.Y),
            };

            this.GroupSelector = KeyFrameGroupType.Position;
            this.GroupType = AtkTimelineKeyGroupType.Float2;
        }
    }

    /// <summary>
    /// Gets or sets alpha transparency, expects 0 to 255.
    /// </summary>
    public byte Alpha
    {
        get => this.Value.Byte;
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                Byte = value,
            };

            this.GroupType = AtkTimelineKeyGroupType.Byte;
            this.GroupSelector = KeyFrameGroupType.Alpha;
        }
    }

    /// <summary>
    /// Sets add Color.
    /// </summary>
    public Vector3 AddColor
    {
        set
        {
            this.nodeTint.AddColor = value;
            this.UpdateNodeTint();
        }
    }

    /// <summary>
    /// Sets multiply Color.
    /// </summary>
    public Vector3 MultiplyColor
    {
        set
        {
            this.nodeTint.MultiplyColor = value;
            this.UpdateNodeTint();
        }
    }

    /// <summary>
    /// Gets or sets rotation in radians.
    /// </summary>
    public float Rotation
    {
        get => this.Value.Float;
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                Float = value,
            };

            this.GroupType = AtkTimelineKeyGroupType.Float;
            this.GroupSelector = KeyFrameGroupType.Rotation;
        }
    }

    /// <summary>
    /// Gets or sets scale.
    /// </summary>
    public Vector2 Scale
    {
        get => new(this.Value.Float2.Item1, this.Value.Float2.Item2);
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                Float2 = new StdPair<float, float>(value.X, value.Y),
            };

            this.GroupType = AtkTimelineKeyGroupType.Float2;
            this.GroupSelector = KeyFrameGroupType.Scale;
        }
    }

    /// <summary>
    /// Gets or sets text Color.
    /// </summary>
    public Vector3 TextColor
    {
        get => new Vector3(this.Value.RGB.R, this.Value.RGB.G, this.Value.RGB.B) * 255.0f;
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                RGB = value.AsVector4().ToByteColor(),
            };

            this.GroupType = AtkTimelineKeyGroupType.RGB;
            this.GroupSelector = KeyFrameGroupType.TextColor;
        }
    }

    /// <summary>
    /// Gets or sets text outline color.
    /// </summary>
    public Vector3 TextEdgeColor
    {
        get => new Vector3(this.Value.RGB.R, this.Value.RGB.G, this.Value.RGB.B) * 255.0f;
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                RGB = value.AsVector4().ToByteColor(),
            };

            this.GroupType = AtkTimelineKeyGroupType.RGB;
            this.GroupSelector = KeyFrameGroupType.TextEdge;
        }
    }

    /// <summary>
    /// Sets part id to use.
    /// </summary>
    public uint PartId
    {
        set
        {
            this.Value = new AtkTimelineKeyValue
            {
                UShort = (ushort)value,
            };

            this.GroupType = AtkTimelineKeyGroupType.UShort;
            this.GroupSelector = KeyFrameGroupType.PartId;
        }
    }

    private void UpdateNodeTint()
    {
        this.Value = new AtkTimelineKeyValue
        {
            NodeTint = this.nodeTint,
        };

        this.GroupType = AtkTimelineKeyGroupType.NodeTint;
        this.GroupSelector = KeyFrameGroupType.Tint;
    }
}
