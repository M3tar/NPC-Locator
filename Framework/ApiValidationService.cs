using System.Collections;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;

namespace NpcLocator.Framework;

/// <summary>Temporary, read-only probes for the phase-0 API verification.</summary>
internal static class ApiValidationService
{
    private const BindingFlags InstanceMembers =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run(IMonitor monitor, string npcName)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("API validation requires a loaded save.", LogLevel.Warn);
            return;
        }

        monitor.Log($"Phase-0 API validation started for NPC '{npcName}'.", LogLevel.Info);
        LogNpcProbe(monitor, npcName);
        LogQuestProbe(monitor);
        monitor.Log("Phase-0 API validation finished. No game state was changed.", LogLevel.Info);
    }

    private static void LogNpcProbe(IMonitor monitor, string npcName)
    {
        NPC? npc = Game1.getCharacterFromName(npcName);
        if (npc is null)
        {
            monitor.Log($"NPC lookup: '{npcName}' is currently unavailable.", LogLevel.Warn);
            return;
        }

        GameLocation? location = npc.currentLocation;
        string locationName = location?.NameOrUniqueName ?? "<unavailable>";
        string locationDisplayName = location?.DisplayName ?? "<unavailable>";
        monitor.Log(
            $"NPC lookup: internal='{npc.Name}', display='{npc.displayName}', "
            + $"location='{locationName}', locationDisplay='{locationDisplayName}', "
            + $"tile=({npc.TilePoint.X}, {npc.TilePoint.Y}).",
            LogLevel.Info
        );

        MemberInfo? scheduleMember = FindMember(npc.GetType(), "Schedule");
        if (scheduleMember is null)
        {
            monitor.Log("NPC schedule: no Schedule field or property was found.", LogLevel.Warn);
            return;
        }

        object? schedule = GetValue(scheduleMember, npc);
        monitor.Log(
            $"NPC schedule: member={DescribeMember(scheduleMember)}, "
            + $"runtimeType='{schedule?.GetType().FullName ?? "<null>"}', entries={Count(schedule)}.",
            LogLevel.Info
        );

        if (schedule is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                object? scheduleEntry = entry.Value;
                monitor.Log(
                    $"NPC schedule entry: key='{entry.Key}', valueType='{scheduleEntry?.GetType().FullName ?? "<null>"}', "
                    + $"value='{scheduleEntry}'.",
                    LogLevel.Info
                );

                if (scheduleEntry is not null)
                    LogReadableMembers(monitor, "NPC schedule member", scheduleEntry);
            }
        }
    }

    private static void LogQuestProbe(IMonitor monitor)
    {
        var quests = Game1.player.questLog;
        List<ItemDeliveryQuest> deliveryQuests = quests.OfType<ItemDeliveryQuest>().ToList();
        monitor.Log(
            $"Quest lookup: total={quests.Count}, standardItemDelivery={deliveryQuests.Count}.",
            LogLevel.Info
        );

        foreach (ItemDeliveryQuest quest in deliveryQuests)
        {
            monitor.Log(
                $"ItemDeliveryQuest: id='{quest.id.Value}', completed={quest.completed.Value}, "
                + $"accepted={quest.accepted.Value}, daysLeft={quest.daysLeft.Value}.",
                LogLevel.Info
            );

            LogReadableMembers(monitor, "Quest member", quest);
        }
    }

    private static void LogReadableMembers(IMonitor monitor, string label, object instance)
    {
        foreach (MemberInfo member in instance.GetType()
                     .GetMembers(InstanceMembers)
                     .Where(IsReadableDataMember)
                     .OrderBy(member => member.Name, StringComparer.Ordinal))
        {
            object? value;
            try
            {
                value = GetValue(member, instance);
            }
            catch (Exception ex)
            {
                monitor.Log(
                    $"{label} '{member.Name}' could not be read: {ex.GetType().Name}.",
                    LogLevel.Info
                );
                continue;
            }

            monitor.Log(
                $"{label}: {DescribeMember(member)}, runtimeType='{value?.GetType().FullName ?? "<null>"}', "
                + $"value='{FormatValue(value)}'.",
                LogLevel.Info
            );
        }
    }

    private static MemberInfo? FindMember(Type type, string name)
    {
        return type.GetProperty(name, InstanceMembers)
            ?? (MemberInfo?)type.GetField(name, InstanceMembers);
    }

    private static bool IsReadableDataMember(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => !field.IsStatic,
            PropertyInfo property => property.GetMethod is { IsStatic: false }
                && property.GetIndexParameters().Length == 0,
            _ => false
        };
    }

    private static object? GetValue(MemberInfo member, object instance)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo property => property.GetValue(instance),
            _ => null
        };
    }

    private static string DescribeMember(MemberInfo member)
    {
        Type memberType = member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => typeof(void)
        };
        return $"{member.MemberType} {member.Name}: {memberType.FullName}";
    }

    private static int? Count(object? value)
    {
        return value switch
        {
            ICollection collection => collection.Count,
            _ => null
        };
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
            return "<null>";

        string text = value.ToString() ?? "<null>";
        return text.Length <= 240 ? text : text[..240] + "…";
    }
}
