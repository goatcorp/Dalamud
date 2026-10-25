using System.Collections.Generic;
using System.IO;
using System.Threading;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <content>Reference-counted in-memory font files.</content>
internal sealed unsafe partial class DirectWriteFontFace
{
    // Backing in-memory font file for non-installed fonts, or null for system fonts.
    private InMemoryFile? memoryFile;

    // Reference-counted in-memory font file with its own IDWriteInMemoryFontFileLoader.
    // DirectWrite retains all files registered on a loader until the loader is released, so using one loader per file frees the copy when the last face releases.
    private sealed class InMemoryFile
    {
        private static readonly Lock RegistryLock = new();

        // By file key, so that the faces of a collection share one copy.
        private static readonly Dictionary<string, InMemoryFile> ByKey = [];

        // Keyed by loader pointer so derived faces can find and retain the same InMemoryFile.
        private static readonly Dictionary<nint, InMemoryFile> ByLoader = [];

        private readonly string key;
        private IDWriteInMemoryFontFileLoader* loader;
        private int references;

        private InMemoryFile(string key, IDWriteInMemoryFontFileLoader* loader, IDWriteFontFile* file)
        {
            this.key = key;
            this.loader = loader;
            this.File = file;
            this.references = 1;
        }

        public IDWriteFontFile* File { get; private set; }

        // Retains an existing InMemoryFile for key, or calls open to load a new copy on a miss.
        public static InMemoryFile Acquire(string key, Func<Stream> open)
        {
            lock (RegistryLock)
            {
                if (ByKey.TryGetValue(key, out var existing))
                {
                    existing.references++;
                    return existing;
                }
            }

            // Read outside the lock. If another thread wins the race, AcquireCopy returns the existing entry.
            // UnmanagedMemoryStream avoids an intermediate managed copy.
            using var stream = open();
            if (stream is UnmanagedMemoryStream mapped)
            {
                return AcquireCopy(
                    key,
                    new ReadOnlySpan<byte>(
                        mapped.PositionPointer, checked((int)Math.Max(mapped.Length - mapped.Position, 0))));
            }

            // The position is relative to the start of the exposed buffer segment.
            if (stream is MemoryStream m && m.TryGetBuffer(out var buffer))
                return AcquireCopy(key, buffer.AsSpan((int)Math.Min(m.Position, buffer.Count)));

            using var ms = new MemoryStream(stream.CanSeek ? checked((int)stream.Length) : 0);
            stream.CopyTo(ms);
            return AcquireCopy(key, ms.GetBuffer().AsSpan(0, (int)ms.Length));
        }

        // Finds and retains the InMemoryFile backing face, or returns null if it was not created here.
        public static InMemoryFile? Find(IDWriteFontFace* face)
        {
            var count = 1u;
            using var file = default(ComPtr<IDWriteFontFile>);
            using var loader = default(ComPtr<IDWriteFontFileLoader>);
            if (face->GetFiles(&count, null).FAILED || count != 1 ||
                face->GetFiles(&count, file.GetAddressOf()).FAILED ||
                file.Get()->GetLoader(loader.GetAddressOf()).FAILED)
            {
                return null;
            }

            lock (RegistryLock)
            {
                if (!ByLoader.TryGetValue((nint)loader.Get(), out var found))
                    return null;
                found.references++;
                return found;
            }
        }

        // Decrements the reference count and unregisters the loader when the last face is released.
        public void Release()
        {
            lock (RegistryLock)
            {
                if (--this.references > 0)
                    return;
                ByKey.Remove(this.key);
                ByLoader.Remove((nint)this.loader);
            }

            this.File->Release();
            this.File = null;
            DirectWriteUtilities.Factory->UnregisterFontFileLoader((IDWriteFontFileLoader*)this.loader);
            this.loader->Release();
            this.loader = null;
        }

        private static InMemoryFile AcquireCopy(string key, ReadOnlySpan<byte> data)
        {
            lock (RegistryLock)
            {
                if (ByKey.TryGetValue(key, out var existing))
                {
                    existing.references++;
                    return existing;
                }

                var factory = DirectWriteUtilities.Factory;
                using var loader = default(ComPtr<IDWriteInMemoryFontFileLoader>);
                using var file = default(ComPtr<IDWriteFontFile>);
                factory->CreateInMemoryFontFileLoader(loader.GetAddressOf()).ThrowOnError();
                factory->RegisterFontFileLoader((IDWriteFontFileLoader*)loader.Get()).ThrowOnError();
                fixed (byte* p = data)
                {
                    var hr = loader.Get()->CreateInMemoryFontFileReference(
                        (IDWriteFactory*)factory, p, (uint)data.Length, null, file.GetAddressOf());
                    if (hr.FAILED)
                    {
                        factory->UnregisterFontFileLoader((IDWriteFontFileLoader*)loader.Get());
                        hr.ThrowOnError();
                    }
                }

                var created = new InMemoryFile(key, loader.Detach(), file.Detach());
                ByKey.Add(key, created);
                ByLoader.Add((nint)created.loader, created);
                return created;
            }
        }
    }
}
