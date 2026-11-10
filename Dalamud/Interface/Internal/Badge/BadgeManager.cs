using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;

namespace Dalamud.Interface.Internal.Badge;

/// <summary>
/// Service responsible for managing user badges.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class BadgeManager : IServiceType
{
    private readonly DalamudConfiguration configuration;

    private readonly List<BadgeInfo> badges =
    [
        new(() => "Test Badge",
            () => "Awarded for testing badges.",
            0,
            "937e8d5fbb48bd4949536cd65b8d35c426b80d2f830c5c308e2cdec422ae2244",
            BadgeUnlockMethod.User),

        new(() => "Fundraiser #1 Donor",
            () => "Awarded for participating in the first patch fundraiser.",
            1,
            "56e752257bd0cbb2944f95cc7b3cb3d0db15091dd043f7a195ed37028d079322",
            BadgeUnlockMethod.User)
    ];

    private readonly List<int> unlockedBadgeIndices = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BadgeManager"/> class.
    /// </summary>
    /// <param name="configuration">Configuration to use.</param>
    [ServiceManager.ServiceConstructor]
    public BadgeManager(DalamudConfiguration configuration)
    {
        this.configuration = configuration;

        foreach (var usedBadge in this.configuration.UsedBadgePasswords)
        {
            this.TryUnlockBadge(usedBadge, BadgeUnlockMethod.Startup, out _);
        }
    }

    /// <summary>
    /// Gets the badges the user has unlocked.
    /// </summary>
    public IEnumerable<BadgeInfo> UnlockedBadges
        => this.badges.Where((_, index) => this.unlockedBadgeIndices.Contains(index));

    /// <summary>
    /// Unlock a badge with the given password and method.
    /// </summary>
    /// <param name="password">The password to unlock the badge with.</param>
    /// <param name="method">How we are unlocking this badge.</param>
    /// <param name="unlockedBadge">The badge that was unlocked, if the function returns true, null otherwise.</param>
    /// <returns>The unlocked badge, if one was unlocked by this call.</returns>
    public bool TryUnlockBadge(string password, BadgeUnlockMethod method, out BadgeInfo unlockedBadge)
    {
        var sha256 = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
        var hashString = Convert.ToHexString(sha256);

        foreach (var (idx, badge) in this.badges.Where(x => x.UnlockMethod == method || method == BadgeUnlockMethod.Startup).Index())
        {
            if (!this.unlockedBadgeIndices.Contains(idx) && badge.UnlockSha256.Equals(hashString, StringComparison.OrdinalIgnoreCase))
            {
                if (method != BadgeUnlockMethod.Startup)
                {
                    this.configuration.UsedBadgePasswords.Add(password);
                    this.configuration.QueueSave();
                }

                this.unlockedBadgeIndices.Add(idx);
                unlockedBadge = badge;
                return true;
            }
        }

        unlockedBadge = null!;
        return false;
    }
}
