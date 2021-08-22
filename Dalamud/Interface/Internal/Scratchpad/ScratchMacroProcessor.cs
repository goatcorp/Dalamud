using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Dalamud.Logging;

namespace Dalamud.Interface.Internal.Scratchpad
{
    /// <summary>
    /// This class converts ScratchPad macros into runnable scripts.
    /// </summary>
    internal class ScratchMacroProcessor
    {
        private const string Template = @"

public class ScratchPlugin : IDalamudPlugin {

    public string Name => ""ScratchPlugin"";

    private readonly DalamudPluginInterface pi;
    private readonly BuddyList buddies;
    private readonly ChatGui chat;
    private readonly ChatHandlers chatHandlers;
    private readonly ClientState clientState;
    private readonly CommandManager commands;
    private readonly Condition condition;
    private readonly DataManager data;
    private readonly FateTable fates;
    private readonly FlyTextGui flyText;
    private readonly Framework framework;
    private readonly GameGui gameGui;
    private readonly GameNetwork gameNetwork;
    private readonly JobGauges gauges;
    private readonly KeyState keyState;
    private readonly LibcFunction libc;
    private readonly ObjectTable objects;
    private readonly PartyFinderGui pfGui;
    private readonly PartyList party;
    private readonly SeStringManager seStringManager;
    private readonly SigScanner sigScanner;
    private readonly TargetManager targets;
    private readonly ToastGui toasts;

    {SETUPBODY}

    public ScratchPlugin(
        DalamudPluginInterface pluginInterface,
        BuddyList buddies,
        ChatGui chat,
        ChatHandlers chatHandlers,
        ClientState clientState,
        CommandManager commands,
        Condition condition,
        DataManager data,
        FateTable fates,
        FlyTextGui flyText,
        Framework framework,
        GameGui gameGui,
        GameNetwork gameNetwork,
        JobGauges gauges,
        KeyState keyState,
        LibcFunction libcFunction,
        ObjectTable objects,
        PartyFinderGui pfGui,
        PartyList party,
        SeStringManager seStringManager,
        SigScanner sigScanner,
        TargetManager targets,
        ToastGui toasts)
    {
        this.pi = pluginInterface;

        this.buddies = buddies;
        this.chat = chat;
        this.chatHandlers = chatHandlers;
        this.clientState = clientState;
        this.commands = commands;
        this.condition = condition;
        this.data = data;
        this.fates = fates;
        this.flyText = flyText;
        this.framework = framework;
        this.gameGui = gameGui;
        this.gameNetwork = gameNetwork;
        this.gauges = gauges;
        this.keyState = keyState;
        this.libc = libcFunction;
        this.objects = objects;
        this.pfGui = pfGui;
        this.party = party;
        this.seStringManager = seStringManager;
        this.sigScanner = sigScanner;
        this.targets = targets;
        this.toasts = toasts;

        this.pi.UiBuilder.Draw += DrawUI;

        {INITBODY}
    }

    private void DrawUI()
    {
        {DRAWBODY}
    }

    {NONEBODY}

    public void Dispose()
    {
        this.pi.UiBuilder.Draw -= DrawUI;
        {DISPOSEBODY}
    }
}
";

        private enum ParseContext
        {
            None,
            Init,
            Draw,
            Hook,
            Dispose,
        }

        /// <summary>
        /// Process the given macro input and return a script.
        /// </summary>
        /// <param name="input">Input to process.</param>
        /// <returns>A runnable script.</returns>
        public string Process(string input)
        {
            var lines = input.Split(new[] { '\r', '\n' });

            var ctx = ParseContext.None;

            var setupBody = string.Empty;
            var noneBody = string.Empty;
            var initBody = string.Empty;
            var disposeBody = string.Empty;
            var drawBody = string.Empty;
            var tHook = new HookInfo();

            var hooks = new List<HookInfo>();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.StartsWith("INITIALIZE:"))
                {
                    ctx = ParseContext.Init;
                    continue;
                }

                if (line.StartsWith("DRAW:"))
                {
                    ctx = ParseContext.Draw;
                    continue;
                }

                if (line.StartsWith("DISPOSE:"))
                {
                    ctx = ParseContext.Dispose;
                    continue;
                }

                if (line.StartsWith("HOOK("))
                {
                    ctx = ParseContext.Hook;

                    var args = Regex.Match(line, "HOOK\\((.+)+\\):").Groups[0].Captures[0].Value
                                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x[0] == ' ' ? x[1..] : x).ToArray();

