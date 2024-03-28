using System.Collections.Generic;

using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Spannables;

/// <summary>A spannable that draws into a rectangular region.</summary>
public interface ISpannable : IDisposable
{
    /// <summary>Occurs when anything about the spannable changes.</summary>
    /// <remarks>Used to determine when to measure again.</remarks>
    event Action<ISpannable>? SpannableChange;

    // ^ TOOD: implement on TextSpannableBuilder

    /// <summary>Gets all the child spannables.</summary>
    /// <returns>A collection of every <see cref="ISpannable"/> children. May contain nulls.</returns>
    IReadOnlyCollection<ISpannable?> GetAllChildSpannables();

    /// <summary>Rents an instance of <see cref="ISpannableMeasurement"/> for this instance of <see cref="ISpannable"/>.
    /// </summary>
    /// <param name="renderer">The renderer for providing auxiliary data.</param>
    /// <returns>A rented instance of <see cref="ISpannableMeasurement"/>. Return using <see cref="ReturnMeasurement"/>.
    /// </returns>
    ISpannableMeasurement RentMeasurement(ISpannableRenderer renderer);

    /// <summary>Returns a measurement rented from <see cref="RentMeasurement"/>.</summary>
    /// <param name="measurement">The instance of <see cref="ISpannableMeasurement"/> to return.</param>
    /// <remarks>Returning a <c>null</c> is a no-op.</remarks>
    void ReturnMeasurement(ISpannableMeasurement? measurement);
}
