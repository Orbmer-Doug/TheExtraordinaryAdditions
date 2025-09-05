using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class ClassicGreenBlock : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ClassicGreenBlock);
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