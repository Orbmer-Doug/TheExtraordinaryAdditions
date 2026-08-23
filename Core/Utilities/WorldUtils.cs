using Terraria;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class WorldUtils
{
    extension(Main)
    {
        public static bool IsLegendaryWorld => Main.masterMode && Main.getGoodWorld;
    }
}
