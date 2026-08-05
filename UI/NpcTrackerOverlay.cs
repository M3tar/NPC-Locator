using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NpcLocator.Config;
using NpcLocator.Framework;
using NpcLocator.Multiplayer;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcLocator.UI;

/// <summary>Maintains and draws one transient NPC tracking target.</summary>
internal sealed class NpcTrackerOverlay
{
    private const int PreferredOverlayWidth = 540;
    private const int ContentPadding = 24;
    private const int HeaderHeight = 76;
    private const int RowHeight = 40;
    private const int BottomPadding = 16;
    private const int LabelWidth = 92;
    private const int CoordinateWidth = 80;

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

    public bool ReceiveLeftClick(int x, int y)
    {
        return Context.IsWorldReady
            && this.CanShow()
            && Game1.activeClickableMenu is null
            && this.GetCloseButtonBounds(this.GetBounds()).Contains(x, y);
    }

    public void Draw(SpriteBatch b)
    {
        if (!this.CanShow())
            return;

        Rectangle bounds = this.GetBounds();
        float opacity = Math.Clamp(this.config.TrackerOpacityPercent, 35, 100) / 100f;
        NativeMenuPanel.Draw(b, bounds, opacity);

        this.DrawContent(b, bounds);
        this.DrawCloseButton(b, bounds);
    }

    private void DrawContent(SpriteBatch b, Rectangle bounds)
    {
        int x = bounds.X + ContentPadding;
        int y = bounds.Y + 20;
        string displayName = this.ResolveNpcDisplayName(this.TrackedNpcName!, this.response?.NpcDisplayName);
        b.DrawString(Game1.dialogueFont, this.i18n.Get("tracker.title", new { npc = displayName }), new Vector2(x, y), Game1.textColor);
        b.Draw(
            Game1.staminaRect,
            new Rectangle(x, bounds.Y + HeaderHeight - 8, bounds.Width - ContentPadding * 2, 1),
            new Color(169, 118, 67) * 0.35f
        );

        y = bounds.Y + HeaderHeight;
        string? hoverText = null;
        foreach (TrackerRow row in this.GetRows())
        {
            this.DrawRow(b, bounds, row, y, ref hoverText);
            y += RowHeight;
        }

        if (hoverText is not null)
            IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
    }

    private List<TrackerRow> GetRows()
    {
        if (this.response is null)
            return new() { new("", this.i18n.Get("tracker.refreshing"), Game1.textColor) };
        if (this.response.Status != QueryStatus.Success)
            return new() { new("", this.TranslateStatus(this.response.Status), Color.DarkRed) };
        if (this.response.Location is null)
            return new() { new("", this.TranslateStatus(this.response.LocationStatus), Color.DarkRed) };

        LocationSnapshot location = this.response.Location;
        string locationName = this.locationNames.Resolve(location.InternalName, location.DisplayName);
        List<TrackerRow> rows = new()
        {
            new(
                this.i18n.Get("tracker.current-label"),
                locationName,
                Game1.textColor,
                location.TileX,
                location.TileY
            )
        };

        if (this.config.ShowNextStop)
        {
            ScheduleEntrySnapshot? next = this.response.Schedule.FirstOrDefault(entry => entry.Time > Game1.timeOfDay);
            if (next is not null)
            {
                string nextName = this.locationNames.Resolve(next.LocationName, next.LocationDisplayName);
                rows.Add(new(
                    this.i18n.Get("tracker.next-label", new { time = FormatTime(next.Time) }),
                    nextName,
                    Game1.textColor,
                    next.TileX,
                    next.TileY
                ));
            }
        }

        if (this.config.ShowDirectionAndDistance && this.IsPlayerInLocation(location.InternalName))
        {
            Point playerTile = Game1.player.TilePoint;
            int dx = location.TileX - playerTile.X;
            int dy = location.TileY - playerTile.Y;
            int distance = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
            string direction = this.GetDirection(dx, dy);
            rows.Add(new(
                this.i18n.Get("tracker.direction-label"),
                this.i18n.Get("tracker.direction-value", new { direction, distance }),
                Game1.textColor
            ));
        }

        return rows;
    }

