using System;
using System.Diagnostics;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.Storage.FileSystem;

namespace Dalamud.Injector.Container;

internal sealed class AppContainer : IDisposable
{
    private PSID sid;

    public PSID Sid => this.sid;

    public AppContainer(string containerName, string? displayName, string? description)
    {
        HRESULT hresult;
        
        hresult = PInvoke.CreateAppContainerProfile(
            containerName,
            displayName ?? containerName,
            description ?? containerName,
            Span<SID_AND_ATTRIBUTES>.Empty,
            out this.sid);

        if (!hresult.Succeeded)
        {
            if (hresult == PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_ALREADY_EXISTS))
            {
                hresult = PInvoke.DeriveAppContainerSidFromAppContainerName(containerName, out this.sid);
                hresult.ThrowOnFailure();
            }
            else
            {
                hresult.ThrowOnFailure();
            }
        }
    }

    ~AppContainer()
    {
        this.DisposeUnmanaged();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DisposeUnmanaged();
    }

    private unsafe void DisposeUnmanaged()
    {
        Debug.Assert(OperatingSystem.IsOSPlatformVersionAtLeast("windows", 8), "unsupported platform");
        PInvoke.FreeSid(this.sid);
    }

    public void GrantFileAccess(string path, FILE_ACCESS_FLAGS accessMask)
    {
        unsafe
        {
            WIN32_ERROR errc;
            EXPLICIT_ACCESS_W access;
            ACL* pacl = null, newPacl = null;

            try
            {
                access.grfAccessMode = ACCESS_MODE.GRANT_ACCESS;
                access.grfAccessPermissions = (uint)accessMask;
                access.grfInheritance = ACE_FLAGS.OBJECT_INHERIT_ACE | ACE_FLAGS.CONTAINER_INHERIT_ACE;
                access.Trustee.pMultipleTrustee = null;
                access.Trustee.TrusteeForm = TRUSTEE_FORM.TRUSTEE_IS_SID;
                access.Trustee.TrusteeType = TRUSTEE_TYPE.TRUSTEE_IS_WELL_KNOWN_GROUP;
                access.Trustee.ptstrName = (PWSTR)(void*)this.sid;

                fixed (char* pPath = path)
                {
                    errc = PInvoke.GetNamedSecurityInfo(
                        pPath,
                        SE_OBJECT_TYPE.SE_FILE_OBJECT,
                        OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                        null,
                        null,
                        &pacl,
                        null,
                        default);
                    if (errc != WIN32_ERROR.ERROR_SUCCESS)
                    {
                        throw new Exception($"Failed to fetch dacl information for {path}");
                    }

                    errc = PInvoke.SetEntriesInAcl(1, &access, pacl, &newPacl);
                    if (errc != WIN32_ERROR.ERROR_SUCCESS)
                    {
                        throw new Exception($"Failed to set acl entries");
                    }

                    errc = PInvoke.SetNamedSecurityInfo(
                        pPath,
                        SE_OBJECT_TYPE.SE_FILE_OBJECT,
                        OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                        default,
                        default,
                        newPacl,
                        null);
                    if (errc != WIN32_ERROR.ERROR_SUCCESS)
                    {
                        throw new Exception($"Failed to set dacl information for {path}");
                    }
                }
            }
            finally
            {
                // if (pacl is not null)
                //     PInvoke.LocalFree((IntPtr)pacl);

                if (newPacl is not null)
                    PInvoke.LocalFree((IntPtr)newPacl);
            }
        }
    }
}
