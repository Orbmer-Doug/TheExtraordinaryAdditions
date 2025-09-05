using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class EbonyNovaBlaster : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EbonyNovaBlaster);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.width = 66;
        Item.height = 22;
        Item.scale = 1f;
        Item.rare = ItemRarityID.Yellow;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;

        Item.useTime = 5;
        Item.useAnimation = 5;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;

        Item.UseSound = null;

        // Weapon Properties
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 300;
        Item.knockBack = 8f;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<EbonyNovaBlasterHeld>();
        Item.noUseGraphic = true;
        Item.shootSpeed = 10f;
    }

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.OnyxBlaster, 1);
        recipe.AddIngredient(ItemID.Ectoplasm, 15);
        recipe.AddIngredient(ItemID.SoulofFright, 12);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}