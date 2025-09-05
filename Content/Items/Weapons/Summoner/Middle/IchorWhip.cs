using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class IchorWhip : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.IchorWhip);
    public override void SetDefaults()
    {
        Item.damage = 74;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 32;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ItemRarityID.Pink;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<IchorWhipProjectile>();
        Item.shootSpeed = 1f;
        Item.knockBack = 4f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(219, 195, 11));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BlandWhip, 1);
        recipe.AddIngredient(ItemID.Ichor, 16);
        recipe.AddIngredient(ItemID.SoulofNight, 10);
        recipe.AddIngredient(ItemID.TissueSample, 8);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();

    }
    public override bool MeleePrefix()
    {
        return true;
    }
}