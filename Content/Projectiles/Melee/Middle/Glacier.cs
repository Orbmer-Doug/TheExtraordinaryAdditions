using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora.TEST;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class Glacier : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlacialSpike);

    public const float Scale = 1f;
    public const int GoRise = 40;
    public override void SetDefaults()
    {
        Projectile.width = (int)(70 * Scale);
        Projectile.height = (int)(156 * Scale);
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public int HitCount
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public float Interpolant => InverseLerp(0f, GoRise, Time);
    public Vector2 SavedPos;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(SavedPos);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        SavedPos = reader.ReadVector2();
    }

    public override void AI()
    {
        if (Utility.FindProjectile(out Projectile berg, ModContent.ProjectileType<BergcrusherSwing>(), Owner.whoAmI))
        {
            BaseSwordSwing swing = berg.As<BaseSwordSwing>();
            BergcrusherSwing crush = berg.As<BergcrusherSwing>();
            if (swing.Rect().Intersects(Projectile.Hitbox) && !crush.HitGlacier && berg.ModProjectile.CanHitNPC(null) == null && !crush.Right)
            {
                Vector2 mouse = swing.InitialMouseAngle.ToRotationVector2();
                for (int i = 0; i < (HitCount == 0 ? 5 : HitCount == 1 ? 9 : 15); i++)
                {
                    Vector2 pos = Projectile.RandAreaInEntity();
                    Projectile.NewProj(pos, mouse * Main.rand.NextFloat(8f, 14f), ModContent.ProjectileType<GlacialShards>(), berg.damage / 4, berg.knockBack / 2, Owner.whoAmI);
                }

                for (int i = 0; i < (HitCount + 5); i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        Vector2 pos = Projectile.RandAreaInEntity();
                        Vector2 vel = -pos.SafeDirectionTo(Projectile.Center) * Main.rand.NextFloat(4f, 6f);
                        ParticleRegistry.SpawnBloomLineParticle(pos, vel, Main.rand.Next(10, 14), Main.rand.NextFloat(.3f, .5f), Color.LightSkyBlue);
                    }

                    for (int k = 0; k < 4; k++)
                        ParticleRegistry.SpawnDustParticle(Projectile.RandAreaInEntity(), Vector2.UnitY, Main.rand.Next(30, 60), Main.rand.NextFloat(.4f, .7f), AuroraGuard.Icey, .1f, true, true);

                    Gore.NewGorePerfect(Projectile.GetSource_FromAI(), Projectile.RandAreaInEntity(), mouse.RotatedByRandom(.4f) * Main.rand.NextFloat(3f, 5f),
                        Mod.Find<ModGore>($"GlacierBreak{Main.rand.Next(1, 5)}").Type);
                }

                Projectile.timeLeft = 200;
                ScreenShakeSystem.New(new(.3f, .2f), Projectile.Center);
                AdditionsSound.ColdPunch.Play(Projectile.Center, .9f, 0f, .11f);
                crush.TimeStop = crush.StopTime;
                crush.HitGlacier = true;
                HitCount++;

                if (HitCount == 3)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 pos = Projectile.RandAreaInEntity();
                        Vector2 vel = crush.SwordDir * Main.rand.NextFloat(4f, 10f);
                        int life = Main.rand.Next(40, 60);
                        ParticleRegistry.SpawnCloudParticle(pos, vel, Color.Cyan, Color.DarkSlateBlue, life, Main.rand.NextFloat(30f, 60f), .8f, 1);
                        Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(30f, 30f), DustID.SilverCoin, vel, 0, default, Main.rand.NextFloat(1.2f, 1.9f));
                    }
                }
            }
        }

        if (Time == 0f)
        {
            SavedPos = Projectile.Center - Vector2.UnitY * Projectile.height / 2;
            Projectile.Center = SavedPos + Vector2.UnitY * (Projectile.height - 10);

            Projectile.netUpdate = true;
        }
        else if (Time == GoRise)
        {
            AdditionsSound.ColdHitMassive.Play(Projectile.Center, .9f, .1f, .1f);
            for (int i = 0; i < 40; i++)
            {
                Vector2 pos = Projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-Projectile.width / 2, Projectile.width / 2) - Vector2.UnitY * 8f;
                Vector2 vel = -pos.SafeDirectionTo(Projectile.Center) * Main.rand.NextFloat(6f, 11f);
                ParticleRegistry.SpawnDustParticle(pos, vel, Main.rand.Next(30, 60), Main.rand.NextFloat(.5f, 1.2f), Color.Cyan, .1f, true, true);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(30, 60), Main.rand.NextFloat(.5f, .9f), Color.DeepSkyBlue, Color.LightSkyBlue, null, 1.5f, 5, true);
            }

            Projectile.timeLeft = 300;
        }
        else if (Time > GoRise)
        {
            float interpol = Animators.MakePoly(8f).OutFunction(InverseLerp(GoRise, GoRise + 20f, Time));
            Projectile.Center = Vector2.Lerp(Projectile.Center, SavedPos, interpol);
        }

        Projectile.Opacity = Animators.MakePoly(2.5f).InFunction(InverseLerp(0f, 20f, Projectile.timeLeft));
        if (Projectile.Opacity <= 0f || HitCount == 3)
            Projectile.Kill();

        Time++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanHitNPC(NPC target)
    {
        return Time.BetweenNum(GoRise, GoRise + 20) ? null : false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Color col = Color.White * Projectile.Opacity;
        Vector2 scale = new Vector2(Animators.MakePoly(3f).OutFunction(InverseLerp(0f, 20f, Time)), 1f) * Scale;
        Color color = Projectile.GetAlpha(Color.White * InverseLerp(35f, 0f, Time)) with { A = 0 };

        ManagedShader shader = AssetRegistry.GetShader("RadialCrackingShader");
        shader.TrySetParameter("Completion", InverseLerp(0, 3, HitCount));

        Main.spriteBatch.EnterShaderRegion(BlendState.NonPremultiplied, shader.Effect);

        shader.Render();
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White * Projectile.Opacity, 0f, tex.Size() / 2, scale);

        Main.spriteBatch.ExitShaderRegion();

        for (int i = 0; i < 10; i++)
            Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, color, 0f, tex.Size() / 2, scale);

        return false;
    }
}
