using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;

public class ScriptureOfTheSuperLoki : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ScriptureOfTheSuperLoki);

    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault("Scripture of the Super Loki");
        // Tooltip.SetDefault("Conjures a Powerful Loki to fight.");
        Main.RegisterItemAnimation(Type, new DrawAnimationVertical(10, 12, false));
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 98, 7));
    }

    public override void SetDefaults()
    {
        Item.damage = 160;
        Item.knockBack = 3f;
        Item.mana = 20;
        Item.width = Item.height = 100;
        Item.useTime = 32;
        Item.useAnimation = 32;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.UseSound = SoundID.Item44;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Summon;
        Item.buffType = ModContent.BuffType<SuperLoki>();
        Item.shoot = ModContent.ProjectileType<SuperIztMinion>();
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);
        Projectile projectile = Projectile.NewProjectileDirect(source, player.Additions().mouseWorld, velocity, type, damage, knockback, Main.myPlayer);
        projectile.originalDamage = Item.damage;
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LunarOre, 60);
        recipe.AddIngredient(ModContent.ItemType<LokiShrine>(), 1);
        recipe.AddIngredient(ItemID.Silk, 18);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.AddTile(TileID.FleshCloningVat);
        recipe.Register();
    }
}