using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// A class representing a single nameplate. Provides mechanisms to look up the game object associated with the
/// nameplate and allows for modification of various backing fields in number and string array data, which in turn
/// affect aspects of the nameplate's appearance when drawn. Instances of this class are only valid for a single frame
/// and should not be kept across frames.
/// </summary>
public interface INamePlateUpdateHandler
{
    /// <summary>
    /// Gets the GameObjectId of the game object associated with this nameplate.
    /// </summary>
    ulong GameObjectId { get; }

    /// <summary>
    /// Gets the <see cref="IGameObject"/> associated with this nameplate, if possible. Performs an object table scan
    /// and caches the result if successful.
    /// </summary>
    IGameObject? GameObject { get; }

    /// <summary>
    /// Gets a read-only view of the nameplate info object data for a nameplate. Modifications to
    /// <see cref="NamePlateUpdateHandler"/> fields do not affect fields in the returned view.
    /// </summary>
    INamePlateInfoView InfoView { get; }

    /// <summary>
    /// Gets the index for this nameplate data in the backing number and string array data. This is not the same as the
    /// rendered or object index, which can be retrieved from <see cref="NamePlateIndex"/>.
    /// </summary>
    int ArrayIndex { get; }

    /// <summary>
    /// Gets the <see cref="IBattleChara"/> associated with this nameplate, if possible. Returns null if the nameplate
    /// has an associated <see cref="IGameObject"/>, but that object cannot be assigned to <see cref="IBattleChara"/>.
    /// </summary>
    IBattleChara? BattleChara { get; }

    /// <summary>
    /// Gets the <see cref="IPlayerCharacter"/> associated with this nameplate, if possible. Returns null if the
    /// nameplate has an associated <see cref="IGameObject"/>, but that object cannot be assigned to
    /// <see cref="IPlayerCharacter"/>.
    /// </summary>
    IPlayerCharacter? PlayerCharacter { get; }

    /// <summary>
    /// Gets the address of the nameplate info struct.
    /// </summary>
    nint NamePlateInfoAddress { get; }

    /// <summary>
    /// Gets the address of the first entry associated with this nameplate in the NamePlate addon's int array.
    /// </summary>
    nint NamePlateObjectAddress { get; }

    /// <summary>
    /// Gets a value indicating what kind of nameplate this is, based on the kind of object it is associated with.
    /// </summary>
    NamePlateKind NamePlateKind { get; }

    /// <summary>
    /// Gets the update flags for this nameplate.
    /// </summary>
    int UpdateFlags { get; }

    /// <summary>
    /// Gets or sets the overall text color for this nameplate. If this value is changed, the appropriate update flag
    /// will be set so that the game will reflect this change immediately.
    /// </summary>
    uint TextColor { get; set; }

    /// <summary>
    /// Gets or sets the overall text edge color for this nameplate. If this value is changed, the appropriate update
    /// flag will be set so that the game will reflect this change immediately.
    /// </summary>
    uint EdgeColor { get; set; }

    /// <summary>
    /// Gets or sets the icon ID for the nameplate's marker icon, which is the large icon used to indicate quest
    /// availability and so on. This value is read from and reset by the game every frame, not just when a nameplate
    /// changes. Setting this to 0 disables the icon.
    /// </summary>
    int MarkerIconId { get; set; }

    /// <summary>
    /// Gets or sets the icon ID for the nameplate's name icon, which is the small icon shown to the left of the name.
    /// Setting this to -1 disables the icon.
    /// </summary>
    int NameIconId { get; set; }

    /// <summary>
    /// Gets the nameplate index, which is the index used for rendering and looking up entries in the object array. For
    /// number and string array data, <see cref="ArrayIndex"/> is used.
    /// </summary>
    int NamePlateIndex { get; }

    /// <summary>
    /// Gets the draw flags for this nameplate.
    /// </summary>
    int DrawFlags { get; }

    /// <summary>
    /// Gets or sets the visibility flags for this nameplate.
    /// </summary>
    int VisibilityFlags { get; set; }

