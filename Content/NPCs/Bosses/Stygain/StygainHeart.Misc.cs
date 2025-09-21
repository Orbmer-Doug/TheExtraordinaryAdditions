using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public sealed partial class StygainHeart : ModNPC
{
    public void SummonMass()
    {
        // not excessive whatsoever
        MassTimer++;

        const float TotalTime = 180f;

        if (this.RunServer() && MassInitialize == false)
        {
            MassPosition = NPC.Center + Main.rand.NextVector2CircularLimited(400f, 400f, .5f, 1f);
            MassSpinStart = RandomRotation();
            MassSpinDir = Main.rand.NextBool().ToDirectionInt();
            MassInitialize = true;
            NPC.netUpdate = true;
        }

        if (MassTimer < TotalTime)
        {
            float completion = 1f - InverseLerp(0f, TotalTime, MassTimer);
            for (int i = 0; i < 6; i++)
            {
                Vector2 pos = MassPosition + (MathHelper.TwoPi * i / 6 + ((MathHelper.TwoPi * completion * MassSpinDir) + MassSpinStart)).ToRotationVector2() * (completion * 400f);
                Vector2 vel = pos.SafeDirectionTo(MassPosition).RotatedByRandom(.15f) * (Main.rand.NextFloat(1f, 6f) * completion);
                int life = Main.rand.Next(20, 40);
                float scale = Main.rand.NextFloat(.4f, .6f);
                Color col = Color.Crimson.Lerp(Color.Red, Main.rand.NextFloat(.4f, .6f)) * completion;
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, col, .7f);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * 1.3f, life, scale * 1.8f, col.Lerp(Color.OrangeRed, .2f), .5f, true, .1f);
            }
        }

        if (MassTimer >= TotalTime)
        {
            if (this.RunServer())
            {
                int mass = ModContent.NPCType<CoalescentMass>();
                NPC.NewNPCBetter(MassPosition, Vector2.Zero, mass, 0, 0f, 0f, 0f, 0f, NPC.target);
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 50 + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(3f, 16f);

                ParticleRegistry.SpawnHeavySmokeParticle(MassPosition, vel * .2f, Main.rand.Next(70, 100),
                    Main.rand.NextFloat(.4f, 1f), Color.Crimson, Main.rand.NextFloat(.6f, .8f));

                ParticleRegistry.SpawnBloomPixelParticle(MassPosition, vel, Main.rand.Next(50, 120), Main.rand.NextFloat(.4f, 1.1f), Color.DarkRed, Color.Crimson, null, 2f);
            }

            StartMakingMass = false;
            MassTimer = 0;
            MassPosition = Vector2.Zero;
            MassInitialize = false;
            MassSpinStart = 0f;
            MassSpinDir = 0;

            NPC.netUpdate = true;
        }
    }

    public static void ApplyLifesteal(ProjOwnedByNPC<StygainHeart> p, Player target, int hit)
    {
        if (hit > 1 && target.HasBuff(ModContent.BuffType<HemorrhageTransfer>()))
        {
            float healAmt = hit * (.25f * Utility.CountNPCs(ModContent.NPCType<CoalescentMass>()));
            p.SpawnProjectile(target.Center, Vector2.Zero, ModContent.ProjectileType<BloodletRelay>(), 0, 0f, healAmt);
        }
    }

    public static void ClearAllProjectiles()
    {
        ProjOwnedByNPC<StygainHeart>.KillAll();

        foreach (NPC n in Main.ActiveNPCs)
        {
            int shield = ModContent.NPCType<CoalescentMass>();
            if (n.type == shield && n != null)
            {
                n.active = false;
            }
        }

        if (Utility.FindProjectile(out Projectile p, ModContent.ProjectileType<HemoglobBarrier>()))
        {
            p.As<HemoglobBarrier>().FadeOut = true;
        }
    }

    public void FixedRotation(Entity target, float weight = 1f)
    {
        Vector2 position = new(NPC.position.X + NPC.width * 0.5f, NPC.position.Y + NPC.height * 0.5f);
        float posX = target.position.X + target.width / 2 - position.X;
        float posY = target.position.Y + target.height / 2 - position.Y;

        NPC.rotation = NPC.rotation.AngleLerp(MathF.Atan2(posY, posX) + (posX.NonZeroSign() < 0 ? MathHelper.Pi : 0f), weight);
        NPC.spriteDirection = posX.NonZeroSign();
    }
}