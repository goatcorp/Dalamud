using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using BitFaster.Caching.Lru;

using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromGameIcon(in GameIconLookup lookup) =>
        this.Shared.GetFromGameIcon(lookup);

    /// <inheritdoc/>
    bool ITextureProvider.TryGetFromGameIcon(
        in GameIconLookup lookup, [NotNullWhen(true)] out ISharedImmediateTexture? texture)
    {
        if (this.Shared.TryGetFromGameIcon(lookup, out var pureImpl))
        {
            texture = pureImpl;
            return true;
        }

        texture = null;
        return false;
    }

    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromGame(string path) =>
        this.Shared.GetFromGame(path);

    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromFile(string path) =>
        this.Shared.GetFromFile(path);
    
    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromFile(FileInfo file) =>
        this.Shared.GetFromFile(file);

    /// <inheritdoc/>
    public ISharedImmediateTexture GetFromFileAbsolute(string fullPath) =>
        this.Shared.GetFromFileAbsolute(fullPath);

    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromManifestResource(Assembly assembly, string name) =>
        this.Shared.GetFromManifestResource(assembly, name);

    /// <summary>A part of texture manager that deals with <see cref="ISharedImmediateTexture"/>s.</summary>
    internal sealed class SharedTextureManager : IDisposable
    {
        private const int PathLookupLruCount = 8192;

        private readonly TextureManager textureManager;
        private readonly ConcurrentLru<GameIconLookup, string> lookupCache = new(PathLookupLruCount);
        private readonly ConcurrentDictionary<string, SharedImmediateTexture> gameDict = new();
        private readonly ConcurrentDictionary<string, SharedImmediateTexture> fileDict = new();
        private readonly ConcurrentDictionary<(Assembly, string), SharedImmediateTexture> manifestResourceDict = new();
        private readonly HashSet<SharedImmediateTexture> invalidatedTextures = new();

        private readonly Thread sharedTextureReleaseThread;

        private readonly CancellationTokenSource disposingCancellationTokenSource = new();

        /// <summary>Initializes a new instance of the <see cref="SharedTextureManager"/> class.</summary>
        /// <param name="textureManager">An instance of <see cref="Interface.Textures.Internal.TextureManager"/>.</param>
        public SharedTextureManager(TextureManager textureManager)
        {
            this.textureManager = textureManager;

            this.sharedTextureReleaseThread = new(this.ReleaseSharedTextures)
            {
                Priority = ThreadPriority.Lowest,
            };
            this.sharedTextureReleaseThread.Start();
        }

        /// <summary>Gets all the loaded textures from game resources.</summary>
        public ICollection<SharedImmediateTexture> ForDebugGamePathTextures => this.gameDict.Values;

        /// <summary>Gets all the loaded textures from filesystem.</summary>
        public ICollection<SharedImmediateTexture> ForDebugFileSystemTextures => this.fileDict.Values;

        /// <summary>Gets all the loaded textures from assembly manifest resources.</summary>
        public ICollection<SharedImmediateTexture> ForDebugManifestResourceTextures => this.manifestResourceDict.Values;

        /// <summary>Gets all the loaded textures that are invalidated from <see cref="InvalidatePaths"/>.</summary>
        /// <remarks><c>lock</c> on use of the value returned from this property.</remarks>
        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Debug use only; users are expected to lock around this")]
        public ICollection<SharedImmediateTexture> ForDebugInvalidatedTextures => this.invalidatedTextures;

        private SharedTextureManager NonDisposed =>
            this.disposingCancellationTokenSource.IsCancellationRequested
                ? throw new ObjectDisposedException(nameof(SharedTextureManager))
                : this;

        /// <inheritdoc/> 
        public void Dispose()
        {
            this.disposingCancellationTokenSource.Cancel();
            this.lookupCache.Clear();
            ReleaseSelfReferences(this.gameDict);
            ReleaseSelfReferences(this.fileDict);
            ReleaseSelfReferences(this.manifestResourceDict);
            this.sharedTextureReleaseThread.Join();
            return;

            static void ReleaseSelfReferences<T>(ConcurrentDictionary<T, SharedImmediateTexture> dict)
            {
                foreach (var v in dict.Values)
                    v.ReleaseSelfReference(true);
                dict.Clear();
            }
        }

        /// <inheritdoc cref="ITextureProvider.GetFromGameIcon"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedImmediateTexture.PureImpl GetFromGameIcon(in GameIconLookup lookup) =>
            this.NonDisposed.GetFromGame(this.lookupCache.GetOrAdd(lookup, this.GetIconPathByValue));
        
        /// <inheritdoc cref="ITextureProvider.TryGetFromGameIcon"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFromGameIcon(in GameIconLookup lookup, [NotNullWhen(true)] out SharedImmediateTexture.PureImpl? texture)
        {
            ObjectDisposedException.ThrowIf(this.disposingCancellationTokenSource.IsCancellationRequested, this);

            texture = null;

            if (!this.lookupCache.TryGet(lookup, out var path))
            {
                if (!this.textureManager.TryGetIconPath(lookup, out path))
                    return false;
                
                this.lookupCache.AddOrUpdate(lookup, path);
            }
            
            texture = this.GetFromGame(path);
            return texture != null;
        }

        /// <inheritdoc cref="ITextureProvider.GetFromGame"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedImmediateTexture.PureImpl GetFromGame(string path) =>
            this.NonDisposed.gameDict.GetOrAdd(path, GamePathSharedImmediateTexture.CreatePlaceholder)
                .PublicUseInstance;

        /// <inheritdoc cref="ITextureProvider.GetFromFile(string)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedImmediateTexture.PureImpl GetFromFile(string path) =>
            this.NonDisposed.GetFromFile(new FileInfo(path));
        
        /// <inheritdoc cref="ITextureProvider.GetFromFile(FileInfo)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedImmediateTexture.PureImpl GetFromFile(FileInfo file) =>
            this.NonDisposed.GetFromFileAbsolute(file.FullName);

        /// <inheritdoc cref="ITextureProvider.GetFromFileAbsolute(string)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedImmediateTexture.PureImpl GetFromFileAbsolute(string fullPath) =>
            this.NonDisposed.fileDict.GetOrAdd(fullPath, FileSystemSharedImmediateTexture.CreatePlaceholder)
                .PublicUseInstance;

        /// <inheritdoc cref="ITextureProvider.GetFromManifestResource"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SharedImmediateTexture.PureImpl GetFromManifestResource(Assembly assembly, string name) =>
            this.NonDisposed.manifestResourceDict.GetOrAdd(
                (assembly, name),
                ManifestResourceSharedImmediateTexture.CreatePlaceholder)
                .PublicUseInstance;

        /// <summary>Invalidates a cached item from <see cref="GetFromGame"/> and <see cref="GetFromGameIcon"/>.
        /// </summary>
        /// <param name="path">The path to invalidate.</param>
        public void FlushFromGameCache(string path)
        {
            if (this.disposingCancellationTokenSource.IsCancellationRequested)
                return;

            if (this.gameDict.TryRemove(path, out var r))
            {
                if (r.ReleaseSelfReference(true) != 0)
                {
                    lock (this.invalidatedTextures)
                        this.invalidatedTextures.Add(r);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetIconPathByValue(GameIconLookup lookup) =>
            this.NonDisposed.textureManager.TryGetIconPath(lookup, out var path)
                ? path
                : throw new IconNotFoundException(lookup);

        private void ReleaseSharedTextures()
        {
            while (!this.disposingCancellationTokenSource.IsCancellationRequested)
            {
                RemoveFinalReleased(this.gameDict);
                RemoveFinalReleased(this.fileDict);
                RemoveFinalReleased(this.manifestResourceDict);

                // ReSharper disable once InconsistentlySynchronizedField
                if (this.invalidatedTextures.Count != 0)
                {
                    lock (this.invalidatedTextures)
                        this.invalidatedTextures.RemoveWhere(TextureFinalReleasePredicate);
                }

                try
                {
                    this.textureManager.framework.DelayTicks(60).Wait(this.disposingCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void RemoveFinalReleased<T>(ConcurrentDictionary<T, SharedImmediateTexture> dict)
            {
                if (dict.IsEmpty)
                    return;

                foreach (var (k, v) in dict)
                {
                    if (TextureFinalReleasePredicate(v))
                        _ = dict.TryRemove(k, out _);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool TextureFinalReleasePredicate(SharedImmediateTexture v) =>
                v.ContentQueried && v.ReleaseSelfReference(false) == 0;
        }
    }
}
