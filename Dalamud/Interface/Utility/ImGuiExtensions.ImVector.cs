using System.Collections.Generic;

using ImGuiNET;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Class containing various extensions to ImGui, aiding with building custom widgets.
/// </summary>
// TODO: This should go into ImDrawList.Manual.cs in ImGui.NET...
public static partial class ImGuiExtensions
{
    /// <summary>
    /// Convert given <see cref="ImVector"/> into a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="vec">The vector.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>Span view of the vector.</returns>
    public static unsafe Span<T> AsSpan<T>(in this ImVector vec) where T : unmanaged => new((void*)vec.Data, vec.Size);

    /// <summary>
    /// Convert given <see cref="ImVector{T}"/> into a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="vec">The vector.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>Span view of the vector.</returns>
    public static unsafe Span<T> AsSpan<T>(in this ImVector<T> vec) where T : unmanaged =>
        new((void*)vec.Data, vec.Size);

    /// <summary>
    /// Interpret given <see cref="ImPtrVector{T}"/> as a <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="vec">The vector.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>Enumerable of the vector.</returns>
    public static IEnumerable<T> AsEnumerable<T>(this ImPtrVector<T> vec) where T : unmanaged
    {
        for (var i = 0; i < vec.Size; i++)
            yield return vec[i];
    }

    /// <summary>
    /// Interprets the given reference of an <see cref="ImVector"/> as a full-fleged <see cref="List{T}"/>-like object.
    /// </summary>
    /// <param name="vec">The underlying vector.</param>
    /// <param name="destroyer">Destroy function; required if planning to remove items from the list, and <typeparamref name="T"/> has a destructor.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns><see cref="ImVectorWrapper{T}"/> view of the vector.</returns>
    /// <remarks><paramref name="vec"/> is expected to be already fixed; being allocated from ImGui counts.</remarks>
    public static unsafe ImVectorWrapper<T> Wrap<T>(
        in this ImVector vec,
        ImVectorWrapper<T>.ImGuiNativeDestroyDelegate? destroyer = null) where T : unmanaged
    {
        fixed (ImVector* pVec = &vec)
            return new(pVec, destroyer);
    }

    /// <summary>
    /// Interprets the given reference of an <see cref="ImVector{T}"/> as a full-fleged <see cref="List{T}"/>-like object.
    /// </summary>
    /// <param name="vec">The underlying vector.</param>
    /// <param name="destroyer">Destroy function; required if planning to remove items from the list, and <typeparamref name="T"/> has a destructor.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns><see cref="ImVectorWrapper{T}"/> view of the vector.</returns>
    /// <remarks><paramref name="vec"/> is expected to be already fixed; being allocated from ImGui counts.</remarks>
    public static unsafe ImVectorWrapper<T> Wrap<T>(
        in this ImVector<T> vec,
        ImVectorWrapper<T>.ImGuiNativeDestroyDelegate? destroyer = null) where T : unmanaged
    {
        fixed (ImVector* pVec = &vec)
            return new(pVec, destroyer);
    }
}
