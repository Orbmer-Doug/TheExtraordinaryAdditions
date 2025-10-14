using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

/// <summary>
/// First gun in the mod
/// </summary>
public class AntiMatterCannon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AntiMatterCannon);

    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault("Anti Matter Sniper Rifle");
        // Tooltip.SetDefault("DESTRUCTION!");

        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.width = Item.height = 50;
        Item.scale = 1f;
        Item.rare = ItemRarityID.Purple;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.useTime = Item.useAnimation = 4;
        Item.autoReuse = true;
        Item.UseSound = null;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noUseGraphic = true;
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 1500;
        Item.knockBack = 10f;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<AntiMatterCannonHoldout>();
        Item.shootSpeed = 10f;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Chocolate);
    }

    public override bool CanShoot(Player player) => false;

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.145f, new Vector2(0f, 0f));
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SniperRifle, 1);
        recipe.AddIngredient(ItemID.FragmentSolar, 12);
        recipe.AddIngredient(ItemID.LunarBar, 30);
        recipe.AddIngredient(ModContent.ItemType<CoreofSunlight>(), 12);
        recipe.AddIngredient(ModContent.ItemType<DivineGeode>(), 14);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}