    /// <summary>
    /// Gets a value indicating whether this nameplate is undergoing a major update or not. This is usually true when a
    /// nameplate has just appeared or something meaningful about the entity has changed (e.g. its job or status). This
    /// flag is reset by the game during the update process (during requested update and before draw).
    /// </summary>
    bool IsUpdating { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the title (when visible) will be displayed above the object's name (a
    /// prefix title) instead of below the object's name (a suffix title).
    /// </summary>
    bool IsPrefixTitle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the title should be displayed at all.
    /// </summary>
    bool DisplayTitle { get; set; }

    /// <summary>
    /// Gets or sets the name for this nameplate.
    /// </summary>
    SeString Name { get; set; }

    /// <summary>
    /// Gets a builder which can be used to help cooperatively build a new name for this nameplate even when other
    /// plugins modifying the name are present. Specifically, this builder allows setting text and text-wrapping
    /// payloads (e.g. for setting text color) separately.
    /// </summary>
    NamePlateSimpleParts NameParts { get; }

    /// <summary>
    /// Gets or sets the title for this nameplate.
    /// </summary>
    SeString Title { get; set; }

    /// <summary>
    /// Gets a builder which can be used to help cooperatively build a new title for this nameplate even when other
    /// plugins modifying the title are present. Specifically, this builder allows setting text, text-wrapping
    /// payloads (e.g. for setting text color), and opening and closing quote sequences separately.
    /// </summary>
    NamePlateQuotedParts TitleParts { get; }

    /// <summary>
    /// Gets or sets the free company tag for this nameplate.
    /// </summary>
    SeString FreeCompanyTag { get; set; }

    /// <summary>
    /// Gets a builder which can be used to help cooperatively build a new FC tag for this nameplate even when other
    /// plugins modifying the FC tag are present. Specifically, this builder allows setting text, text-wrapping
    /// payloads (e.g. for setting text color), and opening and closing quote sequences separately.
    /// </summary>
    NamePlateQuotedParts FreeCompanyTagParts { get; }

    /// <summary>
    /// Gets or sets the status prefix for this nameplate. This prefix is used by the game to add BitmapFontIcon-based
    /// online status icons to player nameplates.
    /// </summary>
    SeString StatusPrefix { get; set; }

    /// <summary>
    /// Gets or sets the target suffix for this nameplate. This suffix is used by the game to add the squared-letter
    /// target tags to the end of combat target nameplates.
    /// </summary>
    SeString TargetSuffix { get; set; }

    /// <summary>
    /// Gets or sets the level prefix for this nameplate. This "Lv60" style prefix is added to enemy and friendly battle
    /// NPC nameplates to indicate the NPC level.
    /// </summary>
    SeString LevelPrefix { get; set; }

    /// <summary>
    /// Removes the contents of the name field for this nameplate. This differs from simply setting the field
    /// to an empty string because it writes a special value to memory, and other setters (except SetField variants)
    /// will refuse to overwrite this value. Therefore, fields removed this way are more likely to stay removed.
    /// </summary>
    void RemoveName();

    /// <summary>
    /// Removes the contents of the title field for this nameplate. This differs from simply setting the field
    /// to an empty string because it writes a special value to memory, and other setters (except SetField variants)
    /// will refuse to overwrite this value. Therefore, fields removed this way are more likely to stay removed.
    /// </summary>
    void RemoveTitle();

    /// <summary>
    /// Removes the contents of the FC tag field for this nameplate. This differs from simply setting the field
    /// to an empty string because it writes a special value to memory, and other setters (except SetField variants)
    /// will refuse to overwrite this value. Therefore, fields removed this way are more likely to stay removed.
    /// </summary>
    void RemoveFreeCompanyTag();

    /// <summary>
    /// Removes the contents of the status prefix field for this nameplate. This differs from simply setting the field
    /// to an empty string because it writes a special value to memory, and other setters (except SetField variants)
    /// will refuse to overwrite this value. Therefore, fields removed this way are more likely to stay removed.
    /// </summary>
    void RemoveStatusPrefix();

    /// <summary>
    /// Removes the contents of the target suffix field for this nameplate. This differs from simply setting the field
    /// to an empty string because it writes a special value to memory, and other setters (except SetField variants)
    /// will refuse to overwrite this value. Therefore, fields removed this way are more likely to stay removed.
    /// </summary>
    void RemoveTargetSuffix();

    /// <summary>
    /// Removes the contents of the level prefix field for this nameplate. This differs from simply setting the field
    /// to an empty string because it writes a special value to memory, and other setters (except SetField variants)
    /// will refuse to overwrite this value. Therefore, fields removed this way are more likely to stay removed.
    /// </summary>
    void RemoveLevelPrefix();

    /// <summary>
    /// Gets a pointer to the string array value in the provided field.
    /// </summary>
    /// <param name="field">The field to read from.</param>
    /// <returns>A pointer to a sequence of non-null bytes.</returns>
    unsafe byte* GetFieldAsPointer(NamePlateStringField field);

    /// <summary>
    /// Gets a byte span containing the string array value in the provided field.
    /// </summary>
    /// <param name="field">The field to read from.</param>
    /// <returns>A ReadOnlySpan containing a sequence of non-null bytes.</returns>
    ReadOnlySpan<byte> GetFieldAsSpan(NamePlateStringField field);

    /// <summary>
    /// Gets a UTF8 string copy of the string array value in the provided field.
    /// </summary>
    /// <param name="field">The field to read from.</param>
    /// <returns>A copy of the string array value as a string.</returns>
    string GetFieldAsString(NamePlateStringField field);

    /// <summary>
    /// Gets a parsed SeString copy of the string array value in the provided field.
    /// </summary>
    /// <param name="field">The field to read from.</param>
    /// <returns>A copy of the string array value as a parsed SeString.</returns>
    SeString GetFieldAsSeString(NamePlateStringField field);

    /// <summary>
    /// Sets the string array value for the provided field.
    /// </summary>
    /// <param name="field">The field to write to.</param>
    /// <param name="value">The string to write.</param>
    void SetField(NamePlateStringField field, string value);

    /// <summary>
    /// Sets the string array value for the provided field.
    /// </summary>
    /// <param name="field">The field to write to.</param>
    /// <param name="value">The SeString to write.</param>
    void SetField(NamePlateStringField field, SeString value);

    /// <summary>
    /// Sets the string array value for the provided field. The provided byte sequence must be null-terminated.
    /// </summary>
    /// <param name="field">The field to write to.</param>
    /// <param name="value">The ReadOnlySpan of bytes to write.</param>
    void SetField(NamePlateStringField field, ReadOnlySpan<byte> value);

    /// <summary>
    /// Sets the string array value for the provided field. The provided byte sequence must be null-terminated.
    /// </summary>
    /// <param name="field">The field to write to.</param>
    /// <param name="value">The pointer to a null-terminated sequence of bytes to write.</param>
    unsafe void SetField(NamePlateStringField field, byte* value);

    /// <summary>
    /// Sets the string array value for the provided field to a fixed pointer to an empty string in unmanaged memory.
    /// Other methods may notice this fixed pointer and refuse to overwrite it, preserving the emptiness of the field.
    /// </summary>
    /// <param name="field">The field to write to.</param>
    void RemoveField(NamePlateStringField field);
}

/// <summary>
/// A class representing a single nameplate. Provides mechanisms to look up the game object associated with the
/// nameplate and allows for modification of various backing fields in number and string array data, which in turn
/// affect aspects of the nameplate's appearance when drawn. Instances of this class are only valid for a single frame
/// and should not be kept across frames.
/// </summary>
internal unsafe class NamePlateUpdateHandler : INamePlateUpdateHandler
{
    private readonly NamePlateUpdateContext context;

