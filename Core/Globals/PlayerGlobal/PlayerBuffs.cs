using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

public sealed class PlayerBuffs : ModPlayer
{
    public bool EternalRested;
    public bool FrigidTonic;
    public bool DentedBySpoon;
    public bool Overheat;
    public bool BigOxygen;

    public override void Load() => ResetBuffs();
    public override void ResetEffects() => ResetBuffs();

    public void ResetBuffs()
    {
        EternalRested = FrigidTonic = DentedBySpoon = Overheat = BigOxygen = false;
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a,
        ref bool fullBright)
    {
        Vector2 randHitbox = Player.RandAreaInEntity();
        bool noShadow = drawInfo.shadow == 0f;

        if (Overheat && !Player.dead)
        {
            if (Main.rand.NextBool(3) && noShadow)
            {
                Vector2 vel = Vector2.UnitY.RotatedByRandom(.25f) * -Main.rand.NextFloat(4f, 10f);
                float scale = Main.rand.NextFloat(.3f, .8f);
                int life = Main.rand.Next(12, 20);
                Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed,
                    Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
                ParticleRegistry.SpawnHeavySmokeParticle(randHitbox, vel, life, scale, color, .9f, true, .09f);

                Dust.NewDustPerfect(randHitbox, DustID.SteampunkSteam, vel * .7f, 0, default, scale * 1.4f);
            }

            g *= 0.3f;
            r *= 0.52f;
            b *= 0.2f;
        }

        if (DentedBySpoon)
        {
            g *= 0.75f;
            r *= 0.0f;
            b *= 0.75f;
        }
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        Vector2 pos = new(drawInfo.Center.X - Main.screenPosition.X, drawInfo.Center.Y - Main.screenPosition.Y);

        if (EternalRested)
        {
            Texture2D glow = AssetRegistry.GennedTextures.GlowSoft;
            Vector2 origin = glow.Size() * .5f;
            float size = .5f + (MathF.Cos(Main.GlobalTimeWrappedHourly * 4f) * .2f + .2f);
            drawInfo.DrawDataCache.Add(new DrawData(glow, pos, null, Color.White with { A = 0 }, 0f, origin, size, 0));
        }
    }
}
