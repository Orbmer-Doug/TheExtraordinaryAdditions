using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> Barrage_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Swings, 1f }, { AsterlinAIType.RotatedDicing, .6f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Barrage()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Barrage, Barrage_PossibleStates, false, () =>
        {
            return AITimer >= Barrage_TotalTime && !AnyProjectile(ModContent.ProjectileType<BarrageBeam>());
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Barrage, DoBehavior_Barrage);
    }

    public static int Barrage_AttackTime => SecondsToFrames(8f);
    public static int Barrage_FadeTime => SecondsToFrames(.8f);
    public static int Barrage_BeamRate => DifficultyBasedValue(30, 14, 12, 10, 8, 6);
    public static int Barrage_HoverTime => 40;
    public static int Barrage_BeamExpandTime => 44;
    public static int Barrage_BeamTime => 130;
    public static int Barrage_BeamFadeTime => 55;

    public static int Barrage_TotalTime => Barrage_AttackTime + Barrage_FadeTime;

    public void DoBehavior_Barrage()
    {
        if (AITimer >= Barrage_AttackTime)
            ZPosition = InverseLerp(Barrage_AttackTime, Barrage_TotalTime, AITimer);
        else
        {
            SetLookingStraight(true);
            ZPosition = InverseLerp(Barrage_FadeTime, 0f, AITimer);
            SetLeftHandTarget(NPC.Center - Vector2.UnitX * 10f + NPC.velocity);
            SetRightHandTarget(NPC.Center + Vector2.UnitX * 10f + NPC.velocity);

            if (AITimer >= Barrage_FadeTime)
            {
                if (AITimer % Barrage_BeamRate == (Barrage_BeamRate - 1))
                {
                    if (this.RunServer())
                        NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BarrageBeam>(), MediumAttackDamage, 0f);
                }

                if (AITimer % (Barrage_BeamRate * 2) == (Barrage_BeamRate * 2 - 1))
                {
                    if (this.RunServer())
                        NPC.NewNPCProj(NPC.Center, Main.rand.NextVector2Circular(40f, 40f), ModContent.ProjectileType<DartBomb>(), MediumAttackDamage, 0f);
                }
            }
        }

        CasualHoverMovement();
    }

    public void Barrage_Draw()
    {
        ManagedShader shader = AssetRegistry.GetShader("EnergyOrbShader");
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1, SamplerState.PointWrap);
        shader.TrySetParameter("pulseIntensity", 0.05f);
        shader.TrySetParameter("glowIntensity", 0.8f);
        shader.TrySetParameter("glowPower", 2.74f);

        void orb()
        {
            float factor = InverseLerp(0f, 40f, AITimer) * Animators.MakePoly(3f).OutFunction(InverseLerp(Barrage_AttackTime, Barrage_AttackTime - 60, AITimer));
            Vector2 size = new Vector2(40) * factor;
            Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            Vector2 scale = size / pixel.Size() * 1.2f;
            Color draw = Color.Cyan;
            Vector2 pixelOrig = pixel.Size() * 0.5f;
            Main.spriteBatch.DrawBetter(pixel, NPC.Center, null, new Color(12, 76, 229), 0f, pixelOrig, scale, 0);
        }
        PixelationSystem.QueueTextureRenderAction(orb, PixelationLayer.OverNPCs, null, shader);
    }
}