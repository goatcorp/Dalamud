using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Data;
using Dalamud.Data.LuminaExtensions;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using Serilog;
using Item = Dalamud.Data.TransientSheet.Item;

namespace Dalamud.Interface
{
    class ItemSearchWindow : IDisposable
    {
        private readonly DataManager data;
        private readonly UiBuilder builder;
        private readonly bool closeOnChoose;

        private string lastSearchText = string.Empty;
        private string searchText = string.Empty;

        private int lastKind = 0;
        private int currentKind = 0;

        private int selectedItemIndex = -1;
        private TextureWrap selectedItemTex;

        private CancellationTokenSource searchCancelTokenSource;
        private ValueTask<List<Item>> searchTask;
        private List<Item> luminaItems;

        public event EventHandler<Item> OnItemChosen;

        public ItemSearchWindow(DataManager data, UiBuilder builder, bool closeOnChoose = true) {
            this.data = data;
            this.builder = builder;
            this.closeOnChoose = closeOnChoose;

            while (!data.IsDataReady)
                Thread.Sleep(1);

            
            Task.Run(() => this.data.GetExcelSheet<Item>().GetRows()).ContinueWith(t => this.luminaItems = t.Result);
        }

        public bool Draw() {
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);

            var isOpen = true;

            if (!ImGui.Begin(Loc.Localize("DalamudItemSelectHeader", "Select an item"), ref isOpen, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return false;
            }

            // Main window
            ImGui.AlignTextToFramePadding();

            ImGui.Text(Loc.Localize("DalamudItemSelect", "Please select an item."));
            if (this.selectedItemTex != null) {
                ImGui.Text(" ");

                ImGui.SetCursorPosY(200f);
                ImGui.SameLine();
                ImGui.Image(this.selectedItemTex.ImGuiHandle, new Vector2(40, 40));
            } else {
                ImGui.Text(" ");
            }

            ImGui.Separator();
            

            ImGui.Text("Search: ");
            ImGui.SameLine();
            ImGui.InputText("##searchbox", ref this.searchText, 32);

            var kinds = new List<string> {Loc.Localize("DalamudItemSelectAll", "All")};
            kinds.AddRange(this.data.GetExcelSheet<ItemSearchCategory>().GetRows().Where(x => !string.IsNullOrEmpty(x.Name)).Select(x => x.Name));
            ImGui.Text(Loc.Localize("DalamudItemSelectCategory", "Category: "));
            ImGui.SameLine();
            ImGui.Combo("##kindbox", ref this.currentKind, kinds.ToArray(),
                        kinds.Count);


            var windowSize = ImGui.GetWindowSize();
            ImGui.BeginChild("scrolling", new Vector2(0, windowSize.Y - ImGui.GetCursorPosY() - 40), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            if (this.luminaItems != null) {
                if (!string.IsNullOrEmpty(this.searchText) || this.currentKind != 0)
                {
                    if (this.lastSearchText != this.searchText || this.lastKind != this.currentKind)
                    {
                        this.lastSearchText = this.searchText;
                        this.lastKind = this.currentKind;

                        this.searchCancelTokenSource?.Cancel();

                        this.searchCancelTokenSource = new CancellationTokenSource();

                        var asyncEnum = this.luminaItems.ToAsyncEnumerable();

                        if (!string.IsNullOrEmpty(this.searchText))
                        {
                            Log.Debug("Searching for " + this.searchText);
                            asyncEnum = asyncEnum.Where(
                                x => (x.Name.ToLower().Contains(this.searchText.ToLower()) ||
                                      int.TryParse(this.searchText, out var parsedId) &&
                                      parsedId == x.RowId));
                        }

                        if (this.currentKind != 0)
                        {
                            Log.Debug("Searching for C" + this.currentKind);
                            asyncEnum = asyncEnum.Where(x => x.ItemSearchCategory == this.currentKind);
                        }

                        this.selectedItemIndex = -1;
                        this.selectedItemTex?.Dispose();
                        this.selectedItemTex = null;

                        this.searchTask = asyncEnum.ToListAsync(this.searchCancelTokenSource.Token);
                    }

                    if (this.searchTask.IsCompletedSuccessfully)
                    {
                        for (var i = 0; i < this.searchTask.Result.Count; i++)
                        {
                            if (ImGui.Selectable(this.searchTask.Result[i].Name, this.selectedItemIndex == i, ImGuiSelectableFlags.AllowDoubleClick))
                            {
                                
                                this.selectedItemIndex = i;

                                try
                                {
                                    var iconTex = this.data.GetIcon(this.searchTask.Result[i].Icon);
                                    this.selectedItemTex?.Dispose();
                                
                                    this.selectedItemTex =
                                       this.builder.LoadImageRaw(iconTex.GetRgbaImageData(), iconTex.Header.Width,
                                                                 iconTex.Header.Height, 4);
                                } catch (Exception ex)
                                {
                                    Log.Debug("Failed loading item texture");
                                    this.selectedItemTex?.Dispose();
                                    this.selectedItemTex = null;
                                }

                                if (ImGui.IsMouseDoubleClicked(0))
                                {
                                    OnItemChosen?.Invoke(this, this.searchTask.Result[i]);
                                    if (this.closeOnChoose)
                                    {
                                        this.selectedItemTex?.Dispose();
                                        isOpen = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.86f, 0.86f, 0.86f, 1.00f), Loc.Localize("DalamudItemSelectHint", "Type to start searching..."));

                    this.selectedItemIndex = -1;
                    this.selectedItemTex?.Dispose();
                    this.selectedItemTex = null;
                }
            } else {
                ImGui.TextColored(new Vector4(0.86f, 0.86f, 0.86f, 1.00f), Loc.Localize("DalamudItemSelectLoading", "Loading item list..."));
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();

            if (ImGui.Button(Loc.Localize("Choose", "Choose"))) {
                OnItemChosen?.Invoke(this, this.searchTask.Result[this.selectedItemIndex]);

                if (this.closeOnChoose) {
                    this.selectedItemTex?.Dispose();
                    isOpen = false;
                }
            }

            if (!this.closeOnChoose) {
                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("Close", "Close")))
                {
                    this.selectedItemTex?.Dispose();
                    isOpen = false;
                }
            }

            ImGui.End();

            return isOpen;
        }

        public void Dispose() {
            this.selectedItemTex?.Dispose();
        }
    }
}
