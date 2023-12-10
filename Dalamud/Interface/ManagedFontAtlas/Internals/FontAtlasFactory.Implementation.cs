// #define VeryVerboseLog

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using ImGuiNET;

using JetBrains.Annotations;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Standalone font atlas.
/// </summary>
internal sealed partial class FontAtlasFactory
{
    /// <summary>
    /// Fallback codepoints for ImFont.
    /// </summary>
    public const string FallbackCodepoints = "\u3013\uFFFD?-";

    /// <summary>
    /// Ellipsis codepoints for ImFont.
    /// </summary>
    public const string EllipsisCodepoints = "\u2026\u0085";

    /// <summary>
    /// If set, disables concurrent font build operation.
    /// </summary>
    private static readonly object? NoConcurrentBuildOperationLock = null; // new();

    private static readonly ModuleLog Log = new(nameof(FontAtlasFactory));

    private static readonly Task<FontAtlasBuiltData> EmptyTask = Task.FromResult(default(FontAtlasBuiltData));

    private struct FontAtlasBuiltData : IDisposable
    {
        public readonly DalamudFontAtlas? Owner;
        public readonly ImFontAtlasPtr Atlas;
        public readonly float Scale;

        public bool IsBuildInProgress;

        private readonly List<IDalamudTextureWrap>? wraps;
        private readonly List<IFontHandleSubstance>? substances;
        private readonly DisposeSafety.ScopedFinalizer? garbage;

        public unsafe FontAtlasBuiltData(
            DalamudFontAtlas owner,
            IEnumerable<IFontHandleSubstance> substances,
            float scale)
        {
            this.Owner = owner;
            this.Scale = scale;
            this.garbage = new();

            try
            {
                var substancesList = this.substances = new();
                foreach (var s in substances)
                    substancesList.Add(this.garbage.Add(s));
                this.garbage.Add(() => substancesList.Clear());

                var wrapsCopy = this.wraps = new();
                this.garbage.Add(() => wrapsCopy.Clear());

                var atlasPtr = ImGuiNative.ImFontAtlas_ImFontAtlas();
                this.Atlas = atlasPtr;
                if (this.Atlas.NativePtr is null)
                    throw new OutOfMemoryException($"Failed to allocate a new {nameof(ImFontAtlas)}.");

                this.garbage.Add(() => ImGuiNative.ImFontAtlas_destroy(atlasPtr));
                this.IsBuildInProgress = true;
            }
            catch
            {
                this.garbage.Dispose();
                throw;
            }
        }

        public readonly DisposeSafety.ScopedFinalizer Garbage =>
            this.garbage ?? throw new ObjectDisposedException(nameof(FontAtlasBuiltData));

        public readonly ImVectorWrapper<ImFontPtr> Fonts => this.Atlas.FontsWrapped();

        public readonly ImVectorWrapper<ImFontConfig> ConfigData => this.Atlas.ConfigDataWrapped();

        public readonly ImVectorWrapper<ImFontAtlasTexture> ImTextures => this.Atlas.TexturesWrapped();

        public readonly IReadOnlyList<IDalamudTextureWrap> Wraps =>
            (IReadOnlyList<IDalamudTextureWrap>?)this.wraps ?? Array.Empty<IDalamudTextureWrap>();

        public readonly IReadOnlyList<IFontHandleSubstance> Substances =>
            (IReadOnlyList<IFontHandleSubstance>?)this.substances ?? Array.Empty<IFontHandleSubstance>();

        public readonly void AddExistingTexture(IDalamudTextureWrap wrap)
        {
            if (this.wraps is null)
                throw new ObjectDisposedException(nameof(FontAtlasBuiltData));

            this.wraps.Add(this.Garbage.Add(wrap));
        }