    private void DrawRow(
        SpriteBatch b,
        Rectangle trackerBounds,
        TrackerRow row,
        int y,
        ref string? hoverText
    )
    {
        int left = trackerBounds.X + ContentPadding;
        int right = trackerBounds.Right - ContentPadding;
        int valueLeft = string.IsNullOrEmpty(row.Label) ? left : left + LabelWidth;
        bool hasCoordinates = row.TileX is not null && row.TileY is not null;
        Rectangle coordinateBounds = new(
            right - CoordinateWidth,
            y,
            CoordinateWidth,
            RowHeight
        );
        int valueRight = hasCoordinates ? coordinateBounds.X - 12 : right;
        Rectangle valueBounds = new(valueLeft, y, Math.Max(1, valueRight - valueLeft), RowHeight);

        if (!string.IsNullOrEmpty(row.Label))
        {
            b.DrawString(
                Game1.smallFont,
                row.Label,
                new Vector2(left, y + 8),
                new Color(121, 93, 67)
            );
        }

        string fittedValue = this.FitText(row.Value, valueBounds.Width, out bool wasTrimmed);
        b.DrawString(Game1.smallFont, fittedValue, new Vector2(valueBounds.X, y + 8), row.ValueColor);
        if (wasTrimmed && valueBounds.Contains(Game1.getMousePosition(true)))
            hoverText = row.Value;

        if (hasCoordinates)
        {
            string coordinates = $"{row.TileX},{row.TileY}";
            Vector2 size = Game1.smallFont.MeasureString(coordinates);
            b.DrawString(
                Game1.smallFont,
                coordinates,
                new Vector2(
                    coordinateBounds.Right - size.X,
                    y + 8
                ),
                new Color(94, 66, 43)
            );
        }
    }

    private bool CanShow()
    {
        return this.config.ShowTrackerOverlay && this.TrackedNpcName is not null;
    }

    private Rectangle GetCloseButtonBounds(Rectangle trackerBounds)
    {
        return new Rectangle(trackerBounds.Right - 56, trackerBounds.Y + 14, 40, 40);
    }

    private void DrawCloseButton(SpriteBatch b, Rectangle trackerBounds)
    {
        Rectangle bounds = this.GetCloseButtonBounds(trackerBounds);
        bool hovered = bounds.Contains(Game1.getMousePosition(true));
        if (hovered)
            b.Draw(Game1.staminaRect, bounds, new Color(222, 184, 120) * 0.4f);

        const string label = "×";
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(
            Game1.smallFont,
            label,
            new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2 - 2),
            hovered ? new Color(120, 78, 48) : Game1.textColor
        );

        if (hovered)
            IClickableMenu.drawHoverText(b, this.i18n.Get("tracker.stop-tracking"), Game1.smallFont);
    }

    private Rectangle GetBounds()
    {
        const int margin = 24;
        int overlayWidth = Math.Min(PreferredOverlayWidth, Game1.uiViewport.Width - margin * 2);
        int overlayHeight = this.GetOverlayHeight();
        return this.config.TrackerPosition switch
        {
            "TopRight" => new Rectangle(Game1.uiViewport.Width - overlayWidth - margin, margin, overlayWidth, overlayHeight),
            "BottomLeft" => new Rectangle(margin, Game1.uiViewport.Height - overlayHeight - margin, overlayWidth, overlayHeight),
            "BottomRight" => new Rectangle(Game1.uiViewport.Width - overlayWidth - margin, Game1.uiViewport.Height - overlayHeight - margin, overlayWidth, overlayHeight),
            _ => new Rectangle(margin, margin, overlayWidth, overlayHeight)
        };
    }

    private int GetOverlayHeight()
    {
        return HeaderHeight + Math.Max(1, this.GetRows().Count) * RowHeight + BottomPadding;
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

    private string FitText(string text, int maxWidth, out bool wasTrimmed)
    {
        if (Game1.smallFont.MeasureString(text).X <= maxWidth)
        {
            wasTrimmed = false;
            return text;
        }

        const string ellipsis = "…";
        int length = text.Length;
        while (length > 1
            && Game1.smallFont.MeasureString(text[..length] + ellipsis).X > maxWidth)
        {
            length--;
        }
        wasTrimmed = true;
        return text[..length] + ellipsis;
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

internal sealed record TrackerRow(
    string Label,
    string Value,
    Color ValueColor,
    int? TileX = null,
    int? TileY = null
);
