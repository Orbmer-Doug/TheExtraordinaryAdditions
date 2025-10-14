using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_GabrielLeave()
    {
        StateMachine.RegisterStateBehavior(AsterlinAIType.GabrielLeave, DoBehavior_GabrielLeave);
    }

    public static int GabrielLeave_HoverTime => SecondsToFrames(2);
    public static int GabrielLeave_DisintegrationWaitTime => SecondsToFrames(.75f);
    public static int GabrielLeave_DisintegrationTime => SecondsToFrames(.95f);
    public static int GabrielLeave_BeamFadeTime => SecondsToFrames(.4f);
    public static int GabrielLeave_MaxTime => GabrielLeave_HoverTime + GabrielLeave_DisintegrationWaitTime + GabrielLeave_DisintegrationTime + GabrielLeave_BeamFadeTime;

    public void DoBehavior_GabrielLeave()
    {
        if (AITimer >= GabrielLeave_HoverTime)
            SetLegFlamesInterpolant(0f);

        SetMotionBlurInterpolant(InverseLerp(50f, 0f, AITimer) * UnveilingZenith_BlurAmount);
        if (AITimer < GabrielLeave_HoverTime)
        {
            HeatDistortionStrength = Utils.Remap(AITimer, 40f, 0f, 0f, DesperationDrama_MaxHeatDistortionStrength);
            HeatDistortionArea = Utils.Remap(AITimer, 40f, 0f, 0f, DesperationDrama_MaxHeatDistortionArea);
            ParticleRegistry.SpawnTechyHolosquareParticle(TopAntennaPosition, -Vector2.UnitY.RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 13f), Main.rand.Next(30, 45), Main.rand.NextFloat(.3f, .5f), Color.Cyan);
            NPC.SmoothFlyNear(new Vector2(Target.Center.X + (300f * (NPC.Center.X > Target.Center.X).ToDirectionInt()), Target.Center.Y - 20f), .07f, .5f);
            SetLegFlamesInterpolant(InverseLerp(GabrielLeave_HoverTime, GabrielLeave_HoverTime - 40, AITimer));
        }
        else if (AITimer == GabrielLeave_HoverTime)
        {
            AdditionsSound.VirtueAttack.Play(NPC.Center, 1.4f);
            ParticleRegistry.SpawnBlurParticle(NPC.Center, 50, .2f, 1900f);
            ScreenShakeSystem.New(new(.2f, .2f), NPC.Center);
        }
        else
        {
            NPC.velocity = Vector2.Zero;
            float interpol = InverseLerp(GabrielLeave_HoverTime + GabrielLeave_DisintegrationWaitTime,
                GabrielLeave_HoverTime + GabrielLeave_DisintegrationWaitTime + GabrielLeave_DisintegrationTime, AITimer);
            DisintegrationInterpolant = interpol;

            if (AITimer >= GabrielLeave_MaxTime)
            {
                AdditionsSound.GabrielWeaponBreak.Play(NPC.Center, 1.2f);
                for (int i = 0; i < 8; i++)
                {
                    float lerper = InverseLerp(0, 8, i);
                    int life = (int)MathHelper.Lerp(60, 180, lerper);
                    float scale = MathHelper.Lerp(300f, 500f, lerper);
                    Color col = Color.Lerp(Color.White, Color.Gold, lerper);
                    ParticleRegistry.SpawnPulseRingParticle(NPC.Center, Vector2.Zero, life, 0f, Vector2.One, 0f, scale, col);
                }

                for (int i = 0; i < 80; i++)
                    ParticleRegistry.SpawnBloomLineParticle(NPC.Center, Main.rand.NextVector2Circular(40, 40) + Main.rand.NextVector2Circular(4, 4),
                        Main.rand.Next(50, 90), Main.rand.NextFloat(.7f, 1.4f), Color.Goldenrod);
                NPC.Kill();
            }
        }
    }

    public void GabrielLeave_DrawBeam()
    {
        if (AITimer < GabrielLeave_HoverTime)
            return;

        Texture2D pix = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        float beamWidth = (1f - Animators.MakePoly(2f).OutFunction(InverseLerp(GabrielLeave_HoverTime + GabrielLeave_DisintegrationWaitTime + GabrielLeave_DisintegrationTime,
            GabrielLeave_HoverTime + GabrielLeave_DisintegrationWaitTime + GabrielLeave_DisintegrationTime + GabrielLeave_BeamFadeTime, AITimer)));

        Vector2 a = NPC.Center - Vector2.UnitY * 3000f;
        Vector2 b = NPC.Center + Vector2.UnitY * 3000f;
        Vector2 tangent = a.SafeDirectionTo(b) * a.Distance(b);
        float rotation = tangent.ToRotation();
        Vector2 middleOrigin = new(0, pix.Height / 2f);

        for (int i = 0; i < 16; i++)
        {
            float interpol = InverseLerp(0f, 16f, i);
            Vector2 middleScale = new(a.Distance(b) / pix.Width, beamWidth * MathHelper.Lerp(200f, 500f, interpol));
            Color col = Color.White.Lerp(Color.DarkGoldenrod, Animators.MakePoly(3f).OutFunction(interpol)) with { A = 0 };
            Main.spriteBatch.Draw(pix, a - Main.screenPosition, null, col, rotation, middleOrigin, middleScale, SpriteEffects.None, 0f);
        }
    }
}