        public readonly int AddNewTexture(IDalamudTextureWrap wrap, bool disposeOnError)
        {
            if (this.wraps is null)
                throw new ObjectDisposedException(nameof(FontAtlasBuiltData));

            var handle = wrap.ImGuiHandle;
            var index = this.ImTextures.IndexOf(x => x.TexID == handle);
            if (index == -1)
            {
                try
                {
                    this.wraps.EnsureCapacity(this.wraps.Count + 1);
                    this.ImTextures.EnsureCapacityExponential(this.ImTextures.Length + 1);

                    index = this.ImTextures.Length;
                    this.wraps.Add(this.Garbage.Add(wrap));
                    this.ImTextures.Add(new() { TexID = handle });
                }
                catch (Exception e)
                {
                    if (disposeOnError)
                        wrap.Dispose();

                    if (this.wraps.Count != this.ImTextures.Length)
                    {
                        Log.Error(
                            e,
                            "{name} failed, and {wraps} and {imtextures} have different number of items",
                            nameof(this.AddNewTexture),
                            nameof(this.Wraps),
                            nameof(this.ImTextures));

                        if (this.wraps.Count > 0 && this.wraps[^1] == wrap)
                            this.wraps.RemoveAt(this.wraps.Count - 1);
                        if (this.ImTextures.Length > 0 && this.ImTextures[^1].TexID == handle)
                            this.ImTextures.RemoveAt(this.ImTextures.Length - 1);

                        if (this.wraps.Count != this.ImTextures.Length)
                            Log.Fatal("^ Failed to undo due to an internal inconsistency; embrace for a crash");
                    }

                    throw;
                }
            }

            return index;
        }

        public unsafe void Dispose()
        {
            if (this.garbage is null)
                return;

            if (this.IsBuildInProgress)
            {
                Log.Error(
                    "[{name}] 0x{ptr:X}: Trying to dispose while build is in progress; waiting for build.\n" +
                    "Stack:\n{trace}",
                    this.Owner?.Name ?? "<?>",
                    (nint)this.Atlas.NativePtr,
                    new StackTrace());
                while (this.IsBuildInProgress)
                    Thread.Sleep(100);
            }

#if VeryVerboseLog
            Log.Verbose("[{name}] 0x{ptr:X}: Disposing", this.Owner?.Name ?? "<?>", (nint)this.Atlas.NativePtr);
#endif
            this.garbage.Dispose();
        }

        public BuildToolkit CreateToolkit(FontAtlasFactory factory, bool isAsync)
        {
            var axisSubstance = this.Substances.OfType<GamePrebakedFontHandle.HandleSubstance>().Single();
            return new(factory, this, axisSubstance, isAsync) { BuildStep = FontAtlasBuildStep.PreBuild };
        }
    }

    private class DalamudFontAtlas : IFontAtlas, DisposeSafety.IDisposeCallback
    {
        private readonly DisposeSafety.ScopedFinalizer disposables = new();
        private readonly FontAtlasFactory factory;
        private readonly DelegateFontHandle.HandleManager delegateFontHandleManager;
        private readonly GamePrebakedFontHandle.HandleManager gameFontHandleManager;
        private readonly IFontHandleManager[] fontHandleManagers;

        private readonly object syncRootPostPromotion = new();
        private readonly object syncRoot = new();

        private Task<FontAtlasBuiltData> buildTask = EmptyTask;
        private FontAtlasBuiltData builtData;

        private int buildSuppressionCounter;
        private bool buildSuppressionSuppressed;