    private ulong? gameObjectId;
    private IGameObject? gameObject;
    private NamePlateInfoView? infoView;
    private NamePlatePartsContainer? partsContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamePlateUpdateHandler"/> class.
    /// </summary>
    /// <param name="context">The current update context.</param>
    /// <param name="arrayIndex">The index for this nameplate data in the backing number and string array data. This is
    /// not the same as the rendered index, which can be retrieved from <see cref="NamePlateIndex"/>.</param>
    internal NamePlateUpdateHandler(NamePlateUpdateContext context, int arrayIndex)
    {
        this.context = context;
        this.ArrayIndex = arrayIndex;
    }

    /// <inheritdoc/>
    public int ArrayIndex { get; }

    /// <inheritdoc/>
    public ulong GameObjectId => this.gameObjectId ??= this.NamePlateInfo->ObjectId;

    /// <inheritdoc/>
    public IGameObject? GameObject => this.gameObject ??= this.context.ObjectTable[
                                          this.context.Ui3DModule->NamePlateObjectInfoPointers[this.ArrayIndex]
                                              .Value->GameObject->ObjectIndex];

    /// <inheritdoc/>
    public IBattleChara? BattleChara => this.GameObject as IBattleChara;

    /// <inheritdoc/>
    public IPlayerCharacter? PlayerCharacter => this.GameObject as IPlayerCharacter;

