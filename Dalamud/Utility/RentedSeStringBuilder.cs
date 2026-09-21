using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Lumina.Text;
using Lumina.Text.Expressions;
using Lumina.Text.Parse;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Microsoft.Extensions.ObjectPool;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Dalamud.Utility;

/// <summary>
/// Provides a temporarily rented <see cref="SeStringBuilder"/> from a shared pool.
/// </summary>
public readonly struct RentedSeStringBuilder() : IResettable, IDisposable
{
    /// <summary>
    /// Gets the rented <see cref="SeStringBuilder"/> value from the shared pool.
    /// </summary>
    public SeStringBuilder Builder { get; } = SeStringBuilder.SharedPool.Get();

    /// <summary>
    /// Returns the rented <see cref="SeStringBuilder"/> to the shared pool.
    /// </summary>
    public void Dispose() => SeStringBuilder.SharedPool.Return(this.Builder);

    #region Lumina\Text\SeStringBuilder.cs

    /// <inheritdoc cref="SeStringBuilder.BeginMacro(MacroCode)"/>
    public SeStringBuilder BeginMacro(MacroCode macroCode) => this.Builder.BeginMacro(macroCode);

    /// <inheritdoc cref="SeStringBuilder.EndMacro()"/>
    public SeStringBuilder EndMacro() => this.Builder.EndMacro();

    /// <inheritdoc cref="SeStringBuilder.AbortMacro()"/>
    public SeStringBuilder AbortMacro() => this.Builder.AbortMacro();

    /// <inheritdoc cref="SeStringBuilder.Clear(bool)"/>
    public SeStringBuilder Clear(bool zeroBuffer = false) => this.Builder.Clear(zeroBuffer);

    /// <inheritdoc cref="SeStringBuilder.GetViewAsMemory"/>
    public ReadOnlyMemory<byte> GetViewAsMemory() => this.Builder.GetViewAsMemory();

    /// <inheritdoc cref="SeStringBuilder.GetViewAsSpan"/>
    public ReadOnlySpan<byte> GetViewAsSpan() => this.Builder.GetViewAsSpan();

    /// <inheritdoc cref="SeStringBuilder.ToArray"/>
    public byte[] ToArray() => this.Builder.ToArray();

    /// <inheritdoc cref="SeStringBuilder.ToReadOnlySeString"/>
    public ReadOnlySeString ToReadOnlySeString() => this.Builder.ToReadOnlySeString();

    /// <inheritdoc cref="SeStringBuilder.TryReset"/>
    public bool TryReset() => this.Builder.TryReset();

    #endregion

    #region Lumina\Text\SeStringBuilder.Append.cs

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlySpan{char})"/>
    public SeStringBuilder Append(ReadOnlySpan<char> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(Span{char})"/>
    public SeStringBuilder Append(Span<char> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlyMemory{char})"/>
    public SeStringBuilder Append(ReadOnlyMemory<char> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(Memory{char})"/>
    public SeStringBuilder Append(Memory<char> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(char[])"/>
    public SeStringBuilder Append(char[] value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(char[],int,int)"/>
    public SeStringBuilder Append(char[] value, int startIndex, int count) => this.Builder.Append(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.Append(string?)"/>
    public SeStringBuilder Append(string? value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(string?,int,int)"/>
    public SeStringBuilder Append(string? value, int startIndex, int count) => this.Builder.Append(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.Append(SeStringBuilder.SeStringInterpolatedStringHandler)"/>
    public SeStringBuilder Append([InterpolatedStringHandlerArgument("")] SeStringBuilder.SeStringInterpolatedStringHandler handler) => this.Builder.Append(handler);

    /// <inheritdoc cref="SeStringBuilder.Append(IFormatProvider?,SeStringBuilder.SeStringInterpolatedStringHandler)"/>
    public SeStringBuilder Append(IFormatProvider? provider, [InterpolatedStringHandlerArgument(["", "provider"])] SeStringBuilder.SeStringInterpolatedStringHandler handler) => this.Builder.Append(provider, handler);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlySpan{byte})"/>
    public SeStringBuilder Append(ReadOnlySpan<byte> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(Span{byte})"/>
    public SeStringBuilder Append(Span<byte> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlyMemory{byte})"/>
    public SeStringBuilder Append(ReadOnlyMemory<byte> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(Memory{byte})"/>
    public SeStringBuilder Append(Memory<byte> value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(byte[])"/>
    public SeStringBuilder Append(byte[] value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(byte[],int,int)"/>
    public SeStringBuilder Append(byte[] value, int startIndex, int count) => this.Builder.Append(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.Append(StringBuilder)"/>
    public SeStringBuilder Append(StringBuilder value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(StringBuilder,int,int)"/>
    public SeStringBuilder Append(StringBuilder value, int startIndex, int count) => this.Builder.Append(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlySeString)"/>
    public SeStringBuilder Append(ReadOnlySeString value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlySeStringSpan)"/>
    public SeStringBuilder Append(ReadOnlySeStringSpan value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlySePayload)"/>
    public SeStringBuilder Append(ReadOnlySePayload value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ReadOnlySePayloadSpan)"/>
    public SeStringBuilder Append(ReadOnlySePayloadSpan value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in UtfEnumerator)"/>
    public SeStringBuilder Append(scoped in UtfEnumerator enumerator) => this.Builder.Append(enumerator);

    /// <inheritdoc cref="SeStringBuilder.AppendChar(int)"/>
    public SeStringBuilder AppendChar(int codepoint) => this.Builder.AppendChar(codepoint);

    /// <inheritdoc cref="SeStringBuilder.AppendChar(uint)"/>
    public SeStringBuilder AppendChar(uint codepoint) => this.Builder.AppendChar(codepoint);

    /// <inheritdoc cref="SeStringBuilder.AppendMacroString(ReadOnlySpan{byte},in MacroStringParseOptions)"/>
    public SeStringBuilder AppendMacroString(ReadOnlySpan<byte> value, scoped in MacroStringParseOptions parseOptions = default) => this.Builder.AppendMacroString(value, parseOptions);

    /// <inheritdoc cref="SeStringBuilder.AppendMacroString(ReadOnlySpan{char},in MacroStringParseOptions)"/>
    public SeStringBuilder AppendMacroString(ReadOnlySpan<char> value, scoped in MacroStringParseOptions parseOptions = default) => this.Builder.AppendMacroString(value, parseOptions);

    #endregion

    #region Lumina\Text\SeStringBuilder.AppendLine.cs

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlySpan{char})"/>
    public SeStringBuilder AppendLine(ReadOnlySpan<char> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(Span{char})"/>
    public SeStringBuilder AppendLine(Span<char> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlyMemory{char})"/>
    public SeStringBuilder AppendLine(ReadOnlyMemory<char> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(Memory{char})"/>
    public SeStringBuilder AppendLine(Memory<char> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(char[])"/>
    public SeStringBuilder AppendLine(char[] value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(char[],int,int)"/>
    public SeStringBuilder AppendLine(char[] value, int startIndex, int count) => this.Builder.AppendLine(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(string?)"/>
    public SeStringBuilder AppendLine(string? value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(string?,int,int)"/>
    public SeStringBuilder AppendLine(string? value, int startIndex, int count) => this.Builder.AppendLine(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(SeStringBuilder.SeStringInterpolatedStringHandler)"/>
    public SeStringBuilder AppendLine([InterpolatedStringHandlerArgument("")] SeStringBuilder.SeStringInterpolatedStringHandler handler) => this.Builder.AppendLine(handler);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(IFormatProvider?,SeStringBuilder.SeStringInterpolatedStringHandler)"/>
    public SeStringBuilder AppendLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument(["", "provider"])] SeStringBuilder.SeStringInterpolatedStringHandler handler) => this.Builder.AppendLine(provider, handler);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlySpan{byte})"/>
    public SeStringBuilder AppendLine(ReadOnlySpan<byte> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(Span{byte})"/>
    public SeStringBuilder AppendLine(Span<byte> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlyMemory{byte})"/>
    public SeStringBuilder AppendLine(ReadOnlyMemory<byte> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(Memory{byte})"/>
    public SeStringBuilder AppendLine(Memory<byte> value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(byte[])"/>
    public SeStringBuilder AppendLine(byte[] value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(byte[],int,int)"/>
    public SeStringBuilder AppendLine(byte[] value, int startIndex, int count) => this.Builder.AppendLine(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(StringBuilder)"/>
    public SeStringBuilder AppendLine(StringBuilder value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(StringBuilder,int,int)"/>
    public SeStringBuilder AppendLine(StringBuilder value, int startIndex, int count) => this.Builder.AppendLine(value, startIndex, count);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlySeString)"/>
    public SeStringBuilder AppendLine(ReadOnlySeString value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlySePayload)"/>
    public SeStringBuilder AppendLine(ReadOnlySePayload value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlySePayloadSpan)"/>
    public SeStringBuilder AppendLine(ReadOnlySePayloadSpan value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(ReadOnlySeStringSpan)"/>
    public SeStringBuilder AppendLine(ReadOnlySeStringSpan value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine(object?)"/>
    public SeStringBuilder AppendLine(object? value) => this.Builder.AppendLine(value);

    /// <inheritdoc cref="SeStringBuilder.AppendLine{T}(in T)"/>
    public SeStringBuilder AppendLine<T>(scoped in T value) where T : struct => this.Builder.AppendLine(value);

    #endregion

    #region Lumina\Text\SeStringBuilder.AppendTypedElement.cs

    /// <inheritdoc cref="SeStringBuilder.Append(Rune)"/>
    public SeStringBuilder Append(Rune rune) => this.Builder.Append(rune);

    /// <inheritdoc cref="SeStringBuilder.Append(Rune,int)"/>
    public SeStringBuilder Append(Rune rune, int repeatCount) => this.Builder.Append(rune, repeatCount);

    /// <inheritdoc cref="SeStringBuilder.Append(UtfValue)"/>
    public SeStringBuilder Append(UtfValue utfValue) => this.Builder.Append(utfValue);

    /// <inheritdoc cref="SeStringBuilder.Append(in UtfEnumerator.Subsequence)"/>
    public SeStringBuilder Append(scoped in UtfEnumerator.Subsequence subsequence) => this.Builder.Append(subsequence);

    /// <inheritdoc cref="SeStringBuilder.Append(bool)"/>
    public SeStringBuilder Append(bool value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(char)"/>
    public SeStringBuilder Append(char value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(byte)"/>
    public SeStringBuilder Append(byte value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ushort)"/>
    public SeStringBuilder Append(ushort value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(uint)"/>
    public SeStringBuilder Append(uint value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(ulong)"/>
    public SeStringBuilder Append(ulong value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(nuint)"/>
    public SeStringBuilder Append(nuint value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(sbyte)"/>
    public SeStringBuilder Append(sbyte value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(short)"/>
    public SeStringBuilder Append(short value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(int)"/>
    public SeStringBuilder Append(int value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(long)"/>
    public SeStringBuilder Append(long value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(nint)"/>
    public SeStringBuilder Append(nint value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(void*)"/>
    public unsafe SeStringBuilder Append(void* value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(Half)"/>
    public SeStringBuilder Append(Half value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(float)"/>
    public SeStringBuilder Append(float value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(double)"/>
    public SeStringBuilder Append(double value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in decimal)"/>
    public SeStringBuilder Append(scoped in decimal value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in BigInteger)"/>
    public SeStringBuilder Append(scoped in BigInteger value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(DateOnly)"/>
    public SeStringBuilder Append(DateOnly value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(TimeOnly)"/>
    public SeStringBuilder Append(TimeOnly value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(TimeSpan)"/>
    public SeStringBuilder Append(TimeSpan value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(DateTime)"/>
    public SeStringBuilder Append(DateTime value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(DateTimeOffset)"/>
    public SeStringBuilder Append(DateTimeOffset value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in Version)"/>
    public SeStringBuilder Append(scoped in Version value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in Complex)"/>
    public SeStringBuilder Append(scoped in Complex value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in UInt128)"/>
    public SeStringBuilder Append(scoped in UInt128 value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(in Int128)"/>
    public SeStringBuilder Append(scoped in Int128 value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(IPAddress)"/>
    public SeStringBuilder Append(IPAddress value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(IPNetwork)"/>
    public SeStringBuilder Append(IPNetwork value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append(object?)"/>
    public SeStringBuilder Append(object? value) => this.Builder.Append(value);

    /// <inheritdoc cref="SeStringBuilder.Append{T}(in T)"/>
    public SeStringBuilder Append<T>(scoped in T value) => this.Builder.Append(value);

    #endregion

    #region Lumina\Text\SeStringBuilder.Expressions.cs

    /// <inheritdoc cref="SeStringBuilder.AppendIntExpression(int)"/>
    public SeStringBuilder AppendIntExpression(int value) => this.Builder.AppendIntExpression(value);

    /// <inheritdoc cref="SeStringBuilder.AppendUIntExpression(uint)"/>
    public SeStringBuilder AppendUIntExpression(uint value) => this.Builder.AppendUIntExpression(value);

    /// <inheritdoc cref="SeStringBuilder.AppendNullaryExpression(ExpressionType)"/>
    public SeStringBuilder AppendNullaryExpression(ExpressionType expressionType) => this.Builder.AppendNullaryExpression(expressionType);

    /// <inheritdoc cref="SeStringBuilder.BeginUnaryExpression(ExpressionType)"/>
    public SeStringBuilder BeginUnaryExpression(ExpressionType expressionType) => this.Builder.BeginUnaryExpression(expressionType);

    /// <inheritdoc cref="SeStringBuilder.BeginBinaryExpression(ExpressionType)"/>
    public SeStringBuilder BeginBinaryExpression(ExpressionType expressionType) => this.Builder.BeginBinaryExpression(expressionType);

    /// <inheritdoc cref="SeStringBuilder.ChangeBinaryExpression(ExpressionType)"/>
    public SeStringBuilder ChangeBinaryExpression(ExpressionType expressionType) => this.Builder.ChangeBinaryExpression(expressionType);

    /// <inheritdoc cref="SeStringBuilder.BeginStringExpression()"/>
    public SeStringBuilder BeginStringExpression() => this.Builder.BeginStringExpression();

    /// <inheritdoc cref="SeStringBuilder.EndExpression()"/>
    public SeStringBuilder EndExpression() => this.Builder.EndExpression();

    /// <inheritdoc cref="SeStringBuilder.AbortExpression()"/>
    public SeStringBuilder AbortExpression() => this.Builder.AbortExpression();

    #endregion

    #region Lumina\Text\SeStringBuilder.Presets.cs

    /// <inheritdoc cref="SeStringBuilder.AppendLocalNumberExpression(int)"/>
    public SeStringBuilder AppendLocalNumberExpression(int id) => this.Builder.AppendLocalNumberExpression(id);

    /// <inheritdoc cref="SeStringBuilder.AppendLocalStringExpression(int)"/>
    public SeStringBuilder AppendLocalStringExpression(int id) => this.Builder.AppendLocalStringExpression(id);

    /// <inheritdoc cref="SeStringBuilder.AppendGlobalNumberExpression(int)"/>
    public SeStringBuilder AppendGlobalNumberExpression(int id) => this.Builder.AppendGlobalNumberExpression(id);

    /// <inheritdoc cref="SeStringBuilder.AppendGlobalStringExpression(int)"/>
    public SeStringBuilder AppendGlobalStringExpression(int id) => this.Builder.AppendGlobalStringExpression(id);

    /// <inheritdoc cref="SeStringBuilder.AppendStringExpression(ReadOnlySeStringSpan)"/>
    public SeStringBuilder AppendStringExpression(ReadOnlySeStringSpan rosss) => this.Builder.AppendStringExpression(rosss);

    /// <inheritdoc cref="SeStringBuilder.AppendStringExpression(ReadOnlySpan{char})"/>
    public SeStringBuilder AppendStringExpression(ReadOnlySpan<char> str) => this.Builder.AppendStringExpression(str);

    /// <inheritdoc cref="SeStringBuilder.AppendNewLine()"/>
    public SeStringBuilder AppendNewLine() => this.Builder.AppendNewLine();

    /// <inheritdoc cref="SeStringBuilder.AppendIcon(uint)"/>
    public SeStringBuilder AppendIcon(uint icon) => this.Builder.AppendIcon(icon);

    /// <inheritdoc cref="SeStringBuilder.AppendItalicized(ReadOnlySeStringSpan)"/>
    public SeStringBuilder AppendItalicized(ReadOnlySeStringSpan rosss) => this.Builder.AppendItalicized(rosss);

    /// <inheritdoc cref="SeStringBuilder.AppendItalicized(ReadOnlySpan{char})"/>
    public SeStringBuilder AppendItalicized(ReadOnlySpan<char> str) => this.Builder.AppendItalicized(str);

    /// <inheritdoc cref="SeStringBuilder.AppendSetItalic(bool)"/>
    public SeStringBuilder AppendSetItalic(bool enable) => this.Builder.AppendSetItalic(enable);

    /// <inheritdoc cref="SeStringBuilder.AppendBold(ReadOnlySeStringSpan)"/>
    public SeStringBuilder AppendBold(ReadOnlySeStringSpan rosss) => this.Builder.AppendBold(rosss);

    /// <inheritdoc cref="SeStringBuilder.AppendBold(ReadOnlySpan{char})"/>
    public SeStringBuilder AppendBold(ReadOnlySpan<char> str) => this.Builder.AppendBold(str);

    /// <inheritdoc cref="SeStringBuilder.AppendSetBold(bool)"/>
    public SeStringBuilder AppendSetBold(bool enable) => this.Builder.AppendSetBold(enable);

    /// <inheritdoc cref="SeStringBuilder.PushLink(LinkMacroPayloadType,uint,uint,uint)"/>
    public SeStringBuilder PushLink(LinkMacroPayloadType type, uint arg1, uint arg2, uint arg3) => this.Builder.PushLink(type, arg1, arg2, arg3);

    /// <inheritdoc cref="SeStringBuilder.PushLink(LinkMacroPayloadType,uint,uint,uint,ReadOnlySeStringSpan)"/>
    public SeStringBuilder PushLink(LinkMacroPayloadType type, uint arg1, uint arg2, uint arg3, ReadOnlySeStringSpan plainText) => this.Builder.PushLink(type, arg1, arg2, arg3, plainText);

    /// <inheritdoc cref="SeStringBuilder.PushLink(LinkMacroPayloadType,uint,uint,uint,ReadOnlySpan{char})"/>
    public SeStringBuilder PushLink(LinkMacroPayloadType type, uint arg1, uint arg2, uint arg3, ReadOnlySpan<char> plainText) => this.Builder.PushLink(type, arg1, arg2, arg3, plainText);

    /// <inheritdoc cref="SeStringBuilder.PushLinkCharacter(ReadOnlySpan{char},uint)"/>
    public SeStringBuilder PushLinkCharacter(ReadOnlySpan<char> characterName, uint worldId = 0) => this.Builder.PushLinkCharacter(characterName, worldId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkItem(uint)"/>
    public SeStringBuilder PushLinkItem(uint itemId) => this.Builder.PushLinkItem(itemId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkItem(uint, ReadOnlySpan{char})"/>
    public SeStringBuilder PushLinkItem(uint itemId, ReadOnlySpan<char> plainText) => this.Builder.PushLinkItem(itemId, plainText);

    /// <inheritdoc cref="SeStringBuilder.PushLinkMapPosition(uint, uint, int, int)"/>
    public SeStringBuilder PushLinkMapPosition(uint territoryId, uint mapId, int rawX, int rawY) => this.Builder.PushLinkMapPosition(territoryId, mapId, rawX, rawY);

    /// <inheritdoc cref="SeStringBuilder.PushLinkQuest(uint)"/>
    public SeStringBuilder PushLinkQuest(uint questId) => this.Builder.PushLinkQuest(questId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkAchievement(uint)"/>
    public SeStringBuilder PushLinkAchievement(uint achievementId) => this.Builder.PushLinkAchievement(achievementId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkHowTo(uint)"/>
    public SeStringBuilder PushLinkHowTo(uint howToId) => this.Builder.PushLinkHowTo(howToId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkPartyFinderNotification()"/>
    public SeStringBuilder PushLinkPartyFinderNotification() => this.Builder.PushLinkPartyFinderNotification();

    /// <inheritdoc cref="SeStringBuilder.PushLinkStatus(uint)"/>
    public SeStringBuilder PushLinkStatus(uint statusId) => this.Builder.PushLinkStatus(statusId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkPartyFinderCrossWorld(uint,uint)"/>
    public SeStringBuilder PushLinkPartyFinderCrossWorld(uint listingId, uint worldId) => this.Builder.PushLinkPartyFinderCrossWorld(listingId, worldId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkPartyFinder(uint)"/>
    public SeStringBuilder PushLinkPartyFinder(uint listingId) => this.Builder.PushLinkPartyFinder(listingId);

    /// <inheritdoc cref="SeStringBuilder.PushLinkAkatsukiNote(uint)"/>
    public SeStringBuilder PushLinkAkatsukiNote(uint akatsukiNoteId) => this.Builder.PushLinkAkatsukiNote(akatsukiNoteId);

    /// <inheritdoc cref="SeStringBuilder.PopLink()"/>
    public SeStringBuilder PopLink() => this.Builder.PopLink();

    /// <inheritdoc cref="SeStringBuilder.PushColorType(uint)"/>
    public SeStringBuilder PushColorType(uint uiColorRowId) => this.Builder.PushColorType(uiColorRowId);

    /// <inheritdoc cref="SeStringBuilder.PopColorType()"/>
    public SeStringBuilder PopColorType() => this.Builder.PopColorType();

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorType(uint)"/>
    public SeStringBuilder PushEdgeColorType(uint uiColorRowId) => this.Builder.PushEdgeColorType(uiColorRowId);

    /// <inheritdoc cref="SeStringBuilder.PopEdgeColorType()"/>
    public SeStringBuilder PopEdgeColorType() => this.Builder.PopEdgeColorType();

    #endregion

    #region Lumina\Text\SeStringBuilder.UIntColors.cs

    /// <inheritdoc cref="SeStringBuilder.AppendBgraIntExpressionFromRgba(Vector4)"/>
    public SeStringBuilder AppendBgraIntExpressionFromRgba(Vector4 rgba) => this.Builder.AppendBgraIntExpressionFromRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.AppendBgraIntExpressionFromRgba(byte,byte,byte,byte)"/>
    public SeStringBuilder AppendBgraIntExpressionFromRgba(byte r, byte g, byte b, byte a) => this.Builder.AppendBgraIntExpressionFromRgba(r, g, b, a);

    /// <inheritdoc cref="SeStringBuilder.AppendBgraIntExpressionFromRgba(uint)"/>
    public SeStringBuilder AppendBgraIntExpressionFromRgba(uint rgba) => this.Builder.AppendBgraIntExpressionFromRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.AppendBgraIntExpression(Vector4)"/>
    public SeStringBuilder AppendBgraIntExpression(Vector4 rgba) => this.Builder.AppendBgraIntExpression(rgba);

    /// <inheritdoc cref="SeStringBuilder.AppendBgraIntExpression(byte,byte,byte,byte)"/>
    public SeStringBuilder AppendBgraIntExpression(byte b, byte g, byte r, byte a) => this.Builder.AppendBgraIntExpression(b, g, r, a);

    /// <inheritdoc cref="SeStringBuilder.PushColorBgra(byte,byte,byte,byte)"/>
    public SeStringBuilder PushColorBgra(byte b, byte g, byte r, byte a) => this.Builder.PushColorBgra(b, g, r, a);

    /// <inheritdoc cref="SeStringBuilder.PushColorBgra(uint)"/>
    public SeStringBuilder PushColorBgra(uint bgra) => this.Builder.PushColorBgra(bgra);

    /// <inheritdoc cref="SeStringBuilder.PushColorBgra(Vector4)"/>
    public SeStringBuilder PushColorBgra(Vector4 bgra) => this.Builder.PushColorBgra(bgra);

    /// <inheritdoc cref="SeStringBuilder.PushColorRgba(byte,byte,byte,byte)"/>
    public SeStringBuilder PushColorRgba(byte r, byte g, byte b, byte a) => this.Builder.PushColorRgba(r, g, b, a);

    /// <inheritdoc cref="SeStringBuilder.PushColorRgba(uint)"/>
    public SeStringBuilder PushColorRgba(uint rgba) => this.Builder.PushColorRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.PushColorRgba(Vector4)"/>
    public SeStringBuilder PushColorRgba(Vector4 rgba) => this.Builder.PushColorRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.PopColor()"/>
    public SeStringBuilder PopColor() => this.Builder.PopColor();

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorBgra(byte,byte,byte,byte)"/>
    public SeStringBuilder PushEdgeColorBgra(byte b, byte g, byte r, byte a) => this.Builder.PushEdgeColorBgra(b, g, r, a);

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorBgra(uint)"/>
    public SeStringBuilder PushEdgeColorBgra(uint bgra) => this.Builder.PushEdgeColorBgra(bgra);

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorBgra(Vector4)"/>
    public SeStringBuilder PushEdgeColorBgra(Vector4 bgra) => this.Builder.PushEdgeColorBgra(bgra);

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorRgba(byte,byte,byte,byte)"/>
    public SeStringBuilder PushEdgeColorRgba(byte r, byte g, byte b, byte a) => this.Builder.PushEdgeColorRgba(r, g, b, a);

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorRgba(uint)"/>
    public SeStringBuilder PushEdgeColorRgba(uint rgba) => this.Builder.PushEdgeColorRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.PushEdgeColorRgba(Vector4)"/>
    public SeStringBuilder PushEdgeColorRgba(Vector4 rgba) => this.Builder.PushEdgeColorRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.PopEdgeColor()"/>
    public SeStringBuilder PopEdgeColor() => this.Builder.PopEdgeColor();

    /// <inheritdoc cref="SeStringBuilder.PushShadowColorBgra(byte,byte,byte,byte)"/>
    public SeStringBuilder PushShadowColorBgra(byte b, byte g, byte r, byte a) => this.Builder.PushShadowColorBgra(b, g, r, a);

    /// <inheritdoc cref="SeStringBuilder.PushShadowColorBgra(uint)"/>
    public SeStringBuilder PushShadowColorBgra(uint bgra) => this.Builder.PushShadowColorBgra(bgra);

    /// <inheritdoc cref="SeStringBuilder.PushShadowColorBgra(Vector4)"/>
    public SeStringBuilder PushShadowColorBgra(Vector4 bgra) => this.Builder.PushShadowColorBgra(bgra);

    /// <inheritdoc cref="SeStringBuilder.PushShadowColorRgba(byte,byte,byte,byte)"/>
    public SeStringBuilder PushShadowColorRgba(byte r, byte g, byte b, byte a) => this.Builder.PushShadowColorRgba(r, g, b, a);

    /// <inheritdoc cref="SeStringBuilder.PushShadowColorRgba(uint)"/>
    public SeStringBuilder PushShadowColorRgba(uint rgba) => this.Builder.PushShadowColorRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.PushShadowColorRgba(Vector4)"/>
    public SeStringBuilder PushShadowColorRgba(Vector4 rgba) => this.Builder.PushShadowColorRgba(rgba);

    /// <inheritdoc cref="SeStringBuilder.PopShadowColor()"/>
    public SeStringBuilder PopShadowColor() => this.Builder.PopShadowColor();

    #endregion
}
