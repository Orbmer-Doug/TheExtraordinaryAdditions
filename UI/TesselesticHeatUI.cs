using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.UI;
/*
                 int heat = (int)proj.As<TesselesticMeltdownProj>().Heat;
                float interpolant = InverseLerp(0f, TesselesticMeltdownProj.LightningWait, heat);

                Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.SmallBar1);
                Texture2D tex2 = AssetRegistry.GetTexture(AdditionsTexture.SmallBar0);
                Point pos = (drawPlayer.Center + new Vector2(-tex.Width / 2, -40) + Vector2.UnitY * drawPlayer.gfxOffY - Main.screenPosition).ToPoint();
                Rectangle target = new(pos.X, pos.Y, (int)(interpolant * tex.Width), tex.Height);
                Rectangle source = new(0, 0, (int)(interpolant * tex.Width), tex.Height);
                Rectangle target2 = new(pos.X, pos.Y + 2, tex2.Width, tex2.Height);

                Main.spriteBatch.Draw(tex2, target2, new Color(40, 40, 40));
                Main.spriteBatch.Draw(tex, target, source, proj.As<TesselesticMeltdownProj>().HeatColor);

 */
public class TesselesticHeatUI : SmartUIState
{
    public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
    public override InterfaceScaleType Scale => InterfaceScaleType.None;

    internal static bool CurrentlyViewing;
    public override bool Visible => CurrentlyViewing;
    public override void Draw(SpriteBatch spriteBatch)
    {
        Player player = Main.LocalPlayer;
        if (player == null || player.active == false || player.heldProj == -1)
            return;
        Projectile proj = Main.projectile[player.heldProj] ?? null;
        if (proj == null || proj.active == false || proj.type != ModContent.ProjectileType<TesselesticMeltdownProj>())
            return;

        int heat = (int)proj.As<TesselesticMeltdownProj>().Heat;
        float interpolant = InverseLerp(0f, TesselesticMeltdownProj.LightningWait, heat);

        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.SmallBar1);
        Texture2D tex2 = AssetRegistry.GetTexture(AdditionsTexture.SmallBar0);
        Point pos = (player.Center + new Vector2(-tex.Width / 2, -40) + Vector2.UnitY * player.gfxOffY - Main.screenPosition).ToPoint();
        Rectangle target = new(pos.X, pos.Y, (int)(interpolant * tex.Width), tex.Height);
        Rectangle source = new(0, 0, (int)(interpolant * tex.Width), tex.Height);
        Rectangle target2 = new(pos.X, pos.Y + 2, tex2.Width, tex2.Height);

        Main.spriteBatch.Draw(tex2, target2, new Color(40, 40, 40));
        Main.spriteBatch.Draw(tex, target, source, proj.As<TesselesticMeltdownProj>().HeatColor);
    }
}

