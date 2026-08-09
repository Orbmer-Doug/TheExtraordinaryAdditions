using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class Cosmireaper : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Cosmireaper);

    public override void SetDefaults()
    {
        Item.damage = 4500;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.width = 92;
        Item.height = 118;
        Item.noMelee = true;
        Item.useTime = Item.useAnimation = 15;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 4f;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.autoReuse = false;
        Item.shoot = ModContent.ProjectileType<CosmireapSweep>();
        Item.shootSpeed = 1f;
        Item.useTurn = false;
        Item.channel = true;
        Item.noUseGraphic = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(ColorSwap(Color.MediumPurple * 1.1f, Color.PaleVioletRed, 4f));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type,
        int damage, float knockback)
    {
        if (player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed)
        {
            player.NewPlayerProj(player.Center, Vector2.Zero, ModContent.ProjectileType<CosmireapThrow>(), damage,
                knockback);
            return false;
        }

        return base.Shoot(player, source, position, velocity, type, damage, knockback);
    }

    public override bool AltFunctionUse(Player player)
    {
        return CanShoot(player) && player.ownedProjectileCounts[ModContent.ProjectileType<LaceratedSpace>()] <= 0;
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0 &&
                                                    player.ownedProjectileCounts[
                                                        ModContent.ProjectileType<CosmireapThrow>()] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Sickle, 1);
        //TODO
        recipe.Register();
    }
}