        private int buildIndex;
        private bool buildQueued;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudFontAtlas"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="atlasName">Name of atlas, for debugging and logging purposes.</param>
        /// <param name="autoRebuildMode">Specify how to auto rebuild.</param>
        /// <param name="isGlobalScaled">Whether the fonts in the atlas are under the effect of global scale.</param>
        public DalamudFontAtlas(
            FontAtlasFactory factory,
            string atlasName,
            FontAtlasAutoRebuildMode autoRebuildMode,
            bool isGlobalScaled)
        {
            this.IsGlobalScaled = isGlobalScaled;
            try
            {
                this.factory = factory;
                this.AutoRebuildMode = autoRebuildMode;
                this.Name = atlasName;

                this.factory.InterfaceManager.AfterBuildFonts += this.OnRebuildRecommend;
                this.disposables.Add(() => this.factory.InterfaceManager.AfterBuildFonts -= this.OnRebuildRecommend);

                this.fontHandleManagers = new IFontHandleManager[]
                {
                    this.delegateFontHandleManager = this.disposables.Add(
                        new DelegateFontHandle.HandleManager(atlasName)),
                    this.gameFontHandleManager = this.disposables.Add(
                        new GamePrebakedFontHandle.HandleManager(atlasName, factory)),
                };
                foreach (var fhm in this.fontHandleManagers)
                    fhm.RebuildRecommend += this.OnRebuildRecommend;
            }
            catch
            {
                this.disposables.Dispose();
                throw;
            }

            this.factory.SceneTask.ContinueWith(
                r =>
                {
                    lock (this.syncRoot)
                    {
                        if (this.disposed)
                            return;

                        r.Result.OnNewRenderFrame += this.ImGuiSceneOnNewRenderFrame;
                        this.disposables.Add(() => r.Result.OnNewRenderFrame -= this.ImGuiSceneOnNewRenderFrame);
                    }

                    if (this.AutoRebuildMode == FontAtlasAutoRebuildMode.OnNewFrame)
                        this.BuildFontsOnNextFrame();
                });
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DalamudFontAtlas"/> class.
        /// </summary>
        ~DalamudFontAtlas()
        {
            lock (this.syncRoot)
            {
                this.buildTask.ToDisposableIgnoreExceptions().Dispose();
                this.builtData.Dispose();
            }
        }

        /// <inheritdoc/>
        public event FontAtlasBuildStepDelegate? BuildStepChange;

        /// <inheritdoc/>
        public event Action? RebuildRecommend;

        /// <inheritdoc/>
        public event Action<DisposeSafety.IDisposeCallback>? BeforeDispose;

        /// <inheritdoc/>
        public event Action<DisposeSafety.IDisposeCallback, Exception?>? AfterDispose;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public FontAtlasAutoRebuildMode AutoRebuildMode { get; }

        /// <inheritdoc/>
        public ImFontAtlasPtr ImAtlas
        {
            get
            {
                lock (this.syncRoot)
                    return this.builtData.Atlas;
            }
        }

        /// <inheritdoc/>
        public Task BuildTask => this.buildTask;

        /// <inheritdoc/>
        public bool HasBuiltAtlas => !this.builtData.Atlas.IsNull();

        /// <inheritdoc/>
        public bool IsGlobalScaled { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.disposed)
                return;

            this.BeforeDispose?.InvokeSafely(this);

            try
            {
                lock (this.syncRoot)
                {
                    this.disposed = true;
                    this.buildTask.ToDisposableIgnoreExceptions().Dispose();
                    this.buildTask = EmptyTask;
                    this.disposables.Add(this.builtData);
                    this.builtData = default;
                    this.disposables.Dispose();
                }

                try
                {
                    this.AfterDispose?.Invoke(this, null);
                }
                catch
                {
                    // ignore
                }
            }
            catch (Exception e)
            {
                try
                {
                    this.AfterDispose?.Invoke(this, e);
                }
                catch
                {
                    // ignore
                }
            }

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public IDisposable SuppressAutoRebuild()
        {
            this.buildSuppressionCounter++;
            return Disposable.Create(
                () =>
                {
                    this.buildSuppressionCounter--;
                    if (this.buildSuppressionSuppressed)
                        this.OnRebuildRecommend();
                });
        }

        /// <inheritdoc/>
        public IFontHandle NewGameFontHandle(GameFontStyle style) => this.gameFontHandleManager.NewFontHandle(style);

        /// <inheritdoc/>
        public IFontHandle NewDelegateFontHandle(FontAtlasBuildStepDelegate buildStepDelegate) =>
            this.delegateFontHandleManager.NewFontHandle(buildStepDelegate);

        /// <inheritdoc/>
        public void BuildFontsOnNextFrame()
        {
            if (this.AutoRebuildMode == FontAtlasAutoRebuildMode.Async)
            {
                throw new InvalidOperationException(
                    $"{nameof(this.BuildFontsOnNextFrame)} cannot be used when " +
                    $"{nameof(this.AutoRebuildMode)} is set to " +
                    $"{nameof(FontAtlasAutoRebuildMode.Async)}.");
            }

            if (!this.buildTask.IsCompleted || this.buildQueued)
                return;

#if VeryVerboseLog
            Log.Verbose("[{name}] Queueing from {source}.", this.Name, nameof(this.BuildFontsOnNextFrame));
#endif

            this.buildQueued = true;
        }

        /// <inheritdoc/>
        public void BuildFontsImmediately()
        {
#if VeryVerboseLog
            Log.Verbose("[{name}] Called: {source}.", this.Name, nameof(this.BuildFontsImmediately));
#endif

            if (this.AutoRebuildMode == FontAtlasAutoRebuildMode.Async)
            {
                throw new InvalidOperationException(
                    $"{nameof(this.BuildFontsImmediately)} cannot be used when " +
                    $"{nameof(this.AutoRebuildMode)} is set to " +
                    $"{nameof(FontAtlasAutoRebuildMode.Async)}.");
            }

            var tcs = new TaskCompletionSource<FontAtlasBuiltData>();
            int rebuildIndex;
            try
            {
                rebuildIndex = ++this.buildIndex;
                lock (this.syncRoot)
                {
                    if (!this.buildTask.IsCompleted)
                        throw new InvalidOperationException("Font rebuild is already in progress.");

                    this.buildTask = tcs.Task;
                }

#if VeryVerboseLog
                Log.Verbose("[{name}] Building from {source}.", this.Name, nameof(this.BuildFontsImmediately));
#endif

                var scale = this.IsGlobalScaled ? ImGuiHelpers.GlobalScaleSafe : 1f;
                var r = this.RebuildFontsPrivate(false, scale);
                r.Wait();
                if (r.IsCompletedSuccessfully)
                    tcs.SetResult(r.Result);
                else if (r.Exception is not null)
                    tcs.SetException(r.Exception);
                else
                    tcs.SetCanceled();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                Log.Error(e, "[{name}] Failed to build fonts.", this.Name);
                throw;
            }

            this.InvokePostPromotion(rebuildIndex, tcs.Task.Result, nameof(this.BuildFontsImmediately));
        }

        /// <inheritdoc/>
        public Task BuildFontsAsync(bool callPostPromotionOnMainThread = true)
        {
#if VeryVerboseLog
            Log.Verbose("[{name}] Called: {source}.", this.Name, nameof(this.BuildFontsAsync));
#endif

            if (this.AutoRebuildMode == FontAtlasAutoRebuildMode.OnNewFrame)
            {
                throw new InvalidOperationException(
                    $"{nameof(this.BuildFontsAsync)} cannot be used when " +
                    $"{nameof(this.AutoRebuildMode)} is set to " +
                    $"{nameof(FontAtlasAutoRebuildMode.OnNewFrame)}.");
            }

            lock (this.syncRoot)
            {
                var scale = this.IsGlobalScaled ? ImGuiHelpers.GlobalScaleSafe : 1f;
                var rebuildIndex = ++this.buildIndex;
                return this.buildTask = this.buildTask.ContinueWith(BuildInner).Unwrap();

                async Task<FontAtlasBuiltData> BuildInner(Task<FontAtlasBuiltData> unused)
                {
                    Log.Verbose("[{name}] Building from {source}.", this.Name, nameof(this.BuildFontsAsync));
                    lock (this.syncRoot)
                    {
                        if (this.buildIndex != rebuildIndex)
                            return default;
                    }

                    var res = await this.RebuildFontsPrivate(true, scale);
                    if (res.Atlas.IsNull())
                        return res;

                    if (callPostPromotionOnMainThread)
                    {
                        await this.factory.Framework.RunOnFrameworkThread(
                            () => this.InvokePostPromotion(rebuildIndex, res, nameof(this.BuildFontsAsync)));
                    }
                    else
                    {
                        this.InvokePostPromotion(rebuildIndex, res, nameof(this.BuildFontsAsync));
                    }

                    return res;
                }
            }
        }

        private void InvokePostPromotion(int rebuildIndex, FontAtlasBuiltData data, [UsedImplicitly] string source)
        {
            lock (this.syncRoot)
            {
                if (this.buildIndex != rebuildIndex)
                {
                    data.ExplicitDisposeIgnoreExceptions();
                    return;
                }

                this.builtData.ExplicitDisposeIgnoreExceptions();
                this.builtData = data;
                this.buildTask = EmptyTask;
                foreach (var substance in data.Substances)
                    substance.Manager.Substance = substance;
            }

            lock (this.syncRootPostPromotion)
            {
                if (this.buildIndex != rebuildIndex)
                {
                    data.ExplicitDisposeIgnoreExceptions();
                    return;
                }

                var toolkit = new BuildToolkitPostPromotion(data);

                try
                {
                    this.BuildStepChange?.Invoke(toolkit);
                }
                catch (Exception e)
                {
                    Log.Error(
                        e,
                        "[{name}] {delegateName} PostPromotion error",
                        this.Name,
                        nameof(FontAtlasBuildStepDelegate));
                }

                foreach (var substance in data.Substances)
                {
                    try
                    {
                        substance.OnPostPromotion(toolkit);
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            e,
                            "[{name}] {substance} PostPromotion error",
                            this.Name,
                            substance.GetType().FullName ?? substance.GetType().Name);
                    }
                }

                foreach (var font in toolkit.Fonts)
                {
                    try
                    {
                        toolkit.BuildLookupTable(font);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "[{name}] BuildLookupTable error", this.Name);
                    }
                }

#if VeryVerboseLog
                Log.Verbose("[{name}] Built from {source}.", this.Name, source);
#endif
            }
        }

