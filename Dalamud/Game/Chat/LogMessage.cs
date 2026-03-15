using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Dalamud.Data;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;

using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Chat;

/// <summary>
/// Interface representing a log message.
/// </summary>
public interface ILogMessage : IEquatable<ILogMessage>
{
    /// <summary>
    /// Gets the address of the log message in memory.
    /// </summary>
    nint Address { get; }

    /// <summary>
    /// Gets the ID of this log message.
    /// </summary>
    uint LogMessageId { get; }

    /// <summary>
    /// Gets the GameData associated with this log message.
    /// </summary>
    RowRef<Lumina.Excel.Sheets.LogMessage> GameData { get; }

    /// <summary>
    /// Gets the entity that is the source of this log message, if any.
    /// </summary>
    ILogMessageEntity? SourceEntity { get; }

    /// <summary>
    /// Gets the entity that is the target of this log message, if any.
    /// </summary>
    ILogMessageEntity? TargetEntity { get; }

    /// <summary>
    /// Gets the number of parameters.
    /// </summary>
    int ParameterCount { get; }

    /// <summary>
    /// Gets a list containing the parameters. The returned object is only valid during the <see cref="global::Dalamud.Plugin.Services.IChatGui.LogMessage"/> event and must not be accessed after returning from it.
    /// </summary>
    IReadOnlyList<SeStringParameter> Parameters { get; }

    /// <summary>
    /// Gets a value indicating whether the message is handled and will not appear in chat.
    /// </summary>
    bool IsHandled { get; }

    /// <summary>
    /// Marks this message as handled (<see cref="ILogMessage.IsHandled"/> = <see langword="true"/>) and prevents it from appearing.
    /// </summary>
    void PreventOriginal();

    /// <summary>
    /// Retrieves the value of a parameter for the log message if it is an int.
    /// </summary>
    /// <param name="index">The index of the parameter to retrieve.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns><see langword="true"/> if the parameter was retrieved successfully.</returns>
    bool TryGetIntParameter(int index, out int value);

    /// <summary>
    /// Retrieves the value of a parameter for the log message if it is a string.
    /// </summary>
    /// <param name="index">The index of the parameter to retrieve.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns><see langword="true"/> if the parameter was retrieved successfully.</returns>
    bool TryGetStringParameter(int index, out ReadOnlySeString value);

    /// <summary>
    /// Formats this log message into an approximation of the string that will eventually be shown in the log.
    /// </summary>
    /// <remarks>This can cause side effects such as playing sound effects and thus should only be used for debugging.</remarks>
    /// <returns>The formatted string.</returns>
    ReadOnlySeString FormatLogMessageForDebugging();
}

/// <summary>
/// This class represents log message in the queue to be added to the chat.
/// </summary>
internal unsafe class LogMessage : ILogMessage
{
    /// <summary>
    /// Gets a shared instance of this class.
    /// </summary>
    public static LogMessage Instance { get; } = new();

    /// <summary>
    /// Gets or sets the native message wrapped by this object.
    /// </summary>
    public LogMessageQueueItem* Pointer { get; set; }

    /// <inheritdoc/>
    public nint Address => (nint)this.Pointer;

    /// <inheritdoc/>
    public uint LogMessageId => this.Pointer->LogMessageId;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.LogMessage> GameData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.LogMessage>(this.Pointer->LogMessageId);

    /// <inheritdoc/>
    ILogMessageEntity? ILogMessage.SourceEntity => this.Pointer->SourceKind == EntityRelationKind.None ? null : this.SourceEntity;

    /// <inheritdoc/>
    ILogMessageEntity? ILogMessage.TargetEntity => this.Pointer->TargetKind == EntityRelationKind.None ? null : this.TargetEntity;

    /// <inheritdoc/>
    public int ParameterCount => this.Pointer->Parameters.Count;

    /// <inheritdoc/>
    public IReadOnlyList<SeStringParameter> Parameters => LogMessageParameterList.Instance;

    /// <inheritdoc/>
    public bool IsHandled { get; set; }

    private LogMessageEntity SourceEntity => new(this.Pointer, true);

    private LogMessageEntity TargetEntity => new(this.Pointer, false);

    public static bool operator ==(LogMessage x, LogMessage y) => x.Equals(y);

    public static bool operator !=(LogMessage x, LogMessage y) => !(x == y);

