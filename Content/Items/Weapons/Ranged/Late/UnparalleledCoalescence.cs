using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late.Zenith;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class UnparalleledCoalescence : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.UnparalleledCoalescence);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(192, 255, 173));
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.2f, new Vector2(0f, 0f));
        return false;
    }

    public override void SetDefaults()
    {
        Item.damage = 1200;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 133;
        Item.height = 291;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.rare = ModContent.RarityType<LegendaryRarity>();
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<CoalescenceHoldout>();
        Item.shootSpeed = 16f;
        Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CopperBow, 1);
        recipe.AddIngredient(ItemID.DaedalusStormbow, 1);
        recipe.AddIngredient(ModContent.ItemType<CharringBarrage>(), 1);
        recipe.AddIngredient(ModContent.ItemType<HallowedGreatbow>(), 1);
        recipe.AddIngredient(ItemID.Tsunami, 1);
        recipe.AddIngredient(ItemID.FairyQueenRangedItem, 1);
        recipe.AddIngredient(ItemID.Phantasm, 1);
        recipe.AddIngredient(ItemID.Celeb2, 1);
        recipe.AddIngredient(ModContent.ItemType<HeavenForgedCannon>(), 1);
        recipe.AddIngredient(ModContent.ItemType<Lanikea>(), 1);
        recipe.AddIngredient(ModContent.ItemType<CosmicImplosion>(), 1);
        recipe.AddIngredient(ModContent.ItemType<DivineGeode>(), 15);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 5);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}