    /// <inheritdoc/>
    public INamePlateInfoView InfoView => this.infoView ??= new NamePlateInfoView(this.NamePlateInfo);

    /// <inheritdoc/>
    public nint NamePlateInfoAddress => (nint)this.NamePlateInfo;

    /// <inheritdoc/>
    public nint NamePlateObjectAddress => (nint)this.NamePlateObject;

    /// <inheritdoc/>
    public NamePlateKind NamePlateKind => (NamePlateKind)this.ObjectData->NamePlateKind;

    /// <inheritdoc/>
    public int UpdateFlags
    {
        get => this.ObjectData->UpdateFlags;
        private set => this.ObjectData->UpdateFlags = value;
    }

    /// <inheritdoc/>
    public uint TextColor
    {
        get => this.ObjectData->NameTextColor;
        set
        {
            if (value != this.TextColor) this.UpdateFlags |= 2;
            this.ObjectData->NameTextColor = value;
        }
    }

    /// <inheritdoc/>
    public uint EdgeColor
    {
        get => this.ObjectData->NameEdgeColor;
        set
        {
            if (value != this.EdgeColor) this.UpdateFlags |= 2;
            this.ObjectData->NameEdgeColor = value;
        }
    }

    /// <inheritdoc/>
    public int MarkerIconId
    {
        get => this.ObjectData->MarkerIconId;
        set => this.ObjectData->MarkerIconId = value;
    }

    /// <inheritdoc/>
    public int NameIconId
    {
        get => this.ObjectData->NameIconId;
        set => this.ObjectData->NameIconId = value;
    }

    /// <inheritdoc/>
    public int NamePlateIndex => this.ObjectData->NamePlateObjectIndex;

    /// <inheritdoc/>
    public int DrawFlags
    {
        get => this.ObjectData->DrawFlags;
        private set => this.ObjectData->DrawFlags = value;
    }

    /// <inheritdoc/>
    public int VisibilityFlags
    {
        get => ObjectData->VisibilityFlags;
        set => ObjectData->VisibilityFlags = value;
    }

    /// <inheritdoc/>
    public bool IsUpdating => (this.UpdateFlags & 1) != 0;

    /// <inheritdoc/>
    public bool IsPrefixTitle
    {
        get => (this.DrawFlags & 1) != 0;
        set => this.DrawFlags = value ? this.DrawFlags | 1 : this.DrawFlags & ~1;
    }

    /// <inheritdoc/>
    public bool DisplayTitle
    {
        get => (this.DrawFlags & 0x80) == 0;
        set => this.DrawFlags = value ? this.DrawFlags & ~0x80 : this.DrawFlags | 0x80;
    }

    /// <inheritdoc/>
    public SeString Name
    {
        get => this.GetFieldAsSeString(NamePlateStringField.Name);
        set => this.WeakSetField(NamePlateStringField.Name, value);
    }

    /// <inheritdoc/>
    public NamePlateSimpleParts NameParts => this.PartsContainer.Name;

    /// <inheritdoc/>
    public SeString Title
    {
        get => this.GetFieldAsSeString(NamePlateStringField.Title);
        set => this.WeakSetField(NamePlateStringField.Title, value);
    }

    /// <inheritdoc/>
    public NamePlateQuotedParts TitleParts => this.PartsContainer.Title;

    /// <inheritdoc/>
    public SeString FreeCompanyTag
    {
        get => this.GetFieldAsSeString(NamePlateStringField.FreeCompanyTag);
        set => this.WeakSetField(NamePlateStringField.FreeCompanyTag, value);
    }

    /// <inheritdoc/>
    public NamePlateQuotedParts FreeCompanyTagParts => this.PartsContainer.FreeCompanyTag;

    /// <inheritdoc/>
    public SeString StatusPrefix
    {
        get => this.GetFieldAsSeString(NamePlateStringField.StatusPrefix);
        set => this.WeakSetField(NamePlateStringField.StatusPrefix, value);
    }

