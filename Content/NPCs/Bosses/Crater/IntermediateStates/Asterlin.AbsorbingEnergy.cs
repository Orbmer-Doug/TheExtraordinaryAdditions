using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_AbsorbingEnergy()
    {
        StateMachine.RegisterTransition(AsterlinAIType.AbsorbingEnergy, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Barrage, 1f } }, false, () =>
        {
            return FightStarted;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.AbsorbingEnergy, DoBehavior_AbsorbingEnergy);
    }

    public void DoBehavior_AbsorbingEnergy()
    {
        if (AITimer == 0)
        {
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<CondensedSoulMass>(), 0, 0f);
        }

        if (Utility.FindProjectile(out Projectile mass, ModContent.ProjectileType<CondensedSoulMass>()))
        {
            SetHeadRotation(EyePosition.AngleTo(mass.Center + Vector2.UnitX * MathF.Cos(Main.GlobalTimeWrappedHourly * .5f) * 40f));
            SetRightHandTarget(mass.Center + Vector2.UnitY * MathF.Sin(Main.GlobalTimeWrappedHourly) * 50f);
            SetLeftLegRotation(-1.5f);
            SetRightLegRotation(-1.5f);
            SetDirection((mass.Center.X > NPC.Center.X).ToDirectionInt());
            SetLegFlamesInterpolant(0f);

            for (int i = 0; i < absorb.Length; i++)
            {
                if (absorb[i] == null || absorb[i]._disposed)
                    absorb[i] = new(c => 24f * mass.scale, (c, pos) => MulticolorLerp(1f - c.X, Color.White, Color.Gold, Color.DarkGoldenrod), null, 200);
            }

            for (int i = 0; i < points.Length; i++)
            {
                points[i] ??= new(200);
                List<Vector2> positions = [RightHandPosition, mass.Center + PolarVector(200f * mass.scale, i == 0 ? -.5f : i == 1 ? .8f : 1.8f), mass.Center];
                for (int j = 0; j < 200; j++)
                    points[i].SetPoint(j, Animators.CatmullRomSpline(positions, InverseLerp(0, 200, j) * Animators.MakePoly(4f).InFunction(mass.scale)));

                if (Main.rand.NextBool(25))
                {
                    Vector2 point = points[i].Points[Main.rand.Next(points[i].Count)];
                    ParticleRegistry.SpawnBloomPixelParticle(point, Main.rand.NextVector2Circular(3f, 3f), Main.rand.Next(50, 90), Main.rand.NextFloat(.5f, 1.1f), Color.Gold, Color.PaleGoldenrod);
                }
            }

            if (Main.rand.NextBool(9))
                ParticleRegistry.SpawnGlowParticle(RightHandPosition, Main.rand.NextVector2Circular(3f, 3f), Main.rand.Next(40, 50), Main.rand.NextFloat(20f, 30f), Color.Gold);
        }

        if (!NPC.dontTakeDamage)
        {
            NPC.dontTakeDamage = true;
            NPC.netUpdate = true;
        }
    }

    public OptimizedPrimitiveTrail[] absorb = new OptimizedPrimitiveTrail[3];
    public ManualTrailPoints[] points = new ManualTrailPoints[3];

    public void AbsorbingEnergy_Draw()
    {
        void draw()
        {
            if (!Utility.FindProjectile(out Projectile mass, ModContent.ProjectileType<CondensedSoulMass>()))
                return;

            for (int i = 0; i < absorb.Length; i++)
            {
                OptimizedPrimitiveTrail trail = absorb[i];
                ManualTrailPoints manual = points[i];
                if (trail == null || trail._disposed || manual == null)
                    continue;

                ManagedShader shader = AssetRegistry.GetShader("OverchargedLaserShader");
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise2), 1, SamplerState.AnisotropicWrap);
                shader.TrySetParameter("time", -Main.GlobalTimeWrappedHourly);
                trail?.DrawTrail(shader, manual.Points, -1, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderNPCs);

        void glow()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(RightHandPosition, new Vector2(20f)), null, Color.White, 0f, tex.Size() / 2);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(RightHandPosition, new Vector2(40f)), null, Color.Gold, 0f, tex.Size() / 2);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(RightHandPosition, new Vector2(60f)), null, Color.DarkGoldenrod, 0f, tex.Size() / 2);
        }
        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.OverNPCs, BlendState.Additive);
    }
}
