using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text.Evaluator;

using Lumina.Text.ReadOnly;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Defines a service for retrieving localized text for various in-game entities.
/// </summary>
public interface ISeStringEvaluator
{
    /// <summary>
    /// Evaluates macros in a <see cref="ReadOnlySeString"/>.
    /// </summary>
    /// <param name="str">The string containing macros.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString Evaluate(ReadOnlySeString str, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates macros in a <see cref="ReadOnlySeStringSpan"/>.
    /// </summary>
    /// <param name="str">The string containing macros.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString Evaluate(ReadOnlySeStringSpan str, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates macros in a macro string.
    /// </summary>
    /// <param name="macroString">The macro string.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString EvaluateMacroString(string macroString, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates macros in a macro string.
    /// </summary>
    /// <param name="macroString">The macro string.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString EvaluateMacroString(ReadOnlySpan<byte> macroString, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates macros in text from the Addon sheet.
    /// </summary>
    /// <param name="addonId">The row id of the Addon sheet.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString EvaluateFromAddon(uint addonId, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates macros in text from the Lobby sheet.
    /// </summary>
    /// <param name="lobbyId">The row id of the Lobby sheet.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString EvaluateFromLobby(uint lobbyId, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates macros in text from the LogMessage sheet.
    /// </summary>
    /// <param name="logMessageId">The row id of the LogMessage sheet.</param>
    /// <param name="localParameters">An optional list of local parameters.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>An evaluated <see cref="ReadOnlySeString"/>.</returns>
    ReadOnlySeString EvaluateFromLogMessage(uint logMessageId, Span<SeStringParameter> localParameters = default, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates ActStr from the given ActionKind and id.
    /// </summary>
    /// <param name="actionKind">The ActionKind.</param>
    /// <param name="id">The action id.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>The name of the action.</returns>
    string EvaluateActStr(ActionKind actionKind, uint id, ClientLanguage? language = null);

    /// <summary>
    /// Evaluates ObjStr from the given ObjectKind and id.
    /// </summary>
    /// <param name="objectKind">The ObjectKind.</param>
    /// <param name="id">The object id.</param>
    /// <param name="language">An optional language override.</param>
    /// <returns>The singular name of the object.</returns>
    string EvaluateObjStr(ObjectKind objectKind, uint id, ClientLanguage? language = null);
}
