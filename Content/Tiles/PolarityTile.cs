using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class PolarityTile : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PolarityTile);
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileWaterDeath[Type] = false;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width = 15;
        TileObjectData.newTile.Height = 11;
        TileObjectData.newTile.Origin = new Point16(2, 2);
        TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16];
        TileObjectData.newTile.LavaDeath = true;
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileID.Sets.FramesOnKillWall[Type] = true;
        TileID.Sets.DisableSmartCursor[Type] = true;

        AddMapEntry(Color.Tan);
        DustType = DustID.WoodFurniture;
    }
}