                    tHook.Sig = args[0].Replace("\"", string.Empty); // Split quotation marks if any
                    tHook.Sig = tHook.Sig.Replace("HOOK(", string.Empty);
                    tHook.RetType = args[1];
                    tHook.Arguments = string.Join(", ", args.Skip(2).ToArray()).Replace("):", string.Empty);

                    var invocationGroups = Regex.Matches(tHook.Arguments, "\\S+ ([a-zA-Z0-9]+),*").Cast<Match>()
                                           .Select(x => x.Groups[1].Value);
                    tHook.Invocation = string.Join(", ", invocationGroups);
                    continue;
                }

                if (line.StartsWith("END;"))
                {
                    switch (ctx)
                    {
                        case ParseContext.None:
                            throw new Exception("Not in a macro!!!");
                        case ParseContext.Init:
                            break;
                        case ParseContext.Draw:
                            break;
                        case ParseContext.Hook:
                            hooks.Add(tHook);
                            tHook = new HookInfo();
                            break;
                        case ParseContext.Dispose:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(paramName: nameof(input));
                    }

                    ctx = ParseContext.None;
                    continue;
                }

                switch (ctx)
                {
                    case ParseContext.None:
                        noneBody += line + "\n";
                        break;
                    case ParseContext.Init:
                        initBody += line + "\n";
                        break;
                    case ParseContext.Draw:
                        drawBody += line + "\n";
                        break;
                    case ParseContext.Hook:
                        tHook.Body += line + "\n";
                        break;
                    case ParseContext.Dispose:
                        disposeBody += line + "\n";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(paramName: nameof(input));
                }
            }

            var hookSetup = string.Empty;
            var hookInit = string.Empty;
            var hookDetour = string.Empty;
            var hookDispose = string.Empty;
            for (var i = 0; i < hooks.Count; i++)
            {
                var hook = hooks[i];

                hookSetup += $"private delegate {hook.RetType} Hook{i}Delegate({hook.Arguments});\n";
                hookSetup += $"private Hook<Hook{i}Delegate> hook{i}Inst;\n";

                hookInit += $"var addrH{i} = pi.TargetModuleScanner.ScanText(\"{hook.Sig}\");\n";
                hookInit += $"this.hook{i}Inst = new Hook<Hook{i}Delegate>(addrH{i}, new Hook{i}Delegate(Hook{i}Detour), this);\n";
                hookInit += $"this.hook{i}Inst.Enable();\n";

                var originalCall = $"this.hook{i}Inst.Original({hook.Invocation});\n";
                if (hook.RetType != "void")
                    originalCall = "return " + originalCall;

                if (hook.Body.Contains($"hook{i}Inst.Original(") || hook.Body.Contains("ORIG("))
                {
                    PluginLog.Warning($"Attention! A manual call to Original() in Hook #{i} was detected. Original calls will not be managed for you.");
                    originalCall = string.Empty;
                }

                if (hook.Body.Contains("ORIG("))
                {
                    PluginLog.Warning($"Normalizing Original() call in Hook #{i}.");
                    hook.Body = hook.Body.Replace("ORIG(", $"this.hook{i}Inst.Original(");
                }

                hookDetour +=
                    $"private {hook.RetType} Hook{i}Detour({hook.Arguments}) {{\n" +
                    (!string.IsNullOrEmpty(originalCall) ? "try {\n" : string.Empty) +
                    $"  {hook.Body}\n";

                if (!string.IsNullOrEmpty(originalCall))
                {
                    hookDetour += "} catch(Exception ex) {\n" +
                                  $"  PluginLog.Error(ex, \"Exception in Hook{i}Detour!!\");\n" +
                                  "}\n" +
                                  $"{originalCall}";
                }

                hookDetour += $"\n}}\n";

                hookDispose += $"this.hook{i}Inst.Dispose();\n";
            }

            setupBody += "\n" + hookSetup;
            initBody = hookInit + "\n" + initBody;
            noneBody += "\n" + hookDetour;
            disposeBody += "\n" + hookDispose;

            var output = Template;
            output = output.Replace("{SETUPBODY}", setupBody);
            output = output.Replace("{INITBODY}", initBody);
            output = output.Replace("{DRAWBODY}", drawBody);
            output = output.Replace("{NONEBODY}", noneBody);
            output = output.Replace("{DISPOSEBODY}", disposeBody);

            return output;
        }

        private class HookInfo
        {
            public string Body { get; set; }

            public string Arguments { get; set; }

            public string Invocation { get; set; }

            public string RetType { get; set; }

            public string Sig { get; set; }
        }
    }
}
