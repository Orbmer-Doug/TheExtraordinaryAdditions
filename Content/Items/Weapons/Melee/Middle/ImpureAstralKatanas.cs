using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class ImpureAstralKatanas : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ImpureAstralKatanas);

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 54;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.rare = ItemRarityID.LightPurple;
        Item.useTime = Item.useAnimation = 60;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 2f;
        Item.autoReuse = true;
        Item.damage = 130;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<AstralKatanaSweep>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(108, 54, 115));
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

//TODO
    //public override bool AltFunctionUse(Player player) => !.HasCooldown(player, AstralDashCooldown.ID);

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2)
        {
            player.NewPlayerProj(position, velocity, ModContent.ProjectileType<AstralKatanaThrow>(), damage, knockback,
                player.whoAmI);
        }
        else
        {
            player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
            Main.projectile[player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI)]
                .As<AstralKatanaSweep>().Orange = true;
        }

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.AdamantiteBar, 10);
        recipe.AddIngredient(ModContent.ItemType<MeteorKatana>(), 1);
        //TODO
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}
