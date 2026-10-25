using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using RegistrationKey = Dalamud.Interface.Fonts.FontRegistration.RegistrationKey;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Reference-counted registry of active <see cref="FontRegistration"/> instances and per-plugin ownership.</summary>
internal static class FontRegistry
{
    private static readonly Lock Gate = new();

    private static readonly Dictionary<RegistrationKey, FontRegistration> Registered = [];

    private static readonly IFontFamilyId[] BuiltInFamilies =
        [.. IFontFamilyId.ListDalamudFonts(), .. IFontFamilyId.ListGameFonts()];

    /// <summary>Gets a wrapped font that stays registered.</summary>
    /// <param name="adjustments">The adjustments.</param>
    /// <param name="chain">The families, not empty and without registrations of plugins.</param>
    /// <returns>The registration.</returns>
    public static FontRegistration Wrap(FontAdjustments adjustments, params IFontFamilyId[] chain)
    {
        lock (Gate)
            return Acquire(new(default, adjustments, new([.. chain])), []);
    }

    /// <summary>Tests whether an equal registration is currently active.</summary>
    /// <param name="registration">The registration.</param>
    /// <returns><see langword="true"/> if active, or <see langword="false"/> otherwise.</returns>
    public static bool IsRegistered(FontRegistration registration)
    {
        lock (Gate)
            return Registered.ContainsKey(registration.Key);
    }

    /// <summary>Finds a family by its English name, case-insensitive.</summary>
    /// <param name="name">The name.</param>
    /// <param name="owner">The registrations of the plugin to look in first, or null.</param>
    /// <returns>The newest registration of the plugin, else a Dalamud or game font, else an installed family, else
    /// null.</returns>
    public static IFontFamilyId? FindFamily(string name, PluginFonts? owner)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (owner?.FindRegistered(name) is { } registered)
            return registered;

        foreach (var family in BuiltInFamilies)
        {
            if (string.Equals(family.Name.English, name, StringComparison.OrdinalIgnoreCase))
                return family;
        }

        return DirectWriteUtilities.TryGetFamilyName(name, out var localized) ? new SystemFontFamilyId(localized) : null;
    }

    // Acquires a reference to a registration, creating it and acquiring its chain if needed. Caller must hold Gate.
    private static FontRegistration Acquire(RegistrationKey key, List<FontRegistration> made)
    {
        if (Registered.TryGetValue(key, out var r))
        {
            r.References++;
            return r;
        }

        foreach (var inner in RegistrationsIn(key))
            Acquire(inner.Key, made);
        r = new(key) { References = 1 };
        Registered.Add(key, r);
        made.Add(r);
        return r;
    }

    // Releases a reference, removing the registration and releasing its chain when the count hits 0. Caller must hold Gate.
    private static void Release(RegistrationKey key, List<FontRegistration> removed)
    {
        if (!Registered.TryGetValue(key, out var r) || --r.References > 0)
            return;
        Registered.Remove(key);
        removed.Add(r);
        foreach (var inner in RegistrationsIn(key))
            Release(inner.Key, removed);
    }

    private static IEnumerable<FontRegistration> RegistrationsIn(RegistrationKey key) =>
        key.Chain.Array.SelectMany(static e => e.Registrations);

    private static void Notify(List<FontRegistration> changed, bool registered)
    {
        if (changed.Count != 0)
            Service<FontResolver>.GetNullable()?.OnRegistrationsChanged(changed, registered);
    }

    /// <summary>The font registrations of one plugin.</summary>
    public sealed class PluginFonts : IDisposable
    {
        // One item per reference, oldest first. Null once disposed.
        private List<FontRegistration>? held = [];

        /// <summary>Takes a reference to the registration of a source.</summary>
        /// <param name="source">The source.</param>
        /// <returns>The registration.</returns>
        /// <exception cref="ArgumentException">If the source is default.</exception>
        /// <exception cref="ObjectDisposedException">If the plugin has unloaded.</exception>
        public FontRegistration Register(FontSource source)
        {
            if (source.IsDefault)
                throw new ArgumentException("The source is default.", nameof(source));
            return this.Register(new RegistrationKey(source, default, default));
        }

        /// <summary>Takes a reference to a wrapped font.</summary>
        /// <param name="adjustments">The adjustments.</param>
        /// <param name="chain">The families.</param>
        /// <returns>The registration.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="chain"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="chain"/> is empty or has a null family.</exception>
        /// <exception cref="ObjectDisposedException">If the plugin has unloaded.</exception>
        public FontRegistration RegisterWrapped(FontAdjustments adjustments, IFontFamilyId[] chain)
        {
            ArgumentNullException.ThrowIfNull(chain);
            if (chain.Length == 0)
                throw new ArgumentException("The chain is empty.", nameof(chain));
            if (chain.Contains(null!))
                throw new ArgumentException("A family in the chain is null.", nameof(chain));
            return this.Register(new RegistrationKey(default, adjustments, new([.. chain])));
        }

        /// <summary>Releases one reference held by this plugin.</summary>
        /// <param name="registration">The registration to release.</param>
        /// <returns><see langword="true"/> if this plugin held a matching reference, or <see langword="false"/> otherwise.</returns>
        public bool Unregister(FontRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            List<FontRegistration> removed = [];
            lock (Gate)
            {
                var index = this.held?.FindLastIndex(r => r.Equals(registration)) ?? -1;
                if (index < 0)
                    return false;
                this.held!.RemoveAt(index);
                Release(registration.Key, removed);
            }

            Notify(removed, false);
            return true;
        }

        /// <summary>Finds the newest font source registration of this plugin with a family name.</summary>
        /// <param name="name">The English name, case-insensitive.</param>
        /// <returns>The registration, or null.</returns>
        /// <remarks>Waits for the names of the registrations to be read.</remarks>
        public FontRegistration? FindRegistered(string name)
        {
            FontRegistration[] newestFirst;
            lock (Gate)
                newestFirst = [.. this.held?.AsEnumerable().Reverse().Where(static r => !r.IsWrapped).Distinct() ?? []];
            foreach (var r in newestFirst)
            {
                r.LoadTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
                if (string.Equals(r.Name.English, name, StringComparison.OrdinalIgnoreCase))
                    return r;
            }

            return null;
        }

        /// <summary>Releases all references held by this plugin.</summary>
        public void Dispose()
        {
            List<FontRegistration> removed = [];
            lock (Gate)
            {
                if (this.held is null)
                    return;
                for (var i = this.held.Count - 1; i >= 0; i--)
                    Release(this.held[i].Key, removed);
                this.held = null;
            }

            Notify(removed, false);
        }

        private FontRegistration Register(RegistrationKey key)
        {
            List<FontRegistration> made = [];
            FontRegistration r;
            lock (Gate)
            {
                ObjectDisposedException.ThrowIf(this.held is null, this);
                this.held.Add(r = Acquire(key, made));
            }

            Notify(made, true);
            return r;
        }
    }
}
