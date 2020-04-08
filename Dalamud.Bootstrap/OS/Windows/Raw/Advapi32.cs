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
    }
}
