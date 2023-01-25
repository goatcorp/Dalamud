using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using Windows.Win32.Security;
using Dalamud.Broker.Win32;
using Serilog;

namespace Dalamud.Broker.Commands;

internal static partial class SetupCommand
{
    public static void Run(SetupCommandOptions options)
    {
        // Relevant DIP: https://github.com/goatcorp/DIPs/pull/59
        //
        // NOTE:
        // Since the retail launcher launches the updater as Administrator (and thus XIVLauncher.PatchInstaller as well)
        // regular users usually have no ways to change the game files' DACL (You need `WRITE_DAC` right or certain privileges
        // to effectively ignore ACL). This is somewhat problematic as the sandboxed ffxiv need at least r-x access to
        // launch the game!
        //
        // To exacerbate the problem, programs running inside AppContainer usually have low integrity level by default.
        // This means the sandboxed game can't write on configs or data already created by the game without this feature,
        // creating a compatability problem.
        //
        // So the goal of this command is to reconcile these problems. It's intend to be used as an RunAfterInstall-esque
        // setup. It sets appropriate ACL/integrity level on files and directories. Running this command as Administrator
        // is recommended. (Unless you already changed DACLs by hand ofc...)
        //
        // Current policy on the filesystem is as follows:
        // (Check DIP#59 if you want reasoning)
        // +--------------------------------------------------------------+
        // | path                          | access | outcome | il        |
        // +--------------------------------------------------------------+
        // | $base_game                    | r-x    | allow   | unchanged |
        // | $game_config                  | rwx    | allow   | low       |
        // | $game_config/downloads        | -w-    | deny    | medium    | 
        // | $xl                           | rwx    | allow   | low       |
        // | $xl/{addon, runtime, patches} | -w-    | deny    | medium    |
        // | $dalamud                      | r-x    | allow   | unchanged |
        // +--------------------------------------------------------------+

        // Create an AppContainer and get its security identifier(SID).
        using var appContainer = AppContainerHelper.CreateContainer();
        var containerSid = appContainer.ToIdentityReference();

        // Initialize policies (see above)
        var policies = new PolicyEntry[]
        {
            // $base_game
            new()
            {
                Path = options.BaseGameDirectory,
                Access = FileSystemRights.ReadAndExecute,
                Outcome = AccessControlType.Allow,
                Integrity = null,
            },

            // $game_config
            new()
            {
                Path = options.GameConfigDirectory,
                Access = FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.ExecuteFile,
                Outcome = AccessControlType.Allow,
                Integrity = WELL_KNOWN_SID_TYPE.WinLowLabelSid,
            },

            // $game_config/downloads
            new()
            {
                Path = Path.Combine(options.GameConfigDirectory, "downloads"),
                Access = FileSystemRights.Write,
                Outcome = AccessControlType.Deny,
                Integrity = WELL_KNOWN_SID_TYPE.WinMediumLabelSid
            },

            // $xl
            new()
            {
                Path = options.XlDataDirectory,
                Access = FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.ExecuteFile,
                Outcome = AccessControlType.Allow,
                Integrity = WELL_KNOWN_SID_TYPE.WinLowLabelSid,
            },

            // $xl/addon
            new()
            {
                Path = Path.Combine(options.XlDataDirectory, "addon"),
                Access = FileSystemRights.Write,
                Outcome = AccessControlType.Deny,
                Integrity = WELL_KNOWN_SID_TYPE.WinMediumLabelSid
            },

            // $xl/runtime
            new()
            {
                Path = Path.Combine(options.XlDataDirectory, "runtime"),
                Access = FileSystemRights.Write,
                Outcome = AccessControlType.Deny,
                Integrity = WELL_KNOWN_SID_TYPE.WinMediumLabelSid
            },

            // $xl/patches
            new()
            {
                Path = Path.Combine(options.XlDataDirectory, "patches"),
                Access = FileSystemRights.Write,
                Outcome = AccessControlType.Deny,
                Integrity = WELL_KNOWN_SID_TYPE.WinMediumLabelSid
            },

            // $dalamud
            new()
            {
                Path = options.DalamudBinaryDirectory,
                Access = FileSystemRights.ReadAndExecute,
                Outcome = AccessControlType.Allow,
                Integrity = null,
            },
        };
        
        // Apply policies
        Log.Information("Changing the file system permissions for the AppContainer {ContainerSid}", containerSid);
        
        foreach (var policy in policies)
        {
            // Update DACL
            Log.Information(@"Changing the DACL for ""{Path}"" ({Outcome}: {Access})", policy.Path,
                            policy.Outcome, policy.Access);
            FileSystemAclHelper.AddDirectoryAce(
                policy.Path,
                containerSid,
                policy.Access,
                InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                policy.Outcome
            );

            // Update IL
            if (policy.Integrity is WELL_KNOWN_SID_TYPE wellKnownSid)
            {
                Log.Information(@"Changing the integrity level for ""{Path}"" to {WellKnownSid}", policy.Path,
                                policy.Integrity);
                FileSystemAclHelper.SetIntegrityLevel(
                    policy.Path,
                    wellKnownSid,
                    ACE_FLAGS.OBJECT_INHERIT_ACE | ACE_FLAGS.CONTAINER_INHERIT_ACE
                );
            }
        }

        Log.Information("Successfully updated the file system permissions for the AppContainer {ContainerSid}",
                        containerSid);
    }
}
