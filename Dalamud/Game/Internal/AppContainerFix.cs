using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.System.String;
using Serilog;

namespace Dalamud.Game.Internal;

// NOTE:
// We need to block main thread as FFXIV will attempt to resolve a path to `My Documents` and might cache the value.
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class AppContainerFix : IServiceType
{
    private Hook<TryGetMyDocumentsPath> mPathHook;
    private Hook<SHGetKnownFolderPathPrototype> mSHGetKnownFolderPathHook;

    [ServiceManager.ServiceConstructor]
    private unsafe AppContainerFix(SigScanner sigScanner)
    {
        // This function internally calls SHGetSpecialFolderLocation
        var pGetMyDocuments = sigScanner.ScanText("4889?????? 57 4881EC??????00 488B05???????? 4833C4 48898424???????? 488BF9 32DB");
        this.mPathHook = Hook<TryGetMyDocumentsPath>.FromAddress(pGetMyDocuments, this.FixTryGetMyDocumentsPath);
        this.mSHGetKnownFolderPathHook = Hook<SHGetKnownFolderPathPrototype>.FromSymbol("shell32.dll", "SHGetKnownFolderPath", this.SHGetKnownFolderPathDetour);

        this.mPathHook.Enable();
        this.mSHGetKnownFolderPathHook.Enable();

        Log.Verbose("AppContainer Fix Test: {Path}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }

    private unsafe delegate nint TryGetMyDocumentsPath(Utf8String* pPath);

    private unsafe delegate uint SHGetKnownFolderPathPrototype(IntPtr rfid, uint dwFlags, IntPtr hToken, IntPtr ppszPath);

    private unsafe nint FixTryGetMyDocumentsPath(Utf8String* pPath)
    {
        var redirectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var redirectedPathUtf8 = Encoding.UTF8.GetBytes(redirectedPath);

        Log.Debug("Redirecting My Documents to '{MyDocument}'", redirectedPath);

        fixed (byte* pRedirectedPathUtf8 = redirectedPathUtf8)
        {
            pPath->SetString(pRedirectedPathUtf8);
        }

        return 1;
    }

    private unsafe uint SHGetKnownFolderPathDetour(IntPtr rfid, uint dwFlags, IntPtr hToken, IntPtr ppszPath)
    {
        dwFlags |= 0x00004000; /* KF_FLAG_DONT_VERIFY */
        return this.mSHGetKnownFolderPathHook.Original(rfid, dwFlags, hToken, ppszPath);
    }
}
