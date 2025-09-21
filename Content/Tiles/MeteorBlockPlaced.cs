using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class MeteorBlockPlaced : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MeteorBlockPlaced);

    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
        HitSound = SoundID.Dig;
        MineResist = 3f;
        MinPick = 225;
        AddMapEntry(new Color(152, 108, 87), null);
    }

    public override bool CreateDust(int i, int j, ref int type)
    {
        Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustID.Meteorite, 0f, 0f, 1, Color.White, 1f);
        Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustID.MeteorHead, 0f, 0f, 1, Color.White, 1f);
        return false;
    }

    private Color GetDrawcolor(int i, int j, Color color)
    {
        Tile val = Main.tile[i, j];
        int colType = val.TileColor;
        Color paintCol = WorldGen.paintColor(colType);
        if (colType >= 13 && colType <= 24)
        {
            color.R = (byte)(paintCol.R / 255f * color.R);
            color.G = (byte)(paintCol.G / 255f * color.G);
            color.B = (byte)(paintCol.B / 255f * color.B);
        }
        return color;
    }
}
