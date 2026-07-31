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
    private readonly Func<IReadOnlyList<DeliveryQuestSnapshot>> getActiveQuests;
    private readonly Action<DeliveryQuestSnapshot> trackQuest;
    private readonly Func<string, bool> isTrackingQuest;
    private readonly Func<TrackerMenuState?> getTrackerState;
    private readonly Func<string?> getTrackedQuestKey;
    private readonly List<NpcListEntry> allNpcs;
    private readonly LocationDisplayNameResolver locationNames;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox searchBox;
    private List<NpcListEntry> filteredNpcs;
    private List<DeliveryQuestSnapshot> activeQuests;
    private string previousSearch = "";
    private int listOffset;
    private int questListOffset;
    private int scheduleOffset;
    private int controllerIndex = -1;
    private int questRefreshTicks;
    private bool showingQuests;
    private string? selectedNpc;
    private string? selectedQuestKey;
    private bool loading;
    private NpcQueryResponse? response;

    public NpcSearchMenu(
        IEnumerable<NpcListEntry> npcs,
        ITranslationHelper i18n,
        Action<string> queryNpc,
        Action<string, NpcQueryResponse?> trackNpc,
        Action stopTracking,
        Func<string, bool> isTracking,
        Func<IReadOnlyList<DeliveryQuestSnapshot>> getActiveQuests,
        Action<DeliveryQuestSnapshot> trackQuest,
        Func<string, bool> isTrackingQuest,
        Func<TrackerMenuState?> getTrackerState,
        Func<string?> getTrackedQuestKey
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
        this.getActiveQuests = getActiveQuests;
        this.trackQuest = trackQuest;
        this.isTrackingQuest = isTrackingQuest;
        this.getTrackerState = getTrackerState;
        this.getTrackedQuestKey = getTrackedQuestKey;
        this.locationNames = new LocationDisplayNameResolver(i18n);
        this.allNpcs = npcs.OrderBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        this.filteredNpcs = new List<NpcListEntry>(this.allNpcs);
        this.activeQuests = this.getActiveQuests().ToList();
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");

        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.searchBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + Padding,
            Y = this.yPositionOnScreen + 118,
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
        if (++this.questRefreshTicks >= 30)
        {
            this.questRefreshTicks = 0;
            this.RefreshActiveQuests();
        }

        if (this.showingQuests)
            return;

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

        if (this.GetNpcTabBounds().Contains(x, y))
        {
            this.SetQuestTab(false);
            return;
        }
        if (this.GetQuestTabBounds().Contains(x, y))
        {
            this.SetQuestTab(true);
            return;
        }

        if (this.showingQuests)
        {
            DeliveryQuestSnapshot? quest = this.GetSelectedQuest();
            if (quest is not null && this.GetTaskTrackButtonBounds().Contains(x, y))
            {
                if (this.isTrackingQuest(quest.Key))
                    this.stopTracking();
                else
                    this.trackQuest(quest);
                Game1.playSound("smallSelect");
                return;
            }

            Rectangle questListBounds = this.GetListBounds();
            if (!questListBounds.Contains(x, y))
                return;
            int visibleQuestIndex = (y - questListBounds.Y) / RowHeight;
            int questIndex = this.questListOffset + visibleQuestIndex;
            if (questIndex < 0 || questIndex >= this.activeQuests.Count)
                return;
            this.controllerIndex = questIndex;
            this.selectedQuestKey = this.activeQuests[questIndex].Key;
            Game1.playSound("smallSelect");
            return;
        }

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
        if (this.showingQuests)
        {
            int maxQuestOffset = Math.Max(0, this.activeQuests.Count - this.GetVisibleListRows());
            this.questListOffset = Math.Clamp(this.questListOffset + delta, 0, maxQuestOffset);
            return;
        }
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
        if (button == Buttons.LeftShoulder || button == Buttons.RightShoulder)
        {
            this.SetQuestTab(!this.showingQuests);
            return;
        }

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
            case Buttons.A when this.showingQuests
                && this.controllerIndex >= 0
                && this.controllerIndex < this.activeQuests.Count:
                this.selectedQuestKey = this.activeQuests[this.controllerIndex].Key;
                Game1.playSound("smallSelect");
                return;
            case Buttons.A when !this.showingQuests
                && this.controllerIndex >= 0
                && this.controllerIndex < this.filteredNpcs.Count:
                this.SelectNpc(this.filteredNpcs[this.controllerIndex]);
                return;
            case Buttons.X when !this.showingQuests && this.selectedNpc is not null:
                this.loading = true;
                this.queryNpc(this.selectedNpc);
                return;
            case Buttons.Y when this.showingQuests && this.GetSelectedQuest() is DeliveryQuestSnapshot quest:
                if (this.isTrackingQuest(quest.Key))
                    this.stopTracking();
                else
                    this.trackQuest(quest);
                Game1.playSound("smallSelect");
                return;
            case Buttons.Y when !this.showingQuests && this.CanTrackSelectedNpc():
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
        this.searchBox.Y = this.yPositionOnScreen + 118;
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
        this.DrawTabs(b);
        if (this.showingQuests)
        {
            this.DrawQuestList(b);
            this.DrawQuestResult(b);
            this.DrawQuestActions(b);
        }
        else
        {
            this.searchBox.Draw(b);
            this.DrawNpcList(b);
            this.DrawResult(b);
            this.DrawActions(b);
        }
        this.DrawTrackingStatus(b);
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private void DrawTabs(SpriteBatch b)
    {
        this.DrawTab(b, this.GetNpcTabBounds(), this.i18n.Get("menu.tab.npcs"), !this.showingQuests);
        this.DrawTab(
            b,
            this.GetQuestTabBounds(),
            this.i18n.Get("menu.tab.quests", new { count = this.activeQuests.Count }),
            this.showingQuests
        );
    }

    private void DrawTab(SpriteBatch b, Rectangle bounds, string label, bool selected)
    {
        Color tint = selected
            ? Color.SandyBrown * 0.55f
            : bounds.Contains(Game1.getMousePosition(true)) ? Color.Wheat * 0.65f : Color.White * 0.35f;
        b.Draw(Game1.staminaRect, bounds, tint);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(
            Game1.smallFont,
            label,
            new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2),
            Game1.textColor
        );
    }

    private void DrawQuestList(SpriteBatch b)
    {
        Rectangle bounds = this.GetListBounds();
        int rows = this.GetVisibleListRows();
        for (int visibleIndex = 0; visibleIndex < rows; visibleIndex++)
        {
            int index = this.questListOffset + visibleIndex;
            if (index >= this.activeQuests.Count)
                break;

            DeliveryQuestSnapshot quest = this.activeQuests[index];
            Rectangle row = new(bounds.X + 4, bounds.Y + visibleIndex * RowHeight, bounds.Width - 8, RowHeight);
            if (string.Equals(quest.Key, this.selectedQuestKey, StringComparison.Ordinal))
                b.Draw(Game1.staminaRect, row, Color.SandyBrown * 0.35f);
            else if (index == this.controllerIndex)
                b.Draw(Game1.staminaRect, row, Color.Wheat * 0.45f);
            else if (row.Contains(Game1.getMousePosition(true)))
                b.Draw(Game1.staminaRect, row, Color.White * 0.25f);

            b.DrawString(Game1.smallFont, quest.Title, new Vector2(row.X + 10, row.Y + 8), Game1.textColor);
        }

        if (this.activeQuests.Count == 0)
        {
            b.DrawString(
                Game1.smallFont,
                this.i18n.Get("quest.menu.none"),
                new Vector2(bounds.X + 10, bounds.Y + 10),
                Game1.textColor
            );
        }
    }

    private void DrawQuestResult(SpriteBatch b)
    {
        int x = this.xPositionOnScreen + Padding + LeftWidth + 28;
        int y = this.yPositionOnScreen + 92;
        DeliveryQuestSnapshot? quest = this.GetSelectedQuest();
        if (quest is null)
        {
            this.DrawLine(b, this.i18n.Get("quest.menu.select-prompt"), x, y, Game1.textColor);
            return;
        }

        b.DrawString(Game1.dialogueFont, quest.Title, new Vector2(x, y), Game1.textColor);
        y += 62;
        this.DrawLine(
            b,
            this.i18n.Get("quest.menu.target", new { npc = quest.NpcDisplayName }),
            x,
            y,
            Game1.textColor
        );
        y += 38;
        this.DrawLine(
            b,
            this.i18n.Get("quest.menu.item", new { item = quest.ItemDisplayName, count = quest.RequiredCount }),
            x,
            y,
            Game1.textColor
        );
        y += 38;
        this.DrawLine(
            b,
            this.i18n.Get("quest.menu.held", new { held = quest.HeldCount, count = quest.RequiredCount }),
            x,
            y,
            Game1.textColor
        );
        y += 38;
        string deadline = quest.DaysLeft is int days
            ? this.i18n.Get("quest.menu.days-left", new { days })
            : this.i18n.Get("quest.menu.no-deadline");
        this.DrawLine(b, deadline, x, y, Game1.textColor);
    }

    private void DrawQuestActions(SpriteBatch b)
    {
        DeliveryQuestSnapshot? quest = this.GetSelectedQuest();
        if (quest is null)
            return;

        string label = this.isTrackingQuest(quest.Key)
            ? this.i18n.Get("quest.menu.stop-tracking")
            : this.i18n.Get("quest.menu.track-target");
        this.DrawButton(b, this.GetTaskTrackButtonBounds(), label, enabled: true);
    }

    private void DrawTrackingStatus(SpriteBatch b)
    {
        TrackerMenuState? state = this.getTrackerState();
        if (state is null)
            return;

        Rectangle bounds = this.GetTrackingStatusBounds();
        b.Draw(Game1.staminaRect, bounds, Color.SandyBrown * 0.28f);
        int x = bounds.X + 14;
        int y = bounds.Y + 8;

        string? questKey = this.getTrackedQuestKey();
        DeliveryQuestSnapshot? quest = questKey is null
            ? null
            : this.activeQuests.FirstOrDefault(
                entry => string.Equals(entry.Key, questKey, StringComparison.Ordinal)
            );
        string heading = questKey is null
            ? this.i18n.Get("tracking.card.manual", new { npc = state.NpcDisplayName })
            : quest is not null
                ? this.i18n.Get("tracking.card.quest", new
                {
                    quest = quest.Title,
                    npc = state.NpcDisplayName
                })
                : this.i18n.Get("tracking.card.quest-fallback", new { npc = state.NpcDisplayName });
        this.DrawLine(b, this.FitText(heading, bounds.Width - 28), x, y, Game1.textColor);
        y += 30;

        NpcQueryResponse? response = state.Response;
        string detail;
        Color detailColor = Game1.textColor;
        if (response is null)
        {
            detail = this.i18n.Get("tracking.card.refreshing");
        }
        else if (response.Status != QueryStatus.Success)
        {
            detail = this.TranslateStatus(response.Status);
            detailColor = Color.DarkRed;
        }
        else if (response.Location is null)
        {
            detail = this.TranslateStatus(response.LocationStatus);
            detailColor = Color.DarkRed;
        }
        else
        {
            string location = this.locationNames.Resolve(
                response.Location.InternalName,
                response.Location.DisplayName
            );
            detail = this.i18n.Get("tracking.card.location", new { location });
        }
        this.DrawLine(b, this.FitText(detail, bounds.Width - 28), x, y, detailColor);
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
            this.yPositionOnScreen + 174,
            LeftWidth,
            this.height - 214
        );
    }

    private Rectangle GetNpcTabBounds()
    {
        return new Rectangle(this.xPositionOnScreen + Padding, this.yPositionOnScreen + 70, 112, 38);
    }

    private Rectangle GetQuestTabBounds()
    {
        Rectangle npcTab = this.GetNpcTabBounds();
        return new Rectangle(npcTab.Right + 8, npcTab.Y, 180, npcTab.Height);
    }

    private int GetVisibleListRows() => this.GetListBounds().Height / RowHeight;

    private int GetVisibleScheduleRows() => this.getTrackerState() is null ? 6 : 4;

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

    private Rectangle GetTaskTrackButtonBounds()
    {
        int x = this.xPositionOnScreen + Padding + LeftWidth + 28;
        return new Rectangle(x, this.yPositionOnScreen + this.height - 68, 230, 48);
    }

    private Rectangle GetTrackingStatusBounds()
    {
        int x = this.xPositionOnScreen + Padding + LeftWidth + 28;
        return new Rectangle(x, this.yPositionOnScreen + this.height - 154, 540, 76);
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
        int count = this.showingQuests ? this.activeQuests.Count : this.filteredNpcs.Count;
        if (count == 0)
            return;

        this.controllerIndex = Math.Clamp(
            this.controllerIndex < 0 ? 0 : this.controllerIndex + delta,
            0,
            count - 1
        );
        int visibleRows = this.GetVisibleListRows();
        if (this.showingQuests)
        {
            if (this.controllerIndex < this.questListOffset)
                this.questListOffset = this.controllerIndex;
            else if (this.controllerIndex >= this.questListOffset + visibleRows)
                this.questListOffset = this.controllerIndex - visibleRows + 1;
        }
        else
        {
            if (this.controllerIndex < this.listOffset)
                this.listOffset = this.controllerIndex;
            else if (this.controllerIndex >= this.listOffset + visibleRows)
                this.listOffset = this.controllerIndex - visibleRows + 1;
        }
        Game1.playSound("shiny4");
    }

    private void SetQuestTab(bool showQuests)
    {
        if (this.showingQuests == showQuests)
            return;

        this.showingQuests = showQuests;
        this.searchBox.Selected = !showQuests;
        Game1.keyboardDispatcher.Subscriber = showQuests ? null : this.searchBox;
        if (showQuests && this.selectedQuestKey is null && this.activeQuests.Count > 0)
            this.selectedQuestKey = this.activeQuests[0].Key;
        this.controllerIndex = showQuests
            ? (this.activeQuests.Count > 0 ? 0 : -1)
            : (this.filteredNpcs.Count > 0 ? 0 : -1);
        Game1.playSound("smallSelect");
    }

    private void RefreshActiveQuests()
    {
        this.activeQuests = this.getActiveQuests().ToList();
        int maxOffset = Math.Max(0, this.activeQuests.Count - this.GetVisibleListRows());
        this.questListOffset = Math.Clamp(this.questListOffset, 0, maxOffset);
        if (this.selectedQuestKey is not null
            && this.activeQuests.All(quest => !string.Equals(quest.Key, this.selectedQuestKey, StringComparison.Ordinal)))
        {
            this.selectedQuestKey = null;
        }
        if (this.showingQuests && this.controllerIndex >= this.activeQuests.Count)
            this.controllerIndex = this.activeQuests.Count - 1;
    }

    private DeliveryQuestSnapshot? GetSelectedQuest()
    {
        return this.selectedQuestKey is null
            ? null
            : this.activeQuests.FirstOrDefault(
                quest => string.Equals(quest.Key, this.selectedQuestKey, StringComparison.Ordinal)
            );
    }

    private string FitText(string text, int maxWidth)
    {
        if (Game1.smallFont.MeasureString(text).X <= maxWidth)
            return text;

        const string ellipsis = "…";
        int length = text.Length;
        while (length > 1
            && Game1.smallFont.MeasureString(text[..length] + ellipsis).X > maxWidth)
        {
            length--;
        }
        return text[..length] + ellipsis;
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
