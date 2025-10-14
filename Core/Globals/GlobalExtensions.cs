using Terraria;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Globals.NPCGlobal;
using TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;

namespace TheExtraordinaryAdditions.Core.Globals;
public static class GlobalExtensions
{
    public static GlobalPlayer Additions(this Player player) => player.GetModPlayer<GlobalPlayer>();

    public static AdditionsGlobalNPC Additions(this NPC npc) => npc.GetGlobalNPC<AdditionsGlobalNPC>();
    public static AdditionsNPCInfo AdditionsInfo(this NPC npc) => npc.GetGlobalNPC<AdditionsNPCInfo>();

    public static AdditionsGlobalProjectile Additions(this Projectile projectile) => projectile.GetGlobalProjectile<AdditionsGlobalProjectile>();
    public static AdditionsProjectileInfo AdditionsInfo(this Projectile projectile) => projectile.GetGlobalProjectile<AdditionsProjectileInfo>();
    public static ProjectileDamageModifiers ProjDamageMod(this Projectile projectile) => projectile.GetGlobalProjectile<ProjectileDamageModifiers>();

    public static AdditionsGlobalItem Additions(this Item item) => item.GetGlobalItem<AdditionsGlobalItem>();
}