using System.Linq;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Text.Noun;
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.Interface.Utility.Raii;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for the NounProcessor service.
/// </summary>
internal class NounProcessorWidget : IDataWindowWidget
{
    /// <summary>A list of German grammatical cases.</summary>
    internal static readonly string[] GermanCases = [string.Empty, "Nominative", "Genitive", "Dative", "Accusative"];

    private static readonly Type[] NounSheets = [
        typeof(Aetheryte),
        typeof(BNpcName),
        typeof(BeastTribe),
        typeof(DeepDungeonEquipment),
        typeof(DeepDungeonItem),
        typeof(DeepDungeonMagicStone),
        typeof(DeepDungeonDemiclone),
        typeof(ENpcResident),
        typeof(EObjName),
        typeof(EurekaAetherItem),
        typeof(EventItem),
        typeof(GCRankGridaniaFemaleText),
        typeof(GCRankGridaniaMaleText),
        typeof(GCRankLimsaFemaleText),
        typeof(GCRankLimsaMaleText),
        typeof(GCRankUldahFemaleText),
        typeof(GCRankUldahMaleText),
        typeof(GatheringPointName),
        typeof(Glasses),
        typeof(GlassesStyle),
        typeof(HousingPreset),
        typeof(Item),
        typeof(MJIName),
        typeof(Mount),
        typeof(Ornament),
        typeof(TripleTriadCard),
    ];

    private ClientLanguage[] languages = [];
    private string[] languageNames = [];

    private int selectedSheetNameIndex;
    private int selectedLanguageIndex;
    private int rowId = 1;
    private int amount = 1;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["noun"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Noun Processor";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.languages = Enum.GetValues<ClientLanguage>();
        this.languageNames = Enum.GetNames<ClientLanguage>();
        this.selectedLanguageIndex = (int)Service<ClientState>.Get().ClientLanguage;

        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var nounProcessor = Service<NounProcessor>.Get();
        var dataManager = Service<DataManager>.Get();

        var sheetType = NounSheets.ElementAt(this.selectedSheetNameIndex);
        var language = this.languages[this.selectedLanguageIndex];

        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("###SelectedSheetName", ref this.selectedSheetNameIndex, NounSheets.Select(t => t.Name).ToArray()))
        {
            this.rowId = 1;
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(120);
        if (ImGui.Combo("###SelectedLanguage", ref this.selectedLanguageIndex, this.languageNames))
        {
            language = this.languages[this.selectedLanguageIndex];
            this.rowId = 1;
        }

        ImGui.SetNextItemWidth(120);
        var sheet = dataManager.Excel.GetSheet<RawRow>(Language.English, sheetType.Name);
        var minRowId = (int)sheet.FirstOrDefault().RowId;
        var maxRowId = (int)sheet.LastOrDefault().RowId;
        if (ImGui.InputInt("RowId###RowId", ref this.rowId, 1, 10, flags: ImGuiInputTextFlags.AutoSelectAll))
        {
            if (this.rowId < minRowId)
                this.rowId = minRowId;

            if (this.rowId >= maxRowId)
                this.rowId = maxRowId;
        }

        ImGui.SameLine();
        ImGui.Text($"(Range: {minRowId} - {maxRowId})");

        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("Amount###Amount", ref this.amount, 1, 10, flags: ImGuiInputTextFlags.AutoSelectAll))
        {
            if (this.amount <= 0)
                this.amount = 1;
        }

        var articleTypeEnumType = language switch
        {
            ClientLanguage.Japanese => typeof(JapaneseArticleType),
            ClientLanguage.German => typeof(GermanArticleType),
            ClientLanguage.French => typeof(FrenchArticleType),
            _ => typeof(EnglishArticleType),
        };

        var numCases = language == ClientLanguage.German ? 4 : 1;

#if DEBUG
        if (ImGui.Button("Copy as self-test entry"u8))
        {
            var sb = new StringBuilder();

            foreach (var articleType in Enum.GetValues(articleTypeEnumType))
            {
                for (var grammaticalCase = 0; grammaticalCase < numCases; grammaticalCase++)
                {
                    var nounParams = new NounParams()
                    {
                        SheetName = sheetType.Name,
                        RowId = (uint)this.rowId,
                        Language = language,
                        Quantity = this.amount,
                        ArticleType = (int)articleType,
                        GrammaticalCase = grammaticalCase,
                    };
                    var output = nounProcessor.ProcessNoun(nounParams).ExtractText().Replace("\"", "\\\"");
                    var caseParam = language == ClientLanguage.German ? $"(int)GermanCases.{GermanCases[grammaticalCase + 1]}" : "1";
                    sb.AppendLine($"new(nameof(LSheets.{sheetType.Name}), {this.rowId}, ClientLanguage.{language}, {this.amount}, (int){articleTypeEnumType.Name}.{Enum.GetName(articleTypeEnumType, articleType)}, {caseParam}, \"{output}\"),");
                }
            }

            ImGui.SetClipboardText(sb.ToString());
        }
#endif

        using var table = ImRaii.Table("TextDecoderTable"u8, 1 + numCases, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("ArticleType"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        for (var i = 0; i < numCases; i++)
            ImGui.TableSetupColumn(language == ClientLanguage.German ? GermanCases[i] : "Text");
        ImGui.TableSetupScrollFreeze(6, 1);
        ImGui.TableHeadersRow();

        foreach (var articleType in Enum.GetValues(articleTypeEnumType))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableHeader(articleType.ToString());

            for (var currentCase = 0; currentCase < numCases; currentCase++)
            {
                ImGui.TableNextColumn();

                try
                {
                    var nounParams = new NounParams()
                    {
                        SheetName = sheetType.Name,
                        RowId = (uint)this.rowId,
                        Language = language,
                        Quantity = this.amount,
                        ArticleType = (int)articleType,
                        GrammaticalCase = currentCase,
                    };
                    ImGui.Text(nounProcessor.ProcessNoun(nounParams).ExtractText());
                }
                catch (Exception ex)
                {
                    ImGui.Text(ex.ToString());
                }
            }
        }
    }
}
