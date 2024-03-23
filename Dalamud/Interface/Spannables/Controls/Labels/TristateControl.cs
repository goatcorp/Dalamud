using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Patterns;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tri-state control for checkboxes and radio buttons.</summary>
public class TristateControl : LabelControl
{
    private bool? @checked = false;
    private TristateIconPattern? normalIcon;
    private TristateIconPattern? hoveredIcon;
    private TristateIconPattern? activeIcon;
    private IconSide side;

    /// <summary>Initializes a new instance of the <see cref="TristateControl"/> class.</summary>
    public TristateControl()
    {
        this.CaptureMouseOnMouseDown = true;
    }

    /// <summary>Occurs when the property <see cref="Checked"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool?>? CheckedChange;

    /// <summary>Occurs when the property <see cref="Side"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, IconSide>? SideChange;

    /// <summary>Occurs when the property <see cref="NormalIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, TristateIconPattern?>? NormalIconChange;

    /// <summary>Occurs when the property <see cref="HoveredIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, TristateIconPattern?>? HoveredIconChange;

    /// <summary>Occurs when the property <see cref="ActiveIcon"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, TristateIconPattern?>? ActiveIconChange;

    /// <summary>Side of an icon.</summary>
    public enum IconSide
    {
        /// <summary>Show the icon on the left.</summary>
        Left,

        /// <summary>Show the icon on the top.</summary>
        Top,

        /// <summary>Show the icon on the right.</summary>
        Right,

        /// <summary>Show the icon on the bottom.</summary>
        Bottom,
    }

    /// <summary>Gets or sets value indicating whether this checkbox is checked.</summary>
    public bool? Checked
    {
        get => this.@checked;
        set => this.HandlePropertyChange(nameof(this.Checked), ref this.@checked, value, this.OnCheckedChange);
    }

    /// <summary>Gets or sets the side to display the icon.</summary>
    public IconSide Side
    {
        get => this.side;
        set => this.HandlePropertyChange(
            nameof(this.Side),
            ref this.side,
            value,
            this.OnSideChange);
    }

    /// <summary>Gets or sets the icon to use when not checked.</summary>
    public TristateIconPattern? NormalIcon
    {
        get => this.normalIcon;
        set => this.HandlePropertyChange(
            nameof(this.NormalIcon),
            ref this.normalIcon,
            value,
            this.OnNormalIconChange);
    }

    /// <summary>Gets or sets the icon to use when checked.</summary>
    public TristateIconPattern? HoveredIcon
    {
        get => this.hoveredIcon;
        set => this.HandlePropertyChange(
            nameof(this.HoveredIcon),
            ref this.hoveredIcon,
            value,
            this.OnHoveredIconChange);
    }

    /// <summary>Gets or sets the icon to use when indeterminate.</summary>
    public TristateIconPattern? ActiveIcon
    {
        get => this.activeIcon;
        set => this.HandlePropertyChange(
            nameof(this.ActiveIcon),
            ref this.activeIcon,
            value,
            this.OnActiveIconChange);
    }

    /// <inheritdoc/> 
    protected override void OnMouseEnter(ControlMouseEventArgs args)
    {
        this.UpdateIcon();
        base.OnMouseEnter(args);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(ControlMouseEventArgs args)
    {
        this.UpdateIcon();
        base.OnMouseLeave(args);
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(ControlMouseEventArgs args)
    {
        this.UpdateIcon();
        base.OnMouseDown(args);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(ControlMouseEventArgs args)
    {
        this.UpdateIcon();
        base.OnMouseUp(args);
    }

    /// <summary>Raises the <see cref="CheckedChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnCheckedChange(PropertyChangeEventArgs<ControlSpannable, bool?> args)
    {
        this.UpdateIcon();
        this.CheckedChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="NormalIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnNormalIconChange(PropertyChangeEventArgs<ControlSpannable, TristateIconPattern?> args)
    {
        this.UpdateIcon();
        this.NormalIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnHoveredIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnHoveredIconChange(PropertyChangeEventArgs<ControlSpannable, TristateIconPattern?> args)
    {
        this.UpdateIcon();
        this.HoveredIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnActiveIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnActiveIconChange(PropertyChangeEventArgs<ControlSpannable, TristateIconPattern?> args)
    {
        this.UpdateIcon();
        this.ActiveIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnSideChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnSideChange(PropertyChangeEventArgs<ControlSpannable, IconSide> args)
    {
        this.UpdateIcon();
        this.SideChange?.Invoke(args);
    }

    /// <inheritdoc/>
    protected override void OnMouseClick(ControlMouseEventArgs args)
    {
        this.Checked = this.Checked switch
        {
            false => true,
            true => false,
            null => true,
        };

        base.OnMouseClick(args);
    }

    /// <summary>Updates the icon.</summary>
    private void UpdateIcon()
    {
        var stateIcon =
            this.IsMouseHovered && this.IsLeftMouseButtonDown
                ? this.activeIcon
                : this.IsMouseHovered
                    ? this.hoveredIcon
                    : this.normalIcon;

        if (this.normalIcon is not null)
            this.normalIcon.State = this.Checked;
        if (this.hoveredIcon is not null)
            this.hoveredIcon.State = this.Checked;
        if (this.activeIcon is not null)
            this.activeIcon.State = this.Checked;

        this.LeftIcon = this.side == IconSide.Left ? stateIcon : null;
        this.TopIcon = this.side == IconSide.Top ? stateIcon : null;
        this.RightIcon = this.side == IconSide.Right ? stateIcon : null;
        this.BottomIcon = this.side == IconSide.Bottom ? stateIcon : null;
    }
}
