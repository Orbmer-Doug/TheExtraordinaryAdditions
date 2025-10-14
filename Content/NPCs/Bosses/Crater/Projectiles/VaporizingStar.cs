using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class VaporizingStar : ProjOwnedByNPC<Asterlin>
{
    public enum StateType
    {
        Grow,
        Fire,
    }

    public ref float Timer => ref Projectile.ai[0];

    public StateType CurrentState
    {
        get => (StateType)Projectile.ai[1];
        set => Projectile.ai[1] = (float)value;
    }

    public int TypeOf
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public int CurrentShots
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = value;
    }

    public static readonly float GrowTime = SecondsToFrames(1.4f);

    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 200;
        Projectile.hostile = true;
        Projectile.Opacity = 0f;
        Projectile.scale = 0f;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 7000;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public static float TelegraphMaxAngularVelocity => MathHelper.ToRadians(2.1f);
    public static readonly int LaserWait = DisintegrationBeam.TelegraphTime + DisintegrationBeam.BeamTime + (DisintegrationBeam.FadeTime / 2);
    public override void SafeAI()
    {
        Vector2 targetPos = Target.Center + new Vector2(350f * TypeOf, -400f);
        float totalTime = GrowTime + LaserWait * Asterlin.Disintegration_TotalShots;

        switch (CurrentState)
        {
            case StateType.Grow:
                Projectile.velocity = Vector2.Lerp(Projectile.Center, targetPos, .1f) - Projectile.Center;

                Projectile.Opacity = Animators.MakePoly(3f).InFunction(InverseLerp(0f, GrowTime * .75f, Timer));
                Projectile.scale = Animators.Sine.InOutFunction(InverseLerp(0f, GrowTime, Timer));
                if (Timer >= GrowTime)
                {
                    CurrentState = StateType.Fire;
                    Timer = 0;
                    return;
                }
                break;
            case StateType.Fire:
                Projectile.velocity = Vector2.Lerp(Projectile.Center, targetPos, .024f) - Projectile.Center;

                if (CurrentShots >= Asterlin.Disintegration_TotalShots)
                {
                    if (!AnyProjectile(ModContent.ProjectileType<DisintegrationBeam>()))
                        Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.05f, 0f, 1f);
                    if (Projectile.Opacity <= 0f)
                        Projectile.Kill();
                }
                else
                {
                    if (Timer % LaserWait == LaserWait - 1)
                    {
                        AdditionsSound.spearLaser.Play(Projectile.Center);
                        if (this.RunServer())
                        {
                            for (int i = 0; i < Asterlin.Disintegration_TotalBeams; i++)
                            {
                                float angularVelocity = Main.rand.NextFloat(0.65f, 1f) * Main.rand.NextFromList(-1f, 1f) * TelegraphMaxAngularVelocity;
                                Vector2 laserDirection = (MathHelper.TwoPi * i / Asterlin.Disintegration_TotalBeams + Main.rand.NextFloatDirection() * 0.16f).ToRotationVector2();

                                int index = SpawnProjectile(Projectile.Center, laserDirection,
                                    ModContent.ProjectileType<DisintegrationBeam>(), Asterlin.HeavyAttackDamage, 0f, ai0: 0f, ai1: Projectile.whoAmI, ai2: angularVelocity);
                                Main.projectile[index].netUpdate = true;
                            }
                        }
                        CurrentShots++;
                        ModOwner.Disintegration_CurrentShot = CurrentShots;
                        Owner.netUpdate = true;
                        Projectile.netUpdate = true;
                    }
                }
                break;
        }

        Timer++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.FlameMap1);
        ManagedShader fireball = ShaderRegistry.FireballShader;
        fireball.SetTexture(noise, 1, SamplerState.AnisotropicWrap);
        fireball.TrySetParameter("mainColor", Color.Lerp(Color.Goldenrod, Color.Gold, 0.3f).ToVector3());
        fireball.TrySetParameter("resolution", new Vector2(Projectile.scale * 400f));
        fireball.TrySetParameter("time", Main.GlobalTimeWrappedHourly * (0.04f + 0.32f));
        fireball.TrySetParameter("opacity", Projectile.Opacity);

        Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, fireball.Effect);
        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        Texture2D invis = AssetRegistry.GetTexture(AdditionsTexture.Invisible);
        fireball.Render();
        Main.spriteBatch.Draw(invis, drawPos, null, Color.White * Projectile.Opacity, 0f, invis.Size() * 0.5f, Projectile.scale * 400f, SpriteEffects.None, 0f);
        Main.spriteBatch.ExitShaderRegion();
        return false;
    }
}