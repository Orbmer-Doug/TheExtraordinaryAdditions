using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class TechnicBlitzripperProj : BaseIdleHoldoutProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TechnicBlitzripper);

    public ref float Timer => ref Projectile.ai[0];
    public ref float ShootDelay => ref Projectile.ai[1];
    public ref float SniperTimer => ref Projectile.ai[2];
    public ref float Recoil => ref Projectile.Additions().ExtraAI[0];
    public ref float Heat => ref Projectile.Additions().ExtraAI[1];

    public ref bool Overheating => ref Owner.GetModPlayer<RipperPlayer>().Overheating;
    public ref int OverheatTimer => ref Owner.GetModPlayer<RipperPlayer>().OverheatTimer;

    public static readonly int FireSniper = SecondsToFrames(5);
    public static readonly int OverheatTime = SecondsToFrames(3);
    public static readonly int MaxHeat = 60;

    public int Dir => Projectile.velocity.X.NonZeroSign();
    public Vector2 Tip => Projectile.Center + PolarVector(105f, Projectile.rotation) + PolarVector(3f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

    public override int AssociatedItemID => ModContent.ItemType<TechnicBlitzripper>();
    public override int IntendedProjectileType => ModContent.ProjectileType<TechnicBlitzripperProj>();

    public override void Defaults()
    {
        Projectile.width = 210;
        Projectile.height = 44;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse), .8f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());

        float rot = new Animators.PiecewiseCurve()
                .Add(0f, -.9f, .3f, Animators.MakePoly(7f).OutFunction)
                .Add(-.9f, 0f, 1f, Animators.MakePoly(3f).InOutFunction)
                .Evaluate(InverseLerp(0f, 34f, OverheatTimer));

        Projectile.rotation = Projectile.velocity.ToRotation() + (rot * Dir * Owner.gravDir);
        Projectile.Center = Center + PolarVector(36f - (Recoil * 4), Projectile.rotation) + PolarVector(10f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation + (.3f * Dir * Owner.gravDir));

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0f && Owner.HasAmmo(Item) && !Overheating && this.RunLocal())
        {
            Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
            Projectile.NewProj(Tip, Projectile.velocity.SafeNormalize(Vector2.Zero) * 12f, type, dmg, kb, Main.myPlayer);

            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnGlowParticle(Tip, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(6, 12), Main.rand.NextFloat(30f, 60f), Color.LightCyan, 1.4f);

            for (int i = 0; i < 8; i++)
            {
                ParticleRegistry.SpawnTechyHolosquareParticle(Tip, Projectile.velocity.RotatedByRandom(.54f) * Main.rand.NextFloat(7f, 14f), Main.rand.Next(50, 90),
                    Main.rand.NextFloat(.7f, 1.5f), Color.Cyan, Main.rand.NextFloat(.8f, 1.1f), Main.rand.NextFloat(1.3f, 1.8f));
                ParticleRegistry.SpawnBloomLineParticle(Tip, Projectile.velocity.RotatedByRandom(.6f) * Main.rand.NextFloat(12f, 22f),
                    Main.rand.Next(10, 12), Main.rand.NextFloat(.3f, .5f), Color.Cyan);
                ParticleRegistry.SpawnMistParticle(Tip, Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(7f, 10f),
                    Main.rand.NextFloat(.4f, .6f), Color.Cyan, Color.DarkCyan, Main.rand.NextFloat(50f, 180f));
            }

            AdditionsSound.banditShot1B.Play(Tip, .85f, 0f, .1f, 20, Name);

            Heat = MathHelper.Clamp(Heat + 1, 0f, MaxHeat);
            Recoil = 4f;
            ShootDelay = 4f;
            this.Sync();
        }

        if (this.RunLocal() && !Modded.SafeMouseLeft.Current)
            Heat = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Heat, -.11f, .04f), 0f, MaxHeat);
        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.25f, .095f), 0f, 40f);

        if (ShootDelay > 0f)
            ShootDelay--;

        if (Overheating)
        {
            OverheatTimer++;

            float comp = InverseLerp(0f, OverheatTime, OverheatTimer);

            float speed = MathHelper.Lerp(11f, 1f, comp);
            float scale = MathHelper.Lerp(.7f, .1f, comp);
            float opac = MathHelper.Lerp(1f, .5f, comp);
            Color col = MulticolorLerp(comp, Color.White, Color.Cyan, Color.DarkCyan);
            ParticleRegistry.SpawnHeavySmokeParticle(Tip, -Vector2.UnitY.RotatedByRandom(.3f) * speed, Main.rand.Next(20, 30), scale, col, opac);
            if (OverheatTimer >= OverheatTime)
            {
                OverheatTimer = 0;
                Overheating = false;
            }
        }

        if (this.RunLocal() && Modded.SafeMouseRight.Current && !Overheating && !Modded.SafeMouseLeft.Current)
        {
            if (SniperTimer >= FireSniper)
            {
                AdditionsSound.LargeSniperFire.Play(Tip, 1.3f, -.1f, 0f, 2, Name);
                Projectile.NewProj(Tip, Projectile.velocity * 20f, ModContent.ProjectileType<EtherealRipBlast>(), Projectile.damage * 70, Projectile.knockBack * 10f);

                for (int i = 0; i < 12; i++)
                    ParticleRegistry.SpawnGlowParticle(Tip, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(6, 12), Main.rand.NextFloat(40f, 60f), Color.LightCyan, 1.4f);

                for (int i = 0; i < 30; i++)
                {
                    ParticleRegistry.SpawnTechyHolosquareParticle(Tip, Projectile.velocity.RotatedByRandom(.4f).RotatedByRandom(.1f) * Main.rand.NextFloat(11f, 22f), Main.rand.Next(20, 30), Main.rand.NextFloat(1f, 2f), Color.Cyan);
                }

                ParticleRegistry.SpawnBlurParticle(Tip, 50, .8f, 200f);
                ScreenShakeSystem.New(new(.5f, .4f), Tip);

                Heat = 0;
                Recoil = 12f;
                Overheating = true;
                SniperTimer = 0f;
                this.Sync();
            }

            float comp = InverseLerp(0f, FireSniper, SniperTimer);
            float speed = MathHelper.Lerp(2f, 9f, comp);
            Vector2 vel = NextVector2EllipseEdge(speed / 4, speed, Projectile.rotation);
            float scale = MathHelper.Lerp(.3f, 1.1f, comp);
            ParticleRegistry.SpawnSquishyLightParticle(Tip, vel, Main.rand.Next(20, 30), scale, Color.Cyan);

            SniperTimer++;
        }
        else if (this.RunLocal() && !Modded.SafeMouseRight.Current || Modded.SafeMouseLeft.Current)
        {
            if (SniperTimer > 0f)
                SniperTimer--;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        SpriteEffects effects = FixedDirection();
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0f);

        float overheatComp = InverseLerp(0f, OverheatTime, OverheatTimer);
        float comp = Overheating ? Animators.MakePoly(2f).InFunction.Evaluate(1.2f, 0f, overheatComp) : MathHelper.Lerp(0f, .7f, InverseLerp(0f, MaxHeat, Heat));
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.TechnicBlitzripperHeat);
        for (int i = 0; i < 8; i++)
        {
            Vector2 off = (MathHelper.TwoPi * i / 8).ToRotationVector2() * 5f * comp;
            Main.spriteBatch.Draw(glow, drawPosition + off, null, Color.Cyan with { A = 0 } * comp * (Overheating ? .9f : .7f), rotation, glow.Size() / 2, Projectile.scale, effects, 0f);
        }

        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            float sniperComp = InverseLerp(0f, FireSniper, SniperTimer);

            for (float i = .9f; i <= 1.3f; i += .1f)
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Tip, new(MathHelper.Lerp(0f, 30f * i, sniperComp), MathHelper.Lerp(0f, 120f * i, sniperComp))),
                    null, Color.DarkCyan.Lerp(Color.Cyan, sniperComp), Projectile.rotation, tex.Size() / 2);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverPlayers, BlendState.Additive);

        DrawScope();

        return false;
    }

    public void DrawScope()
    {
        Texture2D texture = AssetRegistry.InvisTex;

        const float sightsSize = 300f;
        float sightsResolution = 2f;
        Color color = Color.Cyan;

        Vector2 top = Projectile.Center + PolarVector(-12f, Projectile.rotation) + PolarVector(13f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

        ManagedShader scope = ShaderRegistry.PixelatedSightLine;
        scope.TrySetParameter("noiseOffset", Main.GameUpdateCount * -0.003f);
        scope.TrySetParameter("mainOpacity", 1f);
        scope.TrySetParameter("resolution", new Vector2(sightsResolution * sightsSize));
        scope.TrySetParameter("rotation", -Projectile.rotation);
        float sine = Sin01(Main.GlobalTimeWrappedHourly * 2f) * .005f;
        scope.TrySetParameter("width", 0.0025f + sine);
        scope.TrySetParameter("lightStrength", 3f);
        scope.TrySetParameter("color", color.ToVector3());
        scope.TrySetParameter("darkerColor", Color.Black.ToVector3());
        scope.TrySetParameter("bloomSize", 0.29f - sine);
        scope.TrySetParameter("bloomMaxOpacity", 0.4f);
        scope.TrySetParameter("bloomFadeStrength", 7f);

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, scope.Effect);

        Main.EntitySpriteDraw(texture, top - Main.screenPosition, null, Color.White, 0f, texture.Size() * .5f, sightsSize, 0, 0f);

        Main.spriteBatch.ExitShaderRegion();
    }
}

public class RipperPlayer : ModPlayer
{
    public bool Overheating;
    public int OverheatTimer;
}