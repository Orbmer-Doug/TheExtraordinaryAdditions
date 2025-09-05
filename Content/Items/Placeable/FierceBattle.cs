using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class FierceBattle : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FierceBattle);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.CanGetPrefixes[Type] = false; // music boxes can't get prefixes in vanilla
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox; // recorded music boxes transform into the basic form in shimmer

        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.SRank)), ModContent.ItemType<FierceBattle>(), ModContent.TileType<FierceBattlePlaced>());
    }

    public override void SetDefaults()
    {
        Item.DefaultToMusicBox(ModContent.TileType<FierceBattlePlaced>(), 0);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MusicBox, 1);
        recipe.AddIngredient(ItemID.SpikyBall, 20);
        recipe.AddIngredient(ItemID.GoldenPlatform, 20);
        recipe.AddIngredient(ItemID.LunarTabletFragment, 20);
        recipe.AddIngredient(ItemID.SpookyWood, 20);
        recipe.AddIngredient(ItemID.IceBlock, 20);
        recipe.AddIngredient(ItemID.MartianConduitPlating, 20);
        recipe.AddTile(TileID.LunarMonolith);
        recipe.Register();
    }

}