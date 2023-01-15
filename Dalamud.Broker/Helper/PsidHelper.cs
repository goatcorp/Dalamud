using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Dalamud.Broker.Helper;

internal static class PsidHelper
{
    public static string ConvertToString(this PSID psid)
    {
        unsafe
        {
            var psidStr = default(PWSTR);

            try
            {
                var ok = PInvoke.ConvertSidToStringSid(psid, out psidStr);
                if (!ok)
                    throw new Win32Exception();

                return new string(psidStr);
            } finally
            {
                if ((void*)psidStr != null)
                    PInvoke.LocalFree((IntPtr)psidStr.Value);
            }
        }
    }
}
