using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_RotatedDicing()
    {
        StateMachine.RegisterTransition(AsterlinAIType.RotatedDicing, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Swings, 1f }, { AsterlinAIType.Barrage, 1f } }, false, () =>
        {
            return RotatedDicing_FadeTimer >= RotatedDicing_BreatheTime;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.RotatedDicing, DoBehavior_RotatedDicing);
    }

    public static int RotatedDicing_Cycles => DifficultyBasedValue(3, 4, 5, 5, 6, 6);
    public static int RotatedDicing_Wait => DifficultyBasedValue(90, 80, 75, 60, 50, 40);
    public static int RotatedDicing_Spacing => DifficultyBasedValue(200, 160, 150, 140, 130, 120);
    public static int RotatedDicing_TelegraphTime => DifficultyBasedValue(80, 70, 65, 60, 50, 40);
    public static int RotatedDicing_PositioningTime => DifficultyBasedValue(30, 20, 15, 12, 12, 8);
    public static int RotatedDicing_FireCount => DifficultyBasedValue(1, 1, 2, 2, 3, 3);
    public static int RotatedDicing_BreatheTime => 90;
    public int RotatedDicing_Cycle
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }
    public int RotatedDicing_FadeTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = value;
    }

    public void DoBehavior_RotatedDicing()
    {
        if (AITimer == 1)
        {
            if (this.RunServer())
                NPC.NewNPCProj(RightHandPosition, Vector2.Zero, ModContent.ProjectileType<RadiantPulser>(), Asterlin.HeavyAttackDamage, 0f);
        }

        if (RotatedDicing_Cycle >= RotatedDicing_Cycles && !Utility.AnyProjectile(ModContent.ProjectileType<RadiantPulser>()))
            RotatedDicing_FadeTimer++;

        SetLookingStraight(true);

        float fade = InverseLerp(0f, RotatedDicing_BreatheTime, RotatedDicing_FadeTimer);
        SetRightHandTarget(rightArm.RootPosition + PolarVector(Animators.MakePoly(2f).OutFunction.Evaluate(80f, 200f, fade), Animators.MakePoly(4f).InOutFunction.Evaluate(-.5f, MathHelper.PiOver2, fade)));
        SetLeftHandTarget(leftArm.RootPosition + Vector2.UnitY * 100f);
        CasualHoverMovement();
    }

    public void RotatedDicing_Draw()
    {
        if (CurrentState != AsterlinAIType.RotatedDicing)
            return;

        void draw()
        {
            Texture2D smear = AssetRegistry.GetTexture(AdditionsTexture.SemiCircularSmear);
            Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            float fade = Animators.MakePoly(2f).InOutFunction(InverseLerp(RotatedDicing_BreatheTime, 0f, NPC.AdditionsInfo().ExtraAI[1]));

            Main.spriteBatch.DrawBetterRect(glow, ToTarget(RightHandPosition, new(30f)), null, Color.White * fade, 0f, glow.Size() / 2);
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(RightHandPosition, new(40f)), null, Color.Gold * .8f * fade, 0f, glow.Size() / 2);
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(RightHandPosition, new(50f)), null, Color.Goldenrod * .6f * fade, 0f, glow.Size() / 2);
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(RightHandPosition, new(60f)), null, Color.DarkGoldenrod * .4f * fade, 0f, glow.Size() / 2);

            Main.spriteBatch.DrawBetterRect(star, ToTarget(RightHandPosition, new(MathHelper.Lerp(120f, 160f, Sin01(AITimer * .04f)) * fade)), null, Color.Goldenrod * .5f, AITimer * .06f, star.Size() / 2);

            Main.spriteBatch.DrawBetterRect(smear, ToTarget(RightHandPosition, new(100f * fade)), null, Color.Goldenrod, AITimer * .01f, smear.Size() / 2);
            Main.spriteBatch.DrawBetterRect(smear, ToTarget(RightHandPosition, new(180f * fade)), null, Color.Goldenrod, -AITimer * .04f, smear.Size() / 2);

        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverNPCs, BlendState.Additive);
    }
}
