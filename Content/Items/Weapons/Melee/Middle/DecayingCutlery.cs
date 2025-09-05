using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class DecayingCutlery : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DecayingCutlery);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.SkipsInitialUseSound[Item.type] = true;
        ItemID.Sets.Spears[Item.type] = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(166, 147, 38));
    }

    public override void SetDefaults()
    {
        // Common Properties
        Item.rare = ItemRarityID.Pink;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;

        // Use Properties
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = Item.useTime = 28;
        Item.UseSound = SoundID.Item71;
        Item.autoReuse = true;

        // Weapon Properties
        Item.damage = 55;
        Item.knockBack = 2.5f;
        Item.noUseGraphic = true;
        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;

        // Projectile Properties
        Item.shootSpeed = 30f;
        Item.shoot = ModContent.ProjectileType<DecayingCutleryStab>();
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.TheRottedFork, 1);
        recipe.AddIngredient(ItemID.Ichor, 12);
        recipe.AddIngredient(ItemID.SoulofNight, 6);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}