using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class LightripRounds : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LightripRounds);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }
    public override void SetDefaults()
    {
        Item.damage = 22;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 8;
        Item.height = 8;
        Item.maxStack = 1;
        Item.consumable = false;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ModContent.RarityType<CyberneticRarity>();
        Item.shoot = ModContent.ProjectileType<LightripBullet>();
        Item.shootSpeed = 14f;
        Item.ammo = AmmoID.Bullet;
    }
}
