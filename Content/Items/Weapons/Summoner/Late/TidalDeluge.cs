using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;

public class TidalDeluge : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TidalDeluge);

    public override void SetDefaults()
    {
        Item.damage = 430;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 42;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ModContent.RarityType<BrackishRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<DelugeWhip>();
        Item.shootSpeed = 1f;
        Item.knockBack = 1f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(AbyssalCurrents.WaterPalette[2]);
    }

    public override bool MeleePrefix() => true;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.RainbowWhip, 1);
        recipe.AddIngredient(ModContent.ItemType<Lumenyl>(), 18);
        recipe.AddIngredient(ModContent.ItemType<Voidstone>(), 250);
        recipe.AddIngredient(ItemID.LunarBar, 8);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}