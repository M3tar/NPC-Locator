using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MultiplayerNpcLocator.Config;
using MultiplayerNpcLocator.Framework;
using MultiplayerNpcLocator.Multiplayer;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MultiplayerNpcLocator.UI;

/// <summary>Maintains and draws one transient NPC tracking target.</summary>
internal sealed class NpcTrackerOverlay
{
    private const int OverlayWidth = 440;
    private const int OverlayHeight = 190;

    private readonly ModConfig config;
    private readonly ITranslationHelper i18n;
    private readonly LocationDisplayNameResolver locationNames;
    private readonly Action<string> queryNpc;

    private NpcQueryResponse? response;

    public string? TrackedNpcName { get; private set; }

    public NpcTrackerOverlay(
        ModConfig config,
        ITranslationHelper i18n,
        Action<string> queryNpc
    )
    {
        this.config = config;
        this.i18n = i18n;
        this.queryNpc = queryNpc;
        this.locationNames = new LocationDisplayNameResolver(i18n);
    }

    public bool IsTracking(string npcName)
    {
        return string.Equals(this.TrackedNpcName, npcName, StringComparison.OrdinalIgnoreCase);
    }

    public TrackerMenuState? GetMenuState()
    {
        if (this.TrackedNpcName is null)
            return null;

        return new TrackerMenuState(
            this.TrackedNpcName,
            this.ResolveNpcDisplayName(this.TrackedNpcName, this.response?.NpcDisplayName),
            this.response
        );
    }

    public void Track(string npcName, NpcQueryResponse? initialResponse)
    {
        this.TrackedNpcName = npcName;
        this.response = initialResponse;
        this.Refresh();
    }

    public void Stop()
    {
        this.TrackedNpcName = null;
        this.response = null;
    }

    public void Refresh()
    {
        if (this.TrackedNpcName is not null)
            this.queryNpc(this.TrackedNpcName);
    }

    public void SetResponse(NpcQueryResponse result)
    {
        if (this.TrackedNpcName is not null
            && string.Equals(this.TrackedNpcName, result.NpcName, StringComparison.OrdinalIgnoreCase))
        {
            this.response = result;
        }
    }

    public void Draw(SpriteBatch b)
    {
        if (!this.config.ShowTrackerOverlay || this.TrackedNpcName is null)
            return;

        Rectangle bounds = this.GetBounds();
        float opacity = Math.Clamp(this.config.TrackerOpacityPercent, 35, 100) / 100f;
        IClickableMenu.drawTextureBox(
            b,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            Color.White * opacity
        );

        int x = bounds.X + 24;
        int y = bounds.Y + 20;
        string displayName = this.ResolveNpcDisplayName(this.TrackedNpcName, this.response?.NpcDisplayName);
        b.DrawString(Game1.dialogueFont, this.i18n.Get("tracker.title", new { npc = displayName }), new Vector2(x, y), Game1.textColor);
        y += 52;

        if (this.response is null)
        {
            this.DrawLine(b, this.i18n.Get("tracker.refreshing"), x, y, Game1.textColor);
            return;
        }
        if (this.response.Status != QueryStatus.Success)
        {
            this.DrawLine(b, this.TranslateStatus(this.response.Status), x, y, Color.DarkRed);
            return;
        }
        if (this.response.Location is null)
        {
            this.DrawLine(b, this.TranslateStatus(this.response.LocationStatus), x, y, Color.DarkRed);
            return;
        }

        LocationSnapshot location = this.response.Location;
        string locationName = this.locationNames.Resolve(location.InternalName, location.DisplayName);
        this.DrawLine(b, this.i18n.Get("tracker.location", new { location = locationName }), x, y, Game1.textColor);
        y += 30;

        if (this.config.ShowNextStop)
        {
            ScheduleEntrySnapshot? next = this.response.Schedule.FirstOrDefault(entry => entry.Time > Game1.timeOfDay);
            if (next is not null)
            {
                string nextName = this.locationNames.Resolve(next.LocationName, next.LocationDisplayName);
                this.DrawLine(
                    b,
                    this.i18n.Get("tracker.next-stop", new { time = FormatTime(next.Time), location = nextName }),
                    x,
                    y,
                    Game1.textColor
                );
                y += 30;
            }
        }

        if (this.config.ShowDirectionAndDistance && this.IsPlayerInLocation(location.InternalName))
        {
            Point playerTile = Game1.player.TilePoint;
            int dx = location.TileX - playerTile.X;
            int dy = location.TileY - playerTile.Y;
            int distance = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
            string direction = this.GetDirection(dx, dy);
            this.DrawLine(
                b,
                this.i18n.Get("tracker.direction", new { direction, distance }),
                x,
                y,
                Game1.textColor
            );
        }
    }

