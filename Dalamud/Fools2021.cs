using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Dalamud.Interface;
using ImGuiNET;
using ImGuiScene;
using Serilog;

namespace Dalamud
{
    internal class Fools2021 : IDisposable
    {
        private readonly Dalamud dalamud;

        private TextureWrap welcomeTex;
        private const float WelcomeTexScale = 0.74f;
        private const float TippyScale = 1;

        private TextureWrap bubbleTex;
        private const float BubbleScale = 1;

        private long frames = 0;

        private Stopwatch tippyTimer = new Stopwatch();

        public bool IsEnabled = true;

        public Fools2021(Dalamud dalamud)
        {
            this.dalamud = dalamud;

            this.dalamud.ClientState.OnLogin += (sender, args) => this.isTippyDrawing = true;
            this.dalamud.ClientState.OnLogout += (sender, args) => {
                this.isTippyDrawing = false;
                this.tippyState = TippyState.Intro;
            };

            this.welcomeTex = this.dalamud.InterfaceManager.LoadImage(
                Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "welcome.png"));
            this.tippySpriteSheet = this.dalamud.InterfaceManager.LoadImage(
                Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "map.png"));
            this.bubbleTex = this.dalamud.InterfaceManager.LoadImage(
                Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "bubble.png"));

            this.tippyTimer.Start();

            SetTippyAnim(TippyAnimState.Idle, true);
            this.isTippyDrawing = true;
        }

        private void LoadTippyFrames(TippyAnimState anim, int start, int end) {
            var frames = new TextureWrap[end - start];

            for (var i = start; i < end; i++) {
                frames[i - start] = this.dalamud.InterfaceManager.LoadImage(
                    Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "tippy", $"tile{i:D4}.png"));
            }

            this.tippyAnimFrames[anim] = frames;
        }

        public void Draw()
        {
            if (!IsEnabled)
                return;

            if (this.frames < 2000)
            {
                ImGui.SetNextWindowSize(new Vector2(this.welcomeTex.Width, this.welcomeTex.Height + 28) * ImGui.GetIO().FontGlobalScale * WelcomeTexScale, ImGuiCond.Always);

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));

                ImGui.Begin("Please wait...",
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

                ImGui.Image(this.welcomeTex.ImGuiHandle, new Vector2(this.welcomeTex.Width, this.welcomeTex.Height) * ImGui.GetIO().FontGlobalScale * WelcomeTexScale);

                ImGui.End();

                ImGui.PopStyleVar();
            }

            this.frames++;

            if (this.isTippyDrawing)
                DrawTippy();
        }

        private bool isTippyDrawing = false;

        private enum TippyState
        {
            Intro,
            Tips
        }

        private TippyState tippyState = TippyState.Intro;

        private enum TippyAnimState
        {
            Idle,
            GetAttention,
            Banger,
            Sending,
            Reading,
            GetAttention2,
            PointLeft,
            WritingDown,
            CheckingYouOut
        }

        private Dictionary<TippyAnimState, TextureWrap[]> tippyAnimFrames =
            new Dictionary<TippyAnimState, TextureWrap[]>();

        private static string TippyIntro =
            "Hi! I am Tippy, your new virtual assistant.\n\nI will teach you how to play your job!\n\nJust click \"OK\" to begin!";

        private static readonly string[] GeneralTips = new[] {
            "Vuln stacks really don't affect damage you receive by that much, so ignore any healer that complains about you not doing mechanics correctly.",
            "Wiping the party is an excellent method to clear away bad status effects, including death.",
            "Players in your party do not pay your sub. Play the game how you like and report harassment.",
            "In a big pull, always use the ability with the highest potency number on your bar.",
            "Make sure to avoid the stack marker so that your healers have less people to heal during raids!",
            "Put macro'd dialogue on all of your attacks as a tank to also gain enmity from your party members."
        };

        private static readonly string[] PldTips = new[] {
            "Just don't.",
        };

        private static readonly string[] AstTips = new[] {
            "Always use Benediction on cooldown to maximize healing power!",
            "Remember, Rescue works over gaps in the floor. Use it to save fellow players."
        };

        private static Dictionary<uint, string[]> jobTipDict = new Dictionary<uint, string[]>() {
            { 1, PldTips },

            { 33, AstTips }
        };

        private string currentTip = string.Empty;
        private long lastTipFrame = 0;

        private string tippyTextOverride = string.Empty;
        private bool showTippyButton = true;

        private void DrawTippy()
        {
            if (string.IsNullOrEmpty(this.currentTip))
            {
                this.currentTip = CalcNewTip();
                this.lastTipFrame = this.frames;
            }

            if (this.frames - this.lastTipFrame > 2000)
            {
                this.currentTip = CalcNewTip();
                this.lastTipFrame = this.frames;
            }
            
            var displaySize = ImGui.GetIO().DisplaySize;

            var tippyPos = new Vector2(displaySize.X - 400, displaySize.Y - 350);

            ImGui.SetNextWindowPos(tippyPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

            ImGui.Begin("###TippyWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoFocusOnAppearing);

            if (string.IsNullOrEmpty(this.tippyTextOverride))
            {
                switch (tippyState)
                {
                    case TippyState.Intro:
                        DrawTextBox("Hi, I'm Tippy!\n\nI'm your new friend and assistant.\n\nI will help you get better at FFXIV!");
                        break;
                    case TippyState.Tips:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } else {
                DrawTextBox(this.tippyTextOverride);
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(230);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 55);

            DrawTippyAnim();

            ImGui.End();

            

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.6f, 0.6f, 1f));

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));

            if (this.showTippyButton) {
                ImGui.SetNextWindowPos(tippyPos + new Vector2(117, 117), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(90, 40), ImGuiCond.Always);
                ImGui.Begin("###TippyButton", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                ImGui.Button("OK!", new Vector2(80, 20));
                ImGui.End();
            }

            ImGui.PopStyleColor();

            ImGui.PopFont();

            ImGui.PopStyleColor();

            

            ImGui.PopStyleColor();

            ImGui.Begin("Tippy AI debug");

            ImGui.Text("State: " + this.tippyState);

            foreach (var tippyAnimData in this.tippyAnimDatas) {
                if (ImGui.Button(tippyAnimData.Key.ToString()))
                    SetTippyAnim(tippyAnimData.Key, true);
            }

            ImGui.InputTextMultiline("Text Override", ref this.tippyTextOverride, 200, new Vector2(300, 120));
            ImGui.Checkbox("Show button", ref this.showTippyButton);

            ImGui.End();
        }

        private void DrawTextBox(string text) {
            ImGui.PushFont(InterfaceManager.FoolsFont);

            var beforeBubbleCursor = ImGui.GetCursorPos();
            ImGui.Image(this.bubbleTex.ImGuiHandle, new Vector2(this.bubbleTex.Width, this.bubbleTex.Height) * ImGui.GetIO().FontGlobalScale * BubbleScale);
            var afterBubbleCursor = ImGui.GetCursorPos();

            ImGui.SetCursorPos(beforeBubbleCursor + new Vector2(10, 10));
            ImGui.TextColored(new Vector4(0, 0, 0, 1), text);

            ImGui.SetCursorPos(afterBubbleCursor);
        }

        private TippyAnimState tippyAnim = TippyAnimState.Idle;

        private int startTippyFrame = 0;
        private int endTippyFrame = 0;
        private int FramesInTippyAnimation => this.endTippyFrame - this.startTippyFrame;

        private int currentTippyFrame = 0;
        private bool tippyAnimUp = true;
        private bool tippyAnimDone = false;
        private bool tippyLoopAnim = false;

        private long currentFrameTime = 0;
        private long minFrameTime = 150;

        private readonly int tippySpritesheetW = 27; // amount in row + 1
        private readonly int tippySpritesheetH = 27;
        private readonly Vector2 tippySingleSize = new Vector2(124, 93);

        private class TippyAnimData {
            public TippyAnimData(int start, int stop) {
                Start = start;
                Stop = stop;
            }

            public int Start { get; set; }
            public int Stop { get; set; }
        }

        private readonly Dictionary<TippyAnimState, TippyAnimData> tippyAnimDatas =
            new Dictionary<TippyAnimState, TippyAnimData> {
                {TippyAnimState.GetAttention, new TippyAnimData(199, 223)},
                {TippyAnimState.Idle, new TippyAnimData(233, 267)},
                {TippyAnimState.Banger, new TippyAnimData(343, 360)},
                {TippyAnimState.Sending, new TippyAnimData(361, 412)},
                {TippyAnimState.Reading, new TippyAnimData(443, 493)},
                {TippyAnimState.GetAttention2, new TippyAnimData(522, 535)},
                {TippyAnimState.PointLeft, new TippyAnimData(545, 554)},
                {TippyAnimState.WritingDown, new TippyAnimData(555, 624)},
                {TippyAnimState.CheckingYouOut, new TippyAnimData(718, 791)},
            };

        private readonly TextureWrap tippySpriteSheet;

        private Vector2 GetTippyTexCoords(int spriteIndex) {
            var w = spriteIndex % this.tippySpritesheetW;
            var h = spriteIndex / this.tippySpritesheetH;

            return new Vector2(this.tippySingleSize.X * w, this.tippySingleSize.Y * h);
        }

        private void SetTippyAnim(TippyAnimState anim, bool loop) {
            var animData = this.tippyAnimDatas[anim];

            this.startTippyFrame = animData.Start;
            this.endTippyFrame = animData.Stop;

            this.currentTippyFrame = 0;
            this.tippyAnim = anim;
            this.tippyAnimUp = true;
            this.tippyLoopAnim = loop;
        }

        private Vector2 ToSpriteSheetScale(Vector2 input) => new Vector2(input.X / this.tippySpriteSheet.Width, input.Y / this.tippySpriteSheet.Height);

        private void DrawTippyAnim() {
            var frameCoords = GetTippyTexCoords(this.startTippyFrame + this.currentTippyFrame);
            var botRight = ToSpriteSheetScale(frameCoords + this.tippySingleSize);

            Log.Verbose($"Drawing Tippy: {this.tippyAnim} {this.currentTippyFrame} / {FramesInTippyAnimation} up:{this.tippyAnimUp} {this.tippyAnimDone} cx:{frameCoords.X} cy:{frameCoords.Y} rx:{botRight.X} ry:{botRight.Y}");

            if (this.currentTippyFrame > FramesInTippyAnimation - 2) {
                this.tippyAnimDone = true;
                if (!this.tippyLoopAnim)
                    return;
                else
                    this.tippyAnimUp = false;
            }

            if (this.currentTippyFrame == 0) {
                this.tippyAnimUp = true;
            }


            ImGui.Image(this.tippySpriteSheet.ImGuiHandle, this.tippySingleSize * ImGui.GetIO().FontGlobalScale * TippyScale, ToSpriteSheetScale(frameCoords), botRight);

            ImGui.Text(this.currentTippyFrame.ToString());

            this.currentFrameTime += this.tippyTimer.ElapsedMilliseconds;
            this.tippyTimer.Restart();

            if (this.currentFrameTime >= this.minFrameTime) {
                if (this.tippyAnimUp)
                    this.currentTippyFrame++;
                else
                    this.currentTippyFrame--;

                this.currentFrameTime -= this.minFrameTime;

                if (this.currentFrameTime >= this.minFrameTime)
                    this.currentFrameTime = 0;
            }
        }

        private string CalcNewTip()
        {
            var rand = new Random();

            var lp = this.dalamud.ClientState.LocalPlayer;

            var gti = rand.Next(0, GeneralTips.Length);
            var generalTip = GeneralTips[gti];

            if (lp == null || rand.Next(0, 32) < 8)
            {
                return generalTip;
            }
            else
            {
                if (jobTipDict.TryGetValue(lp.ClassJob.Id, out var ccTips))
                {
                    var ti = rand.Next(0, ccTips.Length);
                    return ccTips[ti];
                }
                else
                {
                    return generalTip;
                }
            }
        }

        public void Dispose()
        {
            this.welcomeTex.Dispose();
            this.tippySpriteSheet.Dispose();

            foreach (var anim in this.tippyAnimFrames.Values.SelectMany(x => x))
            {
                anim.Dispose();
            }
        }
    }
}