        private void ImGuiSceneOnNewRenderFrame()
        {
            if (!this.buildQueued)
                return;

            try
            {
                if (this.AutoRebuildMode != FontAtlasAutoRebuildMode.Async)
                    this.BuildFontsImmediately();
            }
            finally
            {
                this.buildQueued = false;
            }
        }

        private Task<FontAtlasBuiltData> RebuildFontsPrivate(bool isAsync, float scale)
        {
            if (NoConcurrentBuildOperationLock is null)
                return this.RebuildFontsPrivateReal(isAsync, scale);
            lock (NoConcurrentBuildOperationLock)
                return this.RebuildFontsPrivateReal(isAsync, scale);
        }

        private async Task<FontAtlasBuiltData> RebuildFontsPrivateReal(bool isAsync, float scale)
        {
            lock (this.syncRoot)
            {
                // this lock ensures that this.buildTask is properly set.
            }

            var sw = new Stopwatch();
            sw.Start();

            var res = default(FontAtlasBuiltData);
            nint atlasPtr = 0;
            try
            {
                res = new(this, this.fontHandleManagers.Select(x => x.NewSubstance()), scale);
                unsafe
                {
                    atlasPtr = (nint)res.Atlas.NativePtr;
                }

                Log.Verbose(
                    "[{name}:{functionname}] 0x{ptr:X}: PreBuild (at {sw}ms)",
                    this.Name,
                    nameof(this.RebuildFontsPrivateReal),
                    atlasPtr,
                    sw.ElapsedMilliseconds);

                using var toolkit = res.CreateToolkit(this.factory, isAsync);
                this.BuildStepChange?.Invoke(toolkit);
                toolkit.PreBuildSubstances();
                toolkit.PreBuild();

#if VeryVerboseLog
                Log.Verbose("[{name}:{functionname}] 0x{ptr:X}: Build (at {sw}ms)", this.Name, nameof(this.RebuildFontsPrivateReal), atlasPtr, sw.ElapsedMilliseconds);
#endif

                toolkit.DoBuild();

#if VeryVerboseLog
                Log.Verbose("[{name}:{functionname}] 0x{ptr:X}: PostBuild (at {sw}ms)", this.Name, nameof(this.RebuildFontsPrivateReal), atlasPtr, sw.ElapsedMilliseconds);
#endif

                toolkit.PostBuild();
                toolkit.PostBuildSubstances();
                this.BuildStepChange?.Invoke(toolkit);

                if (this.factory.SceneTask is { IsCompleted: false } sceneTask)
                {
                    Log.Verbose(
                        "[{name}:{functionname}] 0x{ptr:X}: await SceneTask (at {sw}ms)",
                        this.Name,
                        nameof(this.RebuildFontsPrivateReal),
                        atlasPtr,
                        sw.ElapsedMilliseconds);
                    await sceneTask.ConfigureAwait(!isAsync);
                }

#if VeryVerboseLog
                Log.Verbose("[{name}:{functionname}] 0x{ptr:X}: UploadTextures (at {sw}ms)", this.Name, nameof(this.RebuildFontsPrivateReal), atlasPtr, sw.ElapsedMilliseconds);
#endif
                toolkit.UploadTextures();

                Log.Verbose(
                    "[{name}:{functionname}] 0x{ptr:X}: Complete (at {sw}ms)",
                    this.Name,
                    nameof(this.RebuildFontsPrivateReal),
                    atlasPtr,
                    sw.ElapsedMilliseconds);

                res.IsBuildInProgress = false;
                return res;
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "[{name}:{functionname}] 0x{ptr:X}: Failed (at {sw}ms)",
                    this.Name,
                    nameof(this.RebuildFontsPrivateReal),
                    atlasPtr,
                    sw.ElapsedMilliseconds);
                res.IsBuildInProgress = false;
                res.Dispose();
                throw;
            }
            finally
            {
                this.buildQueued = false;
            }
        }

        private void OnRebuildRecommend()
        {
            if (this.disposed)
                return;

            if (this.buildSuppressionCounter > 0)
            {
                this.buildSuppressionSuppressed = true;
                return;
            }

            this.buildSuppressionSuppressed = false;
            this.factory.Framework.RunOnFrameworkThread(
                () =>
                {
                    this.RebuildRecommend?.InvokeSafely();

                    switch (this.AutoRebuildMode)
                    {
                        case FontAtlasAutoRebuildMode.Async:
                            _ = this.BuildFontsAsync();
                            break;
                        case FontAtlasAutoRebuildMode.OnNewFrame:
                            this.BuildFontsOnNextFrame();
                            break;
                        case FontAtlasAutoRebuildMode.Disable:
                        default:
                            break;
                    }
                });
        }
    }
}
