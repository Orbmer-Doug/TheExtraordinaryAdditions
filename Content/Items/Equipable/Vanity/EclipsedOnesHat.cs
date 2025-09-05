using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Vanity;

[AutoloadEquip(EquipType.Head)]
public class EclipsedOnesHat : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EclipsedOnesHat);
    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = true; // Don't draw the head at all. Used by Space Creature Mask
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; // Draw hair as if a hat was covering the top. Used by Wizards Hat
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; // Draw all hair as normal. Used by Mime Mask, Sunglasses
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 32;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.rare = ItemRarityID.Pink;
        Item.vanity = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Silk, 10);
        recipe.AddIngredient(ItemID.FlinxFur, 3);
        recipe.AddIngredient(ItemID.IceBlock, 25);
        recipe.AddIngredient(ItemID.Bone, 10);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}