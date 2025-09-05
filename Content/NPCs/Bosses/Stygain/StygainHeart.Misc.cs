using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public sealed partial class StygainHeart : ModNPC
{
    public void SummonMass()
    {
        // not excessive whatsoever
        ref float Timer = ref NPC.AdditionsInfo().ExtraAI[24];
        ref float PosX = ref NPC.AdditionsInfo().ExtraAI[25];
        ref float PosY = ref NPC.AdditionsInfo().ExtraAI[26];
        ref float Chose = ref NPC.AdditionsInfo().ExtraAI[27];
        ref float SpinStart = ref NPC.AdditionsInfo().ExtraAI[28];
        ref float SpinDir = ref NPC.AdditionsInfo().ExtraAI[29];
        Timer++;

        const float TotalTime = 180f;

        if (this.RunServer() && Chose == 0f)
        {
            Vector2 rand = NPC.Center + Main.rand.NextVector2CircularLimited(400f, 400f, .5f, 1f);
            PosX = rand.X; PosY = rand.Y;
            SpinStart = RandomRotation();
            SpinDir = Main.rand.NextBool().ToDirectionInt();
            Chose = 1f;
        }

        Vector2 dest = new(PosX, PosY);
        if (Timer < TotalTime)
        {
            float completion = 1f - InverseLerp(0f, TotalTime, Timer);
            for (int i = 0; i < 6; i++)
            {
                Vector2 pos = dest + (MathHelper.TwoPi * i / 6 + ((MathHelper.TwoPi * completion * SpinDir) + SpinStart)).ToRotationVector2() * (completion * 400f);
                Vector2 vel = pos.SafeDirectionTo(dest).RotatedByRandom(.15f) * (Main.rand.NextFloat(1f, 6f) * completion);
                int life = Main.rand.Next(20, 40);
                float scale = Main.rand.NextFloat(.4f, .6f);
                Color col = Color.Crimson.Lerp(Color.Red, Main.rand.NextFloat(.4f, .6f)) * completion;
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, col, .7f);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * 1.3f, life, scale * 1.8f, col.Lerp(Color.OrangeRed, .2f), .5f, true, .1f);
            }
        }

        if (this.RunServer() && Timer >= TotalTime)
        {
            int mass = ModContent.NPCType<CoalescentMass>();
            NPC.NewNPCBetter(dest, Vector2.Zero, mass, 0, 0f, 0f, 0f, 0f, NPC.target);

            for (int i = 0; i < 50; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 50 + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(3f, 16f);

                ParticleRegistry.SpawnHeavySmokeParticle(dest, vel * .2f, Main.rand.Next(70, 100),
                    Main.rand.NextFloat(.4f, 1f), Color.Crimson, Main.rand.NextFloat(.6f, .8f));

                ParticleRegistry.SpawnBloomPixelParticle(dest, vel, Main.rand.Next(50, 120), Main.rand.NextFloat(.4f, 1.1f), Color.DarkRed, Color.Crimson, null, 2f);
            }

            for (int i = 24; i < 29; i++)
                NPC.AdditionsInfo().ExtraAI[i] = 0f;

            MakingMass = false;
            NPC.netUpdate = true;
        }
    }

    public static void ApplyLifesteal(Projectile p, Player target, int hit)
    {
        if (hit > 1 && target.HasBuff(ModContent.BuffType<HemorrhageTransfer>()))
        {
            p.NewProj(target.Center, Vector2.Zero, ModContent.ProjectileType<BloodletRelay>(), 0, 0f, -1, hit * (.25f * Utility.CountNPCs(ModContent.NPCType<CoalescentMass>())));
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
        float PosX = target.position.X + target.width / 2 - position.X;
        float PosY = target.position.Y + target.height / 2 - position.Y;

        NPC.rotation = NPC.rotation.AngleLerp(MathF.Atan2(PosY, PosX) + (PosX.NonZeroSign() < 0 ? MathHelper.Pi : 0f), weight);
        NPC.spriteDirection = PosX.NonZeroSign();
    }
}