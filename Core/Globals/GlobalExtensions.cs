using Terraria;

namespace TheExtraordinaryAdditions.Core.Globals;
public static class GlobalExtensions
{
    public static GlobalPlayer Additions(this Player player) => player.GetModPlayer<GlobalPlayer>();

    public static AdditionsGlobalNPC Additions(this NPC npc) => npc.GetGlobalNPC<AdditionsGlobalNPC>();

    public static AdditionsNPCInfo AdditionsInfo(this NPC npc) => npc.GetGlobalNPC<AdditionsNPCInfo>();

    public static AdditionsGlobalProjectile Additions(this Projectile projectile) => projectile.GetGlobalProjectile<AdditionsGlobalProjectile>();

    public static AdditionsGlobalItem Additions(this Item item) => item.GetGlobalItem<AdditionsGlobalItem>();
}