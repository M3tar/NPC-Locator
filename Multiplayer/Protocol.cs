namespace MultiplayerNpcLocator.Multiplayer;

internal static class Protocol
{
    public const int Version = 1;
    public const string HelloType = "ProtocolHello";
    public const string RequestType = "NpcQueryRequest";
    public const string ResponseType = "NpcQueryResponse";
}

internal static class QueryStatus
{
    public const string Success = "Success";
    public const string NpcNotFound = "NpcNotFound";
    public const string LocationUnavailable = "LocationUnavailable";
    public const string ScheduleUnavailable = "ScheduleUnavailable";
    public const string PermissionDenied = "PermissionDenied";
    public const string UnsupportedProtocol = "UnsupportedProtocol";
    public const string HostNotReady = "HostNotReady";
    public const string NotRequested = "NotRequested";
    public const string RateLimited = "RateLimited";
}
