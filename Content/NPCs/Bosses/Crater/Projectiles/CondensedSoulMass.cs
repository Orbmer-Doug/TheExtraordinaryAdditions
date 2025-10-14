using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class CondensedSoulMass : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;

    public const int AbsorbTime = 50;
    public ref float Time => ref Projectile.ai[0];
    public bool Touched
    {
        get => Projectile.ai[1] == 1;
        set => Projectile.ai[1] = value.ToInt();
    }
    public int PlayerIndex
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 300;
        Projectile.scale = 1f;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90000;
        Projectile.tileCollide = false;
    }

    public LoopedSoundInstance Holy;
    public override void SafeAI()
    {
        Holy ??= LoopedSoundManager.CreateNew(new(AdditionsSound.holyLoop, () => 1.2f * Projectile.scale), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        Holy?.Update(Projectile.Center);

        Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 3f * Projectile.scale);

        if (!Touched)
        {
            Projectile.scale = Owner.Opacity;
            Vector2 pos = new(270f, 300f + MathF.Sin(Main.GlobalTimeWrappedHourly * .2f) * 20f);
            Projectile.Center = Owner.Center - pos;

            foreach (Player player in Main.ActivePlayers)
            {
                if (player.DeadOrGhost)
                    continue;

                if (player.Center.WithinRange(Projectile.Center, 100f))
                {
                    AdditionsSound.BraveEnergy.Play(Projectile.Center, .9f);
                    PlayerIndex = player.whoAmI;
                    Touched = true;
                    this.Sync();
                }
            }
        }

        if (Touched)
        {
            Player player = Main.player?[PlayerIndex] ?? null;
            if (player != null)
                Projectile.velocity = Vector2.Lerp(Projectile.Center, player.Center, .2f) - Projectile.Center;

            Projectile.scale = Animators.MakePoly(2f).InFunction.Evaluate(Time, 0f, AbsorbTime, 1f, 0f);
            if (Time >= AbsorbTime)
            {
                AdditionsSound.BraveHeavyFireHit.Play(Projectile.Center, 1.2f);
                ScreenShakeSystem.New(new(2f, 1f), Projectile.Center);
                ParticleRegistry.SpawnBlurParticle(Projectile.Center, 40, .3f, 600f);
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 600f, Vector2.Zero, 50, Color.Gold, RandomRotation(), Color.DarkGoldenrod);
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 800f, Vector2.Zero, 40, Color.Gold * .6f, RandomRotation(), Color.DarkGoldenrod * .6f);

                for (int i = 0; i < 50; i++)
                {
                    ParticleRegistry.SpawnGlowParticle(Projectile.Center, Vector2.Zero, 20, 130f, Color.LightGoldenrodYellow, 1.4f);
                    ParticleRegistry.SpawnBloomLineParticle(Projectile.Center, Main.rand.NextVector2Circular(10f, 10f) + Main.rand.NextVector2Circular(22f, 22f), Main.rand.Next(37, 52), Main.rand.NextFloat(.6f, .8f), Color.Goldenrod);
                    ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center,
                        Main.rand.NextVector2Circular(40f, 40f), Main.rand.Next(130, 150), Main.rand.NextFloat(1.5f, 5.1f), Color.Gold, Color.LightGoldenrodYellow, 4, false, false, Main.rand.NextFloat(-.1f, .1f));
                }

                for (int i = -1; i <= 1; i += 2)
                {
                    for (int a = -1; a <= 2; a += 2)
                    {
                        for (int o = 0; o < 45; o++)
                        {
                            Vector2 pos = Projectile.Center;
                            float speed = Utils.Remap(o, 0f, 15f, 1f, 50f);
                            Vector2 horiz = (i == -1 ? -Vector2.UnitX * speed : Vector2.UnitX * speed).RotatedBy(Projectile.rotation);
                            Vector2 vert = (a == -1 ? -Vector2.UnitY * speed : Vector2.UnitY * speed).RotatedBy(Projectile.rotation);
                            float size = 160f * (1f - InverseLerp(1f, 12f, speed));
                            int life = 40;
                            Color col = Color.Lerp(Color.Gold, Color.DarkGoldenrod, .5f);
                            ParticleRegistry.SpawnGlowParticle(pos, vert, life, size, col);
                            ParticleRegistry.SpawnGlowParticle(pos, horiz, life, size, col);
                        }
                    }
                }

                ModOwner.FightStarted = true;
                ModOwner.Sync();
                Projectile.Kill();
            }
            Time++;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 size = Projectile.Size;
        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D t = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
        Main.spriteBatch.DrawBetterRect(t, ToTarget(Projectile.Center, size * 1.5f * Projectile.scale), null, Color.Gold, 0f, t.Size() / 2, 0);
        Main.spriteBatch.DrawBetterRect(t, ToTarget(Projectile.Center, size * 2.5f * Projectile.scale), null, Color.Gold, 0f, t.Size() / 2, 0);
        Main.spriteBatch.ResetBlendState();

        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, size), null, Color.White * Projectile.scale, 0f, tex.Size() / 2, 0);
        }
        ManagedShader shader = AssetRegistry.GetShader("SoulMass");
        shader.TrySetParameter("time", TimeSystem.RenderTime * .5f);
        shader.TrySetParameter("scale", Projectile.scale);
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverPlayers, null, shader);
        return false;
    }
}