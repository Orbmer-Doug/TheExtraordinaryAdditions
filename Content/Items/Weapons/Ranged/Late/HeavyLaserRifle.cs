using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class HeavyLaserRifle : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavyLaserRifle);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.AnimatesAsSoul[Type] = true;
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.width = 204;
        Item.height = 48;
        Item.scale = 1f;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.useTime = 60;
        Item.useAnimation = 60;
        Item.useTime = Item.useAnimation = HeavyLaserRifleHold.PlasmaFireTimer;
        Item.autoReuse = true;
        Item.UseSound = null;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 295;
        Item.knockBack = 1f;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.shoot = ProjectileID.PurificationPowder;
        Item.shootSpeed = 5f;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(209, 125, 0));
    }

    public override bool CanShoot(Player player)
    {
        return false;
    }
    
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.2f, new Vector2(0f, 0f));
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LaserMachinegun, 1);
        recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
        recipe.AddIngredient(ItemID.LunarBar, 14);
        recipe.AddIngredient(ItemID.Wire, 120);
        recipe.AddIngredient(ItemID.Glass, 30);
        recipe.AddIngredient(ModContent.ItemType<CoreofSunlight>(), 12);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}