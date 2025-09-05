using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class SereneSatellite : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SereneSatellite);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.CanGetPrefixes[Type] = false; // music boxes can't get prefixes in vanilla
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox; // recorded music boxes transform into the basic form in shimmer

        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.clairdelune)), ModContent.ItemType<SereneSatellite>(), ModContent.TileType<SereneSatellitePlaced>());
    }

    public override void SetDefaults()
    {
        Item.DefaultToMusicBox(ModContent.TileType<SereneSatellitePlaced>(), 0);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MusicBox, 1);
        recipe.AddIngredient(ItemID.Moonglow, 10);
        recipe.AddTile(TileID.BloodMoonMonolith);
        recipe.Register();
    }
}