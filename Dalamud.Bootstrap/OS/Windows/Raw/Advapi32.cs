using System;
using System.Runtime.InteropServices;

namespace Dalamud.Bootstrap.OS.Windows.Raw
{
    internal static unsafe class Advapi32
    {
        private const string Name = "Advapi32";

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InitializeSecurityDescriptor(out SECURITY_DESCRIPTOR pSecurityDescriptor, uint revision);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi)]
        public static extern uint SetEntriesInAclA(ulong cCountOfExplicitEntries, ref ACL oldAcl, out ACL* NewAcl);
        
        [DllImport(Name, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern void BuildExplicitAccessWithNameW(out EXPLICIT_ACCESS_W pExplicitAccess, string pTrusteeName, uint AccessPermissions, ACCESS_MODE AccessMode, uint Inheritance);
        
        [DllImport(Name, CallingConvention = CallingConvention.Winapi)]
        public static extern uint GetSecurityInfo(IntPtr handle, SE_OBJECT_TYPE ObjectType, SECURITY_INFORMATION SecurityInfo, SID** ppsidOwner, SID** ppsidGroup, ACL** ppDacl, ACL** ppSacl, SECURITY_DESCRIPTOR** ppSecurityDescriptor);
        
        [DllImport(Name, CallingConvention = CallingConvention.Winapi)]
        public static extern uint SetSecurityInfo(IntPtr handle, SE_OBJECT_TYPE _OBJECT_TYPE, SECURITY_INFORMATION SecurityInfo, SID* psidOwner, SID* psidGroup, ACL* pDacl, ACL* pSacl);
    }
}
