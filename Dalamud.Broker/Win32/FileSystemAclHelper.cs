using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;

namespace Dalamud.Broker.Win32;

internal static class FileSystemAclHelper
{
    /// <summary>
    /// Set integrity level of the file or directory to designated mandatory level.
    /// </summary>
    public static void SetIntegrityLevel(string path, WELL_KNOWN_SID_TYPE sidType, ACE_FLAGS inheritanceFlags)
    {
        // As much as we want to delegate this task to System.Security.AccessControl,
        // FileSecurity doesn't expose its SecurityDescriptor and is sealed.

        unsafe
        {
            const int defaultAclLength = 1000;

            BOOL ok;
            WIN32_ERROR errc;
            nint pFullPath = default, pAcl = default, psid = default;
            uint wellKnownSidLength = default;

            try
            {
                // SetNamedSecurityInfo demands the path to be absolute and non-const. :\
                var fullPath = Path.GetFullPath(path);
                pFullPath = Marshal.StringToCoTaskMemUni(fullPath);

                // Allocate and initialize the SID object for WinLowLabelSid.
                // Note that first CreateWellKnownSid call is to fetch the required size.
                PInvoke.CreateWellKnownSid(sidType, (PSID)null, (PSID)null,
                                           ref wellKnownSidLength);
                psid = Marshal.AllocCoTaskMem((int)wellKnownSidLength);
                ok = PInvoke.CreateWellKnownSid(sidType, (PSID)null, (PSID)psid.ToPointer(),
                                                ref wellKnownSidLength);
                if (!ok)
                {
                    throw new Win32Exception();
                }

                // Allocate and initialize the acl.
                //
                // Calculating correct `nAclLength` is extremely complex and error-prone[^1][^2]
                // so we just cheat here by hard coding some very large value.
                // [1]: https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-initializeacl#remarks
                // [2]: Not to mention `nAclLength` needs to be well-aligned. (Really, why should caller take care about this?) 
                pAcl = Marshal.AllocCoTaskMem(defaultAclLength);
                ok = PInvoke.InitializeAcl((ACL*)pAcl, defaultAclLength, ACE_REVISION.ACL_REVISION);
                if (!ok)
                {
                    throw new Win32Exception();
                }

                // Remove any pre-existing labels by setting SACL with `len(acl) == 0` on the file object.
                //
                // This is required because Windows has (presumably undocumented) nasty gotcha where
                // `SetNamedSecurityInfo` would silently fail (even if it returns NO_ERROR) if there's a
                // pre-existing label on it.
                errc = PInvoke.SetNamedSecurityInfo(
                    (PWSTR)pFullPath.ToPointer(),
                    SE_OBJECT_TYPE.SE_FILE_OBJECT,
                    OBJECT_SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION,
                    (PSID)null,
                    (PSID)null,
                    null,
                    (ACL*)pAcl
                );
                if (errc != WIN32_ERROR.NO_ERROR)
                {
                    throw new Win32Exception();
                }

                // Add mandatory label to ACL.
                ok = PInvoke.AddMandatoryAce(
                    (ACL*)pAcl,
                    ACE_REVISION.ACL_REVISION,
                    inheritanceFlags,
                    PInvoke.SYSTEM_MANDATORY_LABEL_NO_WRITE_UP,
                    (PSID)psid.ToPointer()
                );
                if (!ok)
                {
                    throw new Win32Exception();
                }

                // Set new SACL to the file.
                errc = PInvoke.SetNamedSecurityInfo(
                    (PWSTR)pFullPath.ToPointer(),
                    SE_OBJECT_TYPE.SE_FILE_OBJECT,
                    OBJECT_SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION,
                    (PSID)null,
                    (PSID)null,
                    null,
                    (ACL*)pAcl
                );
                if (errc != WIN32_ERROR.NO_ERROR)
                {
                    throw new Win32Exception();
                }
            } finally
            {
                if (pFullPath != nint.Zero)
                    Marshal.FreeCoTaskMem(pFullPath);

                if (pAcl != nint.Zero)
                    Marshal.FreeCoTaskMem(pAcl);

                if (psid != nint.Zero)
                    Marshal.FreeCoTaskMem(psid);
            }
        }
    }

    public static void AddFileAce(
        string path, IdentityReference id, FileSystemRights access, InheritanceFlags inheritanceFlags,
        AccessControlType accessControlType)
    {
        var fileInfo = new FileInfo(path);
        var securityDesc = fileInfo.GetAccessControl();

        securityDesc.AddAccessRule(
            new FileSystemAccessRule(id, access, inheritanceFlags, PropagationFlags.None, accessControlType));

        fileInfo.SetAccessControl(securityDesc);
    }

    public static void AddDirectoryAce(
        string path, IdentityReference id, FileSystemRights access, InheritanceFlags inheritanceFlags,
        AccessControlType accessControlType)
    {
        var dirInfo = new DirectoryInfo(path);
        var securityDesc = dirInfo.GetAccessControl();

        securityDesc.AddAccessRule(
            new FileSystemAccessRule(id, access, inheritanceFlags, PropagationFlags.None, accessControlType));

        dirInfo.SetAccessControl(securityDesc);
    }
}
