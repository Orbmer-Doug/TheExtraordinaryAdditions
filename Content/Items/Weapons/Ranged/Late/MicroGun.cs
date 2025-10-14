using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class MicroGun : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MicroGun);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 150;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 267;
        Item.height = 83;
        Item.useTime = Item.useAnimation = 3;
        Item.knockBack = 0;
        Item.rare = ModContent.RarityType<ShadowRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<MicroGunHoldout>();
        Item.crit = 0;
        Item.shootSpeed = 12f;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(181, 62, 33));
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override bool CanConsumeAmmo(Item ammo, Player player)
    {
        if (Utils.NextFloat(Main.rand) > 0.95f)
        {
            return player.ownedProjectileCounts[this.Item.shoot] > 0;
        }
        return false;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile((IEntitySource)(object)source, position, velocity, ModContent.ProjectileType<MicroGunHoldout>(), damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.13f, new Vector2(0f, 0f));
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<Kingsbane>(), 1);
        recipe.AddIngredient(ModContent.ItemType<SDFMG>(), 1);
        recipe.AddIngredient(ModContent.ItemType<ShadowspecBar>(), 5);
        recipe.AddTile(ModContent.TileType<DraedonsForge>());
        recipe.Register();
    }
}