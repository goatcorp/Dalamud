using System.Numerics;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables;

/// <summary>Options for measuring a spannable.</summary>
public interface ISpannableMeasurementOptions : IResettable
{
    /// <summary>Gets or sets the size.</summary>
    /// <value><see cref="float.PositiveInfinity"/> for a dimension means that the boundary will resized to wrap the
    /// content.</value>
    Vector2 Size { get; set; }
    
    /// <summary>Gets or sets the visible size.</summary>
    /// <value><see cref="float.PositiveInfinity"/> if there are no limits.</value>
    Vector2 VisibleSize { get; set; }

    /// <summary>Copies applicable options from another spannable options.</summary>
    /// <param name="source">The source to copy from.</param>
    void CopyFrom(ISpannableMeasurementOptions source);

    /// <summary>Default implementation for <see cref="CopyFrom"/>.</summary>
    /// <param name="source">The source options.</param>
    /// <param name="target">The target options.</param>
    public static void DefaultCopyFrom(ISpannableMeasurementOptions source, ISpannableMeasurementOptions target)
    {
        target.Size = source.Size;
        target.VisibleSize = source.VisibleSize;
    }
}

/// <summary>Minimal set of options for measuring a spannable.</summary>
public class SpannableMeasurementOptions : ISpannableMeasurementOptions
{
    private Vector2 size = new(float.PositiveInfinity);
    private Vector2 visibleSize = new(float.PositiveInfinity);

    /// <summary>Occurs when a property has changed.</summary>
    public event Action<string>? PropertyChanged;

    /// <inheritdoc/>
    public Vector2 Size
    {
        get => this.size;
        set => this.UpdateProperty(nameof(this.Size), ref this.size, value);
    }

    /// <inheritdoc/>
    public Vector2 VisibleSize
    {
        get => this.visibleSize;
        set => this.UpdateProperty(nameof(this.VisibleSize), ref this.visibleSize, value);
    }

    /// <inheritdoc/>
    public virtual void CopyFrom(ISpannableMeasurementOptions source) =>
        ISpannableMeasurementOptions.DefaultCopyFrom(source, this);

    /// <inheritdoc/>
    public virtual bool TryReset()
    {
        this.Size = new(float.PositiveInfinity);
        this.VisibleSize = new(float.PositiveInfinity);
        return true;
    }

    /// <summary>Updates property, and invoke <see cref="PropertyChanged"/>.</summary>
    /// <param name="propName">Name of the changed property.</param>
    /// <param name="storage">Reference to the backing storage of the property.</param>
    /// <param name="newValue">New value.</param>
    /// <typeparam name="T">Type of the property.</typeparam>
    protected void UpdateProperty<T>(string propName, ref T storage, in T newValue)
    {
        if (Equals(storage, newValue))
            return;
        storage = newValue;
        this.PropertyChanged?.Invoke(propName);
    }
}
