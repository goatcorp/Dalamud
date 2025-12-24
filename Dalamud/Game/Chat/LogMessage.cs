using Dalamud.Data;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.Interop;

using Lumina.Excel;
using Lumina.Text.ReadOnly;

using System.Diagnostics.CodeAnalysis;

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
/// This struct represents log message in the queue to be added to the chat.
/// </summary>
/// <param name="ptr">A pointer to the log message.</param>
internal unsafe readonly struct LogMessage(LogMessageQueueItem* ptr) : ILogMessage
{
    /// <inheritdoc/>
    public nint Address => (nint)ptr;

    /// <inheritdoc/>
    public uint LogMessageId => ptr->LogMessageId;

    /// <inheritdoc/>
    public RowRef<Lumina.Excel.Sheets.LogMessage> GameData => LuminaUtils.CreateRef<Lumina.Excel.Sheets.LogMessage>(ptr->LogMessageId);

    public LogMessageEntity SourceEntity => new LogMessageEntity(ptr, true);
    /// <inheritdoc/>
    ILogMessageEntity? ILogMessage.SourceEntity => ptr->SourceKind == EntityRelationKind.None ? null : this.SourceEntity;

    public LogMessageEntity TargetEntity => new LogMessageEntity(ptr, false);

    /// <inheritdoc/>
    ILogMessageEntity? ILogMessage.TargetEntity => ptr->TargetKind == EntityRelationKind.None ? null : this.TargetEntity;

    /// <inheritdoc/>
    public int ParameterCount => ptr->Parameters.Count;

    public bool TryGetParameter(int index, out TextParameter value)
    {
        if (index < 0 || index >= ptr->Parameters.Count)
        {
            value = default;
            return false;
        }

        value = ptr->Parameters[index];
        return true;
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
        logModule->RaptureTextModule->FormatString(rssb.Builder.Append(this.GameData.Value.Text).GetViewAsSpan(), &ptr->Parameters, &utf8);

        return new ReadOnlySeString(utf8.AsSpan());

        void SetName(RaptureLogModule* self, LogMessageEntity item)
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


    public static bool operator ==(LogMessage x, LogMessage y) => x.Equals(y);

    public static bool operator !=(LogMessage x, LogMessage y) => !(x == y);

    public bool Equals(LogMessage other)
    {
        return this.LogMessageId == other.LogMessageId && this.SourceEntity == other.SourceEntity && this.TargetEntity == other.TargetEntity;
    }

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
}
