using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class JudgeOfHellsArmaments : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.JudgeOfHellsArmaments);

    public override void ModifyTooltips(List<TooltipLine> list)
    {
        list.ColorLocalization(new Color(255, 217, 0));
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true;
        Item.damage = 500;
        Item.knockBack = 4.5f;
        Item.width = 98;
        Item.height = 16;
        Item.scale = 1f;
        Item.UseSound = SoundID.Item1;
        Item.rare = ItemRarityID.Master;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<JudgeSwing>();
        Item.shootSpeed = 16f;
        Item.shootsEveryUse = true;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.ArmorPenetration = 20;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2)
        {
            player.NewPlayerProj(position, velocity, ModContent.ProjectileType<JudgeSpear>(), damage, knockback, player.whoAmI);
            return false;
        }
        else
        {
            player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
            Main.projectile[player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI)].As<JudgeSwing>().Splendor = true;
            return false;
        }
    }

    public override bool AltFunctionUse(Player player) => Collision.CanHitLine(player.Center, 20, 20, player.Additions().mouseWorld, 8, 8);
    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override bool MeleePrefix() => false;
    
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.HallowedBar, 12);
        recipe.AddIngredient(ItemID.SoulofFright, 4);
        recipe.AddIngredient(ItemID.SoulofMight, 4);
        recipe.AddIngredient(ItemID.SoulofSight, 4);
        recipe.AddIngredient(ItemID.SoulofLight, 10);
        recipe.AddIngredient(ItemID.Ruby, 7);
        recipe.AddIngredient(ItemID.Sapphire, 5);
        recipe.AddIngredient(ItemID.Topaz, 5);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}