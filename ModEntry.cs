using MultiplayerNpcLocator.Config;
using MultiplayerNpcLocator.Framework;
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
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

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

        // The multiplayer host remains silent by default; single-player is kept for local testing.
        if (Context.IsMultiplayer && Context.IsMainPlayer)
            return;

        List<NpcListEntry> npcs = this.GetNpcList();
        Game1.activeClickableMenu = new NpcSearchMenu(
            npcs,
            this.Helper.Translation,
            npcName => this.queryCoordinator?.QueryFromMenu(npcName)
        );
    }

    private void OnQueryResponse(NpcQueryResponse response)
    {
        if (Game1.activeClickableMenu is NpcSearchMenu menu)
            menu.SetResponse(response);
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
