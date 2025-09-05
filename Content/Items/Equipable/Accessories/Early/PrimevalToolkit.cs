using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;

public class PrimevalToolkit : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PrimevalToolkit);
    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 56;
        Item.value = AdditionsGlobalItem.RarityLimeBuyPrice;
        Item.rare = ItemRarityID.Orange;
        Item.accessory = true;

    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.tileSpeed += 4;
        player.blockRange += 5;
        player.pickSpeed -= 0.25f;
        player.treasureMagnet = true;
        if (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight || player.ZoneUnderworldHeight)
        {
            player.statDefense += 10;
            player.endurance += 0.05f;
            player.pickSpeed -= 0.15f;
        }

        if (!hideVisual)
        {
            player.maxFallSpeed = 25f;
            player.fallStart = 1000;
            player.fallStart2 = 1000;
            player.ignoreWater = true;
            player.canFloatInWater = false;
            player.adjWater = false;
            player.waterWalk = false;
            player.waterWalk2 = false;
            player.jumpBoost = false;
            player.jumpSpeedBoost = -1;
            player.wingTimeMax -= 20;
            player.noKnockback = true;
            player.moveSpeed -= .5f;
            player.runAcceleration *= .9f;
        }
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        {
            recipe.AddIngredient(ModContent.ItemType<TungstenCube>(), 1);
            recipe.AddIngredient(ItemID.ExtendoGrip, 1);
            recipe.AddIngredient(ItemID.MiningPotion, 5);
            recipe.AddIngredient(ItemID.AncientChisel, 1);
            recipe.AddIngredient(ItemID.TreasureMagnet, 1);
            recipe.AddIngredient(ItemID.SiltBlock, 200);
            recipe.AddTile(TileID.TinkerersWorkbench);
        }
        recipe.Register();
    }
}
