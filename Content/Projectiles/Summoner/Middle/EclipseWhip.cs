using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class EclipseWhip : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LunarWhip);
    public override int TipSize => 9;
    public override int SegmentSkip => 3;

    public bool Moon
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public override void Defaults()
    {
        Projectile.Size = new(470, 245);
    }

    public enum MoonType
    {
        Normal,
        Yellow,
        Ringed,
        Mythril,
        BrightBlue,
        Green,
        Pink,
        Orange,
        Purple,
    }

    public static int Phase => (int)MathHelper.Clamp(Main.moonType, 0, 8);

    public Color[] MoonColors =
        [
        new Color(148, 143, 132),
        new Color(199, 159, 95),
        new Color(157, 197, 143),
        new Color(133, 193, 179),
        new Color(231, 254, 255),
        new Color(130, 170, 65),
        new Color(255, 188, 228),
        new Color(231, 181, 40),
        new Color(140, 94, 167)
        ];

    public static readonly Color[] EclipsePalette =
    [
        new(255, 227, 79),
        new(255, 152, 53),
        new(239, 111, 38),
        new(239, 111, 38),
    ];

    public static readonly Color[] StarPalette =
    [
        new(136, 146, 141),
        new(78, 110, 106),
        new(25, 151, 173),
        new(56, 165, 171),
    ];

    public override void SafeAI()
    {
        if (Completion.BetweenNum(.2f, .8f))
        {
            Vector2 vel = OutwardVel.RotatedBy(MathHelper.PiOver2 * Owner.direction * Owner.gravDir).RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 7f);
            if (!Moon)
            {
                Color col = EclipsePalette[Main.rand.Next(EclipsePalette.Length)];
                ParticleRegistry.SpawnDustParticle(Tip, vel, Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .7f), col, .1f, false, true, false, false);
            }
            else
            {
                Color col = MoonColors[Phase];
                ParticleRegistry.SpawnBloomPixelParticle(Tip, vel * .9f, Main.rand.Next(30, 50), Main.rand.NextFloat(.5f, .8f), col, Color.White, null, 1.2f, 4);
            }
        }
    }

    public override void CrackEffects()
    {
        if (!Moon)
        {
            for (int i = 0; i < 15; i++)
            {
                Color col = EclipsePalette[i % EclipsePalette.Length];
                ParticleRegistry.SpawnSquishyPixelParticle(Tip, Main.rand.NextVector2Circular(10f, 10f), Main.rand.Next(60, 90), Main.rand.NextFloat(1f, 1.6f), col, col * 1.8f, 5);
                ParticleRegistry.SpawnHeavySmokeParticle(Tip, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(40, 50), Main.rand.NextFloat(.5f, .7f), col, 1.4f);
            }
            if (this.RunLocal())
                Projectile.CreateFriendlyExplosion(Tip, new(70), Projectile.damage / 2, Projectile.knockBack, 10, 20);
            SoundID.DD2_BetsyFireballImpact.Play(Tip, 1f, .1f);
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                Color col = StarPalette[i % StarPalette.Length];
                ParticleRegistry.SpawnSparkleParticle(Tip, Main.rand.NextVector2Circular(7f, 7f), Main.rand.Next(50, 80), Main.rand.NextFloat(.5f, .9f), col, col * 1.8f, 1.4f);
            }
            SoundID.Item153.Play(Tip, 1f, .14f);
        }
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        if (!Moon)
        {
            for (int i = 0; i < 25; i++)
            {
                Color col = MulticolorLerp(Main.rand.NextFloat(), EclipsePalette);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(5f, 8f),
                    Main.rand.Next(30, 50), Main.rand.NextFloat(.5f, .8f), col);
            }
        }
        else
        {
            for (int i = 0; i < 20; i++)
            {
                ParticleRegistry.SpawnGlowParticle(pos + Main.rand.NextVector2Circular(8, 8), vel.RotatedByRandom(.6f) * Main.rand.NextFloat(2f, 6f),
                    Main.rand.Next(40, 90), Main.rand.NextFloat(20f, 40f), MoonColors[Phase], Main.rand.NextFloat(.8f, 1.5f));
            }
        }

        target.AddBuff(ModContent.BuffType<Eclipsed>(), 180);
        Projectile.damage = (int)(Projectile.damage * .8f);
    }

    public override void ModifyNPCEffects(NPC target, ref NPC.HitModifiers modifiers, in Vector2 pos, in int index)
    {
        if (index > (WhipPoints.Count - 4) && Completion.BetweenNum(.45f, .55f) && Moon)
        {
            modifiers.Knockback += 3f;
            modifiers.SetCrit();
        }
    }

    public override Color LineColor(SystemVector2 completion, Vector2 position)
    {
        if (!Moon)
        {
            return MulticolorLerp(completion.X + Main.GlobalTimeWrappedHourly, EclipsePalette);
        }
        else
        {
            return MoonColors[Phase];
        }
    }

    public override float LineWidth(float completion)
    {
        if (!Moon)
        {
            return MultiLerp(completion, TipSize, 6, 8, 12, TipSize, 0);
        }
        else
        {
            return base.LineWidth(completion);
        }
    }

    public override void DrawLine()
    {
        if (!Moon)
        {
            if (Line != null && !Line.Disposed)
            {
                ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkTurbulentNoise), 1, SamplerState.LinearWrap);
                Line.DrawTrail(shader, WhipPoints.Points);
            }
        }
        else
        {
            base.DrawLine();
        }
    }

    public override void DrawSegments()
    {
        if (!Moon)
        {
            void glow()
            {
                Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                Vector2 orig = tex.Size() / 2;
                float completion = Convert01To010(GetCompletion());
                for (int i = 0; i < EclipsePalette.Length; i++)
                {
                    Rectangle targ = ToTarget(Tip, new Vector2(30 + (i * 10)) * completion);
                    Main.spriteBatch.DrawBetterRect(tex, targ, null, EclipsePalette[i] * completion, 0f, orig);
                }
            }
            PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.OverProjectiles, BlendState.Additive);
        }
        else
        {
            Texture2D tex = Projectile.ThisProjectileTexture();

            Rectangle hiltFrame = GetFrameRectangle(new Point(10, 18), Phase, 0);
            Rectangle seg1Frame = GetFrameRectangle(new Point(10, 16), Phase, 18);
            Rectangle seg2Frame = GetFrameRectangle(new Point(10, 16), Phase, 34);
            Rectangle seg3Frame = GetFrameRectangle(new Point(10, 22), Phase, 50);
            Rectangle tipFrame = GetFrameRectangle(new Point(10, 10), Phase, 72);

            int len = WhipPoints.Points.Length - 1;
            for (int i = 0; i < len; i++)
            {
                Vector2 pos = WhipPoints.Points[i];
                Vector2 next = WhipPoints.Points[i + 1];

                Rectangle frame;
                bool hilt = i == 0;
                bool tip = i == len - 1;
                bool shouldDraw = i % SegmentSkip == (SegmentSkip - 1);
                if (hilt || tip)
                    shouldDraw = true;

                if (hilt)
                    frame = hiltFrame;
                else if (i < (len / 3))
                    frame = seg1Frame;
                else if (i < (len / 2))
                    frame = seg2Frame;
                else if (i < (len - 1))
                    frame = seg3Frame;
                else
                    frame = tipFrame;

                if (shouldDraw)
                {
                    Vector3 light = Lighting.GetSubLight(pos);
                    Color color = Projectile.GetAlpha(new(light.X, light.Y, light.Z));
                    float rotation = (next - pos).ToRotation() - MathHelper.PiOver2;
                    Vector2 orig = frame.Size() / 2;
                    SpriteEffects flip = Owner.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                    Main.spriteBatch.DrawBetter(tex, pos, frame, color, rotation, orig, tip ? Projectile.scale : 1f, flip);
                }
            }
        }
    }
}