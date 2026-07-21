using Dalamud.Interface;
using Dalamud.NativeUi.Enums;
using Dalamud.NativeUi.Extensions;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Bounds = Dalamud.NativeUi.Classes.Bounds;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// .
/// </summary>
internal abstract unsafe partial class NodeBase
{
    private bool? lastIsVisible;

    /// <summary>
    /// Gets or sets the nodes X position relative to its parent node.
    /// </summary>
    public virtual float X
    {
        get => ResNode->GetXFloat();
        set => ResNode->SetXFloat(value);
    }

    /// <summary>
    /// Gets or sets the nodes Y position relative to its parent node.
    /// </summary>
    public virtual float Y
    {
        get => ResNode->GetYFloat();
        set => ResNode->SetYFloat(value);
    }

    /// <summary>
    /// Gets or sets the nodes position relative to its parent node.
    /// </summary>
    public virtual Vector2 Position
    {
        get => ResNode->Position;
        set => ResNode->Position = value;
    }

    /// <summary>
    /// Gets or sets the nodes ScreenX position.
    /// </summary>
    /// <remarks>
    /// Setting this doesn't seem to do anything.
    /// </remarks>
    public virtual float ScreenX
    {
        get => ResNode->ScreenX;
        set => ResNode->ScreenX = value;
    }

    /// <summary>
    /// Gets or sets the nodes ScreenY position.
    /// </summary>
    /// <remarks>
    /// Setting this doesn't seem to do anything.
    /// </remarks>
    public virtual float ScreenY
    {
        get => ResNode->ScreenY;
        set => ResNode->ScreenY = value;
    }

    /// <summary>
    /// Gets the nodes screen position.
    /// </summary>
    public virtual Vector2 ScreenPosition
        => ResNode->ScreenPosition;

