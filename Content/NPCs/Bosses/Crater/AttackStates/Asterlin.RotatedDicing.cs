using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin
{
    public static readonly Dictionary<AsterlinAIType, float> RotatedDicing_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Barrage, 1f }, { AsterlinAIType.Swings, .6f } };

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_RotatedDicing()
    {
        StateMachine.RegisterTransition(AsterlinAIType.RotatedDicing, RotatedDicing_PossibleStates, false,
            () => RotatedDicing_FadeTimer >= RotatedDicing_BreatheTime);
        StateMachine.RegisterStateBehavior(AsterlinAIType.RotatedDicing, DoBehavior_RotatedDicing);
    }

    public static int RotatedDicing_Cycles => DifficultyBasedValue(3, 4, 5, 5, 6, 6);
    public static int RotatedDicing_Wait => DifficultyBasedValue(90, 80, 75, 60, 50, 40);
    public static int RotatedDicing_Spacing => DifficultyBasedValue(200, 160, 150, 140, 130, 120);
    public static int RotatedDicing_TelegraphTime => DifficultyBasedValue(80, 70, 65, 60, 50, 40);
    public static int RotatedDicing_PositioningTime => DifficultyBasedValue(30, 20, 15, 12, 12, 8);
    public static int RotatedDicing_FireCount => DifficultyBasedValue(1, 1, 2, 2, 3, 3);
    public static readonly int RotatedDicing_BreatheTime = 90;

    public int RotatedDicing_Cycle
    {
        get => (int) ExtraAI[0];
        set => ExtraAI[0] = value;
    }

    public int RotatedDicing_FadeTimer
    {
        get => (int) ExtraAI[1];
        set => ExtraAI[1] = value;
    }

    public void DoBehavior_RotatedDicing()
    {
        if (AITimer == 1)
        {
            if (ModNPC.RunServer())
                NPC.NewNPCProj(RightHandPosition, Vector2.Zero, ModContent.ProjectileType<RadiantPulser>(),
                    HeavyAttackDamage, 0f);
        }

        if ((RotatedDicing_Cycle >= RotatedDicing_Cycles &&
             !AnyProjectile(ModContent.ProjectileType<RadiantPulser>())) || AITimer > SecondsToFrames(40))
            RotatedDicing_FadeTimer++;

        SetLookingStraight(true);

        float fade = InverseLerp(0f, RotatedDicing_BreatheTime, RotatedDicing_FadeTimer);
        SetRightHandTarget(RightArm.RootPosition +
                           PolarVector(MakePoly(2f).OutFunction.Evaluate(80f, 200f, fade),
                               MakePoly(4f).InOutFunction.Evaluate(-.5f, MathHelper.PiOver2, fade)));
        SetLeftHandTarget(LeftArm.RootPosition + Vector2.UnitY * 100f);
        CasualHoverMovement();
    }

    public void RotatedDicing_Draw()
    {
        if (CurrentState != AsterlinAIType.RotatedDicing)
            return;

        Texture2D smear = AssetRegistry.GennedTextures.SemiCircularSmear;
        Texture2D glow = AssetRegistry.GennedTextures.GlowParticleSmall;
        Texture2D star = AssetRegistry.GennedTextures.LensStar;
        float fade = MakePoly(2f)
            .InOutFunction(InverseLerp(RotatedDicing_BreatheTime, 0f, RotatedDicing_FadeTimer));
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, glow,
            ToTarget(RightHandPosition, new(30f)), null, Color.White * fade, 0f,
            glow.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, glow,
            ToTarget(RightHandPosition, new(40f)), null, Color.Gold * .8f * fade,
            0f, glow.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, glow,
            ToTarget(RightHandPosition, new(50f)), null,
            Color.Goldenrod * .6f * fade, 0f, glow.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, glow,
            ToTarget(RightHandPosition, new(60f)), null,
            Color.DarkGoldenrod * .4f * fade, 0f, glow.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, star,
            ToTarget(RightHandPosition, new(MathHelper.Lerp(120f, 160f, Sin01(AITimer * .04f)) * fade)), null,
            Color.Goldenrod * .5f, AITimer * .06f, star.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, smear,
            ToTarget(RightHandPosition, new(100f * fade)), null, Color.Goldenrod,
            AITimer * .01f, smear.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.OverNPCs, BlendState.Additive, smear,
            ToTarget(RightHandPosition, new(180f * fade)), null, Color.Goldenrod,
            -AITimer * .04f, smear.Size() / 2);
    }
}
