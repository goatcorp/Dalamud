using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Fools.Helper.YesHealMe;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using ImGuiNET;
using NoTankYou.System;

namespace Dalamud.Fools.Plugins;

public static class YesHealMePluginWindow
{
    private const string WindowName = "##foolsYesHealMeBanner";
    private const string WarningText = "HEAL ME";
    private const int Length = 300;
    private const int SectionHeight = 100;
    private const float Scale = 1f;
    private static readonly Vector2 Position = new(200, 200);
    private static readonly Vector2 Size = new(Length, SectionHeight);

    private static IEnumerable<PlayerCharacter> Characters(PartyListAddon partyListAddon)
    {
            return partyListAddon.Any() ? partyListAddon.Select(pla => pla.PlayerCharacter) : new[] { Service<ClientState>.Get().LocalPlayer };
    }

    private static List<PlayerCharacter> HurtingCharacters(IEnumerable<PlayerCharacter> characters)
    {
        return characters
               .Where(pc => pc.CurrentHp < pc.MaxHp ||
                            Service<DalamudInterface>.Get()
                                                     .IsDevMenuOpen)
               .ToList();
    }

    public static void Draw(PartyListAddon partyListAddon, FontManager fontManager, ref int iconId)
    {
        ImGui.SetNextWindowPos(Position, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(Size);
        ImGui.SetNextWindowSizeConstraints(Size, Size);
        ImGui.Begin(WindowName, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground);
        var playersDrawn = 0;
        var hurtingCharacters = HurtingCharacters(Characters(partyListAddon));
        if (hurtingCharacters.Count > 0)
        {
            var windowPos = ImGui.GetCursorScreenPos();
            foreach (var hurtingCharacter in hurtingCharacters)
            {
                var position = windowPos + new Vector2(0, playersDrawn * SectionHeight);
                var healMeTextSize = DrawUtilities.CalculateTextSize(fontManager, WarningText, Scale);
                var healMePosition = position with
                {
                    X = position.X + healMeTextSize.X,
                };
                DrawHealMeText(fontManager, position);
                DrawPlayerName(fontManager, hurtingCharacter, healMePosition);
                DrawIcon(fontManager, healMePosition);
                playersDrawn += 1;
            }
        }

        ImGui.End();
    }

    private static void DrawIcon(FontManager fontManager, Vector2 healMePosition)
    {
        DrawUtilities.DrawIconWithName(fontManager, healMePosition, 62582, "pls? owo", 1, 1);
    }

    private static void DrawHealMeText(FontManager fontManager, Vector2 position)
    {
        // HEAL ME text
        DrawUtilities.TextOutlined(fontManager, position, WarningText, 1, Colors.White);
    }

    private static void DrawPlayerName(
        FontManager fontManager, PlayerCharacter hurtingCharacter, Vector2 healMePosition)
    {
        var textSize = DrawUtilities.CalculateTextSize(fontManager, hurtingCharacter.Name.TextValue, Scale);
        var namePosition = new Vector2
        {
            X = healMePosition.X - textSize.X / 2.0f,
            Y = healMePosition.Y + textSize.Y,
        };
        DrawUtilities.TextOutlined(
            fontManager,
            namePosition,
            hurtingCharacter.Name.TextValue,
            Scale / 2f,
            Colors.White);
    }
}
