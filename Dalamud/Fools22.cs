using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using Serilog;

using Condition = Dalamud.Game.ClientState.Conditions.Condition;

namespace Dalamud;

public class Fools22 : IDisposable
{
    private readonly TextureWrap erDeathBgTexture;
    private readonly TextureWrap erNormalDeathTexture;
    private readonly TextureWrap erCraftFailedTexture;
    private readonly TextureWrap erEnemyFelledTexture;

    private readonly string synthesisFailsMessage;

    private readonly Stopwatch time = new Stopwatch();
    private readonly SoundPlayer player;

    private bool assetsReady = false;

    private AnimationState currentState = AnimationState.NotPlaying;
    private DeathType currentDeathType = DeathType.Death;

    private Easing alphaEasing;
    private Easing scaleEasing;

    private bool lastFrameUnconscious = false;

    private int msFadeInTime = 1000;
    private int msFadeOutTime = 2000;
    private int msWaitTime = 1600;

    private TextureWrap TextTexture => this.currentDeathType switch
    {
        DeathType.Death => this.erNormalDeathTexture,
        DeathType.CraftFailed => this.erCraftFailedTexture,
        DeathType.EnemyFelled => this.erEnemyFelledTexture,
    };

    private enum AnimationState
    {
        NotPlaying,
        FadeIn,
        Wait,
        FadeOut,
    }

    private enum DeathType
    {
        Death,
        CraftFailed,
        EnemyFelled,
    }

