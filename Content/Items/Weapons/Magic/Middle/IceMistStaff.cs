using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;

public class IceMistStaff : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.IceMistStaff);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 75;
        Item.DamageType = DamageClass.Magic;
        Item.width = 136;
        Item.height = 30;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.rare = ItemRarityID.LightPurple;
        Item.UseSound = SoundID.Item71;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<IceMistHeld>();
        Item.shootSpeed = 0f;
        Item.crit = 10;
        Item.mana = 12;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(76, 146, 237));
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.25f, new Vector2(0f, 0f));
        return false;
    }

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<CracklingFragments>(), 3);
        recipe.AddIngredient(ItemID.SoulofSight, 10);
        recipe.AddIngredient(ItemID.SoulofMight, 10);
        recipe.AddIngredient(ItemID.SoulofFright, 10);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}