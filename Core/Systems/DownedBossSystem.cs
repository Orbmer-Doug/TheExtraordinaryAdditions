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
        downedRegistry.AddRange((List<string>) tag.GetList<string>(nameof(downedRegistry)));
    }

    public static void SetDefeatState<TBossType>(bool isDefeated) where TBossType : ModNPC
    {
        string bossName = ModContent.GetModNPC(ModContent.NPCType<TBossType>()).Name;
        switch (isDefeated)
        {
            case true when !downedRegistry.Contains(bossName):
                downedRegistry.Add(bossName);
                break;
            case false:
                downedRegistry.Remove(bossName);
                break;
        }

        AdditionsNetcode.SyncBossDefeats(Main.myPlayer);
    }

    public static bool HasDefeated<TBossType>() where TBossType : ModNPC =>
        downedRegistry.Contains(ModContent.GetModNPC(ModContent.NPCType<TBossType>()).Name);
}

public interface IBossDowned
{
}

public class GlobalBossDefeatMarker : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (npc.ModNPC is not IBossDowned ||
            BossDownedSaveSystem.downedRegistry.Contains(npc.ModNPC.Name))
            return;

        string bossName = ModContent.GetModNPC(npc.type).Name;
        BossDownedSaveSystem.downedRegistry.Add(bossName);
        AdditionsNetcode.SyncBossDefeats(Main.myPlayer);
    }
}
