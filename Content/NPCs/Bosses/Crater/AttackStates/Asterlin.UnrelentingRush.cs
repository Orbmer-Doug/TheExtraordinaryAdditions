using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_UnrelentingRush()
    {
        StateMachine.RegisterTransition(AsterlinAIType.UnrelentingRush, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.UnveilingZenith, 1f } }, false, () =>
        {
            return UnrelentingRush_WaitTimer >= UnrelentingRush_WaitTime;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.UnrelentingRush, DoBehavior_UnrelentingRush);
    }

    public static int UnrelentingRush_TotalDashes => DifficultyBasedValue(10, 13, 16, 18, 20, 24);
    public static int UnrelentingRush_SlowdownTime => DifficultyBasedValue(SecondsToFrames(.7f), SecondsToFrames(.6f), SecondsToFrames(.5f), SecondsToFrames(.46f), SecondsToFrames(.4f), SecondsToFrames(.36f));
    public static int UnrelentingRush_InitialFadeTime => SecondsToFrames(.7f);
    public static int UnrelentingRush_PortalFadeIn => DifficultyBasedValue(SecondsToFrames(.6f), SecondsToFrames(.44f), SecondsToFrames(.42f), SecondsToFrames(.39f), SecondsToFrames(.35f), SecondsToFrames(.3f));
    public static int UnrelentingRush_PortalFadeOut => SecondsToFrames(.9f);
    public static int UnrelentingRush_PortalLifetime => UnrelentingRush_PortalFadeIn + UnrelentingRush_PortalFadeOut + SecondsToFrames(2f);
    public static int UnrelentingRush_WaitTime => SecondsToFrames(1.2f);

    public enum UnrelentingRush_States
    {
        MakePortal,
        Dash,
        Slowdown,
    }
    public int UnrelentingRush_DashCounter
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }
    public UnrelentingRush_States UnrelentingRush_CurrentState
    {
        get => (UnrelentingRush_States)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = (int)value;
    }
    public ref float UnrelentingRush_SavedRotation => ref NPC.AdditionsInfo().ExtraAI[2];
    public int UnrelentingRush_DashTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[3];
        set => NPC.AdditionsInfo().ExtraAI[3] = value;
    }
    public int UnrelentingRush_WaitTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[4];
        set => NPC.AdditionsInfo().ExtraAI[4] = value;
    }

    public void DoBehavior_UnrelentingRush()
    {
        if (AITimer < UnrelentingRush_InitialFadeTime)
        {
            float interpolant = 1f - InverseLerp(0f, UnrelentingRush_InitialFadeTime, AITimer);
            NPC.Opacity = interpolant;
            SetZPosition(interpolant);
        }
        else
        {
            if (UnrelentingRush_DashCounter < UnrelentingRush_TotalDashes)
            {
                UnrelentingRush_DashTimer++;
                switch (UnrelentingRush_CurrentState)
                {
                    case UnrelentingRush_States.MakePortal:
                        float homeAccuracy = Main.getGoodWorld ? 220f : 110f;
                        Vector2 home = Utility.GetHomingVelocity(NPC.Center, Target.Position, Target.Velocity, homeAccuracy);

                        if (UnrelentingRush_DashTimer == 1)
                        {
                            NPC.velocity = Vector2.Zero;
                            Vector2 spawnPos = Target.Center - Target.Velocity.SafeNormalize(Main.rand.NextVector2Unit()).RotatedByRandom(.2f) * new Vector2(700f, 420f);
                            spawnPos = spawnPos.ClampInWorld();
                            NPC.Center = spawnPos;
                            if (Main.masterMode)
                                home = Utility.GetHomingVelocity(NPC.Center, Target.Position, Target.Velocity, homeAccuracy);

                            Vector2 dir = spawnPos.SafeDirectionTo(Target.Center);
                            if (!Main.masterMode && !Main.getGoodWorld)
                                UnrelentingRush_SavedRotation = dir.ToRotation();
                            NPC.NewNPCProj(spawnPos, Main.masterMode ? home : dir, ModContent.ProjectileType<TechnicPortal>(), 0, 0f);
                            NPC.netUpdate = true;
                        }

                        if (Main.masterMode)
                            UnrelentingRush_SavedRotation = home.ToRotation();

                        NPC.Opacity = 0f;
                        SetZPosition(0f);

                        if (UnrelentingRush_DashTimer >= UnrelentingRush_PortalFadeIn)
                        {
                            UnrelentingRush_DashTimer = 0;
                            UnrelentingRush_CurrentState = UnrelentingRush_States.Dash;
                            NPC.netUpdate = true;
                        }
                        break;
                    case UnrelentingRush_States.Dash:
                        NPC.velocity = UnrelentingRush_SavedRotation.ToRotationVector2() * 220f;
                        NPC.Opacity = 1f;
                        SetZPosition(1f);

                        // Smoke
                        for (int i = 0; i < 40; i++)
                        {
                            Vector2 vel = -NPC.velocity.RotatedByRandom(.5f) * Main.rand.NextFloat(.2f, .6f);
                            ParticleRegistry.SpawnHeavySmokeParticle(NPC.Center, vel, Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, .7f), Color.Cyan);
                        }

                        // Sides
                        for (int i = -1; i <= 1; i += 2)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                float comp = InverseLerp(0f, 20f, j);
                                Vector2 vel = -NPC.velocity.RotatedBy(.45f * i).SafeNormalize(Vector2.Zero) * (Main.rand.NextFloat(20f, 35f) * comp);
                                float scale = MathHelper.Lerp(1.9f, .1f, comp);
                                Color col = Color.LightCyan.Lerp(Color.DarkCyan, comp);
                                ParticleRegistry.SpawnSquishyLightParticle(NPC.Center, vel.RotatedByRandom(.1f), 40, scale, col);
                            }
                        }

                        // Shockwave
                        for (float i = .4f; i <= 1f; i += .1f)
                        {
                            Vector2 vel = -NPC.velocity.SafeNormalize(Vector2.Zero) * 60f * i;
                            int life = (int)(50 * i);
                            float endScale = Utils.Remap(i, .5f, 1f, 800f, 100f);
                            for (int j = 0; j < 2; j++)
                                ParticleRegistry.SpawnPulseRingParticle(NPC.Center, vel, life, NPC.velocity.ToRotation(), new(.5f, 1f), 0f, endScale, Color.Cyan, true);
                        }
                        ParticleRegistry.SpawnBlurParticle(NPC.Center, 30, .6f, 1400f);
                        ParticleRegistry.SpawnChromaticAberration(NPC.Center, 30, .5f, 1400f);
                        ScreenShakeSystem.New(new ScreenShake(1f, .8f, 2000f), NPC.Center);
                        AdditionsSound.IkeFinal.Play(NPC.Center, 2.2f, .3f, .1f);

                        int cnt = 8;
                        for (int i = 0; i < cnt; i++)
                        {
                            float comp = InverseLerp(0, cnt - 1, i);
                            float speed = MathHelper.Lerp(16f, 28f, Convert01To010(comp));
                            Vector2 vel = NPC.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.Lerp(-1.2f, 1.2f, comp)) * speed;
                            NPC.NewNPCProj(NPC.Center, vel, ModContent.ProjectileType<OverchargedLaser>(), LightAttackDamage, 0f, -1, 0f, ai1: 1f);
                        }

                        UnrelentingRush_DashCounter++;
                        UnrelentingRush_DashTimer = 0;
                        UnrelentingRush_CurrentState = UnrelentingRush_States.Slowdown;
                        NPC.netUpdate = true;
                        break;
                    case UnrelentingRush_States.Slowdown:
                        NPC.velocity *= .95f;

                        if (UnrelentingRush_DashTimer >= UnrelentingRush_SlowdownTime)
                        {
                            UnrelentingRush_DashTimer = 0;
                            UnrelentingRush_CurrentState = UnrelentingRush_States.MakePortal;
                            NPC.netUpdate = true;
                        }
                        break;
                }
                SetRightHandTarget(rightArm.RootPosition + PolarVector(400f, UnrelentingRush_SavedRotation));
                SetHeadRotation(UnrelentingRush_SavedRotation);
                SetBodyRotation(UnrelentingRush_SavedRotation + MathHelper.PiOver2);
                SetFlipped(false);

                FlameEngulfInterpolant = InverseLerp(10f, 200f, NPC.velocity.Length());
            }
            else
            {
                FlameEngulfInterpolant = 0f;
                NPC.velocity = Vector2.SmoothStep(NPC.Center, Target.Center + new Vector2(200f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), -90f), .1f) - NPC.Center;
                UnrelentingRush_WaitTimer++;
            }
        }
    }

    public void UnrelentingRush_DrawTelegraph()
    {
        if (!Main.masterMode || UnrelentingRush_CurrentState != UnrelentingRush_States.MakePortal)
            return;

        void draw()
        {
            float completion = InverseLerp(0f, UnrelentingRush_PortalFadeIn, UnrelentingRush_DashTimer);
            Texture2D cap = AssetRegistry.GetTexture(AdditionsTexture.BloomLineCap);
            Texture2D horiz = AssetRegistry.GetTexture(AdditionsTexture.BloomLineHoriz);
            Vector2 dir = UnrelentingRush_SavedRotation.ToRotationVector2();
            float fade = GetLerpBump(0f, .2f, 1f, .8f, completion);
            float dist = 2000;
            Vector2 a = NPC.Center;
            Vector2 b = NPC.Center + dir * dist;
            Vector2 tangent = a.SafeDirectionTo(b) * a.Distance(b);
            float rotation = tangent.ToRotation();
            const float ImageThickness = 8;
            float thicknessScale = 4f / ImageThickness;
            Vector2 capOrigin = new(cap.Width, cap.Height / 2f);
            Vector2 middleOrigin = new(0, horiz.Height / 2f);
            Vector2 middleScale = new(a.Distance(b) / horiz.Width, thicknessScale);
            Color color = Color.DeepSkyBlue.Lerp(Color.LightCyan, completion) * fade;
            Main.spriteBatch.Draw(horiz, a - Main.screenPosition, null, color, rotation, middleOrigin, middleScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(cap, a - Main.screenPosition, null, color, rotation, capOrigin, thicknessScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(cap, b - Main.screenPosition, null, color, rotation + MathHelper.Pi, capOrigin, thicknessScale, SpriteEffects.None, 0f);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);
    }
}
