using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class PyroclasticHover : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PyroclasticVeil);
    public override int AssociatedItemID => ModContent.ItemType<PyroclasticVeil>();
    public override int IntendedProjectileType => ModContent.ProjectileType<PyroclasticHover>();

    public static readonly int AppearTime = SecondsToFrames(.8f);
    public static readonly int FadeTime = SecondsToFrames(.4f);
    public static readonly int CircleSize = 100;
    public ref float Time => ref Projectile.ai[0];
    public ref float AppearTimer => ref Projectile.ai[1];
    public ref float FadeTimer => ref Projectile.ai[2];
    public float AppearCompletion => InverseLerp(0f, AppearTime, AppearTimer);
    public Vector2 ArtifactCenter;
    public override bool? CanDamage() => false;
    public override void WriteExtraAI(BinaryWriter writer) => writer.WriteVector2(ArtifactCenter);
    public override void GetExtraAI(BinaryReader reader) => ArtifactCenter = reader.ReadVector2();
    public override void Defaults()
    {
        Projectile.width = Projectile.height = CircleSize;
        Projectile.DamageType = DamageClass.Magic;
    }

    public LoopedSoundInstance fire;

    public override void SafeAI()
    {
        if (Owner.statMana <= 0)
        {
            Owner.statMana = 0;
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = FadeTime;
        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (this.RunLocal())
        {
            Projectile.velocity = center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        }
        Owner.ChangeDir(Projectile.spriteDirection);

        int height = 116;
        if (this.RunLocal() && Modded.SafeMouseLeft.Current)
        {
            if (AppearTimer <= AppearTime)
                AppearTimer++;
            this.Sync();
        }
        else
        {
            if (AppearTimer > 0f)
            {
                AppearTimer--;
            }
            else
            {
                Projectile.rotation = Projectile.rotation.SmoothAngleLerp(-MathHelper.PiOver2, .4f, .5f);
            }
        }

        if (AppearTimer > 0)
        {
            Projectile.rotation = Projectile.rotation.SmoothAngleLerp(Projectile.velocity.ToRotation(), .4f, .5f);
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        }

        if (AppearCompletion >= 1f)
        {
            // Release flames outward.
            MetaballRegistry.SpawnLavaMetaball(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.2f) * 14f, SecondsToFrames(7), 120, Owner.whoAmI, Projectile.damage);
            if (Time % 2 == 1)
                Item.CheckManaBetter(Owner, 1, true);
        }

        fire ??= LoopedSoundManager.CreateNew(new AdditionsLoopedSound(AdditionsSound.FireBreathe4, () => .6f, () => .2f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => AppearCompletion >= 1f);
        fire.Update(Projectile.Center);

        float dist = Animators.MakePoly(4f).InOutFunction.Evaluate(CircleSize, CircleSize * 2.2f, AppearCompletion);
        Projectile.Center = Vector2.SmoothStep(Projectile.Center, Owner.Center + PolarVector(dist, Projectile.rotation), MathHelper.Lerp(.3f, .5f, AppearCompletion));
        Projectile.width = (int)Animators.MakePoly(3f).OutFunction.Evaluate(CircleSize, CircleSize / 3, AppearCompletion * 2f);
        ArtifactCenter = center + PolarVector(height / 2, Projectile.rotation);
        Projectile.Opacity = InverseLerp(0f, 20f, Time);

        Time++;
    }

    public override void DieEffect()
    {
        AppearTimer = MathHelper.Lerp(AppearTimer, 0f, .4f);
        FadeTimer++;
        Projectile.Opacity = InverseLerp(FadeTime, 0f, FadeTimer);

        if (FadeTimer >= FadeTime)
        {
            Projectile.Kill();
            return;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D tex = Projectile.ThisProjectileTexture();
            Vector2 orig = tex.Size() / 2;

            if (AppearCompletion < 1f)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver4;
                ManagedShader shader = ShaderRegistry.AppearShader;
                shader.TrySetParameter("completion", 1f - AppearCompletion);
                shader.TrySetParameter("dir", new Vector2(-.707f, .707f));
                Main.spriteBatch.EnterShaderRegion(null, shader.Effect);
                Main.spriteBatch.DrawBetter(tex, ArtifactCenter, null, Color.White * Projectile.Opacity, rotation, orig, Projectile.scale);
                Main.spriteBatch.ExitShaderRegion();
            }
            else
                Main.spriteBatch.DrawBetter(tex, ArtifactCenter, null, Color.White * Projectile.Opacity, Projectile.rotation + MathHelper.PiOver4, orig, Projectile.scale);

            float rot = Main.GameUpdateCount / 40f;
            ManagedShader effect = ShaderRegistry.MagicRing;
            effect.TrySetParameter("firstCol", Color.OrangeRed.ToVector3() * Projectile.Opacity);
            effect.TrySetParameter("secondCol", Color.Chocolate.ToVector3() * Projectile.Opacity);
            effect.TrySetParameter("opacity", MathHelper.Lerp(2f, 1.2f, AppearCompletion));

            effect.TrySetParameter("time", -rot * 2f);
            effect.TrySetParameter("cosine", (float)Math.Cos(-rot * 2f));

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, effect.Effect);

            Texture2D magic = AssetRegistry.GetTexture(AdditionsTexture.EpidemicCircle);
            Main.spriteBatch.DrawBetterRect(magic, ToTarget(Projectile.Center - PolarVector(Animators.MakePoly(3f).OutFunction.Evaluate(0f, 30f, AppearCompletion), Projectile.rotation),
                Projectile.width * 3, Projectile.height * 3), null, Color.White, Projectile.rotation, magic.Size() / 2);

            Main.spriteBatch.ExitShaderRegion();

            effect.TrySetParameter("time", rot);
            effect.TrySetParameter("cosine", (float)Math.Cos(rot));

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, effect.Effect);

            magic = AssetRegistry.GetTexture(AdditionsTexture.ArmageddonCircle);
            Main.spriteBatch.DrawBetterRect(magic, ToTarget(Projectile.Center, Projectile.width, Projectile.height), null, Color.White, Projectile.rotation, magic.Size() / 2);

            Main.spriteBatch.ExitShaderRegion();
        }
        LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.UnderPlayers);
        return false;
    }
}