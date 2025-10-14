using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheExtraordinaryAdditions.Core.Netcode;

namespace TheExtraordinaryAdditions.Core.Systems;

public class BossDownedSaveSystem : ModSystem
{
    internal static List<string> downedRegistry = [];

    public override void OnWorldLoad()
    {
        if (!SubworldSystem.AnyActive())
            downedRegistry?.Clear();
    }

    public override void OnWorldUnload()
    {
        if (!SubworldSystem.AnyActive())
            downedRegistry?.Clear();
    }

    public override void SaveWorldData(TagCompound tag) => tag[nameof(downedRegistry)] = downedRegistry;

    public override void LoadWorldData(TagCompound tag)
    {
        downedRegistry.Clear();
        downedRegistry.AddRange((List<string>)tag.GetList<string>(nameof(downedRegistry)));
    }

    public static void SetDefeatState<BossType>(bool isDefeated) where BossType : ModNPC
    {
        string bossName = ModContent.GetModNPC(ModContent.NPCType<BossType>()).Name;
        if (isDefeated && !downedRegistry.Contains(bossName))
            downedRegistry.Add(bossName);
        if (!isDefeated)
            downedRegistry.Remove(bossName);

        AdditionsNetcode.SyncBossDefeats(Main.myPlayer);
    }

    public static bool HasDefeated<BossType>() where BossType : ModNPC =>
        downedRegistry.Contains(ModContent.GetModNPC(ModContent.NPCType<BossType>()).Name);
}

public interface IBossDowned { }

public class GlobalBossDefeatMarker : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (npc.ModNPC is not null and IBossDowned downed && !BossDownedSaveSystem.downedRegistry.Contains(npc.ModNPC.Name))
        {
            string bossName = ModContent.GetModNPC(npc.type).Name;
            BossDownedSaveSystem.downedRegistry.Add(bossName);
            AdditionsNetcode.SyncBossDefeats(Main.myPlayer);
        }
    }
}