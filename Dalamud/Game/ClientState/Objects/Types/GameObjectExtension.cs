using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace Dalamud.Game.ClientState.Objects.Types;

#pragma warning disable SA1101 // Prefix local calls with this

/// <summary>
/// Extensions for <see cref="IGameObject"/>.
/// </summary>
public static class GameObjectExtension
{
    extension(IGameObject gameObject)
    {
        /// <summary>
        /// Gets the current position of a GameObject as a <see cref="MapCoordinate"/> (visible to players in the
        /// minimap or chat).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if ClientState is unavailable.</exception>
        /// <returns>A MapCoordinate of this object.</returns>
        public unsafe WorldCoordinate GetWorldCoordinate()
        {
            var territoryId = GameMain.Instance()->CurrentTerritoryTypeId;
            if (territoryId == 0)
                return default;

            var mapId = TerritoryInfo.Instance()->ChatLinkMapIdOverride;
            if (mapId == 0)
                mapId = GameMain.Instance()->CurrentMapId;
            if (mapId == 0)
                return default;

            var instanceId = (uint)CSFramework.Instance()->GetNetworkModuleProxy()->GetCurrentInstance();
            return new WorldCoordinate(territoryId, mapId, gameObject.Position.X, gameObject.Position.Z, gameObject.Position.Y, instanceId);
        }

        /// <summary>
        /// Gets the current position of a GameObject as a <see cref="MapCoordinate"/> (visible to players in the
        /// minimap or chat).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if ClientState is unavailable.</exception>
        /// <returns>A MapCoordinate of this object.</returns>
        public MapCoordinate GetMapCoordinate()
        {
            return gameObject.GetWorldCoordinate().ToMapCoordinate();
        }
    }
}
