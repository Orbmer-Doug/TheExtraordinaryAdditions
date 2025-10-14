using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class AcheronHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Acheron);
    public override int AssociatedItemID => ModContent.ItemType<Acheron>();
    public override int IntendedProjectileType => ModContent.ProjectileType<AcheronHeld>();

    public override void Defaults()
    {
        Projectile.width = 186;
        Projectile.height = 30;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override bool? CanDamage() => false;

    public Vector2 SummonPos;
    public override void WriteExtraAI(BinaryWriter writer) => writer.WriteVector2(SummonPos);
    public override void GetExtraAI(BinaryReader reader) => SummonPos = reader.ReadVector2();

    public const int TeleWidth = 84;
    public int Wait
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float Time => ref Projectile.ai[1];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public Vector2 Eye => Projectile.Center + PolarVector(70, Projectile.rotation);
    public float Flash => MathHelper.Lerp(1f, 5f, InverseLerp(0f, Owner.itemAnimationMax, Wait));
    public override void SafeAI()
    {
        if (telegraph == null || telegraph.Disposed)
            telegraph = new(WidthFunct, ColorFunct, null, 50);

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);

        if (this.RunLocal())
        {
            SummonPos = new Vector2(MathHelper.Clamp(Main.MouseWorld.X, Owner.Center.X - Main.screenWidth / 2 + TeleWidth / 2,
                Owner.Center.X + Main.screenWidth / 2 - TeleWidth / 2), Owner.Center.Y + Main.screenHeight / 2);
            this.Sync();
        }

        Projectile.velocity = center.SafeDirectionTo(SummonPos);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = center + PolarVector(50f, Projectile.rotation);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Lighting.AddLight(Eye, Color.Violet.ToVector3() * .7f);

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait == 0 && HasMana())
        {
            Vector2 pos = SummonPos + new Vector2(Main.rand.NextFloat(-TeleWidth, TeleWidth) / 2, -Main.rand.NextFloat(20f, 80f));
            Projectile.NewProj(pos, Vector2.UnitY, ModContent.ProjectileType<HellishLance>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
            SoundID.Item71.Play(SummonPos, 1.1f, 0f, .1f);

            Wait = Owner.itemAnimationMax;
        }

        if (Wait > 0)
            Wait--;

        points.SetPoints((SummonPos + Vector2.UnitY * 40f).GetLaserControlPoints(SummonPos + Vector2.UnitY * -300f * Animators.MakePoly(3f).InOutFunction(InverseLerp(0f, 20f, Time)), 50));
        Time++;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        Color col = Color.Violet.Lerp(Color.DarkViolet, MathHelper.SmoothStep(1f, 0f, c.X)).Lerp(Color.White, Convert01To010(c.Y));
        float opacity = MathHelper.SmoothStep(1f, 0f, c.X) * GetLerpBump(0f, .1f, 1f, .9f, c.X) * .7f;
        return col * opacity * Flash;
    }

    public static float WidthFunct(float c) => TeleWidth;

    public OptimizedPrimitiveTrail telegraph;
    public TrailPoints points = new(50);
    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(lightColor);

        if (this.RunLocal())
        {
            void tele()
            {
                if (telegraph != null)
                {
                    ManagedShader shader = ShaderRegistry.EnlightenedBeam;
                    shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * .4f);
                    shader.TrySetParameter("repeats", 0f);
                    shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoise), 1);
                    shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkTurbulentNoise), 2);

                    telegraph.DrawTrail(shader, points.Points);
                }
            }
            PixelationSystem.QueuePrimitiveRenderAction(tele, PixelationLayer.UnderProjectiles);
        }

        void glow()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Vector2 orig = tex.Size() / 2;
            for (float i = .4f; i <= .6f; i += .1f)
            {
                float bright = i + (Flash * .05f) * .7f;
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Eye, new Vector2(80) * bright), null, Color.Violet * bright, 0f, orig);
            }
        }
        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.Dusts, BlendState.Additive);
        return false;
    }
}
