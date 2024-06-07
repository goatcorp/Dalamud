using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// A player slot in a Party Finder listing.
/// </summary>
public class PartyFinderSlot
{
    private readonly uint accepting;
    private JobFlags[] listAccepting;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderSlot"/> class.
    /// </summary>
    /// <param name="accepting">The flag value of accepted jobs.</param>
    internal PartyFinderSlot(uint accepting)
    {
        this.accepting = accepting;
    }

    /// <summary>
    /// Gets a list of jobs that this slot is accepting.
    /// </summary>
    public IReadOnlyCollection<JobFlags> Accepting
    {
        get
        {
            if (this.listAccepting != null)
            {
                return this.listAccepting;
            }

            this.listAccepting = Enum.GetValues(typeof(JobFlags))
                                     .Cast<JobFlags>()
                                     .Where(flag => this[flag])
                                     .ToArray();

            return this.listAccepting;
        }
    }

    /// <summary>
    /// Tests if this slot is accepting a job.
    /// </summary>
    /// <param name="flag">Job to test.</param>
    public bool this[JobFlags flag] => (this.accepting & (uint)flag) > 0;
}
