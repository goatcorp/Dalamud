using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Configuration.Internal;
using Dalamud.Fools.Plugins;
using Dalamud.Game.ClientState;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;

namespace Dalamud.Fools;

//               . ///*,.. .,*/((#######((((((((((((#(((########(*,****,,....,**///
//               . (//*,,.  ./(#%%%%%%%##((((/////(((((###(#####%#(/**,......,,**/(
//                (*((/*,,,/###%%%%%%%%%%%#((///((((##%%%%%%%%####%#(//,.  ..,,*/(*
//                 #((/*/(((((#%%%%&&%%%%%##(/(((((#%%%%&%%%%%%%##(((((/,...,,*/(#%
//              %,&%#######%%%%%%%%%%%%%%%%###(####%%%%%%&&&%%%%%%#((((((((/**/(#&%
//            ,,#&%%####%%%%%(//##%%%%%%%%%%#%%%%%%%%%%%%%%%%%%%%%%%%%###(####((#%#
//          ,.%&%%%%%%%%%#/.     *#%%%&&&&&&&&&%&&&%%%%%%%%%#*...*(##%%####((##%%%%
//        ..%&&%%%%%%%%#(*       ,%&&&&&&&&&&&&&&&&&&&%%%(/,   .    *(#%%#######%%%
//       /#&&&%%%%%%%%%%#,  *#&&&&&&&&&&&&&&&&&&&&&&&&&&%(,       ,  ,(####%%%%####
//       %&&&&%%%%&%%%&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&%*.        /#%%%%%#%%%%%%
//       (&&%%%%%&&&&&&&&&&&&&%%%%%%%%%%%%##%%&&&&&&&&&&&&&&%%%%###%%%%%%%%%%%%%###
//     *(@&%%%%%&&&&&%%%%%#(#############%%%%%%%&&&&&&&&&&&&&&&%%&%&&&%%%%%%%%%%%%%
//     */&&%%%#((//******/********/(((((((/*/(#%%&&&&&&&&&&&&&&&&&&&&&%%%%%%%%%%%%%
//     ((&%/,................,,,,,,,,*,,,,,*,,,,*(%%%%&&&&&&&&&&&&&&&&&%%%%%%%%%%%%
//     . ,                        ...............,,*(%%&&&&&&&&&&&&&&&&&&%%%%%%%%%%
//    .,                                        .,,,**/(#%%%&&&&&&&&&&&&&&%%%%%%%%%
//    .                                          ..,,,**/#%%%&&&&&&&&&&&&&&%%%%%%%%
//   ,                                               .,*/#%%%%&&&&&&&&&&&&&%%%%%%%%
//  ,,.                                                ./(#%%%&&&&&&&&%&&&%%##%%%%%
// ((%&#                                                ,(#%%%%&&&&&&&%%%&%%%%%%%%%
// #,%%/                                                .*#%%%%&&&&&&&&%&%%###%%%%%
// @(%%(                                               .,(%%%%%%%&&&&&&&%%#%%%%%%%%
//  /%%%*                                              ./#%%%%%%%%%&&&&&%##%%%%%%%(
// #*&%%#.                                          ..,(#%%%%%%%%%%&&&&&%%%%%%%#((,
//  *(%%#/.                                       .,**(#####%%%%%%%%%%%%%%%%%#**..
// #((#%##/.                                      ,,*(####%%%%%%%%%%%&&%%%#(,
//   **###((*.                                  ,(*///((####%%%%%%%&&&%(/.,
//   .,(###(//*,.                             ..,*//(((((##%%%%%%%%%%%%#%.
//     . (#((/**,,..                        ..,*//*/////(##%%%%%%%%%(#(*
//       */(((/////,..                  .....,,,,***///####%%%%%%%#.
//            //*((((*,..              ...,...,,,.,//((((##%%%%%%%,/
//                * //(/******,,,.,,..,,,****/*///((((#(###(/##(,
//
// April fools! <3
// This was a group effort in one week lead by:
// NotNite: FoolsManager, Pixel Imperfect, Screensaver
// Berna: DailyLifeDuty
// Chirp: Oops, Maybe Lalafells!

