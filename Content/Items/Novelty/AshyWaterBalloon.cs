using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;

namespace TheExtraordinaryAdditions.Content.Items.Novelty;

// sorry
public class AshyWaterBalloon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AshyWaterBalloon);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 100;
    }
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.GelBalloon);
        Item.width = 24;
        Item.height = 24;
        Item.rare = ItemRarityID.Red;
        Item.shoot = ModContent.ProjectileType<AshyWaterBalloonProjectile>();
        Item.shootSpeed = 10f;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.autoReuse = true;
        Item.consumable = true;
        Item.maxStack = Item.CommonMaxStack;
        Item.damage = 1;
        Item.knockBack = 10;
        Item.value = Item.buyPrice(platinum: 1);
    }
}