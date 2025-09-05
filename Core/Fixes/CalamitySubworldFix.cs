using SubworldLibrary;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.World.Subworlds;

namespace TheExtraordinaryAdditions.Core.Fixes;

/* CONTEXT:
     * Calamity executes its hardcoded index code for random tile growth (such as lumenyl on voidstone) and abyss checks for the sulphurous sea in a way that does not
     * respect subworlds. As such, when Calamity is on such behaviors should be disabled in subworlds.
     */
public class CalamitySubworldFix : ModSystem
{
    public delegate bool orig_IsSulphSeaActiveMethod(object instance, Player player);

    public delegate bool hook_IsSulphSeaActiveMethod(orig_IsSulphSeaActiveMethod orig, object instance, Player player);

    public override void OnModLoad()
    {
        if (!ModLoader.TryGetMod("CalamityMod", out Mod cal))
            return;

        MethodInfo tileGrowMethod = cal.Code.GetType("CalamityMod.Systems.WorldMiscUpdateSystem")?.GetMethod("HandleTileGrowth", BindingFlags.Public | BindingFlags.Static) ?? null;
        if (tileGrowMethod is null)
        {
            Mod.Logger.Warn("Calamity's 'WorldMiscUpdateSystem' type could not be found! The sulphurous sea subworld interaction fix could not be applied!");
            return;
        }

        MethodInfo sulphSeaActiveMethod = cal.Code.GetType("CalamityMod.BiomeManagers.SulphurousSeaBiome")?.GetMethod("IsBiomeActive", BindingFlags.Public | BindingFlags.Instance) ?? null;
        if (sulphSeaActiveMethod is null)
        {
            Mod.Logger.Warn("Calamity's 'SulphurousSeaBiome' type could not be found! The sulphurous sea subworld interaction fix could not be applied!");
            return;
        }

        MonoModHooks.Add(sulphSeaActiveMethod, (hook_IsSulphSeaActiveMethod)DisableSulphSeaInSubworlds);
    }
    public static bool DisableSulphSeaInSubworlds(orig_IsSulphSeaActiveMethod orig, object instance, Player player)
    {
        // Hardcoded proximity checks don't work in subworlds.
        if (SubworldSystem.IsActive<CloudedCrater>())
            return false;

        return orig(instance, player);
    }
}
