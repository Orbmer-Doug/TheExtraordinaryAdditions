using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_TechnicBombBarrage()
    {
        StateMachine.RegisterTransition(AsterlinAIType.TechnicBombBarrage, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Judgement, 1f }, { AsterlinAIType.Hyperbeam, 1f } }, false, () =>
        {
            return AITimer >= TechnicBombBarrage_TotalTime;
        });
        StateMachine.RegisterStateEntryCallback(AsterlinAIType.TechnicBombBarrage, () =>
        {
            NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<TheTechnicBlitzripper>(), Asterlin.MediumAttackDamage, 0f);
            ReticlePosition = Target.Center - Vector2.UnitY * 200f;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.TechnicBombBarrage, DoBehavior_TechnicBombBarrage);
    }

    public static int TechnicBombBarrage_FireTime => SecondsToFrames(15f);
    public static int TechnicBombBarrage_WaitTime => SecondsToFrames(1f);
    public static int TechnicBombBarrage_TotalTime => TechnicBombBarrage_FireTime + TechnicBombBarrage_WaitTime;
    public static int TechnicBombBarrage_BombReleaseRate => DifficultyBasedValue(30, 28, 27, 25, 24, 21);
    public int TechnicBombBarrage_FadeTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }
    public ref float TechnicBombBarrage_RotationStart => ref NPC.AdditionsInfo().ExtraAI[1];
    public ref float TechnicBombBarrage_RotationDir => ref NPC.AdditionsInfo().ExtraAI[2];

    public Vector2 ReticlePosition;
    public Projectile CurrentBombTarget;

    public void DoBehavior_TechnicBombBarrage()
    {
        if (AITimer <= 1)
        {
            if (this.RunServer())
            {
                TechnicBombBarrage_RotationStart = RandomRotation();
                TechnicBombBarrage_RotationDir = Main.rand.NextFromList(1, -1);
                NPC.netUpdate = true;
            }
        }

        if (AITimer < TechnicBombBarrage_FireTime)
        {
            Vector2 target = Target.Center + (Target.Velocity * 5f) + PolarVector(MathHelper.Lerp(400f, 500f, Sin01(AITimer * .1f)), TechnicBombBarrage_RotationStart + (AITimer * .01f * TechnicBombBarrage_RotationDir));
            NPC.velocity = Vector2.SmoothStep(NPC.Center, target, .1f) - NPC.Center;

            if (AITimer < 30f)
            {
                ReticlePosition = Vector2.SmoothStep(ReticlePosition, NPC.Center - Vector2.UnitY * 200f, .7f);
            }
            else
            {
                if (CurrentBombTarget == null || CurrentBombTarget.active == false)
                {
                    CurrentBombTarget = ProjectileTargeting.GetClosestProjectile(new(Target.Center, 5000, true, ModContent.ProjectileType<TechnicBomb>())) ?? null;
                }

                if (Gun != null)
                {
                    if (CurrentBombTarget != null && CurrentBombTarget.active == true)
                    {
                        if (!Collision.CanHitLine(Gun.Tip, 1, 1, CurrentBombTarget.Center, 1, 1))
                            CurrentBombTarget = null;

                        ReticlePosition = Vector2.SmoothStep(ReticlePosition, CurrentBombTarget.Center, Utils.Remap(ReticlePosition.Distance(CurrentBombTarget.Center), 0f, 400f, .24f, .14f));
                        if (ReticlePosition.WithinRange(CurrentBombTarget.Center, 100f))
                            Gun?.Shoot();
                    }

                    // If the reticle gets too close everything freaks out
                    ReticlePosition = ReticlePosition.ClampOutCircle(Gun.Projectile.Center, 100f);

                    SetLeftHandTarget(leftArm.RootPosition + PolarVector(400f, Utils.AngleLerp(MathHelper.PiOver2 + (.1f * Direction), -MathHelper.PiOver2, Animators.MakePoly(3f).OutFunction(InverseLerp(0f, 40f, AITimer)))));
                    SetRightHandTarget(rightArm.RootPosition + Gun.Projectile.velocity.SafeNormalize(Vector2.Zero) * 400f);
                }

                if (AITimer % TechnicBombBarrage_BombReleaseRate == (TechnicBombBarrage_BombReleaseRate - 1))
                {
                    Vector2 home = Utility.GetHomingVelocity(LeftHandPosition, Target.Center, Target.Velocity, Main.rand.NextFloat(22f, 34f));
                    if (this.RunServer())
                        NPC.NewNPCProj(LeftHandPosition, home, ModContent.ProjectileType<TechnicBomb>(), Asterlin.MediumAttackDamage, 0f);

                    for (int i = 0; i < 18; i++)
                    {
                        ParticleRegistry.SpawnTechyHolosquareParticle(LeftHandPosition, home.RotatedByRandom(.4f) * Main.rand.NextFloat(.1f, .8f), Main.rand.Next(40, 60), Main.rand.NextFloat(.8f, 1.3f), Color.DeepSkyBlue, .8f, 1.1f);
                        ParticleRegistry.SpawnTechyHolosquareParticle(LeftHandPosition, Main.rand.NextVector2Circular(3f, 3f) + Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(40, 60), Main.rand.NextFloat(1.2f, 1.6f), Color.DeepSkyBlue.Lerp(Color.Cyan, Main.rand.NextFloat(0f, .4f)), .8f, 1.1f);
                    }
                }
            }
        }
        else if (AITimer < TechnicBombBarrage_TotalTime)
        {
            NPC.velocity *= .97f;
            TechnicBombBarrage_FadeTimer++;
        }
    }

    public void TechnicBombBarrage_Draw()
    {
        void draw()
        {
            float size = 340f;
            Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), ToTarget(LeftHandPosition - new Vector2(size / 2), new Vector2(size)), Color.White);
        }
        float interpol = InverseLerp(0f, 30f, AITimer) * (1f - InverseLerp(0f, TechnicBombBarrage_WaitTime, TechnicBombBarrage_FadeTimer));
        ManagedShader shader = AssetRegistry.GetShader("RadialTelegraph");
        shader.TrySetParameter("direction", LeftHandPosition.AngleTo(Target.Center));
        shader.TrySetParameter("angle", MathHelper.PiOver4 * interpol);
        shader.TrySetParameter("color", Color.DeepSkyBlue.ToVector4() * interpol);
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverNPCs, BlendState.Additive, shader);
    }

    public void TechnicBombBarrage_DrawReticle()
    {
        float interpol = InverseLerp(0f, 30f, AITimer) * (1f - InverseLerp(0f, TechnicBombBarrage_WaitTime, TechnicBombBarrage_FadeTimer));

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D line = AssetRegistry.GetTexture(AdditionsTexture.DimTrail);
        Vector2 lineOrig = new(line.Width / 2f, 0f);
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                float rot = MathHelper.TwoPi * InverseLerp(0f, 4, i) + MathHelper.PiOver4;
                Vector2 offset = rot.ToRotationVector2() * MathHelper.Lerp(0f, 10f, Sin01(Main.GameUpdateCount * .1f));
                Main.spriteBatch.DrawBetterRect(line, ToTarget(ReticlePosition + offset, new Vector2(56, 208)), null, Color.Cyan * .8f * interpol, rot - MathHelper.PiOver2, lineOrig);
                Main.spriteBatch.DrawBetterRect(line, ToTarget(ReticlePosition + offset, new Vector2(56, 208) / 2), null, Color.LightCyan * interpol, rot - MathHelper.PiOver2, lineOrig);
            }
        }
        Main.spriteBatch.ResetBlendState();

        ManagedShader shader = AssetRegistry.GetShader("ForcefieldLimited");
        shader.TrySetParameter("direction", 0f);
        shader.TrySetParameter("angle", MathHelper.TwoPi);
        shader.TrySetParameter("color", Color.Cyan.ToVector4() * interpol);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1, SamplerState.LinearWrap);
        float size = 100f * interpol;

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, shader.Effect);
        shader.Render();
        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), ToTarget(ReticlePosition - new Vector2(size / 2), new Vector2(size)), Color.White);
        Main.spriteBatch.ExitShaderRegion();
    }
}