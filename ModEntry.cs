using MultiplayerNpcLocator.Config;
using MultiplayerNpcLocator.Framework;
using MultiplayerNpcLocator.Integrations;
using MultiplayerNpcLocator.Multiplayer;
using MultiplayerNpcLocator.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MultiplayerNpcLocator;

/// <summary>The mod entry point.</summary>
public sealed class ModEntry : Mod
{
    private ModConfig? config;
    private MultiplayerQueryCoordinator? queryCoordinator;
    private NpcTrackerOverlay? tracker;
    private QuestPromptOverlay? questPrompt;
    private QuestTrackingService? questTracking;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>();
        this.queryCoordinator = new MultiplayerQueryCoordinator(
            helper,
            this.Monitor,
            this.ModManifest,
            this.config
        );
        this.queryCoordinator.RegisterEvents();
        this.queryCoordinator.ResponseReceived += this.OnQueryResponse;
        this.tracker = new NpcTrackerOverlay(
            this.config,
            helper.Translation,
            npcName => this.queryCoordinator?.QueryFromTracker(npcName)
        );
        this.questTracking = new QuestTrackingService();
        this.questPrompt = new QuestPromptOverlay(
            this.config,
            helper.Translation,
            this.questTracking,
            this.OnTrackQuest,
            () => this.tracker?.Stop(),
            npcName => this.tracker?.IsTracking(npcName) == true,
            () => this.config.ShowTrackerOverlay && this.tracker?.TrackedNpcName is not null
        );
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;

        helper.ConsoleCommands.Add(
            "mnl_validate",
            "Run phase-0 API probes. Usage: mnl_validate [NPC internal name]",
            this.OnValidateCommand
        );
        helper.ConsoleCommands.Add(
            "mnl_query",
            "Run a phase-1 local or host-authoritative query. Usage: mnl_query <NPC internal name>",
            this.OnQueryCommand
        );

