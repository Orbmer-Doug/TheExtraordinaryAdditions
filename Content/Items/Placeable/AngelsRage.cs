using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class AngelsRage : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AngelsRage);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.CanGetPrefixes[Type] = false; // music boxes can't get prefixes in vanilla
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox; // recorded music boxes transform into the basic form in shimmer

        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.Infinite)), ModContent.ItemType<AngelsRage>(), ModContent.TileType<InfiniteBlock>());
    }

    public override void SetDefaults()
    {
        Item.DefaultToMusicBox(ModContent.TileType<InfiniteBlock>(), 0);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MusicBox, 1);
        recipe.AddIngredient(ModContent.ItemType<JudgeOfHellsArmaments>(), 1);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }

}