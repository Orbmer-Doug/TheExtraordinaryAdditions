using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.AuroraTurret;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Lightning;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.SolarGuardian;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class MonsterBanner : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MonsterBanner);
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.AnchorTop = new AnchorData((AnchorType)649, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.DrawYOffset = -2;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.AnchorTop = new AnchorData((AnchorType)256, TileObjectData.newTile.Width, 0);
        TileObjectData.newAlternate.DrawYOffset = -10;
        TileObjectData.addAlternate(0);
        TileObjectData.addTile(Type);
        DustType = -1;
        TileID.Sets.DisableSmartCursor[Type] = true;
        AddMapEntry(new Color(13, 88, 130), Language.GetText("MapObject.Banner"));
    }

    public override void NearbyEffects(int i, int j, bool closer)
    {
        if (!closer)
        {
            return;
        }
        Player player = Main.LocalPlayer;
        if (player != null && player.active && !player.dead)
        {
            Tile val = Main.tile[i, j];
            int npc = GetBannerNPC(val.TileFrameX / 18);
            if (npc != -1)
            {
                Main.SceneMetrics.NPCBannerBuff[npc] = true;
                Main.SceneMetrics.hasBanner = true;
            }
        }
    }

    public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
    {
        PlatformHangOffset(i, j, ref offsetY);
    }

    public static int GetBannerNPC(int style)
    {
        int npc = -1;
        switch (style)
        {
            case 0:
                npc = ModContent.NPCType<GlassPiercer>();
                break;
            case 1:
                npc = ModContent.NPCType<DuneProwler>();
                break;
            case 2:
                npc = ModContent.NPCType<FulminationSpirit>();
                break;
            case 3:
                npc = ModContent.NPCType<SolarGuardian>();
                break;
            case 4:
                npc = ModContent.NPCType<AuroraGuard>();
                break;
        }
        return npc;
    }
}