/// <summary>
/// Manager for all the IFoolsPlugin instances.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal class FoolsManager : IDisposable, IServiceType
{
    public readonly List<FoolsPluginMetadata> FoolsPlugins = new();
    public readonly Dictionary<string, IFoolsPlugin> ActivatedPlugins = new();

    private static readonly ModuleLog Log = new("FOOLS");

    private UiBuilder uiBuilder;
    private ClientState clientState;

    [ServiceManager.ServiceConstructor]
    private FoolsManager()
    {
        this.uiBuilder = new UiBuilder("fools");
        this.uiBuilder.Draw += this.DrawUi;

        this.clientState = Service<ClientState>.Get();
        this.clientState.Login += this.ClientStateOnLogin;

        // reflect over all IFoolsPlugin implementations sometime(?)
        this.FoolsPlugins = new List<FoolsPluginMetadata>
        {
            new("Pixel Imperfect", "PixelImperfectPlugin", "Whoops... we messed up the math on that one.", "Halpo",
                typeof(PixelImperfectPlugin)),
            new("DailyLifeDuty", "DailyLifeDutyPlugin", "Easily Track Daily and Weekly tasks... in real life", "MidoriKami", typeof(DailyLifeDutyPlugin)),
            new("Oops, Maybe Lalafells!", "OopsMaybeLalafellsPlugin", "Turn everyone into Lalafells? Maybe. We haven't quite tested it yet.", "Chrip", typeof(OopsMaybeLalafells)),
            new("Screensaver", "ScreensaverPlugin", "Prevent burn-in on loading screens.", "NotNite", typeof(ScreensaverPlugin)),
        };
    }


    public void ActivatePlugin(string plugin)
    {
        if (this.ActivatedPlugins.ContainsKey(plugin))
        {
            Log.Warning("Trying to activate plugin {0} that is already activated", plugin);
            return;
        }

        var pluginMetadata = this.FoolsPlugins.FirstOrDefault(x => x.InternalName == plugin);
        if (pluginMetadata == null)
        {
            Log.Warning("Trying to activate plugin {0} that does not exist", plugin);
            return;
        }

        var pluginInstance = (IFoolsPlugin)Activator.CreateInstance(pluginMetadata.Type);
        this.ActivatedPlugins.Add(plugin, pluginInstance);
    }

    public void Dispose()
    {
        foreach (var plugin in this.ActivatedPlugins.Values)
        {
            plugin.Dispose();
        }

        this.ActivatedPlugins.Clear();
        ((IDisposable)this.uiBuilder).Dispose();
        this.clientState.Login -= this.ClientStateOnLogin;
    }

    public bool IsPluginActivated(string plugin)
    {
        return this.ActivatedPlugins.ContainsKey(plugin);
    }

    public void DeactivatePlugin(string plugin)
    {
        if (!this.ActivatedPlugins.ContainsKey(plugin))
        {
            Log.Warning("Trying to deactivate plugin {0} that is not activated", plugin);
            return;
        }

        var pluginInstance = this.ActivatedPlugins[plugin];
        pluginInstance.Dispose();
        this.ActivatedPlugins.Remove(plugin);
    }

    private void DrawUi()
    {
        foreach (var plugin in this.ActivatedPlugins.Values)
        {
            plugin.DrawUi();
        }
    }

    private void ClientStateOnLogin(object? o, EventArgs e)
    {
        var dalamudConfig = Service<DalamudConfiguration>.Get();

#if !DEBUG
        if (DateTime.Now is not { Month: 4, Day: 1 })
        {
            return;
        }
#endif

        if (dalamudConfig.HasSeenFools23)
        {
            return;
        }

        var di = Service<DalamudInterface>.Get();
        di.OpenFoolsWindow();

        dalamudConfig.HasSeenFools23 = true;
        dalamudConfig.QueueSave();
    }
}
