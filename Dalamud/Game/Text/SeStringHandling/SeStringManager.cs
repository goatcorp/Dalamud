using System;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// This class facilitates creating new SeStrings and breaking down existing ones into their individual payload components.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
[Obsolete("This class is obsolete. Please use the static methods on SeString instead.")]
public sealed class SeStringManager : IServiceType
{
    [ServiceManager.ServiceConstructor]
    private SeStringManager()
    {
    }

    /// <summary>
    /// Parse a binary game message into an SeString.
    /// </summary>
    /// <param name="ptr">Pointer to the string's data in memory.</param>
    /// <param name="len">Length of the string's data in memory.</param>
    /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public unsafe SeString Parse(byte* ptr, int len) => SeString.Parse(ptr, len);

    /// <summary>
    /// Parse a binary game message into an SeString.
    /// </summary>
    /// <param name="data">Binary message payload data in SE's internal format.</param>
    /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public unsafe SeString Parse(ReadOnlySpan<byte> data) => SeString.Parse(data);

    /// <summary>
    /// Parse a binary game message into an SeString.
    /// </summary>
    /// <param name="bytes">Binary message payload data in SE's internal format.</param>
    /// <returns>An SeString containing parsed Payload objects for each payload in the data.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public SeString Parse(byte[] bytes) => SeString.Parse(new ReadOnlySpan<byte>(bytes));

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="itemId">The id of the item to link.</param>
    /// <param name="isHQ">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public SeString CreateItemLink(uint itemId, bool isHQ, string displayNameOverride = null) => SeString.CreateItemLink(itemId, isHQ, displayNameOverride);

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link an item in the chat log.
    /// </summary>
    /// <param name="item">The Lumina Item to link.</param>
    /// <param name="isHQ">Whether to link the high-quality variant of the item.</param>
    /// <param name="displayNameOverride">An optional name override to display, instead of the actual item name.</param>
    /// <returns>An SeString containing all the payloads necessary to display an item link in the chat log.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public SeString CreateItemLink(Item item, bool isHQ, string displayNameOverride = null) => SeString.CreateItemLink(item, isHQ, displayNameOverride);

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
    /// <param name="mapId">The id of the Map for this map link.</param>
    /// <param name="rawX">The raw x-coordinate for this link.</param>
    /// <param name="rawY">The raw y-coordinate for this link..</param>
    /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public SeString CreateMapLink(uint territoryId, uint mapId, int rawX, int rawY) =>
        SeString.CreateMapLink(territoryId, mapId, rawX, rawY);

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log.
    /// </summary>
    /// <param name="territoryId">The id of the TerritoryType for this map link.</param>
    /// <param name="mapId">The id of the Map for this map link.</param>
    /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public SeString CreateMapLink(uint territoryId, uint mapId, float xCoord, float yCoord, float fudgeFactor = 0.05f) => SeString.CreateMapLink(territoryId, mapId, xCoord, yCoord, fudgeFactor);

    /// <summary>
    /// Creates an SeString representing an entire Payload chain that can be used to link a map position in the chat log, matching a specified zone name.
    /// </summary>
    /// <param name="placeName">The name of the location for this link.  This should be exactly the name as seen in a displayed map link in-game for the same zone.</param>
    /// <param name="xCoord">The human-readable x-coordinate for this link.</param>
    /// <param name="yCoord">The human-readable y-coordinate for this link.</param>
    /// <param name="fudgeFactor">An optional offset to account for rounding and truncation errors; it is best to leave this untouched in most cases.</param>
    /// <returns>An SeString containing all of the payloads necessary to display a map link in the chat log.</returns>
    [Obsolete("This method is obsolete. Please use the static methods on SeString instead.", true)]
    public SeString CreateMapLink(string placeName, float xCoord, float yCoord, float fudgeFactor = 0.05f) => SeString.CreateMapLink(placeName, xCoord, yCoord, fudgeFactor);

    /// <summary>
    /// Creates a list of Payloads necessary to display the arrow link marker icon in chat
    /// with the appropriate glow and coloring.
    /// </summary>
    /// <returns>A list of all the payloads required to insert the link marker.</returns>
    [Obsolete("This data is obsolete. Please use the static version on SeString instead.", true)]
    public List<Payload> TextArrowPayloads() => new(SeString.TextArrowPayloads);
}
