using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MultiplayerNpcLocator.Framework;
using MultiplayerNpcLocator.Multiplayer;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MultiplayerNpcLocator.UI;

/// <summary>A phase-2 game-style NPC search and result menu.</summary>
internal sealed class NpcSearchMenu : IClickableMenu
{
    private const int MenuWidth = 960;
    private const int MenuHeight = 540;
    private const int Padding = 32;
    private const int LeftWidth = 300;
    private const int RowHeight = 42;

    private static readonly Rectangle ParchmentSourceRect = new(0, 0, 320, 180);

    private readonly ITranslationHelper i18n;
    private readonly Action<string> queryNpc;
    private readonly Action<string, NpcQueryResponse?> trackNpc;
    private readonly Action stopTracking;
    private readonly Func<string, bool> isTracking;
    private readonly List<NpcListEntry> allNpcs;
    private readonly LocationDisplayNameResolver locationNames;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox searchBox;
    private List<NpcListEntry> filteredNpcs;
    private string previousSearch = "";
    private int listOffset;
    private int scheduleOffset;
    private int controllerIndex = -1;
    private string? selectedNpc;
    private bool loading;
    private NpcQueryResponse? response;

    public NpcSearchMenu(
        IEnumerable<NpcListEntry> npcs,
        ITranslationHelper i18n,
        Action<string> queryNpc,
        Action<string, NpcQueryResponse?> trackNpc,
        Action stopTracking,
        Func<string, bool> isTracking
    )
        : base(
            (Game1.uiViewport.Width - MenuWidth) / 2,
            (Game1.uiViewport.Height - MenuHeight) / 2,
            MenuWidth,
            MenuHeight,
            showUpperRightCloseButton: true
        )
    {
        this.i18n = i18n;
        this.queryNpc = queryNpc;
        this.trackNpc = trackNpc;
        this.stopTracking = stopTracking;
        this.isTracking = isTracking;
        this.locationNames = new LocationDisplayNameResolver(i18n);
        this.allNpcs = npcs.OrderBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        this.filteredNpcs = new List<NpcListEntry>(this.allNpcs);
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");

        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.searchBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + Padding,
            Y = this.yPositionOnScreen + 72,
            Width = LeftWidth - 8,
            Selected = true
        };
        Game1.keyboardDispatcher.Subscriber = this.searchBox;
    }

    public void SetResponse(NpcQueryResponse result)
    {
        if (this.selectedNpc is null
            || !string.Equals(this.selectedNpc, result.NpcName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        this.loading = false;
        this.response = result;
        this.scheduleOffset = 0;
    }

    public override void update(GameTime time)
    {
        base.update(time);
        this.searchBox.Update();
        if (string.Equals(this.previousSearch, this.searchBox.Text, StringComparison.Ordinal))
            return;

        this.previousSearch = this.searchBox.Text;
        string search = this.searchBox.Text.Trim();
        this.filteredNpcs = this.allNpcs
            .Where(entry => search.Length == 0
                || entry.InternalName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || entry.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        this.listOffset = 0;
        this.controllerIndex = this.filteredNpcs.Count > 0 ? 0 : -1;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.readyToClose() && this.upperRightCloseButton?.containsPoint(x, y) == true)
            return;

        if (!this.loading && this.selectedNpc is not null && this.GetRefreshButtonBounds().Contains(x, y))
        {
            this.loading = true;
            Game1.playSound("smallSelect");
            this.queryNpc(this.selectedNpc);
            return;
        }
        if (this.CanTrackSelectedNpc() && this.GetTrackButtonBounds().Contains(x, y))
        {
            this.ToggleTracking();
            return;
        }

        Rectangle listBounds = this.GetListBounds();
        if (!listBounds.Contains(x, y))
            return;

        int visibleIndex = (y - listBounds.Y) / RowHeight;
        int index = this.listOffset + visibleIndex;
        if (index < 0 || index >= this.filteredNpcs.Count)
            return;

        this.controllerIndex = index;
        this.SelectNpc(this.filteredNpcs[index]);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        Point mouse = Game1.getMousePosition(true);
        int delta = direction > 0 ? -1 : 1;
        if (this.GetListBounds().Contains(mouse))
        {
            int maxOffset = Math.Max(0, this.filteredNpcs.Count - this.GetVisibleListRows());
            this.listOffset = Math.Clamp(this.listOffset + delta, 0, maxOffset);
        }
        else if (this.response is not null)
        {
            int maxOffset = Math.Max(0, this.response.Schedule.Count - this.GetVisibleScheduleRows());
            this.scheduleOffset = Math.Clamp(this.scheduleOffset + delta, 0, maxOffset);
        }
    }

    public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
    {
        if (key == Microsoft.Xna.Framework.Input.Keys.Escape)
        {
            this.exitThisMenu();
            return;
        }
        base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        switch (button)
        {
            case Buttons.B:
                this.exitThisMenu();
                return;
            case Buttons.DPadUp:
            case Buttons.LeftThumbstickUp:
                this.MoveControllerSelection(-1);
                return;
            case Buttons.DPadDown:
            case Buttons.LeftThumbstickDown:
                this.MoveControllerSelection(1);
                return;
            case Buttons.A when this.controllerIndex >= 0 && this.controllerIndex < this.filteredNpcs.Count:
                this.SelectNpc(this.filteredNpcs[this.controllerIndex]);
                return;
            case Buttons.X when this.selectedNpc is not null:
                this.loading = true;
                this.queryNpc(this.selectedNpc);
                return;
            case Buttons.Y when this.CanTrackSelectedNpc():
                this.ToggleTracking();
                return;
        }

        base.receiveGamePadButton(button);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        this.xPositionOnScreen = (Game1.uiViewport.Width - this.width) / 2;
        this.yPositionOnScreen = (Game1.uiViewport.Height - this.height) / 2;
        this.searchBox.X = this.xPositionOnScreen + Padding;
        this.searchBox.Y = this.yPositionOnScreen + 72;
        if (this.upperRightCloseButton is not null)
        {
            this.upperRightCloseButton.bounds.X = this.xPositionOnScreen + this.width - 52;
            this.upperRightCloseButton.bounds.Y = this.yPositionOnScreen - 8;
        }
    }

    protected override void cleanupBeforeExit()
    {
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this.searchBox))
            Game1.keyboardDispatcher.Subscriber = null;
        base.cleanupBeforeExit();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(
            this.parchmentTexture,
            new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height),
            ParchmentSourceRect,
            Color.White
        );

        Vector2 titlePosition = new(this.xPositionOnScreen + Padding, this.yPositionOnScreen + 24);
        b.DrawString(Game1.dialogueFont, this.i18n.Get("menu.title"), titlePosition, Game1.textColor);
        this.searchBox.Draw(b);

        this.DrawNpcList(b);
        this.DrawResult(b);
        this.DrawActions(b);
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private void DrawNpcList(SpriteBatch b)
    {
        Rectangle bounds = this.GetListBounds();
        int rows = this.GetVisibleListRows();
        for (int visibleIndex = 0; visibleIndex < rows; visibleIndex++)
        {
            int index = this.listOffset + visibleIndex;
            if (index >= this.filteredNpcs.Count)
                break;

            NpcListEntry entry = this.filteredNpcs[index];
            Rectangle row = new(bounds.X + 4, bounds.Y + visibleIndex * RowHeight, bounds.Width - 8, RowHeight);
            if (string.Equals(entry.InternalName, this.selectedNpc, StringComparison.OrdinalIgnoreCase))
                b.Draw(Game1.staminaRect, row, Color.SandyBrown * 0.35f);
            else if (index == this.controllerIndex)
                b.Draw(Game1.staminaRect, row, Color.Wheat * 0.45f);
            else if (row.Contains(Game1.getMousePosition(true)))
                b.Draw(Game1.staminaRect, row, Color.White * 0.25f);

            b.DrawString(
                Game1.smallFont,
                entry.DisplayName,
                new Vector2(row.X + 10, row.Y + 8),
                Game1.textColor
            );
        }

        if (this.filteredNpcs.Count == 0)
        {
            b.DrawString(
                Game1.smallFont,
                this.i18n.Get("menu.no-results"),
                new Vector2(bounds.X + 10, bounds.Y + 10),
                Game1.textColor
            );
        }
    }

    private void DrawResult(SpriteBatch b)
    {
        int x = this.xPositionOnScreen + Padding + LeftWidth + 28;
        int y = this.yPositionOnScreen + 82;

        if (this.selectedNpc is null)
        {
            this.DrawLine(b, this.i18n.Get("menu.select-prompt"), x, y, Game1.textColor);
            return;
        }
        if (this.loading)
        {
            this.DrawLine(b, this.i18n.Get("menu.loading"), x, y, Game1.textColor);
            return;
        }
        if (this.response is null)
            return;

        NpcQueryResponse result = this.response;
        string heading = this.ResolveNpcDisplayName(result.NpcName, result.NpcDisplayName);
        b.DrawString(Game1.dialogueFont, heading, new Vector2(x, y), Game1.textColor);
        y += 56;

        if (result.Status != QueryStatus.Success)
        {
            this.DrawLine(b, this.TranslateStatus(result.Status), x, y, Color.DarkRed);
            return;
        }

        this.DrawLine(b, this.i18n.Get("menu.current-location"), x, y, Game1.textColor);
        y += 32;
        if (result.Location is null)
        {
            this.DrawLine(b, this.TranslateStatus(result.LocationStatus), x + 16, y, Color.DarkRed);
        }
        else
        {
            string location = this.locationNames.Resolve(
                result.Location.InternalName,
                result.Location.DisplayName
            );
            this.DrawLine(
                b,
                $"{location}  ({result.Location.TileX}, {result.Location.TileY})",
                x + 16,
                y,
                Game1.textColor
            );
        }

        y += 48;
        this.DrawLine(b, this.i18n.Get("menu.daily-schedule"), x, y, Game1.textColor);
        y += 32;
        if (result.ScheduleStatus != QueryStatus.Success)
        {
            this.DrawLine(b, this.TranslateStatus(result.ScheduleStatus), x + 16, y, Color.DarkRed);
            return;
        }

        int rows = this.GetVisibleScheduleRows();
        foreach (ScheduleEntrySnapshot entry in result.Schedule.Skip(this.scheduleOffset).Take(rows))
        {
            string location = this.locationNames.Resolve(entry.LocationName, entry.LocationDisplayName);
            this.DrawLine(
                b,
                $"{FormatTime(entry.Time)}  {location}  ({entry.TileX}, {entry.TileY})",
                x + 16,
                y,
                Game1.textColor
            );
            y += 32;
        }
    }

    private void DrawLine(SpriteBatch b, string text, int x, int y, Color color)
    {
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
    }

    private Rectangle GetListBounds()
    {
        return new Rectangle(
            this.xPositionOnScreen + Padding,
            this.yPositionOnScreen + 128,
            LeftWidth,
            this.height - 168
        );
    }

    private int GetVisibleListRows() => this.GetListBounds().Height / RowHeight;

    private int GetVisibleScheduleRows() => 6;

    private void DrawActions(SpriteBatch b)
    {
        if (this.selectedNpc is null)
            return;

        this.DrawButton(b, this.GetRefreshButtonBounds(), this.i18n.Get("menu.refresh"), enabled: !this.loading);
        if (this.CanTrackSelectedNpc())
        {
            string label = this.isTracking(this.selectedNpc)
                ? this.i18n.Get("menu.stop-tracking")
                : this.i18n.Get("menu.start-tracking");
            this.DrawButton(b, this.GetTrackButtonBounds(), label, enabled: true);
        }
    }

    private void DrawButton(SpriteBatch b, Rectangle bounds, string label, bool enabled)
    {
        Color tint = !enabled
            ? Color.Gray * 0.65f
            : bounds.Contains(Game1.getMousePosition(true))
                ? Color.Wheat
                : Color.White;
        drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(
            Game1.smallFont,
            label,
            new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2),
            enabled ? Game1.textColor : Color.DarkGray
        );
    }

    private Rectangle GetRefreshButtonBounds()
    {
        int x = this.xPositionOnScreen + Padding + LeftWidth + 28;
        return new Rectangle(x, this.yPositionOnScreen + this.height - 68, 150, 48);
    }

    private Rectangle GetTrackButtonBounds()
    {
        Rectangle refresh = this.GetRefreshButtonBounds();
        return new Rectangle(refresh.Right + 16, refresh.Y, 190, refresh.Height);
    }

    private bool CanTrackSelectedNpc()
    {
        return this.selectedNpc is not null
            && this.response is { Status: QueryStatus.Success };
    }

    private void SelectNpc(NpcListEntry entry)
    {
        this.selectedNpc = entry.InternalName;
        this.loading = true;
        this.response = null;
        this.scheduleOffset = 0;
        Game1.playSound("smallSelect");
        this.queryNpc(entry.InternalName);
    }

    private void ToggleTracking()
    {
        if (!this.CanTrackSelectedNpc())
            return;

        if (this.isTracking(this.selectedNpc!))
            this.stopTracking();
        else
            this.trackNpc(this.selectedNpc!, this.response);
        Game1.playSound("smallSelect");
    }

    private void MoveControllerSelection(int delta)
    {
        if (this.filteredNpcs.Count == 0)
            return;

        this.controllerIndex = Math.Clamp(
            this.controllerIndex < 0 ? 0 : this.controllerIndex + delta,
            0,
            this.filteredNpcs.Count - 1
        );
        int visibleRows = this.GetVisibleListRows();
        if (this.controllerIndex < this.listOffset)
            this.listOffset = this.controllerIndex;
        else if (this.controllerIndex >= this.listOffset + visibleRows)
            this.listOffset = this.controllerIndex - visibleRows + 1;
        Game1.playSound("shiny4");
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

    private static string FormatTime(int time)
    {
        return $"{time / 100:00}:{time % 100:00}";
    }
}

internal sealed record NpcListEntry(string InternalName, string DisplayName);
