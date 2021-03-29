using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
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
        private const float WelcomeTexScale = 1f;
        private const float TippyScale = 1;

        private TextureWrap bubbleTex;
        private const float BubbleScale = 1;

        private long frames = 0;

        private readonly Stopwatch tippyFrameTimer = new Stopwatch();
        private readonly Stopwatch tippyLogicTimer = new Stopwatch();

        public bool IsEnabled = true;

        public Fools2021(Dalamud dalamud)
        {
            this.dalamud = dalamud;

            this.dalamud.ClientState.OnLogin += (sender, args) => {
                this.isTippyDrawing = true;
                this.tippyLogicTimer.Restart();
            };
            this.dalamud.ClientState.OnLogout += (sender, args) => {
                this.isTippyDrawing = false;

                this.tippyText = string.Empty;
                this.tippyState = TippyState.BeforeIntro;
                this.showTippyButton = false;
            };

            this.welcomeTex = this.dalamud.InterfaceManager.LoadImage(
                Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "welcome.png"));
            this.tippySpriteSheet = this.dalamud.InterfaceManager.LoadImage(
                Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "map.png"));
            this.bubbleTex = this.dalamud.InterfaceManager.LoadImage(
                Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "bubble.png"));

            this.tippyFrameTimer.Start();
        }

        public void Draw()
        {
            if (!IsEnabled)
                return;

            if (this.frames < 2000)
            {
                ImGui.SetNextWindowSize(new Vector2(this.welcomeTex.Width, this.welcomeTex.Height) * ImGui.GetIO().FontGlobalScale * WelcomeTexScale, ImGuiCond.Always);

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);

                ImGui.Begin("Please wait...",
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

                ImGui.Image(this.welcomeTex.ImGuiHandle, new Vector2(this.welcomeTex.Width, this.welcomeTex.Height) * ImGui.GetIO().FontGlobalScale * WelcomeTexScale);

                ImGui.End();

                ImGui.PopStyleVar(2);
            }

            this.frames++;

#if DEBUG
            ImGui.Begin("Tippy AI debug");

            ImGui.Text("State: " + this.tippyState);

            foreach (var tippyAnimData in this.tippyAnimDatas)
            {
                if (ImGui.Button(tippyAnimData.Key.ToString()))
                    SetTippyAnim(tippyAnimData.Key, true);
            }

            ImGui.InputTextMultiline("Text Override", ref this.tippyText, 200, new Vector2(300, 120));
            ImGui.Checkbox("Show button", ref this.showTippyButton);
            ImGui.Checkbox("IsTippyDrawing", ref this.isTippyDrawing);

            if (ImGui.Button("Play intro")) {
                this.isTippyDrawing = true;

                this.tippyText = string.Empty;
                this.tippyState = TippyState.BeforeIntro;
                this.showTippyButton = false;

                this.tippyLogicTimer.Restart();
            }

            if (ImGui.Button("SetNewTip"))
                SetNewTip();

            foreach (var lastGeneralTip in this.lastGeneralTips) {
                ImGui.Text(lastGeneralTip);
            }

            ImGui.End();
#endif

            if (this.isTippyDrawing)
                DrawTippy();
        }

        private bool isTippyDrawing = false;

        private enum TippyState
        {
            BeforeIntro,
            Intro,
            Tips,
            Timeout,
            Parse
        }

        private TippyState tippyState = TippyState.BeforeIntro;

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

        private static readonly string[] GeneralTips = new[] {
            "Vuln stacks really don't affect damage \nyou receive by that much, so ignore\nany healer that complains about you\nnot doing mechanics correctly.",
            "Wiping the party is an excellent\nmethod to clear away bad status\neffects, including death.",
            "Players in your party do not pay \nyour sub.\n\nPlay the game however you like and\nreport harassment.",
            "In a big pull, always use the ability with\nthe highest potency number on your bar.",
            "Make sure to avoid the stack marker\nso that your healers have less people\nto heal during raids!",
            "Put macro'd dialogue on all of your\nattacks as a tank to also gain enmity\nfrom your party members.",
            "Make sure to save your LB until the\nboss is at 1 pct.\n\nThis will lead to the greatest effect.",
            "If you want to leave your party quickly\nand blame disconnect, just change\nyour PC time!",
            "Also try the OwO plugin!",
            "I will never leave you!",
            "You cannot hide any longer.",
            "Powered by XIVLauncher!",
            "When playing Hunter, specialize your\npet into taunting to help out your tank!",
            "It doesn't matter if you play BRD or MCH, \nit comes down to personal choice!",
            "Much like doing a \"brake check\" on the\nroad, you can do a \"heal check\"\nin-game! \n\nJust pop Superbolide at a random time,\npreferably about five seconds before \nraidwide damage.",
            "This text is powered by duck energy!"
        };

        private static readonly string Intro =
            "Hi, I'm Tippy!\n\nI'm your new friend and assistant.\n\nI will help you get better at FFXIV!";
        private static readonly string GoodSong = "Man, this song is great!";

        private static readonly string[] PldTips = new[] {
            "Just don't.",
            "Always save your cooldowns for boss\nfights.",
            "Piety matters as much as tenacity.",
            "Meld piety to maximize your DPS."
        };

        private static readonly string[] AstTips = new[] {
            "Always use Benediction on cooldown\nto maximize healing power!",
            "Remember, Rescue works over gaps\nin the floor.\n\nUse it to save fellow players."
        };

        private static readonly string[] MnkTips = new[] {
            "Always use Six-Sided Star on CD.\n\nIt's your highest potency ability.",
            "Use Fists of Fire Earth to mitigate big\ndamage during fights.\n\nYour healers will thank you.",

        };

        private static readonly string[] DrgTips = new[] {
            "Always make sure to use Mirage Dive\ndirectly after Jump so you don't forget\nto use it."
        };

        private static readonly string[] NinTips = new[] {
            "A ninja always appears raiton time!",
            "Tiger Palm is your most important GCD\nfor Brew and Chi generation.\n\nMake sure to cast it in favor of other\nenergy-spending abilities!"
        };

        private static readonly string[] SamTips = new[] {
            "Increase Midare damage by shouting\n\"BANKAI\" in chat.\n\nThis can be accomplished through the\nuse of macros.\n"
        };

        private static readonly string[] DncTips = new[] {
            "Only give Dance Partner to people\nafter they used a dance emote."
        };

        private static readonly string[] BlmTips = new[] {
            "Tired of casting fire so much?\n\nTry out using only ice spells for a\nchange of pace!"
        };

        private static readonly string[] SmnTips = new[] {
            "Titan-Egi can maximize your DPS by\nshielding you from interrupting\ndamage!"
        };

        private static readonly string[] BluTips = new[] {
            "Did you know that Blue Mage is\nthrowaway content?"
        };

        private static readonly string[] WhmTips = new[] {
            "Always use Benediction on cooldown\nto maximize healing power!",
            "Fluid Aura is a DPS gain!",
            "Always use Cure immediately so you\ncan get Freecure procs!",
            "Cure 1 is more mana-efficient than\nCure 2, so use that instead of Glare!",
            "Remember, Rescue works over gaps\nin the floor.\n\nUse it to save fellow players."
        };

        private static readonly string[] SchTips = new[] {
            "Attach Eos to the BRD so they receive\nhealing when at max range.",
            "Swiftcast Succor before raidwide for\nheals and shield mitigation, allowing\nyou to weave in an oGCD!",
            "Remember, Rescue works over gaps\nin the floor.\n\nUse it to save fellow players."
        };

        private static readonly string[] BrdTips = new[] {
            "Use TBN on your co-DRK so they don't\nhave to!",
            "Always save your cooldowns for boss\nfights.",
            "Piety matters as much as tenacity."
        };

        private static readonly string[] WarTips = new[] {
            "Infuriate before Inner Release to\nguarantee a Direct Hit Critical Inner\nChaos.",
            "Apply Nascent Flash to yourself to gain\nNascent Glint for 10% damage \nmitigation.",
            "Always save your cooldowns for boss\nfights.",
            "Piety matters as much as tenacity."
        };

        private static Dictionary<uint, string[]> jobTipDict = new Dictionary<uint, string[]>() {
            { 1, PldTips },

            { 33, AstTips }
        };

        private string currentTip = string.Empty;
        private long lastTipFrame = 0;

        private string tippyText = string.Empty;
        private bool showTippyButton = false;

        private SoundPlayer sounds = new SoundPlayer();

        private Random rand = new Random();

        private void PlayTada() {
            this.sounds.SoundLocation = Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "tada.wav");
            this.sounds.Play();
        }

        private void PlayChord()
        {
            this.sounds.SoundLocation = Path.Combine(dalamud.StartInfo.AssetDirectory, "UIRes", "chord.wav");
            this.sounds.Play();
        }

        private void DrawTippy()
        {
            if (this.tippyState == TippyState.BeforeIntro && this.tippyLogicTimer.ElapsedMilliseconds > 8000) {
                PlayTada();
                SetTippyAnim(TippyAnimState.GetAttention, true);
                this.tippyText = Intro;
                this.showTippyButton = true;
                this.tippyState = TippyState.Intro;
            } else if (this.tippyState == TippyState.BeforeIntro) {
                return;
            }

            switch (tippyState) {
                case TippyState.Tips:
                    if (this.tippyLogicTimer.ElapsedMilliseconds > 120000 && string.IsNullOrEmpty(this.tippyText))
                        SetNewTip();
                    break;
                case TippyState.Timeout:
                    if (this.tippyLogicTimer.ElapsedMilliseconds > 60000) {
                        SetTippyAnim(TippyAnimState.Idle, true);
                        this.tippyText = string.Empty;
                        this.showTippyButton = false;

                        this.tippyLogicTimer.Restart();
                        this.tippyState = TippyState.Tips;
                    }
                    break;
            }

            var displaySize = ImGui.GetIO().DisplaySize;

            var tippyPos = new Vector2(displaySize.X - 400, displaySize.Y - 350);

            ImGui.SetNextWindowPos(tippyPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

            ImGui.Begin("###TippyWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus);

            ImGui.PushFont(InterfaceManager.FoolsFont);

            if (!string.IsNullOrEmpty(this.tippyText))
            {
                DrawTextBox(this.tippyText);
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(230);
            ImGui.SetCursorPosY(18 + 55);

            DrawTippyAnim();

            ImGui.End();

            

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.6f, 0.6f, 1f));

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));

            if (this.showTippyButton) {
                ImGui.SetNextWindowPos(tippyPos + new Vector2(117, 117), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(95, 40), ImGuiCond.Always);
                //ImGui.SetNextWindowFocus();
                ImGui.Begin("###TippyButton", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

                if (ImGui.Button("OK!", new Vector2(80, 20)))
                {
                    switch (tippyState) {
                        case TippyState.Intro:
                            this.showTippyButton = false;
                            this.tippyText = string.Empty;
                            SetTippyAnim(TippyAnimState.Idle, true);

                            this.tippyState = TippyState.Tips;
                            this.tippyLogicTimer.Restart();
                            break;
                        case TippyState.Tips:
                            SetTippyAnim(TippyAnimState.Idle, true);
                            this.tippyText = string.Empty;
                            this.showTippyButton = false;

                            this.tippyLogicTimer.Restart();
                            break;

                        case TippyState.Parse:
                            Process.Start("https://na.finalfantasyxiv.com/jobguide/pvp/");

                            this.showTippyButton = false;
                            this.tippyText = string.Empty;
                            SetTippyAnim(TippyAnimState.Idle, true);

                            this.tippyState = TippyState.Tips;
                            this.tippyLogicTimer.Restart();
                            break;
                    }
                }

                ImGui.End();
            }

            ImGui.PopStyleColor();

            ImGui.PopFont();

            ImGui.PopStyleColor();

            ImGui.PopStyleColor();
        }

        private void DrawTextBox(string text) {

            var beforeBubbleCursor = ImGui.GetCursorPos();
            ImGui.Image(this.bubbleTex.ImGuiHandle, new Vector2(this.bubbleTex.Width, this.bubbleTex.Height) * ImGui.GetIO().FontGlobalScale * BubbleScale);
            var afterBubbleCursor = ImGui.GetCursorPos();

            ImGui.SetCursorPos(beforeBubbleCursor + new Vector2(10, 10));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();

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

            this.currentFrameTime += this.tippyFrameTimer.ElapsedMilliseconds;
            this.tippyFrameTimer.Restart();

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

        private Queue<string> lastGeneralTips = new Queue<string>();

        private string GetGeneralTip()
        {
            var gti = this.rand.Next(0, GeneralTips.Length);
            var generalTip = GeneralTips[gti];

            while (this.lastGeneralTips.Any(x => x == generalTip)) {
                gti = this.rand.Next(0, GeneralTips.Length);
                generalTip = GeneralTips[gti];
            }

            this.lastGeneralTips.Enqueue(generalTip);

            if (this.lastGeneralTips.Count > 5)
                this.lastGeneralTips.Dequeue();

            return generalTip;
        }

        private void SetNewTip()
        {
            var anim = this.rand.Next(0, 3);
            switch (anim) {
                case 0: SetTippyAnim(TippyAnimState.PointLeft, true);
                    break;
                case 1: SetTippyAnim(TippyAnimState.GetAttention2, true);
                    break;
                case 2: SetTippyAnim(TippyAnimState.GetAttention, true);
                    break;
            }

            var lp = this.dalamud.ClientState.LocalPlayer;

            var generalTip = GetGeneralTip();

            var choice = this.rand.Next(0, 28);


            Log.Information($"Choice: {choice}");

            var classJob = 14u;
            if (lp != null)
                classJob = lp.ClassJob.Id;

            var hasJobTips = jobTipDict.TryGetValue(classJob, out var ccTips);

            if (choice < 8)
            {
                this.tippyText = generalTip;

                PlayTada();
                this.showTippyButton = true;
            }
            else if (choice == 9 || choice == 10) {
                this.tippyState = TippyState.Timeout;
                this.tippyText = "Analyzing ERP logs...";
                SetTippyAnim(TippyAnimState.Reading, true);
                this.showTippyButton = false;

                this.tippyLogicTimer.Restart();

                PlayChord();
            }
            else if (choice == 11) {
                this.tippyState = TippyState.Timeout;
                this.tippyText = "I'm always watching.";
                SetTippyAnim(TippyAnimState.CheckingYouOut, true);
                this.showTippyButton = false;

                this.tippyLogicTimer.Restart();

                PlayChord();
            }
            else if (choice == 12)
            {
                this.tippyState = TippyState.Parse;
                this.tippyText = "It seems like you are parsing grey.\n\nDo you want me to help you play your\njob better?";
                SetTippyAnim(TippyAnimState.Reading, true);
                this.showTippyButton = true;

                this.tippyLogicTimer.Restart();

                PlayChord();
            }
            else
            {
                if (hasJobTips)
                {
                    var ti = rand.Next(0, ccTips.Length);
                    this.tippyText = ccTips[ti];
                }
                else
                {
                    this.tippyText = generalTip;
                }

                PlayTada();
                this.showTippyButton = true;
            }
        }

        public void Dispose()
        {
            this.welcomeTex.Dispose();
            this.tippySpriteSheet.Dispose();
            this.bubbleTex.Dispose();
        }
    }
}