    /// <inheritdoc/>
    public SeString TargetSuffix
    {
        get => this.GetFieldAsSeString(NamePlateStringField.TargetSuffix);
        set => this.WeakSetField(NamePlateStringField.TargetSuffix, value);
    }

    /// <inheritdoc/>
    public SeString LevelPrefix
    {
        get => this.GetFieldAsSeString(NamePlateStringField.LevelPrefix);
        set => this.WeakSetField(NamePlateStringField.LevelPrefix, value);
    }

    /// <summary>
    /// Gets or (lazily) creates a part builder container for this nameplate.
    /// </summary>
    internal NamePlatePartsContainer PartsContainer =>
        this.partsContainer ??= new NamePlatePartsContainer(this.context);

    private RaptureAtkModule.NamePlateInfo* NamePlateInfo =>
        this.context.RaptureAtkModule->NamePlateInfoEntries.GetPointer(this.NamePlateIndex);

    private AddonNamePlate.NamePlateObject* NamePlateObject =>
        &this.context.Addon->NamePlateObjectArray[this.NamePlateIndex];

    private AddonNamePlate.AddonNamePlateNumberArray.NamePlateObjectIntArrayData* ObjectData =>
        this.context.NumberStruct->ObjectData.GetPointer(this.ArrayIndex);

    /// <inheritdoc/>
    public void RemoveName() => this.RemoveField(NamePlateStringField.Name);

    /// <inheritdoc/>
    public void RemoveTitle() => this.RemoveField(NamePlateStringField.Title);

    /// <inheritdoc/>
    public void RemoveFreeCompanyTag() => this.RemoveField(NamePlateStringField.FreeCompanyTag);

    /// <inheritdoc/>
    public void RemoveStatusPrefix() => this.RemoveField(NamePlateStringField.StatusPrefix);

    /// <inheritdoc/>
    public void RemoveTargetSuffix() => this.RemoveField(NamePlateStringField.TargetSuffix);

    /// <inheritdoc/>
    public void RemoveLevelPrefix() => this.RemoveField(NamePlateStringField.LevelPrefix);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetFieldAsPointer(NamePlateStringField field)
    {
        return this.context.StringData->StringArray[this.ArrayIndex + (int)field];
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetFieldAsSpan(NamePlateStringField field)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(this.GetFieldAsPointer(field));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetFieldAsString(NamePlateStringField field)
    {
        return Encoding.UTF8.GetString(this.GetFieldAsSpan(field));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeString GetFieldAsSeString(NamePlateStringField field)
    {
        return SeString.Parse(this.GetFieldAsSpan(field));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, string value)
    {
        this.context.StringData->SetValue(this.ArrayIndex + (int)field, value, true, true, true);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, SeString value)
    {
        this.context.StringData->SetValue(
            this.ArrayIndex + (int)field,
            value.EncodeWithNullTerminator(),
            true,
            true,
            true);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, ReadOnlySpan<byte> value)
    {
        this.context.StringData->SetValue(this.ArrayIndex + (int)field, value, true, true, true);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, byte* value)
    {
        this.context.StringData->SetValue(this.ArrayIndex + (int)field, value, true, true, true);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveField(NamePlateStringField field)
    {
        this.context.StringData->SetValue(
            this.ArrayIndex + (int)field,
            (byte*)NamePlateGui.EmptyStringPointer,
            true,
            false,
            true);
    }

    /// <summary>
    /// Resets the state of this handler for re-use in a new update.
    /// </summary>
    internal void ResetState()
    {
        this.gameObjectId = null;
        this.gameObject = null;
        this.infoView = null;
        this.partsContainer = null;
    }

    /// <summary>
    /// Sets the string array value for the provided field, unless it was already set to the special empty string
    /// pointer used by the Remove methods.
    /// </summary>
    /// <param name="field">The field to write to.</param>
    /// <param name="value">The SeString to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WeakSetField(NamePlateStringField field, SeString value)
    {
        if ((nint)this.GetFieldAsPointer(field) == NamePlateGui.EmptyStringPointer)
            return;
        this.context.StringData->SetValue(
            this.ArrayIndex + (int)field,
            value.EncodeWithNullTerminator(),
            true,
            true,
            true);
    }
}
