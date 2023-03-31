using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Fools.Helper;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Logging.Internal;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Fools.Plugins;

// The server for this one is open-source and available at: https://github.com/avafloww/HeyDalamud

public class HeyDalamudPlugin : IFoolsPlugin
{
    public enum DetectionState
    {
        Hotword,
        Action
    }

    private const uint StartSoundId = 23;
    private const uint ErrorSoundId = 24;
    private const uint StopSoundId = 31;

    private const string ApiEndpoint = "https://allagan-voice-terminal.goat.place";

    private static readonly string PluginName = "Hey, Dalamud!";
    private static readonly ModuleLog Log = new("FOOLS-HD");

    private readonly CancellationTokenSource noComprehendTaskCancellationTokenSource = new();

    private Grammar actionGrammar;

    private readonly ClientState clientState;
    private readonly DataManager dataManager;
    private readonly CommandManager commandManager;

    private readonly CultureInfo definedCulture = CultureInfo.GetCultureInfo("en-US");

    private Grammar heyDalamudGrammar;

    private SpeechRecognitionEngine recognizer;

    private DetectionState state;
    private readonly SpeechSynthesizer synthesizer = new();

    public HeyDalamudPlugin()
    {
        this.clientState = Service<ClientState>.Get();
        this.dataManager = Service<DataManager>.Get();
        this.commandManager = Service<CommandManager>.Get();

        try
        {
            Thread.CurrentThread.CurrentCulture = this.definedCulture;
            this.SetupSpeech();

            Log.Information("Voice recognition initialized");
            Chat.Print(PluginName, "Activated",
                       "Hi, welcome to \"Hey, Dalamud!\". I use the most advanced Allagan Voice Recognition Technology (AVRT) to process your commands. Say \"Hey, Dalamud!\" to get started!");
        }
        catch (Exception ex)
        {
            Chat.Print(PluginName, "Error",
                       "Could not start voice recognition. Please make sure that you have the American English Windows Language Pack installed.");
            Log.Error(ex, "Could not init voice recognition");
        }
    }

    public void Dispose()
    {
        this.synthesizer.Dispose();
        this.recognizer.RecognizeAsyncStop();
        this.recognizer.Dispose();
    }

    [DllImport("winmm.dll")]
    public static extern int waveInGetNumDevs();

    private void SetupSpeech()
    {
        this.state = DetectionState.Hotword;

        this.recognizer?.RecognizeAsyncStop();
        this.recognizer?.Dispose();

        this.synthesizer.SetOutputToDefaultAudioDevice();

        this.recognizer = new SpeechRecognitionEngine(this.definedCulture);

        var numDevs = waveInGetNumDevs();
        Log.Information("[REC] NumDevs: {0}", numDevs);

        var heyDalamudBuilder = new GrammarBuilder("hey dalamud");
        heyDalamudBuilder.Culture = this.definedCulture;

        this.heyDalamudGrammar = new Grammar(heyDalamudBuilder);
        this.heyDalamudGrammar.Name = "heyDalamudGrammar";

        var actionBuilder = new GrammarBuilder();
        actionBuilder.Culture = this.definedCulture;
        actionBuilder.AppendDictation();

        this.actionGrammar = new Grammar(actionBuilder)
        {
            Name = "actionGrammar"
        };

        // Create and load a dictation grammar.
        this.recognizer.LoadGrammar(this.heyDalamudGrammar);

        // Add a handler for the speech recognized event.  
        this.recognizer.SpeechRecognized += this.recognizer_SpeechRecognized;

        // Configure input to the speech recognizer.  
        this.recognizer.SetInputToDefaultAudioDevice();

        // Start asynchronous, continuous speech recognition.  
        this.recognizer.RecognizeAsync(RecognizeMode.Multiple);
    }

