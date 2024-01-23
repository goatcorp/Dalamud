using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// A font handle representing a user-callback generated font.
/// </summary>
internal sealed class DelegateFontHandle : FontHandle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateFontHandle"/> class.
    /// </summary>
    /// <param name="manager">An instance of <see cref="IFontHandleManager"/>.</param>
    /// <param name="callOnBuildStepChange">Callback for <see cref="IFontAtlas.BuildStepChange"/>.</param>
    public DelegateFontHandle(IFontHandleManager manager, FontAtlasBuildStepDelegate callOnBuildStepChange)
        : base(manager)
    {
        this.CallOnBuildStepChange = callOnBuildStepChange;
    }

    /// <summary>
    /// Gets the function to be called on build step changes.
    /// </summary>
    public FontAtlasBuildStepDelegate CallOnBuildStepChange { get; }

    /// <summary>
    /// Manager for <see cref="DelegateFontHandle"/>s.
    /// </summary>
    internal sealed class HandleManager : IFontHandleManager
    {
        private readonly HashSet<DelegateFontHandle> handles = new();
        private readonly object syncRoot = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleManager"/> class.
        /// </summary>
        /// <param name="atlasName">The name of the owner atlas.</param>
        public HandleManager(string atlasName) => this.Name = $"{atlasName}:{nameof(DelegateFontHandle)}:Manager";

        /// <inheritdoc/>
        public event Action? RebuildRecommend;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public IFontHandleSubstance? Substance { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (this.syncRoot)
                this.handles.Clear();
        }

        /// <inheritdoc cref="IFontAtlas.NewDelegateFontHandle"/>
        public IFontHandle NewFontHandle(FontAtlasBuildStepDelegate buildStepDelegate)
        {
            var key = new DelegateFontHandle(this, buildStepDelegate);
            lock (this.syncRoot)
                this.handles.Add(key);
            this.RebuildRecommend?.Invoke();
            return key;
        }

        /// <inheritdoc/>
        public void FreeFontHandle(IFontHandle handle)
        {
            if (handle is not DelegateFontHandle cgfh)
                return;

            lock (this.syncRoot)
                this.handles.Remove(cgfh);
        }

        /// <inheritdoc/>
        public IFontHandleSubstance NewSubstance(IRefCountable dataRoot)
        {
            lock (this.syncRoot)
                return new HandleSubstance(this, dataRoot, this.handles.ToArray());
        }
    }

    /// <summary>
    /// Substance from <see cref="HandleManager"/>.
    /// </summary>
    internal sealed class HandleSubstance : IFontHandleSubstance
    {
        private static readonly ModuleLog Log = new($"{nameof(DelegateFontHandle)}.{nameof(HandleSubstance)}");

        // Owned by this class, but ImFontPtr values still do not belong to this.
        private readonly Dictionary<DelegateFontHandle, ImFontPtr> fonts = new();
        private readonly Dictionary<DelegateFontHandle, Exception?> buildExceptions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleSubstance"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="dataRoot">The data root.</param>
        /// <param name="relevantHandles">The relevant handles.</param>
        public HandleSubstance(
            IFontHandleManager manager,
            IRefCountable dataRoot,
            DelegateFontHandle[] relevantHandles)
        {
            // We do not call dataRoot.AddRef; this object is dependant on lifetime of dataRoot.

            this.Manager = manager;
            this.DataRoot = dataRoot;
            this.RelevantHandles = relevantHandles;
        }

        /// <summary>
        /// Gets the relevant handles.
        /// </summary>
        // Not owned by this class. Do not dispose.
        public DelegateFontHandle[] RelevantHandles { get; }

        /// <inheritdoc/>
        ICollection<FontHandle> IFontHandleSubstance.RelevantHandles => this.RelevantHandles;

        /// <inheritdoc/>
        public IRefCountable DataRoot { get; }

        /// <inheritdoc/>
        public IFontHandleManager Manager { get; }

        /// <inheritdoc/>
        [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
        public IFontAtlasBuildToolkitPreBuild? PreBuildToolkitForApi9Compat { get; set; }

        /// <inheritdoc/>
        [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
        public bool CreateFontOnAccess { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.fonts.Clear();
            this.buildExceptions.Clear();
        }

        /// <inheritdoc/>
        public ImFontPtr GetFontPtr(IFontHandle handle) =>
            handle is DelegateFontHandle cgfh ? this.fonts.GetValueOrDefault(cgfh) : default;

        /// <inheritdoc/>
        public Exception? GetBuildException(IFontHandle handle) =>
            handle is DelegateFontHandle cgfh ? this.buildExceptions.GetValueOrDefault(cgfh) : default;

        /// <inheritdoc/>
        public void OnPreBuild(IFontAtlasBuildToolkitPreBuild toolkitPreBuild)
        {
            var fontsVector = toolkitPreBuild.Fonts;
            foreach (var k in this.RelevantHandles)
            {
                var fontCountPrevious = fontsVector.Length;

                try
                {
                    toolkitPreBuild.Font = default;
                    k.CallOnBuildStepChange(toolkitPreBuild);
                    if (toolkitPreBuild.Font.IsNull())
                    {
                        if (fontCountPrevious == fontsVector.Length)
                        {
                            throw new InvalidOperationException(
                                $"{nameof(FontAtlasBuildStepDelegate)} must either set the " +
                                $"{nameof(IFontAtlasBuildToolkitPreBuild.Font)} property, or add at least one font.");
                        }

                        toolkitPreBuild.Font = fontsVector[^1];
                    }
                    else
                    {
                        var found = false;
                        unsafe
                        {
                            for (var i = fontCountPrevious; !found && i < fontsVector.Length; i++)
                            {
                                if (fontsVector[i].NativePtr == toolkitPreBuild.Font.NativePtr)
                                    found = true;
                            }
                        }

                        if (!found)
                        {
                            throw new InvalidOperationException(
                                "The font does not exist in the atlas' font array. If you need an empty font, try" +
                                "adding Noto Sans from Dalamud Assets, but using new ushort[]{ ' ', ' ', 0 } as the" +
                                "glyph range.");
                        }
                    }

                    if (fontsVector.Length - fontCountPrevious != 1)
                    {
                        Log.Warning(
                            "[{name}:Substance] {n} fonts added from {delegate} PreBuild call; " +
                            "Using the most recently added font. " +
                            "Did you mean to use {sfd}.{sfdprop} or {ifcp}.{ifcpprop}?",
                            this.Manager.Name,
                            fontsVector.Length - fontCountPrevious,
                            nameof(FontAtlasBuildStepDelegate),
                            nameof(SafeFontConfig),
                            nameof(SafeFontConfig.MergeFont),
                            nameof(ImFontConfigPtr),
                            nameof(ImFontConfigPtr.MergeMode));
                    }

                    for (var i = fontCountPrevious; i < fontsVector.Length; i++)
                    {
                        if (fontsVector[i].ValidateUnsafe() is { } ex)
                        {
                            throw new InvalidOperationException(
                                "One of the newly added fonts seem to be pointing to an invalid memory address.",
                                ex);
                        }
                    }

                    // Check for duplicate entries; duplicates will result in free-after-free
                    for (var i = 0; i < fontCountPrevious; i++)
                    {
                        for (var j = fontCountPrevious; j < fontsVector.Length; j++)
                        {
                            unsafe
                            {
                                if (fontsVector[i].NativePtr == fontsVector[j].NativePtr)
                                    throw new InvalidOperationException("An already added font has been added again.");
                            }
                        }
                    }

                    this.fonts[k] = toolkitPreBuild.Font;
                }
                catch (Exception e)
                {
                    this.fonts[k] = default;
                    this.buildExceptions[k] = e;

                    Log.Error(
                        e,
                        "[{name}:Substance] An error has occurred while during {delegate} PreBuild call.",
                        this.Manager.Name,
                        nameof(FontAtlasBuildStepDelegate));

                    // Sanitization, in a futile attempt to prevent crashes on invalid parameters
                    unsafe
                    {
                        var distinct =
                            fontsVector
                                .DistinctBy(x => (nint)x.NativePtr) // Remove duplicates
                                .Where(x => x.ValidateUnsafe() is null) // Remove invalid entries without freeing them
                                .ToArray();

                        // We're adding the contents back; do not destroy the contents
                        fontsVector.Clear(true);
                        fontsVector.AddRange(distinct.AsSpan());
                    }
                }
            }
        }

        /// <inheritdoc/>        
        public void OnPreBuildCleanup(IFontAtlasBuildToolkitPreBuild toolkitPreBuild)
        {
            // irrelevant
        }

        /// <inheritdoc/>
        public void OnPostBuild(IFontAtlasBuildToolkitPostBuild toolkitPostBuild)
        {
            foreach (var k in this.RelevantHandles)
            {
                if (!this.fonts[k].IsNotNullAndLoaded())
                    continue;

                try
                {
                    toolkitPostBuild.Font = this.fonts[k];
                    k.CallOnBuildStepChange.Invoke(toolkitPostBuild);
                }
                catch (Exception e)
                {
                    this.fonts[k] = default;
                    this.buildExceptions[k] = e;

                    Log.Error(
                        e,
                        "[{name}] An error has occurred while during {delegate} PostBuild call.",
                        this.Manager.Name,
                        nameof(FontAtlasBuildStepDelegate));
                }
            }
        }
    }
}
