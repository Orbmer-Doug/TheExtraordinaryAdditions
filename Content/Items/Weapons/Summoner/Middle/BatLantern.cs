using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class BatLantern : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BatLantern);

    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault("Midnight Bat Caller");
        // Tooltip.SetDefault("Calls in Midnight Bats. Effected by mage and summon damage");
        ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 45;
        Item.knockBack = 2f;
        Item.mana = 10;
        Item.width = 100;
        Item.height = 15;
        Item.useTime = 16;
        Item.useAnimation = 16;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
        Item.UseSound = SoundID.Item32;
        Item.noMelee = true;
        Item.DamageType = DamageClass.MagicSummonHybrid;
        Item.buffType = ModContent.BuffType<MidnightBats>();
        Item.shoot = ModContent.ProjectileType<BatSummon>();
        Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(102, 37, 11));
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
        recipe.AddIngredient(ItemID.SpookyTwig, 1);
        recipe.AddIngredient(ItemID.Torch, 3);
        recipe.AddIngredient(ItemID.Ectoplasm, 8);
        recipe.AddIngredient(ItemID.SoulofNight, 5);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}