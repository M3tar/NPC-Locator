using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;

namespace NpcLocator.Framework;

/// <summary>Reads the local player's standard item-delivery quests without changing them.</summary>
internal sealed class QuestTrackingService
{
    public IReadOnlyList<DeliveryQuestSnapshot> GetActiveQuests()
    {
        if (!Context.IsWorldReady)
            return Array.Empty<DeliveryQuestSnapshot>();

        return Game1.player.questLog
            .OfType<ItemDeliveryQuest>()
            .Where(quest => !quest.completed.Value && !quest.destroy.Value)
            .Select(this.CreateSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!)
            .ToList();
    }

    private DeliveryQuestSnapshot? CreateSnapshot(ItemDeliveryQuest quest)
    {
        string npcName = quest.target.Value ?? "";
        string itemId = quest.ItemId.Value ?? "";
        if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(itemId))
            return null;

        NPC? npc = Game1.getCharacterFromName(npcName);
        string npcDisplayName = npc?.displayName ?? npcName;
        string itemDisplayName;
        try
        {
            itemDisplayName = ItemRegistry.Create(itemId).DisplayName;
        }
        catch
        {
            itemDisplayName = itemId;
        }

        int required = Math.Max(1, quest.number.Value);
        int held = Game1.player.Items
            .Where(item => item is not null
                && string.Equals(item.QualifiedItemId, itemId, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item!.Stack);
        string questId = quest.id.Value ?? "";
        string key = string.Join("|", questId, npcName, itemId, quest.dayQuestAccepted.Value);

        return new DeliveryQuestSnapshot(
            key,
            questId,
            quest.questTitle,
            npcName,
            npcDisplayName,
            itemId,
            itemDisplayName,
            required,
            held,
            quest.dailyQuest.Value ? quest.daysLeft.Value : null
        );
    }
}

internal sealed record DeliveryQuestSnapshot(
    string Key,
    string QuestId,
    string Title,
    string NpcName,
    string NpcDisplayName,
    string ItemId,
    string ItemDisplayName,
    int RequiredCount,
    int HeldCount,
    int? DaysLeft
);
