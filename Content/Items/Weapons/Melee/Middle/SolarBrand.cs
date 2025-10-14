using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class SolarBrand : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SolarBrand);

    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8, false));
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 134;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.useTime = 24;
        Item.useAnimation = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTurn = false;
        Item.knockBack = 7f;
        Item.noUseGraphic = true;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ItemRarityID.Red;
        Item.UseSound = SoundID.Item20;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<SolarBrandSword>();
        Item.channel = true;
        Item.noMelee = true;

        Item.width = 76;
        Item.height = 194;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.FirstOrDefault(n => n.Name == "Damage").Text = tooltips.FirstOrDefault(n => n.Name == "Damage").Text.Replace("damage", GetTextValue("Items.SolarBrand.Damage"));
        tooltips.ColorLocalization(new(255, 72, 31));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => false;

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, .25f, new Vector2(0f, 0f));
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 4);
        recipe.AddIngredient(ItemID.FragmentSolar, 18);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
