using StardewModdingAPI.Utilities;

namespace MultiplayerNpcLocator.Config;

internal sealed class ModConfig
{
    public KeybindList OpenMenuKey { get; set; } = KeybindList.Parse("F3");

    public bool EnableQuestDetection { get; set; } = true;

    public bool ShowTrackerOverlay { get; set; } = true;
    public string TrackerPosition { get; set; } = "TopLeft";
    public int TrackerOpacityPercent { get; set; } = 90;
    public bool ShowNextStop { get; set; } = true;
    public bool ShowDirectionAndDistance { get; set; } = true;

    public bool AllowRemoteQueries { get; set; } = true;
    public bool ShareCurrentLocation { get; set; } = true;
    public bool ShareDailySchedule { get; set; } = true;
    public bool ShowHostNotifications { get; set; } = false;
    public int MaxRequestsPerSecond { get; set; } = 4;
}
