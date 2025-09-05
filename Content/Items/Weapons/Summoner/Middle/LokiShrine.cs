using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class LokiShrine : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LokiShrine);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(245, 141, 5));
    }
    public override void SetDefaults()
    {
        Item.damage = 145;
        Item.knockBack = 2f;
        Item.mana = 10;
        Item.width = 40;
        Item.height = 40;
        Item.useTime = 16;
        Item.useAnimation = 16;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.rare = ItemRarityID.Red;
        Item.UseSound = SoundID.Item44;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Summon;
        Item.buffType = ModContent.BuffType<LokiBuff>();
        Item.shoot = ModContent.ProjectileType<IztMinion>();
    }

    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        position = Main.MouseWorld;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);

        var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
        projectile.originalDamage = Item.damage;

        return false;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.FragmentStardust, 15);
        recipe.AddIngredient(ItemID.Topaz, 5);
        recipe.AddIngredient(ItemID.SandBlock, 75);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}