    /// <summary>
    /// Gets or sets this nodes Width.
    /// </summary>
    /// <remarks>
    /// Triggers <see cref="OnSizeChanged"/>.
    /// </remarks>
    public virtual float Width
    {
        get => ResNode->GetWidth();
        set
        {
            ResNode->SetWidth((ushort)value);
            if (value >= 0)
            {
                this.OnSizeChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets this nodes Height.
    /// </summary>
    /// <remarks>
    /// Triggers <see cref="OnSizeChanged"/>.
    /// </remarks>
    public virtual float Height
    {
        get => ResNode->GetHeight();
        set
        {
            ResNode->SetHeight((ushort)value);

            if (value >= 0)
            {
                this.OnSizeChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets this nodes Size.
    /// </summary>
    /// <remarks>
    /// Triggers <see cref="OnSizeChanged"/>.
    /// </remarks>
    public virtual Vector2 Size
    {
        get => ResNode->Size;
        set
        {
            ResNode->SetWidth((ushort)value.X);
            ResNode->SetHeight((ushort)value.Y);

            if (value is { X: >= 0, Y: >= 0 })
            {
                this.OnSizeChanged();
            }
        }
    }

    /// <summary>
    /// Gets this node's bounds.
    /// </summary>
    public Bounds Bounds
        => ResNode->Bounds;

    /// <summary>
    /// Gets this node's center point.
    /// </summary>
    public Vector2 Center
        => ResNode->Center;

    /// <summary>
    /// Gets or sets this node's scale in the X direction.
    /// </summary>
    public virtual float ScaleX
    {
        get => ResNode->GetScaleX();
        set => ResNode->SetScaleX(value);
    }

    /// <summary>
    /// Gets or sets this node's scale in the Y direction.
    /// </summary>
    public virtual float ScaleY
    {
        get => ResNode->GetScaleY();
        set => ResNode->SetScaleY(value);
    }

    /// <summary>
    /// Gets or sets this node's scale.
    /// </summary>
    public virtual Vector2 Scale
    {
        get => ResNode->Scale;
        set => ResNode->Scale = value;
    }

    /// <summary>
    /// Gets or sets this nodes rotation <em>in Radians</em>.
    /// </summary>
    public virtual float Rotation
    {
        get => ResNode->GetRotation();
        set => ResNode->SetRotation(value);
    }

    /// <summary>
    /// Gets or sets this nodes rotation in degrees.
    /// </summary>
    public virtual float RotationDegrees
    {
        get => ResNode->RotationDegrees;
        set => ResNode->RotationDegrees = value;
    }

    /// <summary>
    /// Gets or sets this node's origin's X position.
    /// </summary>
    /// <remarks>
    /// This is used as the reference position for animations.
    /// </remarks>
    public virtual float OriginX
    {
        get => ResNode->OriginX;
        set => ResNode->OriginX = value;
    }

    /// <summary>
    /// Gets or sets this node's origin's Y position.
    /// </summary>
    /// <remarks>
    /// This is used as the reference position for animations.
    /// </remarks>
    public virtual float OriginY
    {
        get => ResNode->OriginY;
        set => ResNode->OriginY = value;
    }

    /// <summary>
    /// Gets or sets this node's origin.
    /// </summary>
    /// <remarks>
    /// This is used as the reference position for animations.
    /// </remarks>
    public virtual Vector2 Origin
    {
        get => ResNode->Origin;
        set => ResNode->Origin = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this node is visible.
    /// </summary>
    /// <remarks>
    /// Triggers <see cref="OnVisibilityToggled"/> if the visibility has changed.
    /// </remarks>
    public virtual bool IsVisible
    {
        get => ResNode->Visible;
        set
        {
            ResNode->Visible = value;
            if (this.lastIsVisible is null || this.lastIsVisible != value)
            {
                this.OnVisibilityToggled?.Invoke(value);
                this.lastIsVisible = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the nodes flags.
    /// </summary>
    public NodeFlags NodeFlags
    {
        get => ResNode->NodeFlags;
        set => ResNode->NodeFlags = value;
    }

    /// <summary>
    /// Gets or sets this nodes Color.
    /// </summary>
    /// <remarks>
    /// Expected value ranges are from 0.0f to 1.0f.
    /// </remarks>
    public virtual Vector4 Color
    {
        get => ResNode->ColorVector;
        set => ResNode->ColorVector = value;
    }

    /// <summary>
    /// Gets or sets this nodes Color via HSVA.
    /// </summary>
    /// <remarks>
    /// Due to converting to and from HSVA there may be some error.
    /// </remarks>
    public virtual ColorHelpers.HsvaColor ColorHsva
    {
        get => ResNode->ColorHsva;
        set => ResNode->ColorHsva = value;
    }

    /// <summary>
    /// Gets or sets this nodes Alpha value.
    /// </summary>
    /// <remarks>
    /// Expected value ranges are from 0.0f to 1.0f.
    /// </remarks>
    public virtual float Alpha
    {
        get => ResNode->Color.A;
        set => ResNode->SetAlpha((byte)(value * 255.0f));
    }

    /// <summary>
    /// Gets or sets this node's AddColor.
    /// </summary>
    /// <remarks>
    /// Expected value ranges from 0.0f to 1.0f.
    /// </remarks>
    public virtual Vector3 AddColor
    {
        get => ResNode->AddColor;
        set => ResNode->AddColor = value;
    }

    /// <summary>
    /// Gets or sets this node's AddColor via HSVA.
    /// </summary>
    /// <remarks>
    /// Due to converting to and from HSVA there may be some error.
    /// </remarks>
    public virtual ColorHelpers.HsvaColor AddColorHsva
    {
        get => ResNode->AddColorHsva;
        set => ResNode->AddColorHsva = value;
    }

    /// <summary>
    /// Gets or sets this node's MultiplyColor.
    /// </summary>
    /// <remarks>
    /// Expected value ranges from 0.0f to 1.0f.
    /// </remarks>
    public virtual Vector3 MultiplyColor
    {
        get => ResNode->MultiplyColor;
        set => ResNode->MultiplyColor = value;
    }

    /// <summary>
    /// Gets or sets this node's MultiplyColor via HSVA.
    /// </summary>
    /// <remarks>
    /// Due to converting to and from HSVA there may be some error.
    /// </remarks>
    public virtual ColorHelpers.HsvaColor MultiplyColorHsva
    {
        get => ResNode->MultiplyColorHsva;
        set => ResNode->MultiplyColorHsva = value;
    }

    /// <summary>
    /// Gets or sets this nodes id.
    /// </summary>
    public uint NodeId
    {
        get => ResNode->NodeId;
        set => ResNode->NodeId = value;
    }

    /// <summary>
    /// Gets or sets this node's DrawFlags.
    /// </summary>
    public virtual DrawFlags DrawFlags
    {
        get => (DrawFlags)ResNode->DrawFlags;
        set => ResNode->DrawFlags = ((uint)value & 0b1111_1111_1111_1100_0000_0011_1111_1111) |
                                    (ResNode->DrawFlags & 0b0000_0000_0000_0011_1111_1100_0000_0000);
    }

    /// <summary>
    /// Gets or sets this node's ClipCount.
    /// </summary>
    public virtual int ClipCount
    {
        get => (int)((ResNode->DrawFlags & 0b0000_0000_0000_0011_1111_1100_0000_0000) >> 10);
        set => ResNode->DrawFlags = (uint)((value << 10) & 0b0000_0000_0000_0011_1111_1100_0000_0000)
                                    | (ResNode->DrawFlags & 0b1111_1111_1111_1100_0000_0011_1111_1111);
    }

    /// <summary>
    /// Gets or sets this nodes Priority.
    /// </summary>
    public int Priority
    {
        get => ResNode->GetPriority();
        set => ResNode->SetPriority((ushort)value);
    }

    /// <summary>
    /// Gets this nodes child count.
    /// </summary>
    public virtual int ChildCount
        => ResNode->ChildCount;

    /// <summary>
    /// Gets or sets this nodes transform value.
    /// </summary>
    public Matrix2x2 Transform
    {
        get => ResNode->Transform;
        set => ResNode->Transform = value;
    }

    private Action<bool>? OnVisibilityToggled { get; set; }

    /// <summary>
    /// Add the specified draw flags to this node's DrawFlags.
    /// </summary>
    /// <param name="flags">Flags to add.</param>
    public void AddDrawFlags(params DrawFlags[] flags)
    {
        foreach (var flag in flags)
        {
            this.DrawFlags |= flag;
        }
    }

    /// <summary>
    /// Removes the specified draw flags from this node's DrawFlags.
    /// </summary>
    /// <param name="flags">Flags to remove.</param>
    public void RemoveDrawFlags(params DrawFlags[] flags)
    {
        foreach (var flag in flags)
        {
            this.DrawFlags &= ~flag;
        }
    }

    /// <summary>
    /// Add the specified node flags to this node's NodeFlags.
    /// </summary>
    /// <param name="flags">Flags to add.</param>
    public void AddNodeFlags(params NodeFlags[] flags)
    {
        foreach (var flag in flags)
        {
            this.NodeFlags |= flag;
        }
    }

    /// <summary>
    /// Remove the specified node flags from this node's NodeFlags.
    /// </summary>
    /// <param name="flags">Flags to remove.</param>
    public void RemoveNodeFlags(params NodeFlags[] flags)
    {
        foreach (var flag in flags)
        {
            this.NodeFlags &= ~flag;
        }
    }

    /// <summary>
    /// Marks this node and all of its children as dirty causing the game to update them next frame.
    /// </summary>
    public void MarkDirty()
    {
        foreach (var child in GetAllChildren(this))
        {
            child.ResNode->AddDrawFlag([DrawFlags.IsDirty]);
        }

        ResNode->AddDrawFlag([DrawFlags.IsDirty]);
    }

    /// <summary>
    /// Check collision with this node using short-value coords.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="inclusive">If the bounds check included the edge.</param>
    /// <returns>True if the value is within the nodes bounds.</returns>
    public bool CheckCollision(short x, short y, bool inclusive = true)
        => ResNode->CheckCollision(x, y, inclusive);

    /// <summary>
    /// Check collision with this node using short-value coords.
    /// </summary>
    /// <param name="position">Coordinate.</param>
    /// <param name="inclusive">If the bounds check included the edge.</param>
    /// <returns>True if the value is within the nodes bounds.</returns>
    public bool CheckCollision(Vector2 position, bool inclusive = true)
        => ResNode->CheckCollision((short)position.X, (short)position.Y, inclusive);

    /// <summary>
    /// Check collision with this node using coords read from a AtkEventData object.
    /// </summary>
    /// <param name="eventData">Pointer to an events data struct, for which to extract the cursor position from.</param>
    /// <param name="inclusive">If the bounds check included the edge.</param>
    /// <returns>True if the value is within the nodes bounds.</returns>
    public bool CheckCollision(AtkEventData* eventData, bool inclusive = true)
        => ResNode->CheckCollision(eventData, inclusive);

    /// <summary>
    /// Overridable function that is called whenever this node's size is changed.
    /// </summary>
    protected virtual void OnSizeChanged()
    {
    }
}