    /// <inheritdoc/>
    public bool Equals(ILogMessage? other)
    {
        return other is LogMessage logMessage && this.Equals(logMessage);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is LogMessage logMessage && this.Equals(logMessage);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.LogMessageId, this.SourceEntity, this.TargetEntity);
    }

    /// <inheritdoc/>
    public void PreventOriginal()
    {
        this.IsHandled = true;
    }

    /// <inheritdoc/>
    public bool TryGetIntParameter(int index, out int value)
    {
        value = 0;
        if (!this.TryGetParameter(index, out var parameter)) return false;
        if (parameter.Type != TextParameterType.Integer) return false;
        value = parameter.IntValue;
        return true;
    }

    /// <inheritdoc/>
    public bool TryGetStringParameter(int index, out ReadOnlySeString value)
    {
        value = default;
        if (!this.TryGetParameter(index, out var parameter)) return false;
        if (parameter.Type == TextParameterType.String)
        {
            value = new(parameter.StringValue.AsSpan());
            return true;
        }

        if (parameter.Type == TextParameterType.ReferencedUtf8String)
        {
            value = new(parameter.ReferencedUtf8StringValue->Utf8String.AsSpan());
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public ReadOnlySeString FormatLogMessageForDebugging()
    {
        var logModule = RaptureLogModule.Instance();

        // the formatting logic is taken from RaptureLogModule_Update

        using var utf8 = new Utf8String();
        SetName(logModule, this.SourceEntity);
        SetName(logModule, this.TargetEntity);

        using var rssb = new RentedSeStringBuilder();
        logModule->RaptureTextModule->FormatString(rssb.Builder.Append(this.GameData.Value.Text).GetViewAsSpan(), &this.Pointer->Parameters, &utf8);

        return new ReadOnlySeString(utf8.AsSpan());

        static void SetName(RaptureLogModule* self, LogMessageEntity item)
        {
            var name = item.NameSpan.GetPointer(0);

            if (item.IsPlayer)
            {
                var str = self->TempParseMessage.GetPointer(item.IsSourceEntity ? 8 : 9);
                self->FormatPlayerLink(name, str, null, 0, item.Kind != 1 /* LocalPlayer */, item.HomeWorldId, false, null, false);

                if (item.HomeWorldId != 0 && item.HomeWorldId != AgentLobby.Instance()->LobbyData.HomeWorldId)
                {
                    var crossWorldSymbol = self->RaptureTextModule->UnkStrings0.GetPointer(3);
                    if (!crossWorldSymbol->StringPtr.HasValue)
                        self->RaptureTextModule->ProcessMacroCode(crossWorldSymbol, "<icon(88)>\0"u8);
                    str->Append(crossWorldSymbol);
                    if (self->UIModule->GetWorldHelper()->AllWorlds.TryGetValuePointer(item.HomeWorldId, out var world))
                        str->ConcatCStr(world->Name);
                }

                name = str->StringPtr;
            }

            if (item.IsSourceEntity)
            {
                self->RaptureTextModule->SetGlobalTempEntity1(name, item.Sex, item.ObjStrId);
            }
            else
            {
                self->RaptureTextModule->SetGlobalTempEntity2(name, item.Sex, item.ObjStrId);
            }
        }
    }

    private bool TryGetParameter(int index, out TextParameter value)
    {
        if (index < 0 || index >= this.Pointer->Parameters.Count)
        {
            value = default;
            return false;
        }

        value = this.Pointer->Parameters[index];
        return true;
    }

    private bool Equals(LogMessage other)
    {
        return this.LogMessageId == other.LogMessageId && this.SourceEntity == other.SourceEntity && this.TargetEntity == other.TargetEntity;
    }
}

/// <summary>
/// This struct represents log message in the queue to be added to the chat.
/// </summary>
internal unsafe class LogMessageParameterList : IReadOnlyList<SeStringParameter>
{
    /// <summary>
    /// Gets a shared instance of this class.
    /// </summary>
    public static LogMessageParameterList Instance { get; } = new();

    /// <summary>
    /// Gets or sets the native list wrapped by this object.
    /// </summary>
    public StdDeque<TextParameter>* Pointer { get; set; }

    /// <inheritdoc />
    public int Count => this.Pointer->Count;

    /// <inheritdoc />
    public SeStringParameter this[int index]
    {
        get
        {
            var p = (*this.Pointer)[index];

            if (p.Type == TextParameterType.Uninitialized)
                return default;
            if (p.Type == TextParameterType.Integer)
                return new((uint)p.IntValue);
            if (p.Type == TextParameterType.String)
                return new(p.StringValue.AsReadOnlySeString());
            if (p.Type == TextParameterType.ReferencedUtf8String)
                return new(p.ReferencedUtf8StringValue->Utf8String.AsReadOnlySeString());

            throw new InvalidOperationException($"Invalid parameter type {p.Type}");
        }
    }

    /// <inheritdoc />
    public IEnumerator<SeStringParameter> GetEnumerator()
    {
        for (var i = 0; i < this.Count; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
