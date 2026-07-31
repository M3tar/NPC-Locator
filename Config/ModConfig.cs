using StardewModdingAPI.Utilities;

namespace MultiplayerNpcLocator.Config;

internal sealed class ModConfig
{
    public KeybindList OpenMenuKey { get; set; } = KeybindList.Parse("F3");

    public bool AllowRemoteQueries { get; set; } = true;
    public bool ShareCurrentLocation { get; set; } = true;
    public bool ShareDailySchedule { get; set; } = true;
    public bool ShowHostNotifications { get; set; } = false;
    public int MaxRequestsPerSecond { get; set; } = 4;
}
