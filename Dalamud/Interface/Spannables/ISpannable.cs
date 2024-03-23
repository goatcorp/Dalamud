using System.Collections.Generic;

using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannable : IDisposable
{
    /// <summary>Gets the generation of the state.</summary>
    /// <remarks>Increase this by 1 every time anything about the spannable changes.</remarks>
    int StateGeneration { get; }

    /// <summary>Gets all the child spannables.</summary>
    /// <returns>A collection of every <see cref="ISpannable"/> children. May contain nulls.</returns>
    IReadOnlyCollection<ISpannable?> GetAllChildSpannables();

    /// <summary>Rents a render pass.</summary>
    /// <returns>The rented render pass.</returns>
    /// <param name="renderer">The arguments.</param>
    ISpannableRenderPass RentRenderPass(ISpannableRenderer renderer);

    /// <summary>Returns a render pass.</summary>
    /// <param name="pass">The render pass to return.</param>
    /// <remarks>If <paramref name="pass"/> is null, the call is a no-op.</remarks>
    void ReturnRenderPass(ISpannableRenderPass? pass);
}
