using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Content.Items.Placeable;

namespace TheExtraordinaryAdditions.Content.Tiles;


public class SereneSatellitePlaced : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SereneSatellitePlaced);
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
        TileObjectData.newTile.Origin = new Point16(2, 1);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 1;
        TileObjectData.newTile.StyleLineSkip = 1;
        TileObjectData.addTile(Type);

        LocalizedText name = CreateMapEntryName();
        AddMapEntry(new Color(200, 200, 200), name);
    }

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<SereneSatellite>();
    }
}