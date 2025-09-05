using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class WereYouFoolin : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WereYouFoolin);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.CanGetPrefixes[Type] = false; // music boxes can't get prefixes in vanilla
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox; // recorded music boxes transform into the basic form in shimmer

        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.wereyoufoolin)), ModContent.ItemType<WereYouFoolin>(), ModContent.TileType<JazzBlock>());
    }

    public override void SetDefaults()
    {
        Item.DefaultToMusicBox(ModContent.TileType<JazzBlock>(), 0);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MusicBox, 1);
        recipe.AddIngredient(ItemID.CopperBrick, 10);
        recipe.AddIngredient(ItemID.YellowPaint, 15);
        recipe.AddIngredient(ItemID.Glass, 10);
        recipe.AddIngredient(ItemID.Wire, 30);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }

}