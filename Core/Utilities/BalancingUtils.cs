using System;
using Terraria;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class BalancingUtils
{
    public static int FixDamageFromDifficulty(int damage)
    {
        float damageJankCorrectionFactor = 1f / 2f;
        if (Main.expertMode)
            damageJankCorrectionFactor = 1f / 4f;
        if (Main.masterMode)
            damageJankCorrectionFactor = 1f / 6f;
        return (int) (damage * damageJankCorrectionFactor);
    }

    public static int DifficultyBasedValue(int normal, int? expert = null, int? master = null, int? ftw = null,
        int? legendary = null, int? gfb = null)
    {
        int val = normal;
        if (expert.HasValue && Main.expertMode)
            val = expert.Value;
        if (master.HasValue && Main.masterMode)
            val = master.Value;
        if (ftw.HasValue && Main.getGoodWorld)
            val = ftw.Value;
        if (legendary.HasValue && Main.IsLegendaryWorld)
            val = legendary.Value;
        if (gfb.HasValue && Main.zenithWorld)
            val = gfb.Value;
        return val;
    }

    public static float DifficultyBasedValue(float normal, float? expert = null, float? master = null,
        float? ftw = null, float? legendary = null, float? gfb = null)
    {
        float val = normal;
        if (expert.HasValue && Main.expertMode)
            val = expert.Value;
        if (master.HasValue && Main.masterMode)
            val = master.Value;
        if (ftw.HasValue && Main.getGoodWorld)
            val = ftw.Value;
        if (legendary.HasValue && Main.IsLegendaryWorld)
            val = legendary.Value;
        if (gfb.HasValue && Main.zenithWorld)
            val = gfb.Value;
        return val;
    }

    public static void SetLifeMaxByMode(this NPC npc, int normal, int expert, int master, int? fixedboi = null)
    {
        npc.lifeMax = normal;
        if (Main.expertMode)
            npc.lifeMax = expert;
        if (Main.masterMode)
            npc.lifeMax = master;
        if (Main.zenithWorld && fixedboi.HasValue)
            npc.lifeMax = fixedboi.Value;
    }

    public static int DamageSoftCap(double dmgInput, int cap)
    {
        if (dmgInput < cap)
            return (int) dmgInput;

        double cappedRatio = Math.Pow(dmgInput / cap, 0.5) / 1.25 + 0.2;
        return (int) (cap * cappedRatio);
    }
}
