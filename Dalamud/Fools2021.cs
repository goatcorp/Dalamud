using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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

        private long frames = 0;

        private Stopwatch tippyTimer = new Stopwatch();

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

            var watch = new Stopwatch();
            watch.Start();

            LoadTippyFrames(TippyAnimState.GetAttention, 199, 223);
            LoadTippyFrames(TippyAnimState.Idle, 233, 267);

            watch.Stop();
            Log.Information($"Loading tippy frames took {watch.ElapsedMilliseconds}ms");

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
            GetAttention
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

        private void DrawTippy()
        {
            /*
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
           
            */
            var windowSize = ImGui.GetIO().DisplaySize;
        
            var tippyX = windowSize.X - 200;
            var tippyY = windowSize.Y - 200;

            ImGui.SetNextWindowPos(new Vector2(tippyX, tippyY), ImGuiCond.Always);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

            ImGui.Begin("###TippyWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoFocusOnAppearing);

            switch (tippyState)
            {
                case TippyState.Intro:
                    
                    break;
                case TippyState.Tips:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            DrawTippyAnim();

            ImGui.End();

            ImGui.PopStyleColor();
        }

        private TippyAnimState tippyAnim = TippyAnimState.Idle;

        private long currentTippyFrame = 0;
        private bool tippyAnimUp = true;
        private bool tippyAnimDone = false;
        private bool tippyLoopAnim = false;

        private long currentFrameTime = 0;
        private long minFrameTime = 150;

        private void SetTippyAnim(TippyAnimState anim, bool loop) {
            this.tippyAnim = anim;
            this.tippyAnimUp = true;
            this.tippyLoopAnim = loop;
        }

        private void DrawTippyAnim() {
            var animFrames = this.tippyAnimFrames[this.tippyAnim];

            Log.Verbose($"Drawing Tippy: {this.tippyAnim} {this.currentTippyFrame} / {animFrames.Length} up:{this.tippyAnimUp} {this.tippyAnimDone}");

            if (this.currentTippyFrame > animFrames.Length - 2) {
                this.tippyAnimDone = true;
                if (!this.tippyLoopAnim)
                    return;
                else
                    this.tippyAnimUp = false;
            }

            if (this.currentTippyFrame == 0) {
                this.tippyAnimUp = true;
            }

            var frame = animFrames[this.currentTippyFrame];
            ImGui.Image(frame.ImGuiHandle, new Vector2(frame.Width, frame.Height) * ImGui.GetIO().FontGlobalScale * TippyScale);

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

            foreach (var anim in this.tippyAnimFrames.Values.SelectMany(x => x))
            {
                anim.Dispose();
            }
        }
    }
}
