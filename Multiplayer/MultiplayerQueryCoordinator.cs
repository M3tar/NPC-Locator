using NpcLocator.Config;
using NpcLocator.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace NpcLocator.Multiplayer;

/// <summary>Coordinates phase-1 requests, responses, timeouts, and host validation.</summary>
internal sealed class MultiplayerQueryCoordinator
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IManifest manifest;
    private readonly ModConfig config;
    private readonly NpcLocatorService locator = new();
    private readonly RequestRateLimiter rateLimiter = new();
    private readonly Dictionary<string, PendingRequest> pendingRequests = new(StringComparer.Ordinal);

    public event Action<NpcQueryResponse>? ResponseReceived;

    public MultiplayerQueryCoordinator(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        ModConfig config
    )
    {
        this.helper = helper;
        this.monitor = monitor;
        this.manifest = manifest;
        this.config = config;
    }

    public void RegisterEvents()
    {
        this.helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        this.helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
        this.helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
        this.helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        this.helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    public void QueryFromConsole(string npcName)
    {
        this.Query(npcName, logToConsole: true);
    }

    public void QueryFromMenu(string npcName)
    {
        this.Query(npcName, logToConsole: false);
    }

    public void QueryFromTracker(string npcName)
    {
        this.Query(npcName, logToConsole: false);
    }

    private void Query(string npcName, bool logToConsole)
    {
        if (!Context.IsWorldReady)
        {
            this.ReportClientFailure(npcName, "NPC query requires a loaded save.", logToConsole);
            return;
        }

        npcName = npcName.Trim();
        if (npcName.Length is < 1 or > 100)
        {
            this.monitor.Log("NPC name must contain between 1 and 100 characters.", LogLevel.Warn);
            return;
        }

        string requestId = Guid.NewGuid().ToString("N");
        if (!Context.IsMultiplayer || Context.IsMainPlayer)
        {
            NpcQueryResponse localResponse = this.locator.Query(
                requestId,
                npcName,
                includeSchedule: true,
                shareCurrentLocation: true,
                shareDailySchedule: true
            );
            if (logToConsole)
                this.LogResponse(localResponse, "local");
            this.ResponseReceived?.Invoke(localResponse);
            return;
        }

        IMultiplayerPeer? host = this.helper.Multiplayer.GetConnectedPlayers()
            .FirstOrDefault(peer => peer.IsHost);
        if (host is null)
        {
            this.ReportClientFailure(npcName, "The host connection is not ready.", logToConsole);
            return;
        }
        if (!host.HasSmapi || host.GetMod(this.manifest.UniqueID) is null)
        {
            this.ReportClientFailure(
                npcName,
                "The host doesn't have NPC Locator installed.",
                logToConsole
            );
            return;
        }

        NpcQueryRequest request = new()
        {
            ProtocolVersion = Protocol.Version,
            RequestId = requestId,
            NpcName = npcName,
            IncludeSchedule = true
        };
        this.pendingRequests[requestId] = new PendingRequest(
            npcName,
            DateTimeOffset.UtcNow,
            host.PlayerID,
            logToConsole
        );
        this.helper.Multiplayer.SendMessage(
            request,
            Protocol.RequestType,
            modIDs: new[] { this.manifest.UniqueID },
            playerIDs: new[] { host.PlayerID }
        );
        if (logToConsole)
            this.monitor.Log($"Sent NPC query '{requestId}' for '{npcName}' to the host.", LogLevel.Info);
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!e.Peer.HasSmapi || e.Peer.GetMod(this.manifest.UniqueID) is null)
            return;

        this.helper.Multiplayer.SendMessage(
            new ProtocolHello
            {
                ProtocolVersion = Protocol.Version,
                ModVersion = this.manifest.Version.ToString()
            },
            Protocol.HelloType,
            modIDs: new[] { this.manifest.UniqueID },
            playerIDs: new[] { e.Peer.PlayerID }
        );
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        this.rateLimiter.Forget(e.Peer.PlayerID);
        foreach (string requestId in this.pendingRequests
                     .Where(pair => pair.Value.HostPlayerId == e.Peer.PlayerID)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            PendingRequest pending = this.pendingRequests[requestId];
            this.pendingRequests.Remove(requestId);
            this.monitor.Log($"NPC query '{requestId}' was cancelled because the host disconnected.", LogLevel.Warn);
            this.ResponseReceived?.Invoke(new NpcQueryResponse
            {
                RequestId = requestId,
                NpcName = pending.NpcName,
                Status = QueryStatus.HostNotReady,
                Message = "The host disconnected."
            });
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.pendingRequests.Clear();
        this.rateLimiter.Clear();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!e.IsMultipleOf(30) || this.pendingRequests.Count == 0)
            return;

        DateTimeOffset cutoff = DateTimeOffset.UtcNow - RequestTimeout;
        foreach ((string requestId, PendingRequest pending) in this.pendingRequests.ToArray())
        {
            if (pending.SentAt > cutoff)
                continue;

            this.pendingRequests.Remove(requestId);
            if (pending.LogToConsole)
            {
                this.monitor.Log(
                    $"NPC query '{requestId}' for '{pending.NpcName}' timed out after {RequestTimeout.TotalSeconds:0} seconds.",
                    LogLevel.Warn
                );
            }
            this.ResponseReceived?.Invoke(new NpcQueryResponse
            {
                RequestId = requestId,
                NpcName = pending.NpcName,
                Status = QueryStatus.HostNotReady,
                Message = $"The host did not respond within {RequestTimeout.TotalSeconds:0} seconds."
            });
        }
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!string.Equals(e.FromModID, this.manifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            switch (e.Type)
            {
                case Protocol.HelloType:
                    this.HandleHello(e);
                    break;
                case Protocol.RequestType:
                    this.HandleRequest(e);
                    break;
                case Protocol.ResponseType:
                    this.HandleResponse(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Ignored invalid multiplayer message '{e.Type}': {ex}", LogLevel.Warn);
        }
    }

    private void HandleHello(ModMessageReceivedEventArgs e)
    {
        ProtocolHello hello = e.ReadAs<ProtocolHello>();
        this.monitor.Log(
            $"Peer {e.FromPlayerID} reports NPC Locator {hello.ModVersion} with protocol {hello.ProtocolVersion}.",
            hello.ProtocolVersion == Protocol.Version ? LogLevel.Trace : LogLevel.Warn
        );
    }

    private void HandleRequest(ModMessageReceivedEventArgs e)
    {
        NpcQueryRequest request = e.ReadAs<NpcQueryRequest>();
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            this.SendResponse(e.FromPlayerID, new NpcQueryResponse
            {
                RequestId = request.RequestId,
                NpcName = request.NpcName,
                Status = QueryStatus.HostNotReady,
                Message = "The host world is not ready."
            });
            return;
        }
        IMultiplayerPeer? requester = this.helper.Multiplayer.GetConnectedPlayer(e.FromPlayerID);
        bool isFullyConnected = Game1.getOnlineFarmers()
            .Any(farmer => farmer.UniqueMultiplayerID == e.FromPlayerID);
        if (requester is null || requester.IsHost || !isFullyConnected)
        {
            this.SendResponse(e.FromPlayerID, new NpcQueryResponse
            {
                RequestId = request.RequestId,
                NpcName = request.NpcName,
                Status = QueryStatus.HostNotReady,
                Message = "The requesting player is not fully connected."
            });
            return;
        }
        if (!Guid.TryParseExact(request.RequestId, "N", out _)
            || string.IsNullOrWhiteSpace(request.NpcName)
            || request.NpcName.Length > 100)
        {
            this.monitor.Log($"Rejected malformed NPC query from player {e.FromPlayerID}.", LogLevel.Warn);
            return;
        }
        if (request.ProtocolVersion != Protocol.Version)
        {
            this.SendResponse(e.FromPlayerID, new NpcQueryResponse
            {
                RequestId = request.RequestId,
                NpcName = request.NpcName,
                Status = QueryStatus.UnsupportedProtocol,
                Message = $"Host protocol is {Protocol.Version}."
            });
            return;
        }
        if (!this.config.AllowRemoteQueries)
        {
            this.SendResponse(e.FromPlayerID, new NpcQueryResponse
            {
                RequestId = request.RequestId,
                NpcName = request.NpcName,
                Status = QueryStatus.PermissionDenied,
                Message = "The host disabled remote NPC queries."
            });
            return;
        }
        if (!this.rateLimiter.TryAcquire(e.FromPlayerID, this.config.MaxRequestsPerSecond))
        {
            this.SendResponse(e.FromPlayerID, new NpcQueryResponse
            {
                RequestId = request.RequestId,
                NpcName = request.NpcName,
                Status = QueryStatus.RateLimited,
                Message = "Too many NPC queries were sent."
            });
            return;
        }
        NpcQueryResponse response = this.locator.Query(
            request.RequestId,
            request.NpcName.Trim(),
            request.IncludeSchedule,
            this.config.ShareCurrentLocation,
            this.config.ShareDailySchedule
        );
        this.SendResponse(e.FromPlayerID, response);
        if (this.config.ShowHostNotifications)
        {
            this.monitor.Log(
                $"Answered NPC query '{request.RequestId}' for '{request.NpcName}' from player {e.FromPlayerID}.",
                LogLevel.Info
            );
        }
    }

    private void HandleResponse(ModMessageReceivedEventArgs e)
    {
        NpcQueryResponse response = e.ReadAs<NpcQueryResponse>();
        if (!this.pendingRequests.TryGetValue(response.RequestId, out PendingRequest? pending))
        {
            this.monitor.Log($"Ignored expired or unknown NPC response '{response.RequestId}'.", LogLevel.Trace);
            return;
        }
        if (e.FromPlayerID != pending.HostPlayerId)
        {
            this.monitor.Log($"Ignored NPC response '{response.RequestId}' from a non-host player.", LogLevel.Warn);
            return;
        }
        if (response.ProtocolVersion != Protocol.Version)
        {
            this.pendingRequests.Remove(response.RequestId);
            this.monitor.Log(
                $"NPC response '{response.RequestId}' uses unsupported protocol {response.ProtocolVersion}.",
                LogLevel.Warn
            );
            this.ResponseReceived?.Invoke(new NpcQueryResponse
            {
                RequestId = response.RequestId,
                NpcName = pending.NpcName,
                Status = QueryStatus.UnsupportedProtocol,
                Message = $"The host response uses protocol {response.ProtocolVersion}."
            });
            return;
        }

        this.pendingRequests.Remove(response.RequestId);
        if (pending.LogToConsole)
            this.LogResponse(response, $"host player {e.FromPlayerID}");
        this.ResponseReceived?.Invoke(response);
    }

    private void SendResponse(long playerId, NpcQueryResponse response)
    {
        this.helper.Multiplayer.SendMessage(
            response,
            Protocol.ResponseType,
            modIDs: new[] { this.manifest.UniqueID },
            playerIDs: new[] { playerId }
        );
    }

    private void ReportClientFailure(string npcName, string message, bool logToConsole)
    {
        if (logToConsole)
            this.monitor.Log(message, LogLevel.Warn);
        this.ResponseReceived?.Invoke(new NpcQueryResponse
        {
            RequestId = Guid.NewGuid().ToString("N"),
            NpcName = npcName,
            Status = QueryStatus.HostNotReady,
            Message = message
        });
    }

    private void LogResponse(NpcQueryResponse response, string source)
    {
        this.monitor.Log(
            $"NPC response from {source}: request='{response.RequestId}', status={response.Status}, "
            + $"npc='{response.NpcName}', display='{response.NpcDisplayName}', "
            + $"locationStatus={response.LocationStatus}, scheduleStatus={response.ScheduleStatus}.",
            response.Status == QueryStatus.Success ? LogLevel.Info : LogLevel.Warn
        );

        if (response.Location is not null)
        {
            this.monitor.Log(
                $"Current location: '{response.Location.InternalName}' / '{response.Location.DisplayName}' "
                + $"tile=({response.Location.TileX}, {response.Location.TileY}).",
                LogLevel.Info
            );
        }
        foreach (ScheduleEntrySnapshot entry in response.Schedule)
        {
            this.monitor.Log(
                $"Schedule {entry.Time:0000}: '{entry.LocationName}' / '{entry.LocationDisplayName}' "
                + $"tile=({entry.TileX}, {entry.TileY}), facing={entry.FacingDirection}, behavior='{entry.EndBehavior ?? ""}'.",
                LogLevel.Info
            );
        }
        if (!string.IsNullOrWhiteSpace(response.Message))
            this.monitor.Log(response.Message, LogLevel.Warn);
    }

    private sealed record PendingRequest(
        string NpcName,
        DateTimeOffset SentAt,
        long HostPlayerId,
        bool LogToConsole
    );
}
