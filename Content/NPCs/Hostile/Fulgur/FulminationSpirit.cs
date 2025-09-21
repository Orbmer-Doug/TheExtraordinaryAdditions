using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;
using TheExtraordinaryAdditions.Content.Items.Materials.Early;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Lightning;

/// <summary>
/// Phase 1 - release bolts
/// Phase 2 - slo down and charge up a lightning strike and attempt to hit player
/// Remember to smoothen rotation
/// </summary>
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
            NPC.lifeMax = 800;
        }
        if (Main.hardMode)
        {
            NPC.damage = 50;
            NPC.defense = 8;
            NPC.lifeMax = 1500;
        }
        if (NPC.downedPlantBoss)
        {
            NPC.damage = 60;
            NPC.defense = 16;
            NPC.lifeMax = 2000;
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
                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
            ]);

    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        if (spawnInfo.Sky)
            return 0.09f;
        return 0f;
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter += 0.15f;
        NPC.frameCounter %= Main.npcFrameCount[NPC.type];
        int frame = (int)NPC.frameCounter;
        NPC.frame.Y = frame * frameHeight;
    }

    public static int VoltDamage => Main.hardMode ? DifficultyBasedValue(26, 52, 78) : DifficultyBasedValue(70, 140, 210);
    public static int FireWait => DifficultyBasedValue(90, 70, 60);
    public static int startFindingTarget => SecondsToFrames(3);
    public static int maxFindingTargetTime => SecondsToFrames(6);
    public static int findingTargetWait => SecondsToFrames(4);
    public static float maxSpeed => 7f;
    public static float bounceFactor => 0.7f;
    public static float maxDistanceForSpeedBoost => 600f;

    public int Time
    {
        get => (int)NPC.ai[0];
        set => NPC.ai[0] = value;
    }
    public int FireTime
    {
        get => (int)NPC.ai[1];
        set => NPC.ai[1] = value;
    }
    public int CantHitTimer
    {
        get => (int)NPC.ai[2];
        set => NPC.ai[2] = value;
    }

    public override void AI()
    {
        NPC.SearchForPlayerTarget(out Player target, false);

        float acceleration = .17f;
        bool canHit = Collision.CanHitLine(NPC.Center, 4, 4, target.position, target.width, target.height);
        bool finding = CantHitTimer >= startFindingTarget;

        Vector2 destination = target.Center - Vector2.UnitY * NPC.height;
        Vector2 snappedTargetPos = new Vector2(
            (int)(destination.X / 8f) * 8,
            (int)(destination.Y / 8f) * 8
        );
        Vector2 snappedNpcPos = new Vector2(
            (int)(NPC.Center.X / 8f) * 8,
            (int)(NPC.Center.Y / 8f) * 8
        );

        // Calculate direction vector from NPC to target
        Vector2 dir = snappedTargetPos - snappedNpcPos;
        float distanceToTarget = dir.Length();
        bool isFarFromTarget = distanceToTarget > maxDistanceForSpeedBoost;

        // Handle zero distance to avoid division by zero
        if (distanceToTarget == 0f)
            dir = NPC.velocity; // Maintain current velocity direction
        else
            // Normalize direction and scale by max speed
            dir = dir / distanceToTarget * maxSpeed;

        if (target.dead)
            dir = new Vector2(NPC.direction * maxSpeed / 2f, -maxSpeed / 2f);

        // Fly around if it cant hit the player
        if (!canHit)
        {
            CantHitTimer++;
            if (finding)
            {
                isFarFromTarget = false;
                acceleration /= 2;
                dir = -dir;

                int realDir = float.IsNegative(MathF.Cos(NPC.AngleTo(target.Center + target.velocity))) ? -1 : 1;
                dir = dir.RotatedBy(.95f * (NPC.Center.Y > target.Center.Y).ToDirectionInt() * realDir);
            }
            if (CantHitTimer > (startFindingTarget + maxFindingTargetTime))
                CantHitTimer = -findingTargetWait; // Give some time to fly back to try again
        }
        else
            CantHitTimer = 0;

        // Adjust velocity towards target direction
        if (NPC.velocity.X < dir.X)
            NPC.velocity.X += acceleration;
        else if (NPC.velocity.X > dir.X)
            NPC.velocity.X -= acceleration;
        if (NPC.velocity.Y < dir.Y)
            NPC.velocity.Y += acceleration;
        else if (NPC.velocity.Y > dir.Y)
            NPC.velocity.Y -= acceleration;

        float pushForce = .2f;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type != Type || npc.whoAmI == NPC.whoAmI)
                continue;

            float taxicabDist = Math.Abs(NPC.position.X - npc.position.X) + Math.Abs(NPC.position.Y - npc.position.Y);
            if (taxicabDist < NPC.width)
            {
                if (NPC.position.X < npc.position.X)
                    NPC.velocity.X -= pushForce;
                else
                    NPC.velocity.X += pushForce;

                if (NPC.position.Y < npc.position.Y)
                    NPC.velocity.Y -= pushForce;
                else
                    NPC.velocity.Y += pushForce;
            }
        }

        NPC.rotation = NPC.rotation.AngleLerp(finding ? NPC.velocity.ToRotation() : NPC.AngleTo(target.Center + target.velocity), .2f);
        NPC.spriteDirection = float.IsNegative(MathF.Cos(NPC.rotation)) ? -1 : 1;

        // Push away if too close on same axis
        if (NPC.Center.X > target.Center.X - 20f && NPC.Center.X < target.Center.X)
            NPC.velocity.X -= .1f;
        if (NPC.Center.X < target.Center.X + 20f && NPC.Center.X > target.Center.X)
            NPC.velocity.X += .1f;

        if (FireTime > FireWait && canHit)
        {
            Vector2 vel = NPC.rotation.ToRotationVector2();
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, vel * 10f, ModContent.ProjectileType<LightningVolt>(), VoltDamage, 0f);
            for (int i = 0; i < 14; i++)
            {
                ParticleRegistry.SpawnSparkParticle(NPC.Center, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(7f, 15f), Main.rand.Next(30, 45), Main.rand.NextFloat(.9f, 1.4f), Color.Purple);
            }
            FireTime = 0;
            NPC.netUpdate = true;
        }
        FireTime++;

        Vector2 tilePosition = NPC.Center / 16f;
        if (!WorldGen.SolidTile((int)tilePosition.X, (int)tilePosition.Y, false))
        {
            Lighting.AddLight((int)tilePosition.X, (int)tilePosition.Y, 0.5f, 0f, 0.5f);
        }

        // Handle collision with tiles
        if (NPC.collideX)
        {
            NPC.netUpdate = true;
            NPC.velocity.X = -NPC.oldVelocity.X * bounceFactor;

            if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 2f)
                NPC.velocity.X = 2f;
            if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -2f)
                NPC.velocity.X = -2f;
        }

        if (NPC.collideY)
        {
            NPC.netUpdate = true;
            NPC.velocity.Y = -NPC.oldVelocity.Y * bounceFactor;

            if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1.5f)
                NPC.velocity.Y = 2f;
            if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1.5f)
                NPC.velocity.Y = -2f;
        }

        // Apply speed boost when far from target
        if (isFarFromTarget)
        {
            if ((NPC.velocity.X > 0f && dir.X > 0f) || (NPC.velocity.X < 0f && dir.X < 0f))
            {
                if (Math.Abs(NPC.velocity.X) < 12f)
                    NPC.velocity.X *= 1.05f;
            }
            else
                NPC.velocity.X *= 0.9f;
        }

        if (((NPC.velocity.X > 0f && NPC.oldVelocity.X < 0f) || (NPC.velocity.X < 0f && NPC.oldVelocity.X > 0f) ||
             (NPC.velocity.Y > 0f && NPC.oldVelocity.Y < 0f) || (NPC.velocity.Y < 0f && NPC.oldVelocity.Y > 0f)) && !NPC.justHit)
        {
            NPC.netUpdate = true;
        }
        Time++;
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

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (NPC.IsABestiaryIconDummy)
            return true;

        Texture2D tex = NPC.ThisNPCTexture();
        Vector2 orig = NPC.frame.Size() / 2;
        float rot = NPC.spriteDirection == -1 ? NPC.rotation + MathHelper.Pi : NPC.rotation;
        SpriteEffects fx = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.DrawBetter(tex, NPC.Center, NPC.frame, Color.White, rot, orig, 1f, fx);
        return false;
    }
}
