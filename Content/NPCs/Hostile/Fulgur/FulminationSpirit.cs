using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;
using TheExtraordinaryAdditions.Content.Items.Materials.Early;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Lightning;

public class FulminationSpirit : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FulminationSpirit);
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 6;

        NPCID.Sets.TrailingMode[NPC.type] = 1;
    }
    public override bool? CanFallThroughPlatforms()
    {
        return true;
    }

    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.npcSlots = 1f;
        NPC.width = 92;
        NPC.height = 96;
        if (!Main.hardMode)
        {
            NPC.damage = 40;
            NPC.defense = 4;
            NPC.lifeMax = 1200;
        }
        if (Main.hardMode)
        {
            NPC.damage = 50;
            NPC.defense = 8;
            NPC.lifeMax = 1900;
        }
        if (NPC.downedPlantBoss)
        {
            NPC.defense = 16;
            NPC.lifeMax = 2400;
            NPC.damage = 60;
        }
        NPC.knockBackResist = 0f;
        NPC.value = Item.buyPrice(0, 0, 80, 50);
        NPC.noGravity = true;
        NPC.lavaImmune = true;
        NPC.HitSound = SoundID.NPCHit36;
        NPC.DeathSound = SoundID.NPCDeath39;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<FulminationSpiritBanner>();

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            Velocity = 2f, // Draws the NPC in the bestiary as if its walking +2 tiles in the x direction
            Direction = -1 // -1 is left and 1 is right.
        };
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,

                new FlavorTextBestiaryInfoElement("When mother nature burns away the filth of the land with destructive power, lightning gains sentience by power of excess energies")
            ]);

    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (spawnInfo.Player.ZoneSkyHeight)
        {
            return 0.09f;
        }
        return 0f;
    }
    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter += 0.15f;
        NPC.frameCounter %= Main.npcFrameCount[NPC.type];
        int frame = (int)NPC.frameCounter;
        NPC.frame.Y = frame * frameHeight;
    }

    public override void AI()
    {
        Player target = Main.player[NPC.target];
        if (NPC.target < 0 || NPC.target == 255 || target.dead)
        {
            NPC.TargetClosest(true);
        }
        float num = 7f;
        float acceleration = 0.17f;
        Vector2 source = NPC.Center;
        float targetX = target.Center.X;
        float targetY = target.Center.Y;
        targetX = (int)(targetX / 8f) * 8;
        targetY = (int)(targetY / 8f) * 8;
        source.X = (int)(source.X / 8f) * 8;
        source.Y = (int)(source.Y / 8f) * 8;
        targetX -= source.X;
        targetY -= source.Y;
        float distance = (float)Math.Sqrt(targetX * targetX + targetY * targetY);
        float distance2 = distance;
        bool flag = false;
        if (distance > 600f)
        {
            flag = true;
        }
        if (distance == 0f)
        {
            targetX = NPC.velocity.X;
            targetY = NPC.velocity.Y;
        }
        else
        {
            distance = num / distance;
            targetX *= distance;
            targetY *= distance;
        }
        if (distance2 > 100f)
        {
            NPC.ai[0] += 1f;
            if (NPC.ai[0] > 0f)
            {
                NPC.velocity.Y += 0.023f;
            }
            else
            {
                NPC.velocity.Y -= 0.023f;
            }
            if (NPC.ai[0] < -100f || NPC.ai[0] > 100f)
            {
                NPC.velocity.X += 0.023f;
            }
            else
            {
                NPC.velocity.X -= 0.023f;
            }
            if (NPC.ai[0] > 200f)
            {
                NPC.ai[0] = -200f;
            }
        }
        if (target.dead)
        {
            targetX = NPC.direction * num / 2f;
            targetY = (0f - num) / 2f;
        }
        if (NPC.velocity.X < targetX)
        {
            NPC.velocity.X += acceleration;
        }
        else if (NPC.velocity.X > targetX)
        {
            NPC.velocity.X -= acceleration;
        }
        if (NPC.velocity.Y < targetY)
        {
            NPC.velocity.Y += acceleration;
        }
        else if (NPC.velocity.Y > targetY)
        {
            NPC.velocity.Y -= acceleration;
        }
        NPC.localAI[0] += 1f;
        float whentofire = Main.expertMode ? 60f : Main.masterMode ? 50f : 100f;
        if (Main.netMode != NetmodeID.MultiplayerClient && NPC.localAI[0] >= whentofire)
        {
            NPC.localAI[0] = 0f;
            if (Collision.CanHit(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
            {
                int dmg = 10;
                if (Main.expertMode)
                {
                    dmg += 15;
                }
                if (NPC.downedPlantBoss)
                {
                    targetX *= 1.3f;
                    targetY *= 1.3f;
                    dmg = 45;
                }
                int projType = ModContent.ProjectileType<LightningVolt>();
                Vector2 vel = new(targetX, targetY);
                NPC.Shoot(source, vel, projType, dmg, 0f, Main.myPlayer, 1f, 0f, 0f);
                for (int i = 0; i < 20; i++)
                {
                    ParticleRegistry.SpawnSparkParticle(source, vel.RotatedByRandom(.3f), Main.rand.Next(36, 48), Main.rand.NextFloat(.85f, 1.2f), Color.Purple);
                }
            }
        }
        int num9 = (int)NPC.Center.X;
        int num2 = (int)NPC.Center.Y;
        int num10 = num9 / 16;
        num2 /= 16;
        if (!WorldGen.SolidTile(num10, num2, false))
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.5f, 0f, 0.5f);
        }
        if (targetX > 0f)
        {
            NPC.spriteDirection = 1;
            NPC.rotation = (float)Math.Atan2(targetY, targetX);
        }
        if (targetX < 0f)
        {
            NPC.spriteDirection = -1;
            NPC.rotation = (float)Math.Atan2(targetY, targetX) + (float)Math.PI;
        }
        float num3 = 0.7f;
        if (NPC.collideX)
        {
            NPC.netUpdate = true;
            NPC.velocity.X = NPC.oldVelocity.X * (0f - num3);
            if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 2f)
            {
                NPC.velocity.X = 2f;
            }
            if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -2f)
            {
                NPC.velocity.X = -2f;
            }
        }
        if (NPC.collideY)
        {
            NPC.netUpdate = true;
            NPC.velocity.Y = NPC.oldVelocity.Y * (0f - num3);
            if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1.5f)
            {
                NPC.velocity.Y = 2f;
            }
            if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1.5f)
            {
                NPC.velocity.Y = -2f;
            }
        }
        if (flag)
        {
            if (NPC.velocity.X > 0f && targetX > 0f || NPC.velocity.X < 0f && targetX < 0f)
            {
                if (Math.Abs(NPC.velocity.X) < 12f)
                {
                    NPC.velocity.X *= 1.05f;
                }
            }
            else
            {
                NPC.velocity.X *= 0.9f;
            }
        }
        if ((NPC.velocity.X > 0f && NPC.oldVelocity.X < 0f || NPC.velocity.X < 0f && NPC.oldVelocity.X > 0f || NPC.velocity.Y > 0f && NPC.oldVelocity.Y < 0f || NPC.velocity.Y < 0f && NPC.oldVelocity.Y > 0f) && !NPC.justHit)
        {
            NPC.netUpdate = true;
        }
    }
    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockCatalyst>(), 1, 4, 10));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FulminicEye>(), 8, 1, 1));

        LeadingConditionRule rule = npcLoot.DefineConditionalDropSet(DropHelper.PostSkele());
        rule.Add(ItemDropRule.Common(ModContent.ItemType<BrewingStorms>(), 9, 1, 1));
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0)
        {
            target.AddBuff(BuffID.Electrified, 180, true, false);
        }
        for (int i = 0; i < 30; i++)
        {
            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 9f;
            Dust dust = Dust.NewDustPerfect(target.Center, DustID.WitherLightning, shootVelocity, default, default, 1.6f);
            dust.noGravity = true;
        }
        SoundEngine.PlaySound(SoundID.NPCHit52, target.Center);
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        for (int i = 0; i < 6; i++)
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.WitherLightning, hit.HitDirection, -1f, 0, default, 1f);

        if (NPC.life > 0)
            return;

        for (int i = 0; i < 25; i++)
            ParticleRegistry.SpawnLightningArcParticle(NPC.RandAreaInEntity(), Main.rand.NextVector2CircularLimited(120f, 120f, .6f, 1.1f), Main.rand.Next(38, 46), Main.rand.NextFloat(.5f, .9f), Color.Purple);

        for (int i = 0; i < 10; i++)
        {
            Dust lightning = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, DustID.WitherLightning, 0f, 0f, 100, default, 3f)];
            lightning.velocity *= 5f;
        }
    }
}
