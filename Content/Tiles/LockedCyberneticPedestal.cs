using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Tiles;

public class LockedCyberneticPedestal : ModTile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LockedCyberneticPedestal);
    public SlotId ShimmerID;
    public const int Width = 4;
    public const int Height = 1;

    public override void SetStaticDefaults()
    {
        MinPick = 225;
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileSpelunker[Type] = true;
        Main.tileLighted[Type] = true;
        Main.tileNoAttach[ModContent.TileType<LockedCyberneticPedestal>()] = false;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
        TileObjectData.newTile.Width = Width;
        TileObjectData.newTile.Height = Height;
        TileObjectData.newTile.Origin = new Point16(2, 0);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.CoordinateHeights = [16];
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(0, 255, 255));
    }

    public override bool CanExplode(int i, int j) => false;

    public override bool CreateDust(int i, int j, ref int type)
    {
        type = DustID.Electric;
        return true;
    }
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        r = 0.0f;
        g = 1.0f;
        b = 1.0f;
    }
    public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
    {
        int xFrameOffset = Main.tile[i, j].TileFrameX;
        int yFrameOffset = Main.tile[i, j].TileFrameY;
        if (xFrameOffset != 0 || yFrameOffset != 0)
            return;

        Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
    }
    public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
        Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset;

        ManagedShader effect = ShaderRegistry.SpreadTelegraph;
        effect.TrySetParameter("centerOpacity", .7f);
        effect.TrySetParameter("mainOpacity", 1f);
        effect.TrySetParameter("halfSpreadAngle", MathHelper.Lerp(MathHelper.PiOver4, PiOver3, AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f));
        effect.TrySetParameter("edgeColor", Color.DarkCyan.ToVector3());
        effect.TrySetParameter("centerColor", ColorSwap(Color.Cyan, Color.DeepSkyBlue, 5f).ToVector3());
        effect.TrySetParameter("edgeBlendLength", 0.09f);
        effect.TrySetParameter("edgeBlendStrength", 13f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect.Effect, Matrix.Identity);

        Texture2D invis = AssetRegistry.InvisTex;
        Vector2 origin = new(invis.Width / 2f, invis.Height / 2f);
        Main.EntitySpriteDraw(invis, drawPosition + Vector2.UnitX * 32f, null, Color.White, -MathHelper.PiOver2, origin, 900f, 0, 0f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

        Texture2D swordtex = AssetRegistry.GetTexture(AdditionsTexture.LockedCyberneticSword);
        Main.EntitySpriteDraw(swordtex, drawPosition + Vector2.UnitX * 28f - Vector2.UnitY * (125f + (float)Math.Sin(Main.GameUpdateCount * 0.06f) * 30f), null, Color.White, -MathHelper.PiOver4, swordtex.Size() * .5f, 1f, 0, 0f);
    }
}
