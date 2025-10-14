using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;

public class Acheron : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Acheron);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 60;
        Item.DamageType = DamageClass.Magic;
        Item.width = 186;
        Item.height = 30;
        Item.useTime = 15;
        Item.useAnimation = 15;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 11f;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.rare = ItemRarityID.Pink;
        Item.UseSound = SoundID.Item71;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<HellishLance>();
        Item.shootSpeed = 60;
        Item.crit = 20;
        Item.mana = 20;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(200, 0, 241));
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.29f, new Vector2(0f, 0f));
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.UnholyTrident, 1);
        recipe.AddIngredient(ItemID.DemonScythe, 1);
        recipe.AddIngredient(ItemID.SoulofFright, 11);
        recipe.AddIngredient(ItemID.SoulofSight, 9);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}