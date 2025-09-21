using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

// enchanced darts near impact, making it so that you have to get near unless you get sniped
public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Judgement()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Judgement, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.TechnicBombBarrage, 1f }, { AsterlinAIType.Hyperbeam, 1f } }, false, () =>
        {
            return Judgement_WaitTimer > Judgement_PillarFlameTime;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Judgement, DoBehavior_Judgement);
    }

    public int Judgement_Cycle
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }

    public int Judgement_WaitTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = value;
    }
    
    public bool Judgement_StopGravity
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[2] == 1;
        set => NPC.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }

    public static int Judgement_HammerReelTime => DifficultyBasedValue(SecondsToFrames(1.4f), SecondsToFrames(1.35f), SecondsToFrames(1.16f), SecondsToFrames(1f), SecondsToFrames(.9f), SecondsToFrames(.85f));
    public static int Judgement_RockCount => DifficultyBasedValue(12, 15, 20, 25, 30, 40);
    public static int Judgement_PillarTelegraphTime => 50;
    public static int Judgement_PillarFlameTime => SecondsToFrames(2.5f);
    public static int Judgement_PillarFadeTime => 50;
    public static int Judgement_Cycles => DifficultyBasedValue(3, 3, 4, 4, 5, 5);
    public void DoBehavior_Judgement()
    {
        Vector2 ideal = Utility.GetHomingVelocity(NPC.Center, Target.Center, Target.Velocity, 20f);
        Vector2 destination = new(Target.Center.X + ideal.X, Target.Center.Y - 340f);
        NPC.SmoothFlyNear(destination, .2f, .9f);

        if (Judgement_Cycle < Judgement_Cycles)
        {
            int wrappedTimer = AITimer % (Judgement_HammerReelTime + Judgement_PillarTelegraphTime + Judgement_PillarFlameTime + Judgement_PillarFadeTime);
            if (wrappedTimer.BetweenNum(Judgement_HammerReelTime, Judgement_HammerReelTime * 3))
            {
                float raise = Judgement_HammerReelTime;
                float raiseAnim = InverseLerp(Judgement_HammerReelTime, Judgement_HammerReelTime * 2, wrappedTimer);

                float fall = Judgement_HammerReelTime * 2;
                float fallAnim = InverseLerp(Judgement_HammerReelTime * 2, Judgement_HammerReelTime * 3, wrappedTimer);

                if (raiseAnim < 1f)
                {
                    ParticleRegistry.SpawnBloomLineParticle(LeftHandPosition + Main.rand.NextVector2Circular(15f, 15f), Vector2.UnitY * Main.rand.NextFloat(2f, 9f), Main.rand.Next(20, 40), Main.rand.NextFloat(.3f, .6f), Color.Goldenrod);
                    SetLeftHandTarget(leftArm.RootPosition + PolarVector(400f, Utils.AngleLerp(MathHelper.PiOver2 + (.1f * Direction), -MathHelper.PiOver2, Animators.MakePoly(3f).InFunction(raiseAnim))));
                }
                else if (fallAnim < 1f)
                {
                    if (PlayerTarget != null && !Judgement_StopGravity)
                    {
                        bool hitGround = Collision.SolidCollision(PlayerTarget.BottomLeft, PlayerTarget.width, 30, true);
                        Tile groundTile = Framing.GetTileSafely((int)(PlayerTarget.Center.X / 16f), (int)(PlayerTarget.Bottom.Y / 16f) + 1);
                        if (TileID.Sets.Platforms[groundTile.TileType] && groundTile.HasUnactuatedTile)
                            hitGround = true;
                        if (hitGround)
                        {
                            Vector2 pos = PlayerTarget.RotHitbox().Bottom;
                            AdditionsSound.RockBreak.Play(pos, .8f, -.4f, .1f);
                            ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, 20, 0f, new(1f, .5f), 0f, 200f, Color.Gray);
                            ParticleRegistry.SpawnBlurParticle(pos, 50, .2f, 900f);
                            Judgement_StopGravity = true;
                        }

                        ParticleRegistry.SpawnSquishyLightParticle(PlayerTarget.RotHitbox().RandomPoint(), -PlayerTarget.velocity * Main.rand.NextFloat(.5f, 1f),
                            Main.rand.Next(30, 40), Main.rand.NextFloat(.3f, .6f), Color.Gold, Main.rand.NextFloat(.6f, 1f), .7f, 3.3f);
                        PlayerTarget.mount?.Dismount(PlayerTarget);
                        PlayerTarget.Additions().LungingDown = true;
                        PlayerTarget.velocity.X *= .95f;
                        PlayerTarget.velocity.Y += MathHelper.Lerp(0f, 11f, fallAnim);
                    }
                    SetLeftHandTarget(leftArm.RootPosition + PolarVector(400f, Utils.AngleLerp(-MathHelper.PiOver2, MathHelper.PiOver2 + (.1f * Direction), Animators.MakePoly(5f).OutFunction(fallAnim))));
                }
            }

            if (wrappedTimer == 1)
            {
                Judgement_StopGravity = false;
                if (this.RunServer())
                    NPC.NewNPCProj(RightHandPosition, Vector2.Zero, ModContent.ProjectileType<JudgementHammer>(), Asterlin.HeavyAttackDamage, 0f);
                Judgement_Cycle++;
                NPC.netUpdate = true;
            }
        }
        else if (Judgement_Cycle >= Judgement_Cycles)
        {
            Judgement_WaitTimer++;
        }
    }
}
