using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;

public class FungalSatchel : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FungalSatchel);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.AliceBlue);
    }
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }
    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 40;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.accessory = true;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, new Color(95, 110, 255).ToVector3() * 1.5f);
        player.Additions().FungalSatchel = true;
    }
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, new Color(95, 110, 255).ToVector3() * 1f);
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        {
            recipe.AddIngredient(ItemID.GlowingMushroom, 150);
            recipe.AddIngredient(ItemID.ShinePotion, 3);
            recipe.AddIngredient(ItemID.Leather, 5);
            recipe.AddTile(TileID.WorkBenches);
        }
        recipe.Register();
    }
}
