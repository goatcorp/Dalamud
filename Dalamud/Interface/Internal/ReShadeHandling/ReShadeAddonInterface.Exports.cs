using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Support;

using Serilog;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.ReShadeHandling;

[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal sealed unsafe partial class ReShadeAddonInterface
{
    private static ExportsStruct Exports => ExportDiscovery.Exports;

    /// <summary>Gets the ReShade module selected for addon registration.</summary>
    public static ProcessModule? ReShadeModule => ExportDiscovery.ReShadeModule;

    /// <summary>Gets all modules exporting the required ReShade addon functions.</summary>
    public static ImmutableArray<ProcessModule> AllReShadeModules => ExportDiscovery.AllReShadeModules;

    /// <summary>Gets a value indicating whether the selected module's signing certificate names ReShade as issuer.</summary>
    /// <remarks>The certificate's trust and signature validity are not verified.</remarks>
    public static bool ReShadeIsSignedByReShade => ExportDiscovery.ReShadeIsSignedByReShade;

    /// <summary>Finds the address of <c>DXGISwapChain::on_present</c> in <see cref="ReShadeModule"/>.</summary>
    /// <returns>Address of the function, or <c>0</c> if not found.</returns>
    public static nint FindReShadeDxgiSwapChainOnPresent()
    {
        if (ReShadeModule is not { } rsm)
            return 0;

        var m = new ReadOnlySpan<byte>((void*)rsm.BaseAddress, rsm.ModuleMemorySize);

        // Signature validated against 5.0.0 to 6.2.0
        var i = m.IndexOf(new byte[] { 0xCC, 0xF6, 0xC2, 0x01, 0x0F, 0x85 });
        if (i == -1)
            return 0;

        return rsm.BaseAddress + i + 1;
    }

    /// <summary>Gets an embedded signing certificate's issuer name without verifying the signature.</summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>The signing certificate's issuer display name.</returns>
    // https://learn.microsoft.com/en-us/previous-versions/troubleshoot/windows/win32/get-information-authenticode-signed-executables
    private static string GetSignatureSignerNameWithoutVerification(ReadOnlySpan<char> path)
    {
        var hCertStore = default(HCERTSTORE);
        var hMsg = default(HCRYPTMSG);
        var pCertContext = default(CERT_CONTEXT*);
        try
        {
            fixed (void* pwszFile = path)
            {
                uint dwMsgAndCertEncodingType;
                uint dwContentType;
                uint dwFormatType;
                void* pvContext;
                if (!CryptQueryObject(
                        CERT.CERT_QUERY_OBJECT_FILE,
                        pwszFile,
                        CERT.CERT_QUERY_CONTENT_FLAG_ALL,
                        CERT.CERT_QUERY_FORMAT_FLAG_ALL,
                        0,
                        &dwMsgAndCertEncodingType,
                        &dwContentType,
                        &dwFormatType,
                        &hCertStore,
                        &hMsg,
                        &pvContext))
                {
                    throw new Win32Exception("CryptQueryObject");
                }
            }

            var pcb = 0u;
            if (!CryptMsgGetParam(hMsg, CMSG.CMSG_SIGNER_INFO_PARAM, 0, null, &pcb))
                throw new Win32Exception("CryptMsgGetParam(1)");

            var signerInfo = GC.AllocateArray<byte>((int)pcb, true);
            var pSignerInfo = (CMSG_SIGNER_INFO*)Unsafe.AsPointer(ref signerInfo[0]);
            if (!CryptMsgGetParam(hMsg, CMSG.CMSG_SIGNER_INFO_PARAM, 0, pSignerInfo, &pcb))
                throw new Win32Exception("CryptMsgGetParam(2)");

            var certInfo = new CERT_INFO
            {
                Issuer = pSignerInfo->Issuer,
                SerialNumber = pSignerInfo->SerialNumber,
            };
            pCertContext = CertFindCertificateInStore(
                hCertStore,
                X509.X509_ASN_ENCODING | PKCS.PKCS_7_ASN_ENCODING,
                0,
                CERT.CERT_FIND_SUBJECT_CERT,
                &certInfo,
                null);
            if (pCertContext == default)
                throw new Win32Exception("CertFindCertificateInStore");

            pcb = CertGetNameStringW(
                pCertContext,
                CERT.CERT_NAME_SIMPLE_DISPLAY_TYPE,
                CERT.CERT_NAME_ISSUER_FLAG,
                null,
                null,
                pcb);
            if (pcb == 0)
                throw new Win32Exception("CertGetNameStringW(1)");

            var issuerName = GC.AllocateArray<char>((int)pcb, true);
            pcb = CertGetNameStringW(
                pCertContext,
                CERT.CERT_NAME_SIMPLE_DISPLAY_TYPE,
                CERT.CERT_NAME_ISSUER_FLAG,
                null,
                (char*)Unsafe.AsPointer(ref issuerName[0]),
                pcb);
            if (pcb == 0)
                throw new Win32Exception("CertGetNameStringW(2)");

            // The string is null-terminated.
            return new(issuerName.AsSpan()[..^1]);
        }
        finally
        {
            if (pCertContext != default) CertFreeCertificateContext(pCertContext);
            if (hCertStore != default) CertCloseStore(hCertStore, 0);
            if (hMsg != default) CryptMsgClose(hMsg);
        }
    }

    /// <summary>Defers module scanning until native exports or module metadata are requested.</summary>
    private static class ExportDiscovery
    {
        public static readonly ExportsStruct Exports;

        static ExportDiscovery()
        {
            var modules = new List<ProcessModule>();
            foreach (var m in CurrentProcessModules.ModuleCollection.Cast<ProcessModule>())
            {
                ExportsStruct e;
                if (!GetProcAddressInto(m, nameof(e.ReShadeRegisterAddon), &e.ReShadeRegisterAddon) ||
                    !GetProcAddressInto(m, nameof(e.ReShadeUnregisterAddon), &e.ReShadeUnregisterAddon) ||
                    !GetProcAddressInto(m, nameof(e.ReShadeRegisterEvent), &e.ReShadeRegisterEvent) ||
                    !GetProcAddressInto(m, nameof(e.ReShadeUnregisterEvent), &e.ReShadeUnregisterEvent))
                    continue;

                // Retain every compatible module; use the first for addon registration and signature metadata.
                modules.Add(m);
                if (modules.Count == 1)
                {
                    try
                    {
                        var signerName = GetSignatureSignerNameWithoutVerification(m.FileName);
                        ReShadeIsSignedByReShade = signerName == "ReShade";
                        Log.Information(
                            "ReShade DLL is signed by {signerName}. {vn}={v}",
                            signerName,
                            nameof(ReShadeIsSignedByReShade),
                            ReShadeIsSignedByReShade);
                    }
                    catch (Exception ex)
                    {
                        Log.Information(ex, "ReShade DLL did not had a valid signature.");
                    }

                    ReShadeModule = m;
                    Exports = e;
                }
            }

            AllReShadeModules = [.. modules];

            return;

            static bool GetProcAddressInto(ProcessModule m, ReadOnlySpan<char> name, void* res)
            {
                Span<byte> name8 = stackalloc byte[Encoding.UTF8.GetByteCount(name) + 1];
                name8[Encoding.UTF8.GetBytes(name, name8)] = 0;
                *(nint*)res = (nint)GetProcAddress((HMODULE)m.BaseAddress, (sbyte*)Unsafe.AsPointer(ref name8[0]));
                return *(nint*)res != 0;
            }
        }

        public static ProcessModule? ReShadeModule { get; }

        public static ImmutableArray<ProcessModule> AllReShadeModules { get; }

        public static bool ReShadeIsSignedByReShade { get; }
    }

    private struct ExportsStruct
    {
        public delegate* unmanaged<HMODULE, uint, bool> ReShadeRegisterAddon;
        public delegate* unmanaged<HMODULE, void> ReShadeUnregisterAddon;
        public delegate* unmanaged<AddonEvent, void*, void> ReShadeRegisterEvent;
        public delegate* unmanaged<AddonEvent, void*, void> ReShadeUnregisterEvent;
    }
}
