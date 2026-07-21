using System.Numerics;

using Dalamud.Game.Text.Evaluator;
using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;
using Dalamud.NativeUi.Extensions;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games CounterNode.
/// </summary>
internal unsafe class CounterNode : NodeBase<AtkCounterNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CounterNode"/> class.
    /// </summary>
    public CounterNode()
        : base(NodeType.Counter)
    {
        this.PartsList = new PartsList();
        this.PartsList.Add(new Part());

        Node->PartsList = this.PartsList.InternalPartsList;

        this.NumberWidth = 10;
        this.CommaWidth = 8;
        this.SpaceWidth = 6;
        this.TextAlignment = AlignmentType.Right;
        this.CounterWidth = 32;
        this.Font = CounterFont.MoneyFont;
    }

    /// <summary>
    /// Gets the parts list for this node.
    /// </summary>
    public PartsList PartsList { get; }

    /// <summary>
    /// Gets or sets the width of each digit.
    /// </summary>
    public uint NumberWidth
    {
        get => Node->NumberWidth;
        set => Node->NumberWidth = (byte)value;
    }

    /// <summary>
    /// Gets or sets the width of the numeric separator.
    /// </summary>
    public uint CommaWidth
    {
        get => Node->CommaWidth;
        set => Node->CommaWidth = (byte)value;
    }

    /// <summary>
    /// Gets or sets the width of spaces.
    /// </summary>
    public uint SpaceWidth
    {
        get => Node->SpaceWidth;
        set => Node->SpaceWidth = (byte)value;
    }

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public AlignmentType TextAlignment
    {
        get => (AlignmentType)Node->TextAlign;
        set => Node->TextAlign = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the width of the counter itself.
    /// </summary>
    public float CounterWidth
    {
        get => Node->CounterWidth;
        set => Node->CounterWidth = value;
    }

    /// <summary>
    /// Gets or sets the number displayed.
    /// </summary>
    /// <remarks>
    /// The value is actually saved as a string, so this incurs parsing costs.
    /// </remarks>
    public int Number
    {
        get => int.Parse(Node->NodeText.ToString());
        set => Node->SetText(ParseNumber(value));
    }

    /// <summary>
    /// Gets or sets the string displayed.
    /// </summary>
    public ReadOnlySeString String
    {
        get => Node->NodeText.AsSpan();
        set => Node->SetText(ParseString(value));
    }

    /// <summary>
    /// Gets or sets the font used for the counter.
    /// </summary>
    /// <remarks>
    /// Defaults to MoneyFont.
    /// </remarks>
    public CounterFont Font
    {
        get;
        set
        {
            field = value;

            var fontPath = string.Empty;
            var partSize = Vector2.Zero;

            switch (value)
            {
                case CounterFont.MoneyFont:
                    fontPath = "ui/uld/Money_Number.tex";
                    partSize = new Vector2(22.0f, 22.0f);
                    break;

                case CounterFont.ChocoboRace:
                    fontPath = "ui/uld/RaceChocoboNum.tex";
                    partSize = new Vector2(30.0f, 60.0f);
                    break;
            }

            if (fontPath != string.Empty && partSize != Vector2.Zero)
            {
                this.PartsList[0]->Width = (ushort)partSize.X;
                this.PartsList[0]->Height = (ushort)partSize.Y;
                this.PartsList[0]->LoadTexture(fontPath);
            }
        }
    }

    /// <summary>
    /// Gets or sets the texture path for the font used by this counter node.
    /// </summary>
    protected string TexturePath
    {
        get => this.PartsList[0]->LoadedPath;
        set => this.PartsList[0]->LoadTexture(value);
    }

    /// <summary>
    /// Gets or sets the texture coordinates used for the font used by this counter node.
    /// </summary>
    protected Vector2 TextureCoordinates
    {
        get => new(this.PartsList[0]->U, this.PartsList[0]->V);
        set
        {
            this.PartsList[0]->U = (ushort)value.X;
            this.PartsList[0]->V = (ushort)value.X;
        }
    }

    /// <summary>
    /// Gets or sets the texture size of the font texture used by this counter node.
    /// </summary>
    protected Vector2 TextureSize
    {
        get => new(this.PartsList[0]->Width, this.PartsList[0]->Height);
        set
        {
            this.PartsList[0]->Width = (ushort)value.X;
            this.PartsList[0]->Height = (ushort)value.X;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing, bool isNativeDestructor)
    {
        if (disposing && !this.IsDisposed)
        {
            if (!isNativeDestructor)
            {
                this.PartsList.Dispose();
                Node->PartsList = null;
            }

            base.Dispose(disposing, isNativeDestructor);
        }
    }

    private static ReadOnlySeString ParseString(ReadOnlySeString value)
    {
        using var builder = new RentedSeStringBuilder();
        return builder.Builder.Append(value).GetViewAsSpan();
    }

    private static ReadOnlySeString ParseNumber(int value)
    {
        using var rentedBuilder = new RentedSeStringBuilder();

        // <kilo(lnum1,\,)>
        var evaluatedString = Service<SeStringEvaluator>.Get().EvaluateFromAddon(18, [value]);

        foreach (var payload in evaluatedString)
        {
            switch (payload.Type)
            {
                // Fix for French thousands separators.
                // The game calls FormatAddonText2 that does this.
                case ReadOnlySePayloadType.Macro when payload.MacroCode is MacroCode.NonBreakingSpace:
                    rentedBuilder.Builder.Append(' ');
                    break;

                default:
                    rentedBuilder.Builder.Append(payload);
                    break;
            }
        }

        return rentedBuilder.Builder.GetViewAsSpan();
    }
}
