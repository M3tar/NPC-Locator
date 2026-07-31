namespace MultiplayerNpcLocator.Multiplayer;

public sealed class ProtocolHello
{
    public int ProtocolVersion { get; set; }
    public string ModVersion { get; set; } = "";
}

public sealed class NpcQueryRequest
{
    public int ProtocolVersion { get; set; }
    public string RequestId { get; set; } = "";
    public string NpcName { get; set; } = "";
    public bool IncludeSchedule { get; set; } = true;
}

public sealed class NpcQueryResponse
{
    public int ProtocolVersion { get; set; } = Protocol.Version;
    public string RequestId { get; set; } = "";
    public string Status { get; set; } = QueryStatus.HostNotReady;
    public string? Message { get; set; }
    public string NpcName { get; set; } = "";
    public string NpcDisplayName { get; set; } = "";
    public string LocationStatus { get; set; } = QueryStatus.LocationUnavailable;
    public LocationSnapshot? Location { get; set; }
    public string ScheduleStatus { get; set; } = QueryStatus.NotRequested;
    public List<ScheduleEntrySnapshot> Schedule { get; set; } = new();
}

public sealed class LocationSnapshot
{
    public string InternalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int TileX { get; set; }
    public int TileY { get; set; }
}

public sealed class ScheduleEntrySnapshot
{
    public int Time { get; set; }
    public string LocationName { get; set; } = "";
    public string LocationDisplayName { get; set; } = "";
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int FacingDirection { get; set; }
    public string? EndBehavior { get; set; }
    public string? EndMessage { get; set; }
}
