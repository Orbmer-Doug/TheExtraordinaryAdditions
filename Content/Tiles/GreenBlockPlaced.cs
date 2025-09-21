using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class GreenBlockPlaced : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GreenBlockPlaced);
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileMergeDirt[Type] = true;
        Main.tileBlockLight[Type] = true;

        DustType = DustID.GrassBlades;

        AddMapEntry(new Color(32, 117, 29));
    }

    public override void NumDust(int i, int j, bool fail, ref int num)
    {
        num = fail ? 1 : 3;
    }
}