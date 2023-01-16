using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using Windows.Win32.Security;
using Dalamud.Broker.Helper;
using Dalamud.Broker.Win32;

namespace Dalamud.Broker.Commands;

internal static class SetupCommand
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
        var containerSid = appContainer.GetIdentityReference();

        // Resolve paths
        // TODO: clean up this "optional arguments" + concat dir handling stuff..
        // TODO: also this code is extremely verbose... (but it's very unlikely to be changed so I don't think abstraction is not needed..? idk)
        var baseGameDirectory = options.BaseGameDirectory;
        var gameConfigDirectory = options.GameConfigDirectory ?? GetDefaultGameConfigDirectory();
        var gameConfigDownloadDirectory = Path.Combine(gameConfigDirectory, "downloads");
        var xlDirectory = options.XlDataDirectory ?? GetDefaultXlDirectory();
        var xlAddonDirectory = Path.Combine(xlDirectory, "addon");
        var xlRuntimeDirectory = Path.Combine(xlDirectory, "runtime");
        var xlPatchesDirectory = Path.Combine(xlDirectory, "patches");
        var dalamudDirectory = options.DalamudBinaryDirectory ?? GetDefaultDalamudDirectory();

        // $base_game
        FileSystemAclHelper.AddDirectoryAce(baseGameDirectory, containerSid, FileSystemRights.ReadAndExecute,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Allow);

        // $gmae_config
        FileSystemAclHelper.AddDirectoryAce(gameConfigDirectory, containerSid,
                                            FileSystemRights.Read | FileSystemRights.Write |
                                            FileSystemRights.ExecuteFile,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Allow);
        FileSystemAclHelper.SetIntegrityLevel(
            gameConfigDirectory,
            WELL_KNOWN_SID_TYPE.WinLowLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);

        // $game_config/downloads
        FileSystemAclHelper.AddDirectoryAce(gameConfigDirectory, containerSid,
                                            FileSystemRights.Write,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Deny);
        FileSystemAclHelper.SetIntegrityLevel(
            gameConfigDownloadDirectory,
            WELL_KNOWN_SID_TYPE.WinMediumLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);

        // $xl
        FileSystemAclHelper.AddDirectoryAce(xlDirectory, containerSid,
                                            FileSystemRights.Read | FileSystemRights.Write |
                                            FileSystemRights.ExecuteFile,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Allow);
        FileSystemAclHelper.SetIntegrityLevel(
            xlDirectory,
            WELL_KNOWN_SID_TYPE.WinLowLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);

        // $xl/{..}
        FileSystemAclHelper.AddDirectoryAce(xlAddonDirectory, containerSid,
                                            FileSystemRights.Write,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Deny);
        FileSystemAclHelper.SetIntegrityLevel(
            xlAddonDirectory,
            WELL_KNOWN_SID_TYPE.WinMediumLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);
        FileSystemAclHelper.AddDirectoryAce(xlRuntimeDirectory, containerSid,
                                            FileSystemRights.Write,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Deny);
        FileSystemAclHelper.SetIntegrityLevel(
            xlRuntimeDirectory,
            WELL_KNOWN_SID_TYPE.WinMediumLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);
        FileSystemAclHelper.AddDirectoryAce(xlPatchesDirectory, containerSid,
                                            FileSystemRights.Write,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Deny);
        FileSystemAclHelper.SetIntegrityLevel(
            xlPatchesDirectory,
            WELL_KNOWN_SID_TYPE.WinMediumLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);

        // $dalamud
        FileSystemAclHelper.AddDirectoryAce(dalamudDirectory, containerSid,
                                            FileSystemRights.ReadAndExecute,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Allow);
    }

    private static string GetDefaultGameConfigDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "My Games",
            "FINAL FANTASY XIV - A Realm Reborn"
        );
    }

    private static string GetDefaultXlDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher"
        );
    }

    private static string GetDefaultDalamudDirectory()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
        
        return assemblyDirectory ?? Environment.CurrentDirectory;
    }
}
