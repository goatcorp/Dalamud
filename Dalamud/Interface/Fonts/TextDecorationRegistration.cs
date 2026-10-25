using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>A text decoration a plugin registered.</summary>
/// <seealso cref="IUiBuilder.RegisterTextDecoration"/>
public sealed class TextDecorationRegistration : IDisposable
{
    private readonly Action<TextDecorationRegistration>? onDispose;

    /// <summary>Initializes a new instance of the <see cref="TextDecorationRegistration"/> class.</summary>
    /// <param name="id">The ID.</param>
    /// <param name="onDispose">Called once when it is disposed, after the decoration is unregistered.</param>
    internal TextDecorationRegistration(TextDecorationId id, Action<TextDecorationRegistration>? onDispose = null)
    {
        this.Id = id;
        this.onDispose = onDispose;
    }

    /// <summary>Gets the ID, for <see cref="TextDecoration"/>.</summary>
    public TextDecorationId Id { get; }

    /// <summary>Gets a value indicating whether it has been unregistered.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Gets the default <see cref="TextDecoration.Context"/> value when <see cref="ToDecoration"/> is
    /// used without specifying the <c>context</c> parameter or <see cref="op_Implicit"/> is used.</summary>
    public uint DefaultContext { get; init; } = 0;

    /// <summary>Gets the default <see cref="TextDecoration.Data"/> value when <see cref="ToDecoration"/> is
    /// used without specifying the <c>data</c> parameter or <see cref="op_Implicit"/> is used.</summary>
    public ulong DefaultData { get; init; } = 0;

    public static implicit operator TextDecoration(in TextDecorationRegistration d) => d.ToDecoration();

    /// <summary>Makes a decoration of this kind.</summary>
    /// <param name="context">The context to draw it with, or null for <see cref="DefaultContext"/>.</param>
    /// <param name="data">The data to draw it with, or null for <see cref="DefaultData"/>.</param>
    /// <returns>The decoration.</returns>
    public TextDecoration ToDecoration(uint? context = null, ulong? data = null) => new(
        this.Id,
        context ?? this.DefaultContext,
        data ?? this.DefaultData);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;
        this.IsDisposed = true;
        TextDecorationRegistry.Unregister(this.Id);
        this.onDispose?.Invoke(this);
    }
}
