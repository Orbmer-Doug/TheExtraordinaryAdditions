using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.SolarGuardian;

public class SolarGuardian : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SolarGuardian);

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 17;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
        NPCID.Sets.TrailingMode[NPC.type] = 3;
        NPCID.Sets.TrailCacheLength[NPC.type] = 9;
    }

    public override void SetDefaults()
    {
        NPC.npcSlots = 3f;
        NPC.noGravity = true;
        NPC.damage = Main.expertMode ? 105 : 50;
        NPC.width = NPC.height = 162;
        NPC.defense = 12;
        NPC.lifeMax = 8000;
        NPC.knockBackResist = 0f;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.value = Item.buyPrice(0, 6, 25, 0);
        NPC.HitSound = SoundID.NPCHit30;
        NPC.DeathSound = SoundID.NPCDeath18;
        NPC.rarity = 4;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<SolarGuardianBanner>();
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
            ]);
    }

    public enum GuardianStates
    {
        Idle,
        Shooting,
        Dash,
    }

    public int Time
    {
        get => (int)NPC.ai[0];
        set => NPC.ai[0] = value;
    }

    public GuardianStates State
    {
        get => (GuardianStates)NPC.ai[1];
        set => NPC.ai[1] = (int)value;
    }

    public ref float Rotate => ref NPC.ai[2];

    public int FireCounter
    {
        get => (int)NPC.ai[3];
        set => NPC.ai[3] = value;
    }

    public int RotateDir
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }

    public static int SunrayDamage => DifficultyBasedValue(90, 170, 260);
    public static float ChargeTime => SecondsToFrames(1.2f);
    public static int FireWait => SecondsToFrames(.4f);
    public static int FireCount => DifficultyBasedValue(2, 3, 4);
    public static int ReelBackTime => SecondsToFrames(1.4f);
    public static int DashTime => SecondsToFrames(.2f);
    public static int SlowdownTime => SecondsToFrames(.8f);

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter += State == GuardianStates.Dash ? 0.4f : 0.15f;
        NPC.frameCounter %= Main.npcFrameCount[NPC.type];
        NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
    }

    public override void AI()
    {
        if (!NPC.SearchForPlayerTarget(out Player target, true) && State != GuardianStates.Idle)
        {
            Time = 0;
            State = GuardianStates.Idle;
            NPC.netUpdate = true;
        }

        switch (State)
        {
            case GuardianStates.Idle:
                NPC.velocity.X *= .94f;
                NPC.velocity.Y = MathHelper.Lerp(-5f, 5f, Sin01(Time * .03f));
                NPC.rotation = NPC.rotation.SmoothAngleLerp(0f, .8f, .1f);
                if (NPC.HasValidTarget)
                {
                    RotateDir = Main.rand.NextFromCollection<int>([-1, 1]);
                    Time = 0;
                    State = GuardianStates.Shooting;
                    NPC.netUpdate = true;
                }
                break;
            case GuardianStates.Shooting:
                Vector2 dest = target.Center + PolarVector(300f, Rotate);
                NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(dest) * MathF.Min(NPC.Distance(dest), 17f), .2f);
                Rotate += .01f * RotateDir;
                NPC.rotation = NPC.rotation.SmoothAngleLerp(NPC.Center.AngleTo(target.Center), .2f, .1f);

                if (Time < ChargeTime)
                {
                    ParticleRegistry.SpawnDustParticle(NPC.RotHitbox().RandomPoint(), -Vector2.UnitY * Main.rand.NextFloat(3f, 8f),
                        Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, .8f), Color.OrangeRed, .1f, false, true, true);
                }
                else
                {
                    if (Time % FireWait == (FireWait - 1))
                    {
                        int count = FireCounter % 2 == 0 ? 3 : 2;
                        for (int i = 0; i < count; i++)
                        {
                            Vector2 vel = NPC.rotation.ToRotationVector2().RotatedBy(MathHelper.Lerp(-.4f, .4f, InverseLerp(0f, count - 1, i))) * 3f;
                            if (this.RunServer())
                                NPC.NewNPCProj(NPC.Center, vel, ModContent.ProjectileType<Sunray>(), SunrayDamage, 1f);
                            for (int o = 0; o < 6; o++)
                            {
                                ParticleRegistry.SpawnBloomLineParticle(NPC.Center + Main.rand.NextVector2Circular(20f, 20f),
                                    vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.7f, 3f), Main.rand.Next(30, 50), Main.rand.NextFloat(.3f, .6f), Color.OrangeRed);
                                ParticleRegistry.SpawnBloomPixelParticle(NPC.Center + Main.rand.NextVector2Circular(20f, 20f),
                                    vel.RotatedByRandom(.3f) * Main.rand.NextFloat(.5f, 2.5f), Main.rand.Next(40, 60), Main.rand.NextFloat(.4f, .7f), Color.OrangeRed, Color.Orange, null, 1.1f, 3);
                            }
                        }
                        FireCounter++;
                    }

                    if (FireCounter >= FireCount)
                    {
                        Time = FireCounter = 0;
                        State = GuardianStates.Dash;
                        NPC.netUpdate = true;
                    }
                }

                break;
            case GuardianStates.Dash:
                if (Time < ReelBackTime)
                {
                    NPC.velocity = -NPC.SafeDirectionTo(target.Center) * Animators.MakePoly(4f).InFunction.Evaluate(Time, 0f, ReelBackTime, 7f, 1f);
                    NPC.rotation = NPC.AngleTo(target.Center + target.velocity * 5f);
                }
                else if (Time < (ReelBackTime + DashTime))
                {
                    NPC.velocity = NPC.rotation.ToRotationVector2() * 50f;
                }
                else if (Time < (ReelBackTime + DashTime + SlowdownTime))
                {
                    NPC.velocity *= .95f;
                }
                else
                {
                    RotateDir = Main.rand.NextFromCollection<int>([-1, 1]);
                    Time = 0;
                    State = GuardianStates.Shooting;
                    NPC.netUpdate = true;
                }

                if (NPC.velocity.Length() > 9f)
                    ParticleRegistry.SpawnGlowParticle(NPC.RotHitbox().RandomPoint(), -NPC.velocity * Main.rand.NextFloat(.2f, .4f), Main.rand.Next(30, 50), Main.rand.NextFloat(40f, 90f), Color.OrangeRed, 1.2f);

                Rotate = target.Center.AngleTo(NPC.Center);
                break;
        }
        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f);

        Time++;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (Main.hardMode && spawnInfo.Sky)
            return 0.03f;
        return 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0)
            target.AddBuff(BuffID.OnFire3, SecondsToFrames(4));
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        for (int j = 0; j < 3; j++)
        {
            Dust o = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.OrangeTorch, hit.HitDirection * 2, hit.HitDirection * 2, 0, default(Color), 3f);
            o.noGravity = true;
        }
        if (this.NPC.life <= 0)
        {
            for (int j = 0; j < 70; j++)
            {
                Dust obj = Dust.NewDustDirect(NPC.position, (int)(NPC.width * this.NPC.scale), (int)(NPC.height * this.NPC.scale * 0.6f), DustID.SolarFlare, hit.HitDirection * 3f, -1f, 0, default(Color), 6f);
                obj.scale *= Utils.NextFloat(Main.rand, 0.85f, 1.15f);
                obj.fadeIn = 0.5f;
                obj.noGravity = true;
            }
        }
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => State == GuardianStates.Dash;

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WrithingLight>(), 1, 1, 1));
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (NPC.IsABestiaryIconDummy)
            return true;

        Texture2D tex = NPC.ThisNPCTexture();
        Vector2 orig = NPC.frame.Size() / 2;

        if (State == GuardianStates.Dash)
        {
            int imageAmt = NPCID.Sets.TrailCacheLength[Type];
            for (int i = 0; i < imageAmt; i++)
            {
                Vector2 pos = NPC.oldPos[i] + orig;
                float rot = NPC.oldRot[i];
                float opac = InverseLerp(imageAmt, 0, i) * .7f * InverseLerp(0f, 20f, Time) * InverseLerp(ReelBackTime + DashTime + SlowdownTime, ReelBackTime + DashTime, Time);
                spriteBatch.DrawBetter(tex, pos, NPC.frame, Color.Orange with { A = 0 } * opac, rot, orig, 1f, SpriteEffects.None);
            }
        }
        spriteBatch.DrawBetter(tex, NPC.Center, NPC.frame, Color.White, NPC.rotation, orig, 1f, SpriteEffects.None);
        if (State == GuardianStates.Shooting)
        {
            if (Time < ChargeTime)
            {
                for (int i = 0; i < 6; i++)
                {
                    float comp = InverseLerp(0, 6, i);
                    float anim = InverseLerp(0, ChargeTime, Time);
                    float animSize = Animators.MakePoly(3f).InFunction.Evaluate(1.7f, 1f, anim);
                    Vector2 offset = PolarVector(20f * (1f - anim), MathHelper.TwoPi * comp);
                    float opac = MathHelper.Lerp(0f, .6f, InverseLerp(0f, 10f, Time)) * (1f - anim);
                    spriteBatch.DrawBetter(tex, NPC.Center + offset, NPC.frame, Color.OrangeRed with { A = 0 } * opac, NPC.rotation, orig, 1f, SpriteEffects.None);
                }
            }
        }

        return false;
    }
}
