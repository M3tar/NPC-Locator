using StardewModdingAPI;
using StardewValley;

namespace MultiplayerNpcLocator.Framework;

/// <summary>Resolves location names in the viewing player's language.</summary>
internal sealed class LocationDisplayNameResolver
{
    private const string RoomSuffix = "Room";

    private readonly ITranslationHelper i18n;

    public LocationDisplayNameResolver(ITranslationHelper i18n)
    {
        this.i18n = i18n;
    }

    public string Resolve(string? internalName, string? hostDisplayName)
    {
        if (string.IsNullOrWhiteSpace(internalName))
            return this.i18n.Get("location.unknown");

        GameLocation? localLocation = Game1.getLocationFromName(internalName);
        if (IsLocalizedDisplayName(localLocation?.DisplayName, internalName))
            return localLocation!.DisplayName;

        if (IsLocalizedDisplayName(hostDisplayName, internalName))
            return hostDisplayName!;

        if (internalName.EndsWith(RoomSuffix, StringComparison.OrdinalIgnoreCase))
        {
            string npcName = internalName[..^RoomSuffix.Length];
            NPC? npc = Game1.getCharacterFromName(npcName);
            if (npc is not null && !string.IsNullOrWhiteSpace(npc.displayName))
                return this.i18n.Get("location.npc-room", new { npc = npc.displayName });
        }

        return this.i18n.Get("location.custom", new { name = internalName });
    }

    private static bool IsLocalizedDisplayName(string? displayName, string internalName)
    {
        return !string.IsNullOrWhiteSpace(displayName)
            && !string.Equals(displayName, internalName, StringComparison.OrdinalIgnoreCase);
    }
}