        this.Monitor.Log(
            "Multiplayer NPC Locator 0.1.0 loaded. Use 'mnl_query <NPC name>' for the phase-1 query test.",
            LogLevel.Info
        );
    }

    private void OnValidateCommand(string command, string[] args)
    {
        string npcName = args.Length > 0 ? string.Join(" ", args) : "Pam";
        ApiValidationService.Run(this.Monitor, npcName);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? api = this.Helper.ModRegistry
            .GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null || this.config is null)
            return;

        api.Register(this.ModManifest, this.ResetConfig, () => this.Helper.WriteConfig(this.config));

        api.AddSectionTitle(this.ModManifest, () => this.Helper.Translation.Get("config.section.general"));
        api.AddKeybindList(
            this.ModManifest,
            () => this.config.OpenMenuKey,
            value => this.config.OpenMenuKey = value,
            () => this.Helper.Translation.Get("config.open-menu-key.name"),
            () => this.Helper.Translation.Get("config.open-menu-key.tooltip"),
            fieldId: nameof(ModConfig.OpenMenuKey)
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.EnableQuestDetection,
            value => this.config.EnableQuestDetection = value,
            () => this.Helper.Translation.Get("config.quest-detection.name"),
            () => this.Helper.Translation.Get("config.quest-detection.tooltip"),
            fieldId: nameof(ModConfig.EnableQuestDetection)
        );

        api.AddSectionTitle(this.ModManifest, () => this.Helper.Translation.Get("config.section.tracker"));
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShowTrackerOverlay,
            value => this.config.ShowTrackerOverlay = value,
            () => this.Helper.Translation.Get("config.show-tracker.name"),
            fieldId: nameof(ModConfig.ShowTrackerOverlay)
        );
        api.AddTextOption(
            this.ModManifest,
            () => this.config.TrackerPosition,
            value => this.config.TrackerPosition = value,
            () => this.Helper.Translation.Get("config.tracker-position.name"),
            allowedValues: new[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" },
            formatAllowedValue: value => this.Helper.Translation.Get($"config.position.{value}"),
            fieldId: nameof(ModConfig.TrackerPosition)
        );
        api.AddNumberOption(
            this.ModManifest,
            () => this.config.TrackerOpacityPercent,
            value => this.config.TrackerOpacityPercent = value,
            () => this.Helper.Translation.Get("config.tracker-opacity.name"),
            min: 35,
            max: 100,
            interval: 5,
            formatValue: value => $"{value}%",
            fieldId: nameof(ModConfig.TrackerOpacityPercent)
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShowNextStop,
            value => this.config.ShowNextStop = value,
            () => this.Helper.Translation.Get("config.show-next-stop.name"),
            fieldId: nameof(ModConfig.ShowNextStop)
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShowDirectionAndDistance,
            value => this.config.ShowDirectionAndDistance = value,
            () => this.Helper.Translation.Get("config.show-direction.name"),
            fieldId: nameof(ModConfig.ShowDirectionAndDistance)
        );

        api.AddSectionTitle(
            this.ModManifest,
            () => this.Helper.Translation.Get("config.section.host"),
            () => this.Helper.Translation.Get("config.section.host.tooltip")
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.AllowRemoteQueries,
            value => this.config.AllowRemoteQueries = value,
            () => this.Helper.Translation.Get("config.allow-remote.name"),
            fieldId: nameof(ModConfig.AllowRemoteQueries)
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShareCurrentLocation,
            value => this.config.ShareCurrentLocation = value,
            () => this.Helper.Translation.Get("config.share-location.name"),
            fieldId: nameof(ModConfig.ShareCurrentLocation)
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShareDailySchedule,
            value => this.config.ShareDailySchedule = value,
            () => this.Helper.Translation.Get("config.share-schedule.name"),
            fieldId: nameof(ModConfig.ShareDailySchedule)
        );
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShowHostNotifications,
            value => this.config.ShowHostNotifications = value,
            () => this.Helper.Translation.Get("config.host-notifications.name"),
            fieldId: nameof(ModConfig.ShowHostNotifications)
        );
        api.AddNumberOption(
            this.ModManifest,
            () => this.config.MaxRequestsPerSecond,
            value => this.config.MaxRequestsPerSecond = value,
            () => this.Helper.Translation.Get("config.max-requests.name"),
            () => this.Helper.Translation.Get("config.max-requests.tooltip"),
            min: 1,
            max: 10,
            interval: 1,
            fieldId: nameof(ModConfig.MaxRequestsPerSecond)
        );
    }

    private void ResetConfig()
    {
        if (this.config is null)
            return;

        ModConfig defaults = new();
        this.config.OpenMenuKey = defaults.OpenMenuKey;
        this.config.EnableQuestDetection = defaults.EnableQuestDetection;
        this.config.ShowTrackerOverlay = defaults.ShowTrackerOverlay;
        this.config.TrackerPosition = defaults.TrackerPosition;
        this.config.TrackerOpacityPercent = defaults.TrackerOpacityPercent;
        this.config.ShowNextStop = defaults.ShowNextStop;
        this.config.ShowDirectionAndDistance = defaults.ShowDirectionAndDistance;
        this.config.AllowRemoteQueries = defaults.AllowRemoteQueries;
        this.config.ShareCurrentLocation = defaults.ShareCurrentLocation;
        this.config.ShareDailySchedule = defaults.ShareDailySchedule;
        this.config.ShowHostNotifications = defaults.ShowHostNotifications;
        this.config.MaxRequestsPerSecond = defaults.MaxRequestsPerSecond;
    }

    private void OnQueryCommand(string command, string[] args)
    {
        if (args.Length == 0)
        {
            this.Monitor.Log("Usage: mnl_query <NPC internal name>", LogLevel.Warn);
            return;
        }

        this.queryCoordinator?.QueryFromConsole(string.Join(" ", args));
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == SButton.MouseLeft
            && this.questPrompt?.ReceiveLeftClick(
                Game1.getMousePosition(true).X,
                Game1.getMousePosition(true).Y
            ) == true)
        {
            this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (!Context.IsWorldReady
            || this.config is null
            || !this.config.OpenMenuKey.JustPressed())
        {
            return;
        }

        if (Game1.activeClickableMenu is NpcSearchMenu)
        {
            Game1.exitActiveMenu();
            return;
        }
        if (Game1.activeClickableMenu is not null)
            return;

        List<NpcListEntry> npcs = this.GetNpcList();
        Game1.activeClickableMenu = new NpcSearchMenu(
            npcs,
            this.Helper.Translation,
            npcName => this.queryCoordinator?.QueryFromMenu(npcName),
            this.OnTrackNpc,
            this.OnStopTracking,
            npcName => this.tracker?.IsTracking(npcName) == true,
            () => this.questTracking?.GetActiveQuests() ?? Array.Empty<DeliveryQuestSnapshot>(),
            quest => this.questPrompt?.TrackQuest(quest),
            questKey => this.questPrompt?.IsTrackingQuest(questKey) == true
        );
    }

    private void OnQueryResponse(NpcQueryResponse response)
    {
        this.tracker?.SetResponse(response);
        if (Game1.activeClickableMenu is NpcSearchMenu menu)
            menu.SetResponse(response);
    }

    private void OnTrackNpc(string npcName, NpcQueryResponse? response)
    {
        this.questPrompt?.DetachTaskTracking();
        this.tracker?.Track(npcName, response);
    }

    private void OnTrackQuest(DeliveryQuestSnapshot quest)
    {
        this.tracker?.Track(quest.NpcName, null);
    }

    private void OnStopTracking()
    {
        this.questPrompt?.DetachTaskTracking();
        this.tracker?.Stop();
    }

    private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        this.questPrompt?.Scan();
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        this.tracker?.Refresh();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.tracker?.Refresh();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.tracker?.Stop();
        this.questPrompt?.Clear();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.Player.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID)
            this.tracker?.Refresh();
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (Context.IsWorldReady && Game1.activeClickableMenu is null)
        {
            this.tracker?.Draw(e.SpriteBatch);
            this.questPrompt?.Draw(e.SpriteBatch);
        }
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        if (e.Peer.IsHost)
            this.tracker?.Stop();
    }

    private List<NpcListEntry> GetNpcList()
    {
        Dictionary<string, string> giftTastes = this.Helper.GameContent
            .Load<Dictionary<string, string>>("Data/NPCGiftTastes");

        return giftTastes.Keys
            .Where(name => !name.StartsWith("Universal_", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                NPC? npc = Game1.getCharacterFromName(name);
                return new NpcListEntry(name, npc?.displayName ?? name);
            })
            .ToList();
    }
}
