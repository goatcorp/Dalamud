using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Patterns;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tri-state control for checkboxes and radio buttons.</summary>
public class BooleanControl : LabelControl
{
    private bool @checked;
    private bool indeterminate;
    private TristateIconPattern? normalIcon;
    private TristateIconPattern? hoveredIcon;
    private TristateIconPattern? activeIcon;
    private IconSide side;

    /// <summary>Initializes a new instance of the <see cref="BooleanControl"/> class.</summary>
    public BooleanControl()
    {
    }

    /// <summary>Occurs when the property <see cref="Checked"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CheckedChange;

    /// <summary>Occurs when the property <see cref="Indeterminate"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? IndeterminateChange;

    /// <summary>Occurs when the property <see cref="Side"/> is changing.</summary>
    public event PropertyChangeEventHandler<IconSide>? SideChange;

    /// <summary>Occurs when the property <see cref="NormalIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<TristateIconPattern?>? NormalIconChange;

    /// <summary>Occurs when the property <see cref="HoveredIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<TristateIconPattern?>? HoveredIconChange;

    /// <summary>Occurs when the property <see cref="ActiveIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<TristateIconPattern?>? ActiveIconChange;

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

    /// <summary>Gets or sets a value indicating whether this checkbox is checked.</summary>
    public bool Checked
    {
        get => this.@checked;
        set => this.HandlePropertyChange(nameof(this.Checked), ref this.@checked, value, this.OnCheckedChange);
    }

    /// <summary>Gets or sets a value indicating whether this checkbox is indeterminate.</summary>
    /// <remarks>If <c>true</c>, then automatic <see cref="Checked"/> toggling will be disabled.</remarks>
    public bool Indeterminate
    {
        get => this.indeterminate;
        set => this.HandlePropertyChange(
            nameof(this.Indeterminate),
            ref this.indeterminate,
            value,
            this.OnIndeterminateChange);
    }

    /// <summary>Gets or sets the side to display the icon.</summary>
    public IconSide Side
    {
        get => this.side;
        set => this.HandlePropertyChange(nameof(this.Side), ref this.side, value, this.OnSideChange);
    }

    /// <summary>Gets or sets the icon to use when not checked.</summary>
    public TristateIconPattern? NormalIcon
    {
        get => this.normalIcon;
        set => this.HandlePropertyChange(nameof(this.NormalIcon), ref this.normalIcon, value, this.OnNormalIconChange);
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
        set => this.HandlePropertyChange(nameof(this.ActiveIcon), ref this.activeIcon, value, this.OnActiveIconChange);
    }

    /// <inheritdoc/> 
    protected override void OnMouseEnter(SpannableMouseEventArgs args)
    {
        base.OnMouseEnter(args);
        this.UpdateIcon();
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(SpannableMouseEventArgs args)
    {
        base.OnMouseLeave(args);
        this.UpdateIcon();
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(SpannableMouseEventArgs args)
    {
        base.OnMouseDown(args);
        this.UpdateIcon();
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(SpannableMouseEventArgs args)
    {
        base.OnMouseUp(args);
        this.UpdateIcon();
    }

    /// <summary>Raises the <see cref="CheckedChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCheckedChange(PropertyChangeEventArgs<bool> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.CheckedChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="CheckedChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIndeterminateChange(PropertyChangeEventArgs<bool> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.IndeterminateChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="NormalIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNormalIconChange(PropertyChangeEventArgs<TristateIconPattern?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.NormalIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnHoveredIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHoveredIconChange(PropertyChangeEventArgs<TristateIconPattern?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.HoveredIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnActiveIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnActiveIconChange(PropertyChangeEventArgs<TristateIconPattern?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.ActiveIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnSideChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSideChange(PropertyChangeEventArgs<IconSide> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.SideChange?.Invoke(args);
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

        bool? state = this.indeterminate ? null : this.@checked;
        if (this.normalIcon is not null)
            this.normalIcon.State = state;
        if (this.hoveredIcon is not null)
            this.hoveredIcon.State = state;
        if (this.activeIcon is not null)
            this.activeIcon.State = state;

        this.LeftIcon = this.side == IconSide.Left ? stateIcon : null;
        this.TopIcon = this.side == IconSide.Top ? stateIcon : null;
        this.RightIcon = this.side == IconSide.Right ? stateIcon : null;
        this.BottomIcon = this.side == IconSide.Bottom ? stateIcon : null;
    }
}