    private Rectangle GetBounds()
    {
        const int margin = 24;
        return this.config.TrackerPosition switch
        {
            "TopRight" => new Rectangle(Game1.uiViewport.Width - OverlayWidth - margin, margin, OverlayWidth, OverlayHeight),
            "BottomLeft" => new Rectangle(margin, Game1.uiViewport.Height - OverlayHeight - margin, OverlayWidth, OverlayHeight),
            "BottomRight" => new Rectangle(Game1.uiViewport.Width - OverlayWidth - margin, Game1.uiViewport.Height - OverlayHeight - margin, OverlayWidth, OverlayHeight),
            _ => new Rectangle(margin, margin, OverlayWidth, OverlayHeight)
        };
    }

    private bool IsPlayerInLocation(string internalName)
    {
        string current = Game1.currentLocation?.NameOrUniqueName ?? "";
        return string.Equals(current, internalName, StringComparison.OrdinalIgnoreCase);
    }

    private string GetDirection(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return this.i18n.Get("direction.here");

        string vertical = dy < 0 ? "north" : "south";
        string horizontal = dx < 0 ? "west" : "east";
        string key;
        if (Math.Abs(dx) <= Math.Max(1, Math.Abs(dy) / 2))
            key = vertical;
        else if (Math.Abs(dy) <= Math.Max(1, Math.Abs(dx) / 2))
            key = horizontal;
        else
            key = vertical + "-" + horizontal;

        return this.i18n.Get($"direction.{key}");
    }

    private string ResolveNpcDisplayName(string internalName, string? hostDisplayName)
    {
        NPC? localNpc = Game1.getCharacterFromName(internalName);
        if (localNpc is not null && !string.IsNullOrWhiteSpace(localNpc.displayName))
            return localNpc.displayName;
        if (!string.IsNullOrWhiteSpace(hostDisplayName))
            return hostDisplayName;
        return internalName;
    }

    private string TranslateStatus(string status)
    {
        return status switch
        {
            QueryStatus.NpcNotFound => this.i18n.Get("status.npc-not-found"),
            QueryStatus.LocationUnavailable => this.i18n.Get("status.location-unavailable"),
            QueryStatus.ScheduleUnavailable => this.i18n.Get("status.schedule-unavailable"),
            QueryStatus.PermissionDenied => this.i18n.Get("status.permission-denied"),
            QueryStatus.UnsupportedProtocol => this.i18n.Get("status.unsupported-protocol"),
            QueryStatus.HostNotReady => this.i18n.Get("status.host-not-ready"),
            QueryStatus.RateLimited => this.i18n.Get("status.rate-limited"),
            _ => status
        };
    }

    private void DrawLine(SpriteBatch b, string text, int x, int y, Color color)
    {
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
    }

    private static string FormatTime(int time)
    {
        return $"{time / 100:00}:{time % 100:00}";
    }
}

internal sealed record TrackerMenuState(
    string NpcName,
    string NpcDisplayName,
    NpcQueryResponse? Response
);
