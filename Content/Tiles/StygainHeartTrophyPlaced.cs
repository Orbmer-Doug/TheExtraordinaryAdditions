using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Content.Items.Placeable;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class StygainHeartTrophyPlaced : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StygainHeartTrophyPlaced);
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.FramesOnKillWall[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.addTile(Type);

        AddMapEntry(new(212, 48, 48), Language.GetText("MapObject.Trophy"));
        DustType = 7;
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 32, ModContent.ItemType<StygainHeartTrophy>());
    }
}
