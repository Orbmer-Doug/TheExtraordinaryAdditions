using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class TorrentialTides : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TorrentialTides);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(96, 143, 181));
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 60;
        Item.damage = 1950;
        Item.knockBack = 4.5f;
        Item.width = 132;
        Item.height = 328;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.rare = ModContent.RarityType<BrackishRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<TorrentialCleave>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }
    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("Voidstone", out ModItem Voidstone) && calamityMod.TryFind("ReaperTooth", out ModItem ReaperTooth) && calamityMod.TryFind("Lumenyl", out ModItem Lumenyl))
        {
            recipe.AddIngredient(ModContent.ItemType<FerrymansToken>(), 1);
            recipe.AddIngredient(ItemID.Flairon, 1);
            recipe.AddIngredient(ItemID.RazorbladeTyphoon, 1);
            recipe.AddIngredient(Lumenyl.Type, 18);
            recipe.AddIngredient(Voidstone.Type, 250);
            recipe.AddIngredient(ReaperTooth.Type, 6);
        }
        else
        {
            recipe.AddIngredient(ModContent.ItemType<FerrymansToken>(), 1);
            recipe.AddIngredient(ItemID.Flairon, 1);
            recipe.AddIngredient(ItemID.RazorbladeTyphoon, 1);
            recipe.AddIngredient(ItemID.LunarBar, 10);
            recipe.AddIngredient(ItemID.SoulofFlight, 30);
        }
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}