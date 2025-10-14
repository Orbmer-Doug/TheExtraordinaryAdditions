using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;

public class ObsidianFlail : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ObsidianFlail);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ToolTipDamageMultiplier[Type] = 2f;
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 45;
        Item.useTime = 45;
        Item.knockBack = 5.5f;
        Item.width = 30;
        Item.height = 32;
        Item.damage = 60;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<ObsidianMaceProj>();
        Item.shootSpeed = 12f;
        Item.UseSound = null;
        Item.rare = ItemRarityID.Orange;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.channel = true;
        Item.noMelee = true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.BlueMoon, 1)
            .AddIngredient(ItemID.Sunfury, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}