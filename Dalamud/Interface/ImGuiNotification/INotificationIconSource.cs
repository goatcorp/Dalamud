using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiNotification.Internal.IconSource;
using Dalamud.Interface.Internal;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Icon source for <see cref="INotification"/>.</summary>
/// <remarks>Plugins should NOT implement this interface.</remarks>
public interface INotificationIconSource : ICloneable, IDisposable
{
    /// <summary>The internal interface.</summary>
    internal interface IInternal : INotificationIconSource
    {
        /// <summary>Materializes the icon resource.</summary>
        /// <returns>The materialized resource.</returns>
        INotificationMaterializedIcon Materialize();
    }

    /// <summary>Gets a new instance of <see cref="INotificationIconSource"/> that will source the icon from an
    /// <see cref="SeIconChar"/>.</summary>
    /// <param name="iconChar">The icon character.</param>
    /// <returns>A new instance of <see cref="INotificationIconSource"/> that should be disposed after use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource From(SeIconChar iconChar) => new SeIconCharIconSource(iconChar);

    /// <summary>Gets a new instance of <see cref="INotificationIconSource"/> that will source the icon from an
    /// <see cref="FontAwesomeIcon"/>.</summary>
    /// <param name="iconChar">The icon character.</param>
    /// <returns>A new instance of <see cref="INotificationIconSource"/> that should be disposed after use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource From(FontAwesomeIcon iconChar) => new FontAwesomeIconIconSource(iconChar);

    /// <summary>Gets a new instance of <see cref="INotificationIconSource"/> that will source the icon from an
    /// <see cref="IDalamudTextureWrap"/>.</summary>
    /// <param name="wrap">The texture wrap.</param>
    /// <param name="takeOwnership">
    /// If <c>true</c>, this class will own the passed <paramref name="wrap"/>, and you <b>must not</b> call
    /// <see cref="IDisposable.Dispose"/> on the passed wrap.
    /// If <c>false</c>, this class will create a new reference of the passed wrap, and you <b>should</b> call
    /// <see cref="IDisposable.Dispose"/> on the passed wrap.
    /// In both cases, the returned object must be disposed after use.</param>
    /// <returns>A new instance of <see cref="INotificationIconSource"/> that should be disposed after use.</returns>
    /// <remarks>If any errors are thrown or <paramref name="wrap"/> is <c>null</c>, the default icon will be displayed
    /// instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource From(IDalamudTextureWrap? wrap, bool takeOwnership = true) =>
        new TextureWrapIconSource(wrap, takeOwnership);

    /// <summary>Gets a new instance of <see cref="INotificationIconSource"/> that will source the icon from an
    /// <see cref="Func{TResult}"/> returning a <see cref="Task{TResult}"/> resulting in an
    /// <see cref="IDalamudTextureWrap"/>.</summary>
    /// <param name="wrapTaskFunc">The function that returns a task that results a texture wrap.</param>
    /// <returns>A new instance of <see cref="INotificationIconSource"/> that should be disposed after use.</returns>
    /// <remarks>If any errors are thrown or <paramref name="wrapTaskFunc"/> is <c>null</c>, the default icon will be
    /// displayed instead.<br />
    /// Use <see cref="Task.FromResult{TResult}"/> if you will have a wrap available without waiting.<br />
    /// <paramref name="wrapTaskFunc"/> should not contain a reference to a resource; if it does, the resource will be
    /// released when all instances of <see cref="INotificationIconSource"/> derived from the returned object are freed
    /// by the garbage collector, which will result in non-deterministic resource releases.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource From(Func<Task<IDalamudTextureWrap?>?>? wrapTaskFunc) =>
        new TextureWrapTaskIconSource(wrapTaskFunc);

    /// <summary>Gets a new instance of <see cref="INotificationIconSource"/> that will source the icon from a texture
    /// file shipped as a part of the game resources.</summary>
    /// <param name="gamePath">The path to a texture file in the game virtual file system.</param>
    /// <returns>A new instance of <see cref="INotificationIconSource"/> that should be disposed after use.</returns>
    /// <remarks>If any errors are thrown, the default icon will be displayed instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource FromGame(string gamePath) => new GamePathIconSource(gamePath);

    /// <summary>Gets a new instance of <see cref="INotificationIconSource"/> that will source the icon from an image
    /// file from the file system.</summary>
    /// <param name="filePath">The path to an image file in the file system.</param>
    /// <returns>A new instance of <see cref="INotificationIconSource"/> that should be disposed after use.</returns>
    /// <remarks>If any errors are thrown, the default icon will be displayed instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIconSource FromFile(string filePath) => new FilePathIconSource(filePath);

    /// <inheritdoc cref="ICloneable.Clone"/>
    new INotificationIconSource Clone();

    /// <inheritdoc/>
    object ICloneable.Clone() => this.Clone();
}
