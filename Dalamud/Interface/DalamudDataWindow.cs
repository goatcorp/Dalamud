using System.Numerics;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using ImGuiNET;
using Newtonsoft.Json;

namespace Dalamud.Interface
{
    class DalamudDataWindow {
        private Dalamud dalamud;

        private bool wasReady;
        private string serverOpString;
        private string cfcString = "N/A";

        private int currentKind;

        public DalamudDataWindow(Dalamud dalamud) {
            this.dalamud = dalamud;

            Load();
        }

        private void Load() {
            if (this.dalamud.Data.IsDataReady)
            {
                this.serverOpString = JsonConvert.SerializeObject(this.dalamud.Data.ServerOpCodes, Formatting.Indented);
                this.wasReady = true;
            }
        }

        public bool Draw() {
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);

            var isOpen = true;

            if (!ImGui.Begin("Dalamud Data", ref isOpen, ImGuiWindowFlags.NoCollapse)) {
                ImGui.End();
                return false;
            }

            // Main window
            if (ImGui.Button("Force Reload"))
                Load();
            ImGui.SameLine();
            var copy = ImGui.Button("Copy all");
            ImGui.SameLine();
            ImGui.Combo("Data kind", ref this.currentKind, new[] {"ServerOpCode", "ContentFinderCondition", "State"},
                        3);

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (copy)
                ImGui.LogToClipboard();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            if (this.wasReady)
                switch (this.currentKind) {
                    case 0:
                        ImGui.TextUnformatted(this.serverOpString);
                        break;
                    case 1:
                        ImGui.TextUnformatted(this.cfcString);
                        break;
                    case 2: {
                        var stateString = string.Empty;
                        // LocalPlayer is null in a number of situations (at least with the current visible-actors list)
                        // which would crash here.
                        if (this.dalamud.ClientState.Actors.Length == 0 || this.dalamud.ClientState.LocalPlayer == null)
                        {
                            ImGui.TextUnformatted("Data not ready.");
                        }
                        else
                        {
                            stateString += $"FrameworkBase: {this.dalamud.Framework.Address.BaseAddress.ToInt64():X}\n";

                            stateString += $"ActorTableLen: {this.dalamud.ClientState.Actors.Length}\n";
                            stateString += $"LocalPlayerName: {this.dalamud.ClientState.LocalPlayer.Name}\n";
                            stateString += $"CurrentWorldName: {this.dalamud.ClientState.LocalPlayer.CurrentWorld.GameData.Name}\n";
                            stateString += $"HomeWorldName: {this.dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name}\n";
                            stateString += $"LocalCID: {this.dalamud.ClientState.LocalContentId:X}\n";
                            stateString += $"LastLinkedItem: {this.dalamud.Framework.Gui.Chat.LastLinkedItemId.ToString()}\n";
                            stateString += $"TerritoryType: {this.dalamud.ClientState.TerritoryType}\n";

                            for (var i = 0; i < this.dalamud.ClientState.Actors.Length; i++) {
                                var actor = this.dalamud.ClientState.Actors[i];

                                stateString +=
                                    $"   -> {i} - {actor.Name} - {actor.Position.X} {actor.Position.Y} {actor.Position.Z}\n";

                                if (actor is Npc npc)
                                    stateString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

                                if (actor is Chara chara)
                                    stateString +=
                                        $"       Level: {chara.Level} ClassJob: {chara.ClassJob.GameData.Name} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n";

                                if (actor is PlayerCharacter pc)
                                    stateString +=
                                        $"       HomeWorld: {pc.HomeWorld.GameData.Name} CurrentWorld: {pc.CurrentWorld.GameData.Name} FC: {pc.CompanyTag}\n";
                            }
                        }

                        ImGui.TextUnformatted(stateString);
                    }
                        break;
                }
            else
                ImGui.TextUnformatted("Data not ready.");

            ImGui.PopStyleVar();

            ImGui.EndChild();
            ImGui.End();

            return isOpen;
        }
    }
}
