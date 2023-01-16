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
        // https://github.com/goatcorp/DIPs/pull/59

        var gameConfigDirectory = "";

        using var appContainer = AppContainerHelper.GetContainer();
        var containerSid = appContainer.GetIdentityReference();

        // $base_game
        FileSystemAclHelper.AddDirectoryAce(options.BaseGameDirectory, containerSid, FileSystemRights.ReadAndExecute,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Allow);

        // $dalamud
        FileSystemAclHelper.AddDirectoryAce(options.DalamudBinaryDirectory, containerSid,
                                            FileSystemRights.ReadAndExecute,
                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                            AccessControlType.Allow);

        // $game_config
        FileSystemAclHelper.SetIntegrityLevel(
            @"Casdf\FINAL FANTASY XIV - A Realm Reborn",
            WELL_KNOWN_SID_TYPE.WinLowLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);
        
        // $game_config/downloads
        FileSystemAclHelper.SetIntegrityLevel(
            @"asdfs\FINAL FANTASY XIV - A Realm Reborn\downloads",
            WELL_KNOWN_SID_TYPE.WinMediumLabelSid,
            ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE);

        // $xl
        // TODO

        // TODO: other fix ups...
    }
}
