using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables;

/// <summary>Minimal set of options for measuring a spannable.</summary>
public class SpannableOptions : IResettable, ICloneable
{
    private readonly PropertyChangeEventArgs propertyChangeEventArgs = new();

    private Vector2 preferredSize = new(float.PositiveInfinity);
    private Vector2 visibleSize = new(float.PositiveInfinity);
    private float renderScale = 1f;

    /// <summary>Occurs when a property has changed.</summary>
    public event PropertyChangeEventHandler? PropertyChanged;

    /// <summary>Gets or sets the preferred size from the parent.</summary>
    /// <value><see cref="float.PositiveInfinity"/> for a dimension means that the boundary will resized to wrap the
    /// content.</value>
    /// <remarks>This is not a hard limiting value.</remarks>
    public Vector2 PreferredSize
    {
        get => this.preferredSize;
        set => this.UpdateProperty(
            nameof(this.PreferredSize),
            ref this.preferredSize,
            value,
            this.preferredSize == value);
    }

    /// <summary>Gets or sets the visible size.</summary>
    /// <value><see cref="float.PositiveInfinity"/> if there are no limits.</value>
    public Vector2 VisibleSize
    {
        get => this.visibleSize;
        set => this.UpdateProperty(nameof(this.VisibleSize), ref this.visibleSize, value, this.visibleSize == value);
    }

    /// <summary>Gets or sets the render scale.</summary>
    /// <remarks>Used only for loading underlying resources that will accommodate drawing without being blurry.
    /// Setting this property alone does not mean scaling the result.</remarks>
    public float RenderScale
    {
        get => this.renderScale;
        set => this.UpdateProperty(
            nameof(this.RenderScale),
            ref this.renderScale,
            value,
            this.renderScale - value == 0f);
    }

    /// <summary>Copies applicable options from another spannable options.</summary>
    /// <param name="source">The source to copy from.</param>
    public virtual void CopyFrom(SpannableOptions source)
    {
        this.PreferredSize = source.PreferredSize;
        this.VisibleSize = source.VisibleSize;
        this.RenderScale = source.RenderScale;
    }

    /// <inheritdoc/>
    public object Clone()
    {
        var x = (SpannableOptions)Activator.CreateInstance(this.GetType());
        x!.CopyFrom(this);
        return x;
    }

    /// <inheritdoc/>
    public virtual bool TryReset()
    {
        this.PreferredSize = new(float.PositiveInfinity);
        this.VisibleSize = new(float.PositiveInfinity);
        this.renderScale = 1f;
        return true;
    }

    /// <summary>Updates property, and invoke <see cref="PropertyChanged"/>.</summary>
    /// <param name="propName">Name of the changed property.</param>
    /// <param name="storage">Reference to the backing storage of the property.</param>
    /// <param name="newValue">New value.</param>
    /// <param name="eq">Whether the values are equal.</param>
    /// <typeparam name="T">Type of the property.</typeparam>
    protected void UpdateProperty<T>(string propName, ref T storage, in T newValue, bool eq)
    {
        if (eq)
            return;

        this.propertyChangeEventArgs.Initialize(this, SpannableEventStep.DirectTarget);
        this.propertyChangeEventArgs.InitializePropertyChangeEvent(propName, PropertyChangeState.Before);
        this.OnPropertyChanged(this.propertyChangeEventArgs);

        if (this.propertyChangeEventArgs.State == PropertyChangeState.Cancelled)
        {
            this.propertyChangeEventArgs.Initialize(this, SpannableEventStep.DirectTarget);
            this.propertyChangeEventArgs.InitializePropertyChangeEvent(propName, PropertyChangeState.Cancelled);
            this.OnPropertyChanged(this.propertyChangeEventArgs);
        }

        storage = newValue;

        this.propertyChangeEventArgs.Initialize(this, SpannableEventStep.DirectTarget);
        this.propertyChangeEventArgs.InitializePropertyChangeEvent(propName, PropertyChangeState.After);
        this.OnPropertyChanged(this.propertyChangeEventArgs);
    }

    /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs"/> that contains the event data.</param>
    protected virtual void OnPropertyChanged(PropertyChangeEventArgs args) => this.PropertyChanged?.Invoke(args);
}
