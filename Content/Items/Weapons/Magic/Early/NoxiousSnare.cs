using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;

public class NoxiousSnare : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.NoxiousSnare);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(23, 244, 23));
    }

    public override void SetDefaults()
    {
        Item.damage = 9;
        Item.DamageType = DamageClass.Magic;
        Item.width = 28;
        Item.height = 30;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityGreenBuyPrice;
        Item.rare = ItemRarityID.Green;
        Item.UseSound = SoundID.Grass;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<SnareHoldout>();
        Item.shootSpeed = 16f;
        Item.mana = 2;
        Item.noUseGraphic = true;
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Book, 1);
        recipe.AddIngredient(ItemID.JungleSpores, 10);
        recipe.AddIngredient(ItemID.Vine, 2);
        recipe.AddIngredient(ItemID.MudBlock, 50);
        recipe.AddTile(TileID.Bookcases);
        recipe.Register();
    }
}