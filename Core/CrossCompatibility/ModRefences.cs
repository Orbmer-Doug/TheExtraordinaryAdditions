using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.CrossCompatibility;

public class ModReferences : ModSystem
{
    public static Mod BaseCalamity
    {
        get;
        internal set;
    }
    public static Mod Fables
    {
        get;
        internal set;
    }
    public static Mod BossChecklist
    {
        get;
        private set;
    }
    public static Mod Infernum
    {
        get;
        private set;
    }
    public static Mod NycrosNohitMod
    {
        get;
        private set;
    }

    public override void Load()
    {
        if (ModLoader.TryGetMod("CalamityMod", out Mod cal))
            BaseCalamity = cal;
        if (ModLoader.TryGetMod("CalamityFables", out Mod fablesssomgg))
            Fables = fablesssomgg;
        if (ModLoader.TryGetMod("BossChecklist", out Mod bcl))
            BossChecklist = bcl;
        if (ModLoader.TryGetMod("Infernum", out Mod inf))
            Infernum = inf;
        if (ModLoader.TryGetMod("EfficientNohits", out Mod efh))
            NycrosNohitMod = efh;
    }
}
