using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

public class RejuvenationArtifact : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RejuvenationArtifact);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 30, 0));
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 40;
        Item.accessory = true;
        Item.rare = ItemRarityID.Yellow;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.maxStack = 1;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        int gem = ModContent.ProjectileType<RejuvenationHover>();
        if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[gem] <= 0)
            player.NewPlayerProj(player.Center, Vector2.Zero, gem, 0, 0f, player.whoAmI);

        player.GetModPlayer<RejuvenationArtifactPlayer>().Equipped = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BandofRegeneration, 1);
        recipe.AddIngredient(ItemID.LifeCrystal, 1);
        recipe.AddIngredient(ItemID.LifeforcePotion, 2);
        recipe.AddIngredient(ItemID.HeartreachPotion, 6);
        recipe.AddIngredient(ItemID.SpectreBar, 10);
        recipe.AddTile(TileID.TinkerersWorkbench);
        recipe.Register();
    }
}

public sealed class RejuvenationArtifactPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
}