using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Chat;
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

        private bool drawActors = false;
        private float maxActorDrawDistance = 20;

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
            ImGui.Combo("Data kind", ref this.currentKind, new[] {"ServerOpCode", "ContentFinderCondition", "Actor Table", "Font Test", "Party List"},
                        5);

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
                        if (this.dalamud.ClientState.Actors.Length == 0) {
                            ImGui.TextUnformatted("Data not ready.");
                        } else if (this.dalamud.ClientState.LocalPlayer == null) {
                            ImGui.TextUnformatted("LocalPlayer null.");
                        } else {
                            stateString += $"FrameworkBase: {this.dalamud.Framework.Address.BaseAddress.ToInt64():X}\n";

                            stateString += $"ActorTableLen: {this.dalamud.ClientState.Actors.Length}\n";
                            stateString += $"LocalPlayerName: {this.dalamud.ClientState.LocalPlayer.Name}\n";
                            stateString += $"CurrentWorldName: {this.dalamud.ClientState.LocalPlayer.CurrentWorld.GameData.Name}\n";
                            stateString += $"HomeWorldName: {this.dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name}\n";
                            stateString += $"LocalCID: {this.dalamud.ClientState.LocalContentId:X}\n";
                            stateString += $"LastLinkedItem: {this.dalamud.Framework.Gui.Chat.LastLinkedItemId.ToString()}\n";
                            stateString += $"TerritoryType: {this.dalamud.ClientState.TerritoryType}\n\n";

                            ImGui.Checkbox("Draw actors on screen", ref this.drawActors);
                            ImGui.SliderFloat("Draw Distance", ref this.maxActorDrawDistance, 2f, 40f);

                            for (var i = 0; i < this.dalamud.ClientState.Actors.Length; i++) {
                                var actor = this.dalamud.ClientState.Actors[i];

                                if (actor == null) 
                                    continue;

                                stateString +=
                                    $"{actor.Address.ToInt64():X}:{actor.ActorId:X}[{i}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX}\n";

                                if (actor is Npc npc)
                                    stateString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

                                if (actor is Chara chara)
                                    stateString +=
                                        $"       Level: {chara.Level} ClassJob: {chara.ClassJob.GameData.Name} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n";

                                if (actor is PlayerCharacter pc)
                                    stateString +=
                                        $"       HomeWorld: {pc.HomeWorld.GameData.Name} CurrentWorld: {pc.CurrentWorld.GameData.Name} FC: {pc.CompanyTag}\n";

                                if (this.drawActors && this.dalamud.Framework.Gui.WorldToScreen(actor.Position, out var screenCoords)) {
                                    ImGui.PushID("ActorWindow" + i);
                                    ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));

                                    if (actor.YalmDistanceX > this.maxActorDrawDistance)
                                        continue;

                                    ImGui.SetNextWindowBgAlpha(Math.Max(1f - (actor.YalmDistanceX / this.maxActorDrawDistance), 0.2f));
                                    if (ImGui.Begin("Actor" + i,
                                                    ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
                                                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoMouseInputs |
                                                    ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav)) {
                                        ImGui.Text($"{actor.Address.ToInt64():X}:{actor.ActorId:X}[{i}] - {actor.ObjectKind} - {actor.Name}");
                                        ImGui.End();
                                    }
                                    ImGui.PopID();
                                }
                            }
                        }

                        ImGui.TextUnformatted(stateString);
                    }
                        break;
                    case 3:
                        var specialChars = string.Empty;
                        for (var i = 0xE020; i <= 0xE0DB; i++) {
                            specialChars += $"0x{i:X} - {(SeIconChar) i} - {(char) i}\n";
                        }

                        ImGui.TextUnformatted(specialChars);
                        break;
                    case 4:
                        var partyString = string.Empty;

                        if (this.dalamud.ClientState.PartyList.Length == 0) {
                            ImGui.TextUnformatted("Data not ready.");
                        } else {

                            partyString += $"{this.dalamud.ClientState.PartyList.Count} Members\n";
                            for (var i = 0; i < this.dalamud.ClientState.PartyList.Count; i++) {
                                var member = this.dalamud.ClientState.PartyList[i];
                                if (member == null) {
                                    partyString +=
                                        $"[{i}] was null\n";
                                    continue;
                                }

                                partyString +=
                                    $"[{i}] {member.CharacterName} - {member.ObjectKind} - {member.Actor.ActorId}\n";
                            }

                            ImGui.TextUnformatted(partyString);
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
