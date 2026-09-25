using System.Collections;
using System.Collections.Generic;

using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Tooltip.Classes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Tooltip.TooltipArgTypes;

/// <summary>
/// Object representing a tooltip args object passed to a plugin to either construct a custom node,
/// or edit/respond to the data provided.
/// </summary>
/// <remarks>
/// The game does not clear unused fields when generating a tooltip,
/// some values may be invalid to read or set depending on the item.
/// </remarks>
public unsafe class ItemTooltipArgs : TooltipArgs
{
    /// <inheritdoc/>
    public override TooltipType Type => TooltipType.Item;

    /// <summary>
    /// Gets a pointer to the addon that triggered these args.
    /// </summary>
    public required AtkUnitBasePtr AddonPointer { get; init; }

    /// <summary>
    /// Gets the itemId for this tooltip.
    /// </summary>
    public uint ItemId => AgentItemDetail.Instance()->ItemId;

    /// <summary>
    /// Gets or sets the icon id.
    /// </summary>
    public uint IconId
    {
        get => (uint)this.NumberArrayData->Span[0];
        set
        {
            if (value is 0)
            {
                // Throw to prevent setting to zero, as we check IconId to detemine if the RequestedUpdate is non-fist-init.
                throw new IndexOutOfRangeException("Setting IconId to Zero is invalid");
            }

            this.NumberArrayData->SetValue(0, (int)value, suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the displayed spiritbond % (0 - 100).
    /// </summary>
    public int Spiritbond
    {
        get => this.NumberArrayData->Span[20];
        set => this.NumberArrayData->SetValue(20, value, suppressUpdates: true);
    }

    /// <summary>
    /// Gets or sets the displayed Condition % (0 - 100).
    /// </summary>
    public int Condition
    {
        get => this.NumberArrayData->Span[21];
        set => this.NumberArrayData->SetValue(21, value, suppressUpdates: true);
    }

    /// <summary>
    /// Gets or sets the item name string.
    /// </summary>
    public ReadOnlySeString ItemNameString
    {
        get => this.StringArrayData->Span[0].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(0, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the item ui category string.
    /// </summary>
    public ReadOnlySeString ItemUiCategoryString
    {
        get => this.StringArrayData->Span[2].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(2, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets the primary stat value strings.
    /// </summary>
    /// <remarks>
    /// Starts at string array index 4, and can hold up to 3 values.
    /// </remarks>
    public StringArrayHelper StatLabelStrings => new(this.StringArrayData, 7, 3);

    /// <summary>
    /// Gets the primary stat value strings.
    /// </summary>
    /// <remarks>
    /// Starts at string array index 7, and can hold up to 3 values.
    /// </remarks>
    public StringArrayHelper StatValueStrings => new(this.StringArrayData, 7, 3);

    /// <summary>
    /// Gets or sets the description string.
    /// </summary>
    public ReadOnlySeString DescriptionString
    {
        get => this.StringArrayData->Span[13].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(13, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the quantities string.
    /// </summary>
    public ReadOnlySeString QuantitiesString
    {
        get => this.StringArrayData->Span[14].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(14, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the effects value string.
    /// </summary>
    public ReadOnlySeString EffectsValueString
    {
        get => this.StringArrayData->Span[16].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(16, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the ClassJobCategory string.
    /// </summary>
    public ReadOnlySeString ClassJobCategoryString
    {
        get => this.StringArrayData->Span[22].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(22, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the level string.
    /// </summary>
    public ReadOnlySeString LevelString
    {
        get => this.StringArrayData->Span[23].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(23, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the sell value string.
    /// </summary>
    public ReadOnlySeString SellValueString
    {
        get => this.StringArrayData->Span[25].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(25, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the item level string.
    /// </summary>
    public ReadOnlySeString ItemLevelString
    {
        get => this.StringArrayData->Span[27].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(27, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the condition string.
    /// </summary>
    public ReadOnlySeString ConditionString
    {
        get => this.StringArrayData->Span[28].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(28, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the spiritbond label string.
    /// </summary>
    public ReadOnlySeString SpiritbondLabelString
    {
        get => this.StringArrayData->Span[29].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(29, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the spiritbond string.
    /// </summary>
    /// <remarks>
    /// Can alternatively use <see cref="Spiritbond"/>.
    /// </remarks>
    public ReadOnlySeString SpiritbondString
    {
        get => this.StringArrayData->Span[30].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(30, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the repair level string.
    /// </summary>
    public ReadOnlySeString RepairLevelString
    {
        get => this.StringArrayData->Span[31].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(31, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the repair materials string.
    /// </summary>
    public ReadOnlySeString RepairMaterialsString
    {
        get => this.StringArrayData->Span[32].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(32, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the quick repairs cost string.
    /// </summary>
    public ReadOnlySeString QuickRepairsString
    {
        get => this.StringArrayData->Span[33].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(33, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the properties string.
    /// </summary>
    /// <remarks>
    /// This represents things like "Extractable: Y Projectable: Y Desynthesizable: 100.00".
    /// </remarks>
    public ReadOnlySeString PropertiesString
    {
        get => this.StringArrayData->Span[35].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(35, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the bonuses label string.
    /// </summary>
    public ReadOnlySeString BonusesLabelString
    {
        get => this.StringArrayData->Span[36].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(36, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets an array indexable bonuses strings object.
    /// </summary>
    /// <remarks>
    /// Starts at string array index 37, and can hold up to 6 values.
    /// </remarks>
    public StringArrayHelper BonusesValueStrings => new(this.StringArrayData, 37, 6);

    /// <summary>
    /// Gets or sets a region specific label, "Crucible Effect:" "Occult Crescent Set Bonus: yadayada".
    /// </summary>
    public ReadOnlySeString RegionEffectLabel
    {
        get => this.StringArrayData->Span[45].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(45, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets an array indexable regional stat bonuse strings object.
    /// </summary>
    /// <remarks>
    /// Starts at string array index 46, and can hold up to 6 values.
    /// </remarks>
    public StringArrayHelper RegionBonusStrings => new(this.StringArrayData, 46, 6);

    /// <summary>
    /// Gets or sets the materia label string.
    /// </summary>
    public ReadOnlySeString MaterialLabelString
    {
        get => this.StringArrayData->Span[52].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(52, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets an array indexable materia name strings object.
    /// </summary>
    /// <remarks>
    /// Starts at string array index 53, and can hold up to 5 values.
    /// </remarks>
    public StringArrayHelper MateriaNameStrings => new(this.StringArrayData, 53, 5);

    /// <summary>
    /// Gets an array indexable materia bonus string objects.
    /// </summary>
    /// <remarks>
    /// Starts at string array index 58, and can hold up to 5 values.
    /// </remarks>
    public StringArrayHelper MateriaBonusesStrings => new(this.StringArrayData, 58, 5);

    /// <summary>
    /// Gets or sets the shop selling price string.
    /// </summary>
    public ReadOnlySeString ShopSellingPriceString
    {
        get => this.StringArrayData->Span[63].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(63, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets or sets the controls hint string.
    /// </summary>
    /// <remarks>
    /// This is the string shown at the very bottom of the tooltip, generally reads "Alt Key Hide item details",
    /// this string has a couple variations.
    /// </remarks>
    public ReadOnlySeString ControlsHintString
    {
        get => this.StringArrayData->Span[64].AsReadOnlySeString();
        set
        {
            using var stringBuilder = new RentedSeStringBuilder();
            this.StringArrayData->SetValue(64, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <summary>
    /// Gets the internal pointer to the NumberArray data as passed from OnRequestedUpdate.
    /// </summary>
    internal NumberArrayData** NumberArrayDataPointer { get; init; }

    /// <summary>
    /// Gets the number array span.
    /// </summary>
    internal NumberArrayData* NumberArrayData
        => this.NumberArrayDataPointer[(int)NumberArrayType.ItemDetail];

    /// <summary>
    /// Gets the internal pointer to the StringArray data as passed from OnRequestedUpdate.
    /// </summary>
    internal StringArrayData** StringArrayDataRoot { get; init; }

    /// <summary>
    /// Gets the string array span.
    /// </summary>
    internal StringArrayData* StringArrayData
        => this.StringArrayDataRoot[(int)StringArrayType.ItemDetail];
}

/// <summary>
/// Generic string array helper to access strings at a certain range.
/// </summary>
public unsafe class StringArrayHelper(StringArrayData* stringArrayData, int startIndex, int size) : IEnumerable<ReadOnlySeString>
{
    /// <summary>
    /// Gets the number of elements this helper is for.
    /// </summary>
    public int Length { get; } = size;

    /// <summary>
    /// Gets or sets a ReadOnlySeString at the specified index.
    /// Valid range depends on which property created this object.
    /// </summary>
    /// <param name="index">Index to access.</param>
    public ReadOnlySeString this[int index]
    {
        get
        {
            if (index < 0 || index > this.Length - 1)
            {
                throw new IndexOutOfRangeException();
            }

            return stringArrayData->Span[startIndex + index].AsReadOnlySeString();
        }

        set
        {
            if (index < 0 || index > this.Length - 1)
            {
                throw new IndexOutOfRangeException();
            }

            using var stringBuilder = new RentedSeStringBuilder();
            stringArrayData->SetValue(startIndex + index, stringBuilder.Builder.Append(value).GetViewAsSpan(), suppressUpdates: true);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<ReadOnlySeString> GetEnumerator()
    {
        for (var i = 0; i < this.Length; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
