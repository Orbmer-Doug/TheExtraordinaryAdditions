using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class TripleKatanas : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TripleKatanas);
    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 60;
        Item.width = 96;
        Item.height = 92;
        Item.damage = 1450;
        Item.knockBack = 0f;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.rare = ItemRarityID.Purple;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.shoot = ModContent.ProjectileType<KatanaCleave>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.shootSpeed = 10f;
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override bool AltFunctionUse(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<KatanaSweep>()] <= 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2)
        {
            player.NewPlayerProj(position, velocity, ModContent.ProjectileType<KatanaSweep>(), damage, knockback, player.whoAmI);
        }
        else
            player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);

        return false;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<ImpureAstralKatanas>(), 1);
        recipe.AddIngredient(ItemID.Silk, 7);
        recipe.AddIngredient(ItemID.SoulofSight, 12);
        recipe.AddIngredient(ItemID.LunarBar, 16);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}