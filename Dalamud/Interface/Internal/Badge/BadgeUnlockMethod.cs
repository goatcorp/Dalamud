namespace Dalamud.Interface.Internal.Badge;

/// <summary>
/// Method by which a badge can be unlocked.
/// </summary>
internal enum BadgeUnlockMethod
{
    /// <summary>
    /// Badge can be unlocked by the user by entering a password.
    /// </summary>
    User,

    /// <summary>
    /// Badge can be unlocked from Dalamud internal features.
    /// </summary>
    Internal,

    /// <summary>
    /// Badge is no longer obtainable and can only be unlocked from the configuration file.
    /// </summary>
    Startup,
}
