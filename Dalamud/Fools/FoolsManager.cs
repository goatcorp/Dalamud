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
            new()
            {
                Name = "Pixel Imperfect",
                InternalName = "PixelImperfectPlugin",
                Description = "Whoops... we messed up the math on that one.",
                Author = "Halpo",
                RealAuthor = "NotNite",
                Type = typeof(PixelImperfectPlugin),
            },
            new()
            {
                Name = "DailyLifeDuty",
                InternalName = "DailyLifeDutyPlugin",
                Description = "We were just informed there are these things called \"chores\" outside the game. No worries, though, we can track them!",
                Author = "MidoriKami",
                RealAuthor = "Berna",
                Type = typeof(DailyLifeDutyPlugin),
            },
            new()
            {
                Name = "Oops, Maybe Lalafells?",
                InternalName = "OopsMaybeLalafellsPlugin",
                Description = "Turn everyone into Lalafells? Maybe. We haven't quite tested it yet.",
                Author = "Chirpopo Chirpo",
                RealAuthor = "Chirp",
                Type = typeof(OopsMaybeLalafells),
            },
            new()
            {
                Name = "Screensaver",
                InternalName = "ScreensaverPlugin",
                Description = "Prevent burn-in on loading screens.",
                Author = "NotNite",
                RealAuthor = "NotNite",
                Type = typeof(ScreensaverPlugin),
            },
            new()
            {
                Name = "Cat Bubbles",
                InternalName = "CatBubblesPlugin",
                Description = "Enables in-game sdfgasdfgkljewriogdfkjghahfvcxbnmlqpwoeiruty",
                Author = "Chirp's Cat, Sir Fluffington III",
                RealAuthor = "Chirp",
                Type = typeof(CatBubblesPlugin),
            },
            /*
            new()
            {
                Name = "YesSoliciting",
                InternalName = "YesSolicitingPlugin",
                Description = "Summon annoying shout messages from beyond the rift.",
                Author = "Anna",
                RealAuthor = "NotNite",
                Type = typeof(YesSolicitingPlugin),
            },
            */
            new()
            {
                Name = "GoodVibes",
                InternalName = "GoodVibesPlugin",
                Description = "Shake things up with this vibe plugin!",
                Author = "C h i r p",
                RealAuthor = "Chirp",
                Type = typeof(GoodVibesPlugin),
            },
            new()
            {
                Name = "YesHealMe",
                InternalName = "YesHealMePlugin",
                Description = "As the saying goes: it's the first missing HP that matters. And the second. And the third...",
                Author = "MidoriKami",
                RealAuthor = "Berna",
                Type = typeof(YesHealMePlugin),
            },
            new()
            {
                Name = "Complicated Tweaks",
                InternalName = "ComplicatedTweaksPlugin",
                Description = "As complicated as it gets!",
                Author = "Caraxi",
                RealAuthor = "NotNite",
                Type = typeof(ComplicatedTweaksPlugin),
            },
            new()
            {
                Name = "Hey Dalamud!",
                InternalName = "HeyDalamudPlugin",
                Description = "Scientists have unearthed advanced Allagan Voice Recognition Technology from before the Calamity, then they used it in a Dalamud plugin. Was it a good idea? That's for you to decide.\nVoice recognition is performed locally, it only listens after \"Hey, Dalamud!\" is detected(a sound will play) and none of your prompts will be stored.",
                Author = "snake",
                RealAuthor = "Avaflow",
                Type = typeof(HeyDalamudPlugin),
            },
        };
    }

    public bool CheckIsApplicableAprilFoolsTime()
    {
        var now = DateTime.Now;
        return now is { Year: 2023, Month: 4, Day: 1 };
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

        /*
        var di = Service<DalamudInterface>.Get();
        di.OpenFoolsWindow();

        dalamudConfig.HasSeenFools23 = true;
        dalamudConfig.QueueSave();
        */
    }
}
