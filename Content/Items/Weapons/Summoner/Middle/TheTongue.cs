using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class TheTongue : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheTongue);

    public override void SetDefaults()
    {
        Item.rare = ItemRarityID.Pink;
        Item.damage = 50;
        Item.channel = true;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.width = 50;
        Item.height = 34;
        Item.DamageType = DamageClass.Summon;
        Item.knockBack = 3f;
        Item.useTime = 25;
        Item.useAnimation = 25;
        Item.autoReuse = true;
        Item.holdStyle = 16;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.UseSound = SoundID.Item1;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<TheTongueWhip>();
        Item.shootSpeed = 10f;
        Item.noUseGraphic = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(219, 195, 11));
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool MeleePrefix()
    {
        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<IchorWhip>(), 1);
        recipe.AddIngredient(ItemID.SoulofFright, 12);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}