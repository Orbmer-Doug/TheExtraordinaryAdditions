using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;

public class TungstenCube : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TungstenCube);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(247, 226, 218));
    }
    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 24;
        Item.accessory = true;
        Item.defense = 18;
        Item.rare = ItemRarityID.Orange;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.maxStack = 1;
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetArmorPenetration(DamageClass.Generic) += 10f;
        player.maxFallSpeed = 50f;
        player.fallStart = 2000;
        player.fallStart2 = 2000;
        player.thorns = .5f;
        player.noKnockback = true;
        player.moveSpeed -= .65f;
        player.runAcceleration *= .9f;
        player.ignoreWater = true;
        player.canFloatInWater = false;
        player.adjWater = false;
        player.waterWalk = false;
        player.waterWalk2 = false;
        player.jumpBoost = false;
        player.jumpSpeedBoost = -1;
        player.wingTimeMax -= 20;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.TungstenBar, 20);
        recipe.AddIngredient(ItemID.Bone, 206);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}