    public Fools22()
    {
        var dalamud = Service<Dalamud>.Get();
        var interfaceManager = Service<InterfaceManager>.Get();
        var framework = Service<Framework>.Get();
        var chatGui = Service<ChatGui>.Get();
        var dataMgr = Service<DataManager>.Get();
        var gameNetwork = Service<GameNetwork>.Get();

        this.erDeathBgTexture = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "fools22", "er_death_bg.png"))!;
        this.erNormalDeathTexture = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "fools22", "er_normal_death.png"))!;
        this.erCraftFailedTexture = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "fools22", "er_craft_failed.png"))!;
        this.erEnemyFelledTexture = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "fools22", "er_enemy_felled.png"))!;

        var soundBytes = File.ReadAllBytes(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "fools22", "snd_death_er.wav"));
        this.player = new SoundPlayer(new MemoryStream(soundBytes));

        if (this.erDeathBgTexture == null || this.erNormalDeathTexture == null || this.erCraftFailedTexture == null)
        {
            Log.Error("Fools22: Failed to load images");
            return;
        }

        this.synthesisFailsMessage = dataMgr.GetExcelSheet<LogMessage>()!.GetRow(1160)!.Text.ToDalamudString().TextValue;

        this.assetsReady = true;

        interfaceManager.Draw += this.Draw;
        framework.Update += this.FrameworkOnUpdate;
        chatGui.ChatMessage += this.ChatGuiOnChatMessage;
        gameNetwork.NetworkMessage += this.GameNetworkOnNetworkMessage;
    }

    private unsafe void GameNetworkOnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
    {
        if (opcode != 0x301)
            return;

        var cat = *(ushort*)(dataptr + 0x00);
        var updateType = *(uint*)(dataptr + 0x08);

        if (cat == 0x6D && updateType == 0x40000003)
        {
            Task.Delay(1000).ContinueWith(t =>
            {
                this.PlayAnimation(DeathType.EnemyFelled);
            });
        }
    }

    private void ChatGuiOnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        if (message.TextValue.Contains(this.synthesisFailsMessage))
        {
            this.PlayAnimation(DeathType.CraftFailed);
            Log.Information("Fools22: Craft failed");
        }
    }

    private void FrameworkOnUpdate(Framework framework)
    {
        var condition = Service<Condition>.Get();
        var isUnconscious = condition[ConditionFlag.Unconscious];

        if (isUnconscious && !this.lastFrameUnconscious)
        {
            this.PlayAnimation(DeathType.Death);
            Log.Information("Fools22: Player died");
        }

        this.lastFrameUnconscious = isUnconscious;
    }

    private void Draw()
    {
#if DEBUG
        if (ImGui.Begin("fools test"))
        {
            if (ImGui.Button("play death"))
            {
                this.PlayAnimation(DeathType.Death);
            }

            if (ImGui.Button("play craft failed"))
            {
                this.PlayAnimation(DeathType.CraftFailed);
            }

            if (ImGui.Button("play enemy felled"))
            {
                Task.Delay(1000).ContinueWith(t =>
                {
                    this.PlayAnimation(DeathType.EnemyFelled);
                });
            }

            ImGui.InputInt("fade in time", ref this.msFadeInTime);
            ImGui.InputInt("fade out time", ref this.msFadeOutTime);
            ImGui.InputInt("wait time", ref this.msWaitTime);

            ImGui.TextUnformatted("state: " + this.currentState);
            ImGui.TextUnformatted("time: " + this.time.ElapsedMilliseconds);

            ImGui.TextUnformatted("scale: " + this.scaleEasing?.EasedPoint.X);
        }

        ImGui.End();
#endif
        var vpSize = ImGuiHelpers.MainViewport.Size;

        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(vpSize.X, vpSize.Y), ImGuiCond.Always);
        ImGuiHelpers.ForceNextWindowMainViewport();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 0));

        if (ImGui.Begin("fools22", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus
                                   | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoScrollbar))
        {
            if (this.currentState != AnimationState.NotPlaying)
            {
                this.alphaEasing?.Update();
                this.scaleEasing?.Update();
            }

            switch (this.currentState)
            {
                case AnimationState.FadeIn:
                    this.FadeIn(vpSize);
                    break;
                case AnimationState.Wait:
                    this.Wait(vpSize);
                    break;
                case AnimationState.FadeOut:
                    this.FadeOut(vpSize);
                    break;
            }
        }

        ImGui.End();

        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar(3);
    }

    private static void AdjustCursorAndDraw(Vector2 vpSize, TextureWrap tex, float scale = 1.0f)
    {
        var width = vpSize.X;
        var height = tex.Height / (float)tex.Width * width;

        if (height < vpSize.Y)
        {
            height = vpSize.Y;
            width = tex.Width / (float)tex.Height * height;
            ImGui.SetCursorPosX((vpSize.X - width) / 2);
        }
        else
        {
            ImGui.SetCursorPosY((vpSize.Y - height) / 2);
        }

        var scaledSize = new Vector2(width, height) * scale;
        var difference = scaledSize - vpSize;

        var cursor = ImGui.GetCursorPos();
        ImGui.SetCursorPos(cursor - (difference / 2));

        ImGui.Image(tex.ImGuiHandle, scaledSize);
    }

    private void FadeIn(Vector2 vpSize)
    {
        if (this.time.ElapsedMilliseconds > this.msFadeInTime)
            this.currentState = AnimationState.Wait;

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, (float)this.alphaEasing.Value);

        AdjustCursorAndDraw(vpSize, this.erDeathBgTexture);
        AdjustCursorAndDraw(vpSize, this.TextTexture, this.scaleEasing.EasedPoint.X);

        ImGui.PopStyleVar();
    }

    private void Wait(Vector2 vpSize)
    {
        if (this.time.ElapsedMilliseconds > this.msFadeInTime + this.msWaitTime)
        {
            this.currentState = AnimationState.FadeOut;
            this.alphaEasing = new InOutCubic(TimeSpan.FromMilliseconds(this.msFadeOutTime));
            this.alphaEasing.Start();
        }

        AdjustCursorAndDraw(vpSize, this.erDeathBgTexture);
        AdjustCursorAndDraw(vpSize, this.TextTexture, this.scaleEasing.EasedPoint.X);
    }

    private void FadeOut(Vector2 vpSize)
    {
        if (this.time.ElapsedMilliseconds > this.msFadeInTime + this.msWaitTime + this.msFadeOutTime)
        {
            this.currentState = AnimationState.NotPlaying;
            this.time.Stop();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1 - (float)this.alphaEasing.Value);

        AdjustCursorAndDraw(vpSize, this.erDeathBgTexture);
        AdjustCursorAndDraw(vpSize, this.TextTexture, this.scaleEasing.EasedPoint.X);

        ImGui.PopStyleVar();
    }

    private void PlayAnimation(DeathType type)
    {
        if (!this.CheckFoolsApplicable())
            return;

        if (this.currentState != AnimationState.NotPlaying)
            return;

        this.currentDeathType = type;

        this.currentState = AnimationState.FadeIn;
        this.alphaEasing = new InOutCubic(TimeSpan.FromMilliseconds(this.msFadeInTime));
        this.alphaEasing.Start();

        this.scaleEasing = new OutCubic(TimeSpan.FromMilliseconds(this.msFadeInTime + this.msWaitTime + this.msFadeOutTime))
        {
            Point1 = new Vector2(0.95f, 0.95f),
            Point2 = new Vector2(1.05f, 1.05f),
        };
        this.scaleEasing.Start();

        this.time.Reset();
        this.time.Start();

        this.player.Play();
    }

    private bool CheckFoolsApplicable()
    {
        var config = Service<DalamudConfiguration>.Get();

        if (!config.Fools22)
            return false;

        if (!(DateTime.Now.Month == 4 && DateTime.Now.Day == 1))
            return false;

        var timeZone = TimeZoneInfo.Local;
        var offset = timeZone.GetUtcOffset(DateTime.UtcNow);

        Log.Information("Fools22: UTC offset: {0}", offset);

        return this.assetsReady;
    }

    public void Dispose()
    {
        this.erDeathBgTexture.Dispose();
        this.erNormalDeathTexture.Dispose();
        this.erCraftFailedTexture.Dispose();

        this.player.Dispose();
    }

}
