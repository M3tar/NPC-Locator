using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MultiplayerNpcLocator.Config;
using MultiplayerNpcLocator.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MultiplayerNpcLocator.UI;

/// <summary>Shows one non-blocking local quest prompt and tracks the user's decision.</summary>
internal sealed class QuestPromptOverlay
{
    private const int Width = 500;
    private const int Height = 220;
    private const int Margin = 24;

    private readonly ModConfig config;
    private readonly ITranslationHelper i18n;
    private readonly QuestTrackingService quests;
    private readonly Action<DeliveryQuestSnapshot> trackQuest;
    private readonly Action stopTracking;
    private readonly Func<string, bool> isTrackingNpc;
    private readonly Func<bool> isTrackerVisible;
    private readonly HashSet<string> handledQuestKeys = new(StringComparer.Ordinal);

    private DeliveryQuestSnapshot? prompt;
    private string? trackedQuestKey;
    private string? trackedNpcName;

    public string? TrackedQuestKey => this.trackedQuestKey;

    public QuestPromptOverlay(
        ModConfig config,
        ITranslationHelper i18n,
        QuestTrackingService quests,
        Action<DeliveryQuestSnapshot> trackQuest,
        Action stopTracking,
        Func<string, bool> isTrackingNpc,
        Func<bool> isTrackerVisible
    )
    {
        this.config = config;
        this.i18n = i18n;
        this.quests = quests;
        this.trackQuest = trackQuest;
        this.stopTracking = stopTracking;
        this.isTrackingNpc = isTrackingNpc;
        this.isTrackerVisible = isTrackerVisible;
    }

    public void Scan()
    {
        if (!Context.IsWorldReady || !this.config.EnableQuestDetection)
        {
            this.prompt = null;
            return;
        }

        IReadOnlyList<DeliveryQuestSnapshot> active = this.quests.GetActiveQuests();
        if (this.trackedQuestKey is not null
            && active.All(quest => !string.Equals(quest.Key, this.trackedQuestKey, StringComparison.Ordinal)))
        {
            if (this.trackedNpcName is not null && this.isTrackingNpc(this.trackedNpcName))
                this.stopTracking();
            this.trackedQuestKey = null;
            this.trackedNpcName = null;
        }

        if (this.prompt is not null
            && active.All(quest => !string.Equals(quest.Key, this.prompt.Key, StringComparison.Ordinal)))
        {
            this.prompt = null;
        }

        this.prompt ??= active.FirstOrDefault(quest => !this.handledQuestKeys.Contains(quest.Key));
        if (this.prompt is not null)
        {
            DeliveryQuestSnapshot? refreshed = active.FirstOrDefault(
                quest => string.Equals(quest.Key, this.prompt.Key, StringComparison.Ordinal)
            );
            if (refreshed is not null)
                this.prompt = refreshed;
        }
    }

    public void DetachTaskTracking()
    {
        this.trackedQuestKey = null;
        this.trackedNpcName = null;
    }

    public bool IsTrackingQuest(string questKey)
    {
        return string.Equals(this.trackedQuestKey, questKey, StringComparison.Ordinal);
    }

    public void TrackQuest(DeliveryQuestSnapshot quest)
    {
        this.handledQuestKeys.Add(quest.Key);
        this.trackedQuestKey = quest.Key;
        this.trackedNpcName = quest.NpcName;
        if (string.Equals(this.prompt?.Key, quest.Key, StringComparison.Ordinal))
            this.prompt = null;
        this.trackQuest(quest);
    }

    public void Clear()
    {
        this.prompt = null;
        this.trackedQuestKey = null;
        this.trackedNpcName = null;
        this.handledQuestKeys.Clear();
    }

    public bool ReceiveLeftClick(int x, int y)
    {
        if (!this.CanShowPrompt())
            return false;

        if (this.GetTrackBounds().Contains(x, y))
        {
            DeliveryQuestSnapshot selected = this.prompt!;
            this.TrackQuest(selected);
            Game1.playSound("smallSelect");
            return true;
        }
        if (this.GetIgnoreBounds().Contains(x, y))
        {
            this.handledQuestKeys.Add(this.prompt!.Key);
            this.prompt = null;
            Game1.playSound("smallSelect");
            return true;
        }

        return false;
    }

    public void Draw(SpriteBatch b)
    {
        if (!this.CanShowPrompt())
            return;

        DeliveryQuestSnapshot quest = this.prompt!;
        Rectangle bounds = this.GetBounds();
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White);

        int x = bounds.X + 24;
        int y = bounds.Y + 18;
        b.DrawString(Game1.dialogueFont, this.i18n.Get("quest.prompt.title"), new Vector2(x, y), Game1.textColor);
        y += 50;
        b.DrawString(Game1.smallFont, quest.Title, new Vector2(x, y), Game1.textColor);
        y += 30;
        b.DrawString(
            Game1.smallFont,
            this.i18n.Get("quest.prompt.delivery", new
            {
                npc = quest.NpcDisplayName,
                item = quest.ItemDisplayName,
                count = quest.RequiredCount
            }),
            new Vector2(x, y),
            Game1.textColor
        );
        y += 30;
        string status = this.i18n.Get("quest.prompt.held", new
        {
            held = quest.HeldCount,
            count = quest.RequiredCount
        });
        if (quest.DaysLeft is int days)
            status += "  " + this.i18n.Get("quest.prompt.days-left", new { days });
        b.DrawString(Game1.smallFont, status, new Vector2(x, y), Game1.textColor);

        this.DrawButton(b, this.GetTrackBounds(), this.i18n.Get("quest.prompt.track"));
        this.DrawButton(b, this.GetIgnoreBounds(), this.i18n.Get("quest.prompt.ignore"));
    }

    private bool CanShowPrompt()
    {
        return this.config.EnableQuestDetection
            && this.prompt is not null
            && Context.IsPlayerFree
            && Game1.activeClickableMenu is null;
    }

    private Rectangle GetBounds()
    {
        bool avoidBottomRightTracker = this.isTrackerVisible()
            && string.Equals(this.config.TrackerPosition, "BottomRight", StringComparison.Ordinal);
        return new Rectangle(
            Game1.uiViewport.Width - Width - Margin,
            avoidBottomRightTracker ? Margin : Game1.uiViewport.Height - Height - Margin,
            Width,
            Height
        );
    }

    private Rectangle GetTrackBounds()
    {
        Rectangle bounds = this.GetBounds();
        return new Rectangle(bounds.X + 24, bounds.Bottom - 62, 210, 44);
    }

    private Rectangle GetIgnoreBounds()
    {
        Rectangle track = this.GetTrackBounds();
        return new Rectangle(track.Right + 16, track.Y, 210, track.Height);
    }

    private void DrawButton(SpriteBatch b, Rectangle bounds, string label)
    {
        Color tint = bounds.Contains(Game1.getMousePosition(true)) ? Color.Wheat : Color.White;
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(
            Game1.smallFont,
            label,
            new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2),
            Game1.textColor
        );
    }
}
