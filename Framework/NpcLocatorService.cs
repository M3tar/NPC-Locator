using NpcLocator.Multiplayer;
using StardewValley;
using StardewValley.Pathfinding;

namespace NpcLocator.Framework;

/// <summary>Reads host-authoritative NPC state without changing game state.</summary>
internal sealed class NpcLocatorService
{
    public NpcQueryResponse Query(
        string requestId,
        string npcName,
        bool includeSchedule,
        bool shareCurrentLocation,
        bool shareDailySchedule
    )
    {
        NpcQueryResponse response = new()
        {
            RequestId = requestId,
            NpcName = npcName
        };

        NPC? npc = Game1.getCharacterFromName(npcName);
        if (npc is null)
        {
            response.Status = QueryStatus.NpcNotFound;
            response.Message = "The NPC is currently unavailable.";
            return response;
        }

        response.Status = QueryStatus.Success;
        response.NpcName = npc.Name;
        response.NpcDisplayName = npc.displayName;

        if (!shareCurrentLocation)
        {
            response.LocationStatus = QueryStatus.PermissionDenied;
        }
        else if (npc.currentLocation is null)
        {
            response.LocationStatus = QueryStatus.LocationUnavailable;
        }
        else
        {
            response.LocationStatus = QueryStatus.Success;
            response.Location = new LocationSnapshot
            {
                InternalName = npc.currentLocation.NameOrUniqueName,
                DisplayName = npc.currentLocation.DisplayName,
                TileX = npc.TilePoint.X,
                TileY = npc.TilePoint.Y
            };
        }

        if (!includeSchedule)
        {
            response.ScheduleStatus = QueryStatus.NotRequested;
        }
        else if (!shareDailySchedule)
        {
            response.ScheduleStatus = QueryStatus.PermissionDenied;
        }
        else if (npc.Schedule is null || npc.Schedule.Count == 0)
        {
            response.ScheduleStatus = QueryStatus.ScheduleUnavailable;
        }
        else
        {
            response.ScheduleStatus = QueryStatus.Success;
            foreach (KeyValuePair<int, SchedulePathDescription> pair in npc.Schedule.OrderBy(pair => pair.Key))
            {
                SchedulePathDescription entry = pair.Value;
                response.Schedule.Add(new ScheduleEntrySnapshot
                {
                    Time = entry.time,
                    LocationName = entry.targetLocationName ?? "",
                    LocationDisplayName = ResolveLocationDisplayName(entry.targetLocationName),
                    TileX = entry.targetTile.X,
                    TileY = entry.targetTile.Y,
                    FacingDirection = entry.facingDirection,
                    EndBehavior = entry.endOfRouteBehavior,
                    EndMessage = entry.endOfRouteMessage
                });
            }
        }

        return response;
    }

    private static string ResolveLocationDisplayName(string? locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return "";

        GameLocation? location = Game1.getLocationFromName(locationName);
        return location?.DisplayName ?? locationName;
    }
}