    private void SwitchToActionMode()
    {
        Log.Information("SwitchToActionMode");
        Chat.Print(PluginName, "Listening", "Allagan Voice Recognition Technology is now active.");
        UIModule.PlaySound(StartSoundId, 0, 0, 0);

        this.recognizer.RecognizeAsyncStop();
        this.state = DetectionState.Action;

        this.recognizer.UnloadAllGrammars();
        this.recognizer.LoadGrammar(this.actionGrammar);

        this.recognizer.SetInputToDefaultAudioDevice();
        this.recognizer.RecognizeAsync(RecognizeMode.Single);

        Task.Run(
            () =>
            {
                Thread.Sleep(9000);

                if (this.state != DetectionState.Action) return;
                UIModule.PlaySound(ErrorSoundId, 0, 0, 0);

                this.synthesizer.SpeakAsync("Sorry, I didn't quite get that, please try again.");
                this.recognizer.RecognizeAsyncStop();

                this.recognizer.UnloadAllGrammars();
                this.recognizer.LoadGrammar(this.heyDalamudGrammar);

                this.state = DetectionState.Hotword;
                this.recognizer.SetInputToDefaultAudioDevice();
                this.recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }, this.noComprehendTaskCancellationTokenSource.Token
        );
    }

    private async void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        Log.Information("[REC] In mode: {0} Recognized text: {1}", this.state, e.Result.Text);

        try
        {
            switch (this.state)
            {
                case DetectionState.Hotword:
                    this.SwitchToActionMode();
                    break;

                case DetectionState.Action:
                    this.state = DetectionState.Hotword;

                    try
                    {
                        Chat.Print(PluginName, "Recognized", e.Result.Text);
                        this.noComprehendTaskCancellationTokenSource.Cancel();

                        this.recognizer.RecognizeAsyncStop();

                        QueryRequestPayload payload;
                        if (this.clientState.LocalPlayer != null)
                        {
                            var territoryType = this.dataManager.GetExcelSheet<TerritoryType>()!
                                                    .GetRow(this.clientState.TerritoryType);

                            var activeDuty = territoryType?.ContentFinderCondition.Value?.Name.RawString;
                            var activeArea = territoryType?.PlaceName.Value?.Name.RawString;

                            payload = new QueryRequestPayload
                            {
                                IsInGame = true,
                                Query = e.Result.Text,
                                CharacterFirstName = this.clientState.LocalPlayer?.Name.TextValue.Split(' ')[0],
                                ActiveAreaName = string.IsNullOrEmpty(activeArea) ? null : activeArea,
                                ActiveDutyName = string.IsNullOrEmpty(activeDuty) ? null : activeDuty,
                            };
                        }
                        else
                        {
                            payload = new QueryRequestPayload
                            {
                                IsInGame = false,
                                Query = e.Result.Text,
                            };
                        }

                        UIModule.PlaySound(StopSoundId, 0, 0, 0);

                        // make the request
                        var json = JsonConvert.SerializeObject(payload);
                        Log.Debug("[REC] Sending request: {0}", json);
                        var response = await Util.HttpClient.PostAsync(
                                           $"{ApiEndpoint}/Query",
                                           new StringContent(json, Encoding.UTF8, "application/json")
                                       );
                        var responseData = await response.Content.ReadAsStringAsync();
                        Log.Debug("[REC] Got response: {0}", responseData);

                        var responseObject = JsonConvert.DeserializeObject<QueryResponsePayload>(responseData);
                        if (!string.IsNullOrEmpty(responseObject.Response))
                        {
                            Chat.Print(PluginName, "Response", responseObject.Response);
                            this.synthesizer.SpeakAsync(responseObject.Response);
                        }

                        if (!string.IsNullOrEmpty(responseObject.Command))
                        {
                            Log.Information("[REC] Executing command: {0}", responseObject.Command);
                            //this.commandManager.ProcessCommand(responseObject.Command);
                        }
                    } finally
                    {
                        this.recognizer.UnloadGrammar(this.actionGrammar);
                        this.recognizer.LoadGrammar(this.heyDalamudGrammar);

                        this.recognizer.SetInputToDefaultAudioDevice();
                        this.recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    }
                    
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in voice handling");
            Chat.Print(PluginName, "Error", "Could not handle your voice input. Please try again.");
        }
    }

    private class QueryRequestPayload
    {
        public string Query { get; set; }

        public bool IsInGame { get; set; }
        public string? CharacterFirstName { get; set; }
        public string? ActiveDutyName { get; set; }
        public string? ActiveAreaName { get; set; }
    }

    private class QueryResponsePayload
    {
        public bool Success { get; set; }
        public string Response { get; set; }
        public string? Command { get; set; }
    }
}
