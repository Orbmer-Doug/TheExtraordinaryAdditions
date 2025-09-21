using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class Epidemic : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Epidemic);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8, false));
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(ColorSwap(Color.LawnGreen, Color.LimeGreen, 5f));
    }

    public override void SetDefaults()
    {
        Item.damage = 210;
        Item.DamageType = DamageClass.Magic;
        Item.width = 92;
        Item.height = 76;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.UseSound = SoundID.Grass;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<EpidemicHoldout>();
        Item.shootSpeed = 16f;
        Item.mana = 2;

        Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player) => false;
    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<VirulentEntrapment>(), 1);
        recipe.AddIngredient(ItemID.Stinger, 6);
        recipe.AddIngredient(ItemID.Vine, 3);
        recipe.AddIngredient(ItemID.JungleSpores, 14);
        recipe.AddIngredient(ItemID.MudBlock, 400);
        recipe.AddIngredient(ModContent.ItemType<MurkyPaste>(), 4);
        recipe.AddIngredient(ModContent.ItemType<UelibloomBar>(), 10);
        recipe.AddTile(TileID.Bookcases);
        recipe.Register();
    }
}