using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Buff;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class SolemnLament : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SolemnLament);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 51;
        Item.DamageType = DamageClass.Melee;
        Item.width = 58;
        Item.height = 50;
        Item.useTime = 7;
        Item.useAnimation = 7;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shootSpeed = 10f;

        Item.shoot = ModContent.ProjectileType<SolemnLamentProj>();
    }

    public override bool AltFunctionUse(Player player) => !player.HasBuff(ModContent.BuffType<EternalRestCooldown>());
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2)
        {
            player.AddBuff(ModContent.BuffType<EternalRest>(), SecondsToFrames(10));
        }
        else
        {
            for (int i = 0; i <= 1; i++)
            {
                SolemnLamentProj p = Main.projectile[Projectile.NewProjectile(source, position,
                    velocity, type, damage, knockback, player.whoAmI, 0f, 0f, 0f)].As<SolemnLamentProj>();
                p.GunType = i;
            }
        }

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Handgun, 1);
        recipe.AddIngredient(ItemID.BeetleHusk, 10);
        recipe.AddRecipeGroup("AnyButterfly", 8);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }

}
