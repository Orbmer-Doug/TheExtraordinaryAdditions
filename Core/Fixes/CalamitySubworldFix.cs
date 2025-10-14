using SubworldLibrary;
using System.Reflection;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.World.Subworlds;

namespace TheExtraordinaryAdditions.Core.Fixes;

/// <summary>
/// Calamity runs hardcoded indexes for things like abyss checks in a fashion which doesn't respect subworlds,
/// so this disables it for compatibility when only in subworlds (not like the crater has anything to grow on)
/// </summary>
public class CalamitySubworldFix : ModSystem
{
    public delegate void orig_HandleTileGrowth();

    public delegate void hook_HandleTileGrowth(orig_HandleTileGrowth orig);

    public override void OnModLoad()
    {
        if (!ModLoader.TryGetMod("CalamityMod", out Mod cal))
            return;

        MethodInfo tileGrowMethod = cal.Code.GetType("CalamityMod.Systems.WorldMiscUpdateSystem")?.GetMethod("HandleTileGrowth", BindingFlags.Public | BindingFlags.Static) ?? null;
        if (tileGrowMethod is null)
        {
            Mod.Logger.Warn("Calamitys 'WorldMiscUpdateSystem' type could not be found! The tile growth interaction fix could not be applied!");
            return;
        }

        MonoModHooks.Add(tileGrowMethod, (hook_HandleTileGrowth)DisableGrowth);
    }

    public static void DisableGrowth(orig_HandleTileGrowth orig)
    {
        // Hardcoded proximity checks dont work in subworlds
        if (SubworldSystem.IsActive<CloudedCrater>())
            return;

        orig();
    }
}
